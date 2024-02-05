using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
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

        private async Task LaunchEnineAsync(string textMessage)
        {            
            var parts = CsvHelper.ParseCsvLine(",", textMessage);
            if (parts.Length < 9)
            {
                Logger.LogError("Invalid textMessage = " + textMessage);
                return;
            }

            string jobId = parts[0] ?? "";
            try
            {                
                string processModelingSessionId = parts[1] ?? "";
                string processModelName = parts[2] ?? "";
                string instructorUserName = parts[3] ?? "";
                InstructorAccessFlags instructorAccessFlags = new Any(new Any(parts[4] ?? "").ValueAsUInt32(false)).ValueAs<InstructorAccessFlags>(false);
                DsFilesStoreDirectoryType binDsFilesStoreDirectoryType = new Any(parts[5] ?? "").ValueAs<DsFilesStoreDirectoryType>(false);
                DsFilesStoreDirectoryType dataDsFilesStoreDirectoryType = new Any(parts[6] ?? "").ValueAs<DsFilesStoreDirectoryType>(false);
                string pathRelativeToDataDirectory = parts[7] ?? "";
                string instanceInfo = parts[8] ?? "";

                var progressInfo = new ProgressInfo(jobId, 0, 85)
                {
                    ProgressLabelResourceName = @""
                };

                var workingDirectoriesOptional = await PrepareWorkingDirectoriesAsync(progressInfo, processModelName,
                    binDsFilesStoreDirectoryType, dataDsFilesStoreDirectoryType, pathRelativeToDataDirectory);
                if (workingDirectoriesOptional is null)
                {
                    throw new Exception(@"Invalid textMessage = " + textMessage);                    
                }
                var workingDirectories = workingDirectoriesOptional.Value;

                progressInfo = new ProgressInfo(jobId, 85, 95)
                {
                    ProgressLabelResourceName = @""
                };

                await PrepareSavesDirectoryAsync(progressInfo, processModelName, instructorUserName, instructorAccessFlags);

                switch (binDsFilesStoreDirectoryType)
                {
                    case DsFilesStoreDirectoryType.ControlEngineBin:
                        LaunchControlEngine(processModelingSessionId, workingDirectories.ProcessDirectoryInfo, workingDirectories.BinDirectoryInfo, instanceInfo);
                        break;
                    case DsFilesStoreDirectoryType.PlatInstructorBin:
                        LaunchPlatInstructor(workingDirectories.ProcessDirectoryInfo, workingDirectories.BinDirectoryInfo, workingDirectories.DataDirectoryInfo, pathRelativeToDataDirectory, instanceInfo);
                        break;
                    default:
                        Logger.LogError("Unknown Modeling Engine.");
                        break;
                }

                await SetJobProgressAsync(jobId, 100, null, null, StatusCodes.Good);
            }
            catch (RpcException ex)
            {
                Logger.LogError(ex, "Launch Engine Failed.");

                await SetJobProgressAsync(jobId, 100, null, ex.Status.Detail, StatusCodes.BadInvalidState);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Launch Engine Failed.");

                await SetJobProgressAsync(jobId, 100, null, ex.Message, StatusCodes.BadInvalidState);
            }            
        }

        private async Task DownloadChangedFilesAsync(string textMessage)
        {
            var parts = CsvHelper.ParseCsvLine(",", textMessage);
            if (parts.Length < 3)
            {
                Logger.LogError("Invalid textMessage = " + textMessage);                
                return;
            }

            string jobId = parts[0] ?? "";            

            if (!ConfigurationHelper.GetValue<bool>(Configuration, @"FilesStoreSyncWithCentralServer", false))
            {
                await SetJobProgressAsync(jobId, 100, null, null, StatusCodes.Good);
                return;
            }            

            try
            {
                var directoryPathsRelativeToRootDirectory = CsvHelper.ParseCsvLine(@",", parts[1]);
                bool includeSubdirectories = new Any(parts[2] ?? "").ValueAsBoolean(false);

                var progressInfo = new ProgressInfo(jobId, 0, 95)
                {
                    ProgressLabelResourceName = @""
                };
                progressInfo.Index = 0;
                progressInfo.Count = directoryPathsRelativeToRootDirectory.Length;

                foreach (var directoryPathRelativeToRootDirectory in directoryPathsRelativeToRootDirectory)
                {
                    if (String.IsNullOrEmpty(directoryPathRelativeToRootDirectory)) continue;

                    if (progressInfo is not null)
                    {
                        progressInfo.Index += 1;
                        if (progressInfo.Index % 10 == 0)
                        {
                            await SetJobProgressAsync(progressInfo.JobId, progressInfo.GetPercent(), progressInfo.ProgressLabelResourceName, null, StatusCodes.Good);
                        }
                    }
                    
                    var request = new GetDirectoryInfoRequest
                    {
                        PathRelativeToRootDirectory = directoryPathRelativeToRootDirectory,
                        FilesAndDirectoriesIncludeLevel = includeSubdirectories ? Int32.MaxValue : 1,
                    };
                    var returnData = await UtilityDataAccessProvider.PassthroughAsync(@"", PassthroughConstants.GetDirectoryInfo,
                        SerializationHelper.GetOwnedData(request));
                    DsFilesStoreDirectory? serverDsFilesStoreDirectory = SerializationHelper.CreateFromOwnedData(returnData,
                        () => new DsFilesStoreDirectory());
                    if (serverDsFilesStoreDirectory is not null)
                    {
                        await DownloadFilesStoreDirectoryAsync(serverDsFilesStoreDirectory, null, includeSubdirectories);
                    }
                }

                await SetJobProgressAsync(jobId, 100, null, null, StatusCodes.Good);
            }
            catch (RpcException ex)
            {
                Logger.LogError(ex, "Launch Engine Failed.");

                await SetJobProgressAsync(jobId, 100, null, ex.Status.Detail, StatusCodes.BadInvalidState);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Launch Engine Failed.");

                await SetJobProgressAsync(jobId, 100, null, ex.Message, StatusCodes.BadInvalidState);
            }
        }

        private async Task UploadChangedFilesAsync(string textMessage)
        {
            var parts = CsvHelper.ParseCsvLine(",", textMessage);
            if (parts.Length < 2)
            {
                Logger.LogError("Invalid textMessage = " + textMessage);
                return;
            }

            string jobId = parts[0] ?? "";

            if (!ConfigurationHelper.GetValue<bool>(Configuration, @"FilesStoreSyncWithCentralServer", false))
            {
                await SetJobProgressAsync(jobId, 100, null, null, StatusCodes.Good);
                return;
            }            

            try
            {
                var directoryPathsRelativeToRootDirectory = CsvHelper.ParseCsvLine(@",", parts[1]);

                var progressInfo = new ProgressInfo(jobId, 0, 95)
                {
                    ProgressLabelResourceName = @""                    
                };
                progressInfo.Index = 0;
                progressInfo.Count = directoryPathsRelativeToRootDirectory.Length;

                foreach (var directoryPathRelativeToRootDirectory in directoryPathsRelativeToRootDirectory)
                {
                    if (String.IsNullOrEmpty(directoryPathRelativeToRootDirectory)) continue;

                    if (progressInfo is not null)
                    {
                        progressInfo.Index += 1;
                        if (progressInfo.Index % 10 == 0)
                        {
                            await SetJobProgressAsync(progressInfo.JobId, progressInfo.GetPercent(), progressInfo.ProgressLabelResourceName, null, StatusCodes.Good);
                        }
                    }

                    try
                    {
                        var dsFilesStoreDirectory = DsFilesStoreHelper.CreateDsFilesStoreDirectoryObject(FilesStoreDirectoryInfo, directoryPathRelativeToRootDirectory, 1);

                        await UploadFilesStoreDirectoryAsync(dsFilesStoreDirectory);
                    }
                    catch(Exception ex)
                    {
                        Logger.LogError(ex, "DsFilesStoreHelper.CreateDsFilesStoreDirectoryObject Failed.");
                    }
                }                

                await SetJobProgressAsync(jobId, 100, null, null, StatusCodes.Good);
            }
            catch (RpcException ex)
            {
                Logger.LogError(ex, "Launch Engine Failed.");

                await SetJobProgressAsync(jobId, 100, null, ex.Status.Detail, StatusCodes.BadInvalidState);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Launch Engine Failed.");

                await SetJobProgressAsync(jobId, 100, null, ex.Message, StatusCodes.BadInvalidState);
            }            
        }

        #endregion
    }
}
