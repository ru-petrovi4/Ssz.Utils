using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Ssz.Dcs.CentralServer;
using Ssz.Dcs.CentralServer.Common;
using Ssz.Dcs.CentralServer.Common.Passthrough;
using Ssz.Utils;
using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer_ClientWindowsService
{
    public partial class MainBackgroundService
    {
        #region private functions

        /// <summary>
        ///     Creates local directory if necesary.
        /// </summary>
        /// <param name="serverDsFilesStoreDirectory"></param>
        /// <param name="progressInfo"></param>
        /// <param name="includeSubdirectories"></param>
        /// <returns></returns>
        private async Task DownloadFilesStoreDirectoryAsync(DsFilesStoreDirectory serverDsFilesStoreDirectory, ProgressInfo? progressInfo,
            bool includeSubdirectories = true)
        {
            if (!UtilityDataAccessProvider.IsConnected) throw new InvalidOperationException();

            var directoryInfo = new DirectoryInfo(Path.Combine(FilesStoreDirectoryInfo.FullName, serverDsFilesStoreDirectory.PathRelativeToRootDirectory));
            // Creates all directories and subdirectories in the specified path unless they already exist.
            try
            {
                Directory.CreateDirectory(directoryInfo.FullName);
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Directory.CreateDirectory Exception. " + directoryInfo.FullName);
            }

            var fileInfosDictionary = new CaseInsensitiveDictionary<FileInfo>();
            try
            {
                foreach (var fileInfo in directoryInfo.GetFiles())
                {
                    fileInfosDictionary[fileInfo.Name] = fileInfo;
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Directory.GetFiles Exception. " + directoryInfo.FullName);
            }

            foreach (var serverDsFilesStoreFile in serverDsFilesStoreDirectory.DsFilesStoreFilesCollection)
            {
                if (progressInfo is not null)
                {
                    progressInfo.Index += 1;
                    if (progressInfo.Index % 10 == 0)
                    {
                        await SetJobProgressAsync(progressInfo.JobId, progressInfo.GetPercent(), progressInfo.ProgressLabelResourceName, null, JobStatusCodes.OK);
                    }
                }                

                bool dowload = false;
                var existingFileInfo = fileInfosDictionary.TryGetValue(serverDsFilesStoreFile.Name);
                if (existingFileInfo is not null)
                {
                    if (serverDsFilesStoreFile.Name != existingFileInfo.Name || // Strict comparison
                        FileSystemHelper.FileSystemTimeIsLess(existingFileInfo.LastWriteTimeUtc, serverDsFilesStoreFile.LastWriteTimeUtc))
                    {
                        try
                        {
                            existingFileInfo.Delete();
                            dowload = true;
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "Delete file error: " + existingFileInfo.FullName);
                        }                        
                    }                    
                    fileInfosDictionary.Remove(existingFileInfo.Name);
                }
                else
                {
                    dowload = true;
                }
                
                if (dowload)
                {
                    await DownloadFilesAsync(new List<string> { Path.Combine(serverDsFilesStoreDirectory.PathRelativeToRootDirectory, serverDsFilesStoreFile.Name) });
                }
            }
            foreach (var fileInfo in fileInfosDictionary.Values)
            {
                try
                {
                    fileInfo.Delete();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Delete file error: " + fileInfo.FullName);
                }
            }

            if (includeSubdirectories)
            {
                // Subdirectories
                var childDirectoryInfosDictionary = new CaseInsensitiveDictionary<DirectoryInfo>();
                try
                {
                    foreach (var childDirectoryInfo in directoryInfo.GetDirectories())
                    {
                        childDirectoryInfosDictionary[childDirectoryInfo.Name] = childDirectoryInfo;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogDebug(ex, "Directory.GetDirectories Exception. " + directoryInfo.FullName);
                }

                foreach (var childServerDsFilesStoreDirectory in serverDsFilesStoreDirectory.ChildDsFilesStoreDirectoriesCollection)
                {
                    await DownloadFilesStoreDirectoryAsync(childServerDsFilesStoreDirectory, progressInfo);
                    childDirectoryInfosDictionary.Remove(childServerDsFilesStoreDirectory.Name);
                }
                foreach (var childDirectoryInfo in childDirectoryInfosDictionary.Values)
                {
                    try
                    {
                        childDirectoryInfo.Delete(true);
                    }
                    catch
                    {
                    }
                }
            }
        }

        /// <summary>
        ///     !!!Warning!!! Not Uploads child directories!!!
        ///     Uploads changed and new files to server.        
        /// </summary>
        /// <param name="dsFilesStoreDirectory"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task UploadFilesStoreDirectoryAsync(DsFilesStoreDirectory dsFilesStoreDirectory)
        {
            if (!UtilityDataAccessProvider.IsConnected) throw new InvalidOperationException();

            DirectoryInfo directoryInfo;
            try
            {
                directoryInfo = new DirectoryInfo(Path.Combine(FilesStoreDirectoryInfo.FullName, dsFilesStoreDirectory.PathRelativeToRootDirectory));
                if (!directoryInfo.Exists)
                    return;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "DirectoryInfo crerate exception: " + Path.Combine(FilesStoreDirectoryInfo.FullName, dsFilesStoreDirectory.PathRelativeToRootDirectory));
                return;
            }            

            var request = new GetDirectoryInfoRequest
            {
                PathRelativeToRootDirectory = dsFilesStoreDirectory.PathRelativeToRootDirectory,
                FilesAndDirectoriesIncludeLevel = 1
            };
            var returnData = await UtilityDataAccessProvider.PassthroughAsync(@"", PassthroughConstants.GetDirectoryInfo,
                SerializationHelper.GetOwnedData(request));
            DsFilesStoreDirectory? serverDsFilesStoreDirectory = SerializationHelper.CreateFromOwnedData(returnData is not null ? returnData.ToArray() : null,
                () => new DsFilesStoreDirectory());
            if (serverDsFilesStoreDirectory is null)
            {
                Logger.LogError("Can not get ServeDsFilesStoreDirectory: " + dsFilesStoreDirectory.PathRelativeToRootDirectory);
                return;
            }

            var serverDsFilesStoreFilesDictionary = new CaseInsensitiveDictionary<DsFilesStoreFile>();
            foreach (DsFilesStoreFile dsFilesStoreFile in serverDsFilesStoreDirectory.DsFilesStoreFilesCollection)
            {
                serverDsFilesStoreFilesDictionary[dsFilesStoreFile.Name] = dsFilesStoreFile;
            }

            foreach (var dsFilesStoreFile in dsFilesStoreDirectory.DsFilesStoreFilesCollection)
            {
                bool upload;
                var existingServerDsFilesStoreFile = serverDsFilesStoreFilesDictionary.TryGetValue(dsFilesStoreFile.Name);
                if (existingServerDsFilesStoreFile is not null)
                {
                    if (dsFilesStoreFile.Name != existingServerDsFilesStoreFile.Name || // Strict comparison
                        FileSystemHelper.FileSystemTimeIsLess(existingServerDsFilesStoreFile.LastWriteTimeUtc, dsFilesStoreFile.LastWriteTimeUtc))
                    {
                        upload = true;                        
                    }
                    else
                    {
                        upload = false;
                    }                    
                }
                else
                {
                    upload = true;
                }

                if (upload)
                {
                    string fileFullName = Path.Combine(FilesStoreDirectoryInfo.FullName, dsFilesStoreDirectory.PathRelativeToRootDirectory, dsFilesStoreFile.Name);

                    byte[]? bytes = null;
                    foreach (int index in Enumerable.Range(0, 5))
                    {
                        try
                        {
                            bytes = await File.ReadAllBytesAsync(fileFullName);
                        }
                        catch
                        {
                            await Task.Delay(500);
                        }
                    }
                    if (bytes is not null)
                    {
                        var dsFilesStoreFileData = new DsFilesStoreFileData
                        {
                            PathRelativeToRootDirectory = dsFilesStoreDirectory.PathRelativeToRootDirectory + Path.DirectorySeparatorChar + dsFilesStoreFile.Name,
                            LastWriteTimeUtc = File.GetLastWriteTimeUtc(fileFullName), // Request LastWriteTimeUtc again, because it can change
                            FileData = bytes
                        };

                        await UploadFilesAsync(new List<DsFilesStoreFileData> { dsFilesStoreFileData });
                    }
                }                
            }            

            //if (includeSubdirectories)
            //{
            //    // Subdirectories                
            //    foreach (var childServerDsFilesStoreDirectory in dsFilesStoreDirectory.ChildDsFilesStoreDirectoriesCollection)
            //    {
            //        await UploadFilesStoreDirectoryAsync(childServerDsFilesStoreDirectory,
            //            Path.Combine(destinationPathRelativeToRootDirectory, childServerDsFilesStoreDirectory.Name), progressInfo);
                    
            //        try
            //        {
            //            string directoryFullPath = Path.Combine(FilesStoreDirectoryInfo.FullName, childServerDsFilesStoreDirectory.PathRelativeToRootDirectory);

            //            if (Directory.GetFileSystemEntries(directoryFullPath).Length == 0)
            //                Directory.Delete(directoryFullPath, true);
            //        }
            //        catch
            //        {
            //        }
            //    }                
            //}            
        }

        private async Task DownloadFilesAsync(List<string> filePathRelativeToRootDirectoryCollection)
        {   
            var returnData = await UtilityDataAccessProvider.PassthroughAsync("", PassthroughConstants.LoadFiles, Encoding.UTF8.GetBytes(CsvHelper.FormatForCsv(@",", filePathRelativeToRootDirectoryCollection)));
            var reply = SerializationHelper.CreateFromOwnedData(returnData is not null ? returnData.ToArray(): null, () => new LoadFilesReply());
            if (reply is not null)
            {
                var t = SaveFilesAsync(reply); // No task wait for time optimization.
            }            
        }

        private async Task SaveFilesAsync(LoadFilesReply loadFilesReply)
        {
            foreach (var dsFilesStoreFileData in loadFilesReply.DsFilesStoreFileDatasCollection)
            {
                string fileFullName = Path.Combine(FilesStoreDirectoryInfo.FullName, dsFilesStoreFileData.PathRelativeToRootDirectory);
                //     Creates a new file, writes the specified byte array to the file, and then closes
                //     the file. If the target file already exists, it is overwritten.
                try
                {
                    await File.WriteAllBytesAsync(fileFullName, dsFilesStoreFileData.FileData);
                    File.SetLastWriteTimeUtc(fileFullName, dsFilesStoreFileData.LastWriteTimeUtc);
                }
                catch (Exception ex)                
                {
                    Logger.LogError(ex, "Cannot write file: " + fileFullName);
                }
            }
        }

        private async Task<uint> UploadFilesAsync(List<DsFilesStoreFileData> dsFilesStoreFileDatasCollection)
        {
            var request = new SaveFilesRequest
            {
                DatasCollection = dsFilesStoreFileDatasCollection
            };

            uint jobStatusCode = await await UtilityDataAccessProvider.LongrunningPassthroughAsync("", LongrunningPassthroughConstants.SaveFiles,
                SerializationHelper.GetOwnedData(request), null);

            return jobStatusCode;
        }

        private async Task SetJobProgressAsync(string jobId, uint progressPercent, string? progressLabelResourceName, string? progressDetails, uint jobStatusCode)
        {
            if (jobId == @"") return;
            try
            {
                GrpcChannel? grpcChannel = UtilityDataAccessProvider.GrpcChannel;
                if (grpcChannel is null) throw new Exception(@"grpcChannel is null");

                var sessionsManagementClient = new ProcessModelingSessionsManagement.ProcessModelingSessionsManagementClient(grpcChannel);
                var request = new NotifyJobProgressRequest
                {
                    JobId = jobId,
                    ProgressPercent = progressPercent,
                    ProgressLabelResourceName = progressLabelResourceName ?? @"",
                    ProgressDetails = progressDetails ?? "",
                    JobStatusCode = jobStatusCode
                };
                var reply = await sessionsManagementClient.NotifyJobProgressAsync(request);                
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "NotifyProgress failed.");
            }
        }

        /// <summary>
        ///     Syncs files and directories with central server if necesary.
        ///     pathRelativeToDataDirectory must not starts with '\'
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="processModelName"></param>
        /// <param name="binDsFilesStoreDirectoryType"></param>
        /// <param name="dataDsFilesStoreDirectoryType"></param>
        /// <param name="pathRelativeToDataDirectory"></param>
        /// <returns></returns>
        private async Task<(DirectoryInfo ProcessDirectoryInfo, DirectoryInfo BinDirectoryInfo, DirectoryInfo DataDirectoryInfo)?> PrepareWorkingDirectoriesAsync(
            ProgressInfo progressInfo, 
            string processModelName,
            DsFilesStoreDirectoryType binDsFilesStoreDirectoryType, 
            DsFilesStoreDirectoryType dataDsFilesStoreDirectoryType, 
            string pathRelativeToDataDirectory = @"")
        {
            DirectoryInfo processDirectoryInfo;
            DirectoryInfo binDirectoryInfo;
            DirectoryInfo dataDirectoryInfo;

            //     Creates all directories and subdirectories in the specified path unless they
            //     already exist.
            Directory.CreateDirectory(Path.Combine(FilesStoreDirectoryInfo.FullName, processModelName, DsFilesStoreHelper.GetDsFilesStoreDirectoryName(dataDsFilesStoreDirectoryType)));

            DsFilesStoreDirectory rootDsFilesStoreDirectory = DsFilesStoreHelper.CreateDsFilesStoreDirectoryObject(FilesStoreDirectoryInfo, @"", 1);
            DsFilesStoreDirectory? existingProcessDsFilesStoreDirectory = DsFilesStoreHelper.CreateDsFilesStoreDirectoryObject(FilesStoreDirectoryInfo, processModelName);
            DsFilesStoreDirectory? existingBinDsFilesStoreDirectory = DsFilesStoreHelper.FindBinDsFilesStoreDirectory(rootDsFilesStoreDirectory, existingProcessDsFilesStoreDirectory, binDsFilesStoreDirectoryType);
            DsFilesStoreDirectory? existingDataDsFilesStoreDirectory = DsFilesStoreHelper.FindDataDsFilesStoreDirectory(existingProcessDsFilesStoreDirectory, dataDsFilesStoreDirectoryType);

            if (ConfigurationHelper.GetValue<bool>(Configuration, @"FilesStoreSyncWithCentralServer", false))
            {
                var request = new GetDirectoryInfoRequest
                {
                    PathRelativeToRootDirectory = @"",
                    FilesAndDirectoriesIncludeLevel = 1
                };
                var returnData = await UtilityDataAccessProvider.PassthroughAsync(@"", PassthroughConstants.GetDirectoryInfo,
                    SerializationHelper.GetOwnedData(request));
                DsFilesStoreDirectory? serverRootDsFilesStoreDirectory = SerializationHelper.CreateFromOwnedData(returnData.ToArray(),
                    () => new DsFilesStoreDirectory());
                if (serverRootDsFilesStoreDirectory is null)
                {
                    Logger.LogError("Invalid ServerRootDsFilesStoreDirectory");
                    return null;
                }

                request = new GetDirectoryInfoRequest
                {
                    PathRelativeToRootDirectory = processModelName,
                    FilesAndDirectoriesIncludeLevel = Int32.MaxValue
                };
                returnData = await UtilityDataAccessProvider.PassthroughAsync(@"", PassthroughConstants.GetDirectoryInfo,
                    SerializationHelper.GetOwnedData(request));
                DsFilesStoreDirectory? serverPocessModelDsFilesStoreDirectory = SerializationHelper.CreateFromOwnedData(returnData.ToArray(),
                    () => new DsFilesStoreDirectory());
                if (serverPocessModelDsFilesStoreDirectory is null)
                {
                    Logger.LogError("Invalid ServerPocessModelDsFilesStoreDirectory");
                    return null;
                }

                DsFilesStoreDirectory? serverDataDsFilesStoreDirectory = DsFilesStoreHelper.FindDataDsFilesStoreDirectory(serverPocessModelDsFilesStoreDirectory, dataDsFilesStoreDirectoryType);
                if (serverDataDsFilesStoreDirectory is null)
                {
                    Logger.LogError("No Server Data Directory; ProcessModelName=" + processModelName + "; DirectoryType=" + dataDsFilesStoreDirectoryType);
                    return null;
                }

                var serverBinDsFilesStoreDirectory = DsFilesStoreHelper.FindBinDsFilesStoreDirectory(serverRootDsFilesStoreDirectory, serverPocessModelDsFilesStoreDirectory, binDsFilesStoreDirectoryType);
                if (serverBinDsFilesStoreDirectory is null)
                {
                    Logger.LogError("No Server Bin Directory; ProcessModelName=" + processModelName + "; DirectoryType=" + binDsFilesStoreDirectoryType);
                    return null;
                }
                if (!serverBinDsFilesStoreDirectory.PathRelativeToRootDirectory.Contains(@"\")) // if in root directory, request again with more deep include level.
                {
                    request = new GetDirectoryInfoRequest
                    {
                        PathRelativeToRootDirectory = serverBinDsFilesStoreDirectory.PathRelativeToRootDirectory,
                        FilesAndDirectoriesIncludeLevel = Int32.MaxValue
                    };
                    returnData = await UtilityDataAccessProvider.PassthroughAsync(@"", PassthroughConstants.GetDirectoryInfo,
                        SerializationHelper.GetOwnedData(request));
                    serverBinDsFilesStoreDirectory = SerializationHelper.CreateFromOwnedData(returnData is not null ? returnData.ToArray() : null,
                        () => new DsFilesStoreDirectory());
                    if (serverBinDsFilesStoreDirectory is null)
                    {
                        Logger.LogError("No Server Bin Directory; ProcessModelName=" + processModelName + "; DirectoryType=" + binDsFilesStoreDirectoryType);
                        return null;
                    }
                }

                DsFilesStoreDirectory serverDataOrChildDsFilesStoreDirectory;
                if (pathRelativeToDataDirectory == @"")
                {
                    serverDataOrChildDsFilesStoreDirectory = serverDataDsFilesStoreDirectory;
                }
                else
                {
                    var dsFilesStoreItem = DsFilesStoreHelper.FindDsFilesStoreItem(serverDataDsFilesStoreDirectory, pathRelativeToDataDirectory);
                    if (dsFilesStoreItem is null)
                    {
                        Logger.LogError("No Server Data Directory File; ProcessModelName=" + processModelName + "; FilePath=" + pathRelativeToDataDirectory);
                        return null;
                    }
                    serverDataOrChildDsFilesStoreDirectory = dsFilesStoreItem.DsFilesStoreDirectory;
                }

                //if (existingDataDsFilesStoreDirectory is not null)
                //{
                //    DsFilesStoreDirectory? existingDataOrChildDsFilesStoreDirectory = null;
                //    if (pathRelativeToDataDirectory is null)
                //    {
                //        existingDataOrChildDsFilesStoreDirectory = existingDataDsFilesStoreDirectory;
                //    }
                //    else
                //    {
                //        var existingDsFilesStoreItem = DsFilesStoreHelper.GetDsFilesStoreItem(existingDataDsFilesStoreDirectory, pathRelativeToDataDirectory);
                //        if (existingDsFilesStoreItem is not null)
                //        {
                //            existingDataOrChildDsFilesStoreDirectory = existingDsFilesStoreItem.DsFilesStoreDirectory;
                //        }
                //    }
                //    if (existingDataOrChildDsFilesStoreDirectory is not null && !String.Equals(existingDataOrChildDsFilesStoreDirectory.PathRelativeToRoot, serverDataOrChildDsFilesStoreDirectory.PathRelativeToRoot))
                //    {
                //        DeleteFilesStoreDirectory(existingDataOrChildDsFilesStoreDirectory.PathRelativeToRoot);
                //    }
                //}
                if (existingBinDsFilesStoreDirectory is not null && existingBinDsFilesStoreDirectory.PathRelativeToRootDirectory.Count(c => c == '\\') > 0 &&
                    !String.Equals(existingBinDsFilesStoreDirectory.PathRelativeToRootDirectory, serverBinDsFilesStoreDirectory.PathRelativeToRootDirectory))
                {
                    var directoryInfo = new DirectoryInfo(Path.Combine(FilesStoreDirectoryInfo.FullName, existingBinDsFilesStoreDirectory.PathRelativeToRootDirectory));
                    if (directoryInfo.Exists)
                        try
                        {
                            directoryInfo.Delete(true);
                        }
                        catch
                        {
                        }
                }

                progressInfo.Index = 0;
                progressInfo.Count = DsFilesStoreHelper.GetDsFilesStoreDirectoryFilesCount(serverDataOrChildDsFilesStoreDirectory) +
                        DsFilesStoreHelper.GetDsFilesStoreDirectoryFilesCount(serverBinDsFilesStoreDirectory);

                await DownloadFilesStoreDirectoryAsync(serverDataOrChildDsFilesStoreDirectory, progressInfo);
                await DownloadFilesStoreDirectoryAsync(serverBinDsFilesStoreDirectory, progressInfo);

                processDirectoryInfo = new DirectoryInfo(Path.Combine(FilesStoreDirectoryInfo.FullName, serverPocessModelDsFilesStoreDirectory.PathRelativeToRootDirectory));
                binDirectoryInfo = new DirectoryInfo(Path.Combine(FilesStoreDirectoryInfo.FullName, serverBinDsFilesStoreDirectory.PathRelativeToRootDirectory));
                dataDirectoryInfo = new DirectoryInfo(Path.Combine(FilesStoreDirectoryInfo.FullName, serverDataDsFilesStoreDirectory.PathRelativeToRootDirectory));
            }
            else
            {
                if (existingProcessDsFilesStoreDirectory is null)
                {
                    Logger.LogError("No Process Model Directory; ProcessModelName=" + processModelName);
                    return null;
                }
                if (existingBinDsFilesStoreDirectory is null)
                {
                    Logger.LogError("No Bin Directory; ProcessModelName=" + processModelName + "; DirectoryType=" + binDsFilesStoreDirectoryType);
                    return null;
                }
                if (existingDataDsFilesStoreDirectory is null)
                {
                    Logger.LogError("No Data Directory; ProcessModelName=" + processModelName + "; DirectoryType=" + dataDsFilesStoreDirectoryType);
                    return null;
                }

                processDirectoryInfo = new DirectoryInfo(Path.Combine(FilesStoreDirectoryInfo.FullName, existingProcessDsFilesStoreDirectory.PathRelativeToRootDirectory));
                binDirectoryInfo = new DirectoryInfo(Path.Combine(FilesStoreDirectoryInfo.FullName, existingBinDsFilesStoreDirectory.PathRelativeToRootDirectory));
                dataDirectoryInfo = new DirectoryInfo(Path.Combine(FilesStoreDirectoryInfo.FullName, existingDataDsFilesStoreDirectory.PathRelativeToRootDirectory));
            }

            return (
                ProcessDirectoryInfo: processDirectoryInfo,
                BinDirectoryInfo: binDirectoryInfo,
                DataDirectoryInfo: dataDirectoryInfo
            );
        }        

        private async Task PrepareSavesDirectoryAsync(
            ProgressInfo progressInfo,
            string processModelName,            
            string userName,
            InstructorAccessFlags instructorAccessFlags)
        {
            DsFilesStoreDirectory? serverSavesDsFilesStoreDirectory;
            DsFilesStoreDirectory? serverSavesUserDsFilesStoreDirectory;
            if (instructorAccessFlags.HasFlag(InstructorAccessFlags.CanReadSavesOfOtherUsers))
            {
                var request = new GetDirectoryInfoRequest
                {
                    PathRelativeToRootDirectory = Path.Combine(processModelName, DsFilesStoreConstants.SavesDirectoryNameUpper),
                    FilesAndDirectoriesIncludeLevel = Int32.MaxValue
                };
                var returnData = await UtilityDataAccessProvider.PassthroughAsync(@"", PassthroughConstants.GetDirectoryInfo,
                    SerializationHelper.GetOwnedData(request));
                serverSavesDsFilesStoreDirectory = SerializationHelper.CreateFromOwnedData(returnData is not null ? returnData.ToArray() : null,
                    () => new DsFilesStoreDirectory());
                if (serverSavesDsFilesStoreDirectory is null)
                {
                    throw new Exception("Invalid ServerPocessModelDsFilesStoreDirectory");
                }

                serverSavesUserDsFilesStoreDirectory = null;
            }
            else
            {
                var request = new GetDirectoryInfoRequest
                {
                    PathRelativeToRootDirectory = Path.Combine(processModelName, DsFilesStoreConstants.SavesDirectoryNameUpper),
                    FilesAndDirectoriesIncludeLevel = 1
                };
                var returnData = await UtilityDataAccessProvider.PassthroughAsync(@"", PassthroughConstants.GetDirectoryInfo,
                    SerializationHelper.GetOwnedData(request));
                serverSavesDsFilesStoreDirectory = SerializationHelper.CreateFromOwnedData(returnData is not null ? returnData.ToArray() : null,
                    () => new DsFilesStoreDirectory());
                if (serverSavesDsFilesStoreDirectory is null)
                {
                    throw new Exception("Invalid ServerPocessModelDsFilesStoreDirectory");
                }

                request = new GetDirectoryInfoRequest
                {
                    PathRelativeToRootDirectory = Path.Combine(processModelName, DsFilesStoreConstants.SavesDirectoryNameUpper,
                        DsFilesStoreHelper.GetDsFilesStoreDirectoryName(userName)),
                    FilesAndDirectoriesIncludeLevel = Int32.MaxValue
                };
                returnData = await UtilityDataAccessProvider.PassthroughAsync(@"", PassthroughConstants.GetDirectoryInfo,
                    SerializationHelper.GetOwnedData(request));
                serverSavesUserDsFilesStoreDirectory = SerializationHelper.CreateFromOwnedData(returnData is not null ? returnData.ToArray() : null,
                    () => new DsFilesStoreDirectory());
                if (serverSavesUserDsFilesStoreDirectory is null)
                {
                    throw new Exception("Invalid ServerPocessModelDsFilesStoreDirectory");
                }
            }            

            progressInfo.Index = 0;
            progressInfo.Count = DsFilesStoreHelper.GetDsFilesStoreDirectoryFilesCount(serverSavesDsFilesStoreDirectory) +
                DsFilesStoreHelper.GetDsFilesStoreDirectoryFilesCount(serverSavesUserDsFilesStoreDirectory);

            await DownloadFilesStoreDirectoryAsync(serverSavesDsFilesStoreDirectory, progressInfo);
            if (serverSavesUserDsFilesStoreDirectory is not null)
                await DownloadFilesStoreDirectoryAsync(serverSavesUserDsFilesStoreDirectory, progressInfo);
        }

        #endregion

        private class ProgressInfo
        {
            #region construction and destruction

            public ProgressInfo(string jobId, double beginPercent, double endPercent)
            {
                JobId = jobId;
                BeginPercent = beginPercent;
                EndPercent = endPercent;
            }

            #endregion

            public string JobId { get; }

            public double BeginPercent { get; }

            public double EndPercent { get; }

            public int Index;

            public int Count;

            public string? ProgressLabelResourceName { get; set; }

            public uint GetPercent()
            {
                return (uint)(BeginPercent + (EndPercent - BeginPercent) * Index / Count);
            }
        }        
    }
}


///// <summary>
/////     Syncs files with central server if necesary.
///// </summary>
///// <param name="textMessage"></param>
///// <returns></returns>
//private async Task<(DirectoryInfo binDirectoryInfo, DirectoryInfo dataDirectoryInfo)?> PrepareWorkingDirectories(string processModelName, string sessionId,
//    DsFilesStoreDirectoryType binDsFilesStoreDirectoryType, DsFilesStoreDirectoryType dataDsFilesStoreDirectoryType, string? fileName = null)
//{
//    try
//    {
//        DsFilesStoreDirectory rootDsFilesStoreDirectory = DsFilesStoreHelper.GetRootDsFilesStoreDirectory(Configuration);
//        DsFilesStoreDirectory? dataDsFilesStoreDirectory = DsFilesStoreHelper.FindDataDsFilesStoreDirectory(rootDsFilesStoreDirectory, processModelName, dataDsFilesStoreDirectoryType, fileName);
//        DsFilesStoreDirectory? binDsFilesStoreDirectory = DsFilesStoreHelper.FindBinDsFilesStoreDirectory(rootDsFilesStoreDirectory, processModelName, binDsFilesStoreDirectoryType);

//        if (ConfigurationHelper.GetValue<bool>(Configuration, @"FilesStoreSyncWithCentralServer", false))
//        {
//            var serverRootDsFilesStoreDirectory = ServerRootDsFilesStoreDirectory;
//            if (serverRootDsFilesStoreDirectory is null)
//            {
//                Logger.LogError("Invalid ServerRootDsFilesStoreDirectory");
//                return null;
//            }

//            var serverDataDsFilesStoreDirectory = DsFilesStoreHelper.FindDataDsFilesStoreDirectory(serverRootDsFilesStoreDirectory, processModelName, dataDsFilesStoreDirectoryType, fileName);
//            if (serverDataDsFilesStoreDirectory is null)
//            {
//                Logger.LogError("No Server Data Directory; ProcessModelName=" + processModelName + "; DirectoryType=" + dataDsFilesStoreDirectoryType);
//                return null;
//            }
//            var serverBinDsFilesStoreDirectory = DsFilesStoreHelper.FindBinDsFilesStoreDirectory(serverRootDsFilesStoreDirectory, processModelName, binDsFilesStoreDirectoryType);
//            if (serverBinDsFilesStoreDirectory is null)
//            {
//                Logger.LogError("No Server Bin Directory; ProcessModelName=" + processModelName + "; DirectoryType=" + binDsFilesStoreDirectoryType);
//                return null;
//            }

//            if (dataDsFilesStoreDirectory is not null && !String.Equals(dataDsFilesStoreDirectory.RelativePath, serverDataDsFilesStoreDirectory.RelativePath))
//            {
//                DeleteFilesStoreDirectory(dataDsFilesStoreDirectory.RelativePath);
//            }
//            if (binDsFilesStoreDirectory is not null && binDsFilesStoreDirectory.RelativePath.Count(c => c == '\\') > 2 &&
//                !String.Equals(binDsFilesStoreDirectory.RelativePath, serverBinDsFilesStoreDirectory.RelativePath))
//            {
//                DeleteFilesStoreDirectory(binDsFilesStoreDirectory.RelativePath);
//            }

//            var progressInfo = new ProgressInfo
//            {
//                BeginPercent = 0,
//                EndPercent = 100,
//                Index = 0,
//                Count = DsFilesStoreHelper.GetDsFilesStoreDirectoryFilesCount(serverDataDsFilesStoreDirectory) +
//                DsFilesStoreHelper.GetDsFilesStoreDirectoryFilesCount(serverBinDsFilesStoreDirectory)
//            };
//            return new BinAndDataDirectoryInfos
//            {
//                DataDirectoryInfo = await SyncFilesStoreDirectoryAsync(serverDataDsFilesStoreDirectory, sessionId, progressInfo),
//                BinDirectoryInfo = await SyncFilesStoreDirectoryAsync(serverBinDsFilesStoreDirectory, sessionId, progressInfo)
//            };
//        }
//        else
//        {
//            if (dataDsFilesStoreDirectory is null)
//            {
//                Logger.LogError("No Data Directory; ProcessModelName=" + processModelName + "; DirectoryType=" + dataDsFilesStoreDirectoryType);
//                return null;
//            }
//            if (binDsFilesStoreDirectory is null)
//            {
//                Logger.LogError("No Bin Directory; ProcessModelName=" + processModelName + "; DirectoryType=" + binDsFilesStoreDirectoryType);
//                return null;
//            }

//            var filesStoreDirectoryInfo = DsFilesStoreHelper.GetFilesStoreDirectoryInfo(Configuration);
//            return new BinAndDataDirectoryInfos
//            {
//                DataDirectoryInfo = new DirectoryInfo(filesStoreDirectoryInfo.FullName + dataDsFilesStoreDirectory.RelativePath),
//                BinDirectoryInfo = new DirectoryInfo(filesStoreDirectoryInfo.FullName + binDsFilesStoreDirectory.RelativePath)
//            };
//        }
//    }
//    catch (Exception ex)
//    {
//        Logger.LogError(ex, "Get Bin and Data directories info Failed.");
//        return null;
//    }
//}


//private Task<DsFilesStoreDirectory?> GetServerRootDsFilesStoreDirectoryFilteredAsync(string processModelName,
//            DsFilesStoreDirectoryType binDsFilesStoreDirectoryType, DsFilesStoreDirectoryType dataDsFilesStoreDirectoryType, string pathRelativeToDataDirectory = @"")
//{
//    //if (UtilityDataAccessProvider.IsConnected && ConfigurationHelper.GetValue<bool>(Configuration, @"FilesStoreSyncWithCentralServer", false))
//    //{
//    //    UtilityDataAccessProvider.Passthrough("", PassthroughConstants.GetDirectoryInfo, new byte[0], OnGetDirectoryInfoCallback);
//    //}

//    var taskCompletionSource = new TaskCompletionSource<DsFilesStoreDirectory?>();
//    UtilityDataAccessProvider.Passthrough("", PassthroughConstants.GetDirectoryInfo, Encoding.UTF8.GetBytes(CsvHelper.FormatForCsv(",",
//        new object?[] { processModelName, binDsFilesStoreDirectoryType, dataDsFilesStoreDirectoryType, pathRelativeToDataDirectory }
//        )),
//        result => OnGetDirectoryInfoCallback(result, taskCompletionSource));
//    return taskCompletionSource.Task;
//}


//private async Task SyncFilesStoreFileAsync(DsFilesStoreDirectory serverDsFilesStoreDirectory, DsFilesStoreFile serverDsFilesStoreFile)
//{
//    if (!UtilityDataAccessProvider.IsConnected) throw new InvalidOperationException();            

//    bool dowload;
//    var existingFileInfo = new FileInfo(Path.Combine(FilesStoreDirectoryInfo.FullName, serverDsFilesStoreDirectory.PathRelativeToRoot, serverDsFilesStoreFile.Name));
//    if (existingFileInfo is not null)
//    {
//        var deltaTimeSpan = TimeSpan.FromSeconds(0.5);
//        if (serverDsFilesStoreFile.LastWriteTimeUtc > existingFileInfo.LastWriteTimeUtc - deltaTimeSpan &&
//            serverDsFilesStoreFile.LastWriteTimeUtc < existingFileInfo.LastWriteTimeUtc + deltaTimeSpan)
//        {
//            dowload = false;
//        }
//        else
//        {
//            existingFileInfo.Delete();
//            dowload = true;
//        }                
//    }
//    else
//    {
//        dowload = true;
//    }

//    if (dowload)
//    {
//        await DownloadFileAsync(Path.Combine(serverDsFilesStoreDirectory.PathRelativeToRoot, serverDsFilesStoreFile.Name), serverDsFilesStoreFile.LastWriteTimeUtc);
//    }
//}