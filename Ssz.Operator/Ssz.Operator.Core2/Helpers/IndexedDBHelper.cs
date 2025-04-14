using Microsoft.Extensions.FileProviders;
using Ssz.Dcs.CentralServer.Common;
using Ssz.Dcs.CentralServer.Common.Passthrough;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Ssz.Utils.Serialization;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Operator.Core
{
    public static class IndexedDBHelper
    {
        #region public functions

        #endregion

        public static async Task<IndexedDBFileProvider> CreateFileProviderAsync(string projectDirectoryInvariantPathRelativeToRootDirectory)
        {
            if (!OperatingSystem.IsBrowser())
                throw new InvalidOperationException();

            await IndexedDBInterop.InitializeInteropAsync();
            await IndexedDBInterop.InitializeAsync(projectDirectoryInvariantPathRelativeToRootDirectory);

            TempIndexedDBDirectory rootTempIndexedDBDirectory = new()
            {
                PhysicalPath = @""
            };

            var r = (object[])(await IndexedDBInterop.GetFileInfosAsync(projectDirectoryInvariantPathRelativeToRootDirectory));
            foreach (System.Runtime.InteropServices.JavaScript.JSObject jSObject in r)
            {
                string indexedDBFilePhysicalPath = jSObject.GetPropertyAsString(@"id") ?? @"";
                DateTimeOffset indexedDBFileLastModified = new Any(jSObject.GetPropertyAsString(@"fileInfo")).ValueAs<DateTimeOffset>(false);

                var indexedDBFile = new IndexedDBFile
                {
                    ProjectDirectoryInvariantPathRelativeToRootDirectory = projectDirectoryInvariantPathRelativeToRootDirectory,
                    PhysicalPath = indexedDBFilePhysicalPath,
                    LastModified = indexedDBFileLastModified
                };
                var parts = indexedDBFilePhysicalPath.Split(Path.DirectorySeparatorChar);

                TempIndexedDBDirectory current_TempIndexedDBDirectory = rootTempIndexedDBDirectory;
                foreach (int i in Enumerable.Range(0, parts.Length))
                {
                    if (i < parts.Length - 1)
                    {
                        string directoryName = parts[i];
                        current_TempIndexedDBDirectory.ChildIndexedDBDirectoriesDictionary.TryGetValue(directoryName, out TempIndexedDBDirectory? child_TempIndexedDBDirectory);
                        if (child_TempIndexedDBDirectory is null)
                        {
                            child_TempIndexedDBDirectory = new TempIndexedDBDirectory
                            {
                                PhysicalPath = current_TempIndexedDBDirectory.PhysicalPath == @"" ? directoryName : Path.Combine(current_TempIndexedDBDirectory.PhysicalPath!, directoryName)
                            };
                            current_TempIndexedDBDirectory.ChildIndexedDBDirectoriesDictionary.Add(directoryName, child_TempIndexedDBDirectory);
                        }
                        current_TempIndexedDBDirectory = child_TempIndexedDBDirectory;
                    }
                    else
                    {
                        current_TempIndexedDBDirectory.IndexedDBFilesDictionary.Add(indexedDBFile.Name, indexedDBFile);
                    }
                }
            }

            IndexedDBDirectory rootIndexedDBDirectory = GetIndexedDBDirectory(rootTempIndexedDBDirectory); 

            return new IndexedDBFileProvider(rootIndexedDBDirectory);
        }

        private static IndexedDBDirectory GetIndexedDBDirectory(TempIndexedDBDirectory tempIndexedDBDirectory)
        {
            return new IndexedDBDirectory()
            {
                PhysicalPath = tempIndexedDBDirectory.PhysicalPath,
                LastModified = tempIndexedDBDirectory.LastModified,
                Length = tempIndexedDBDirectory.Length,
                ChildIndexedDBDirectoriesDictionary = tempIndexedDBDirectory.ChildIndexedDBDirectoriesDictionary
                    .ToFrozenDictionary(kvp => kvp.Key, kvp => GetIndexedDBDirectory(kvp.Value), StringComparer.InvariantCultureIgnoreCase),
                IndexedDBFilesDictionary = tempIndexedDBDirectory.IndexedDBFilesDictionary
                    .ToFrozenDictionary(StringComparer.InvariantCultureIgnoreCase)
            };
        }

        public static async Task DownloadFilesStoreDirectoryAsync(
            IndexedDBDirectory indexedDBDirectory,            
            DsFilesStoreDirectory serverDsFilesStoreDirectory,            
            IDataAccessProvider utilityDataAccessProvider,
            string projectDirectoryInvariantPathRelativeToRootDirectory,
            string currentDirectoryInvariantPathRelativeToProjectDirectory,
            JobProgressInfo jobProgressInfo)
        {            
            var indexedDBFilesDictionary = indexedDBDirectory.IndexedDBFilesDictionary.ToDictionary(StringComparer.InvariantCultureIgnoreCase);
            Dictionary<string, IndexedDBFile> newIndexedDBFilesDictionary = new(indexedDBFilesDictionary.Count);
            foreach (var serverDsFilesStoreFile in serverDsFilesStoreDirectory.DsFilesStoreFilesCollection)
            {
                jobProgressInfo.ProgressCurrentValue += 1;
                if (jobProgressInfo.Stopwatch.ElapsedMilliseconds > 200)
                {
                    jobProgressInfo.Stopwatch.Restart();
                    await jobProgressInfo.JobProgress.SetJobProgressAsync(jobProgressInfo.GetProgressPercent(), null, null, StatusCodes.Good);
                }

                bool dowload = false;
                indexedDBFilesDictionary.Remove(serverDsFilesStoreFile.Name, out IndexedDBFile? existingIndexedDBFile);
                if (existingIndexedDBFile is not null)
                {
                    if (!FileSystemHelper.FileSystemTimeIsEquals(existingIndexedDBFile.LastModified.UtcDateTime, serverDsFilesStoreFile.LastModified.UtcDateTime))
                    {
                        try
                        {
                            await IndexedDBInterop.DeleteFileAsync(projectDirectoryInvariantPathRelativeToRootDirectory, existingIndexedDBFile.PhysicalPath!);                            
                            dowload = true;
                        }
                        catch (Exception)
                        {
                            //Logger.LogError(ex, "Delete file error: " + existingFileInfo.FullName);
                        }
                    }
                    else
                    {
                        newIndexedDBFilesDictionary.Add(existingIndexedDBFile.Name, existingIndexedDBFile);
                    }
                }
                else
                {
                    dowload = true;
                }

                if (dowload)
                {
                    foreach (var indexedDBFile in await DownloadFilesAsync(
                        utilityDataAccessProvider,
                        projectDirectoryInvariantPathRelativeToRootDirectory,
                        new List<string> {
                            currentDirectoryInvariantPathRelativeToProjectDirectory == @"" ?
                                serverDsFilesStoreFile.Name :
                                currentDirectoryInvariantPathRelativeToProjectDirectory + @"/" + serverDsFilesStoreFile.Name
                        }
                        ))
                    {
                        newIndexedDBFilesDictionary.Add(indexedDBFile.Name, indexedDBFile);
                    }
                }
            }
            foreach (var fileInfo in indexedDBFilesDictionary.Values)
            {
                try
                {
                    await IndexedDBInterop.DeleteFileAsync(projectDirectoryInvariantPathRelativeToRootDirectory, fileInfo.PhysicalPath!);
                }
                catch (Exception)
                {
                    //Logger.LogError(ex, "Delete file error: " + fileInfo.FullName);
                }
            }
            indexedDBDirectory.IndexedDBFilesDictionary = newIndexedDBFilesDictionary.ToFrozenDictionary(StringComparer.InvariantCultureIgnoreCase);

            var childIndexedDBDirectoriesDictionary = indexedDBDirectory.ChildIndexedDBDirectoriesDictionary.ToDictionary(StringComparer.InvariantCultureIgnoreCase);
            Dictionary<string, IndexedDBDirectory> newChildIndexedDBDirectoriesDictionary = new(childIndexedDBDirectoriesDictionary.Count, StringComparer.InvariantCultureIgnoreCase);
            foreach (var childServerDsFilesStoreDirectory in serverDsFilesStoreDirectory.ChildDsFilesStoreDirectoriesCollection)
            {
                string childServerDsFilesStoreDirectoryName = childServerDsFilesStoreDirectory.Name;
                childIndexedDBDirectoriesDictionary.Remove(childServerDsFilesStoreDirectoryName, out IndexedDBDirectory? childIndexedDBDirectory);
                if (childIndexedDBDirectory is null)
                {
                    childIndexedDBDirectory = new IndexedDBDirectory()
                    {
                        PhysicalPath = Path.Combine(
                            currentDirectoryInvariantPathRelativeToProjectDirectory.Replace('/', Path.DirectorySeparatorChar),
                            childServerDsFilesStoreDirectoryName),
                        ChildIndexedDBDirectoriesDictionary = new Dictionary<string, IndexedDBDirectory>().ToFrozenDictionary(),
                        IndexedDBFilesDictionary = new Dictionary<string, IndexedDBFile>().ToFrozenDictionary()
                    };
                }
                await DownloadFilesStoreDirectoryAsync(
                    childIndexedDBDirectory,
                    childServerDsFilesStoreDirectory,                                        
                    utilityDataAccessProvider,
                    projectDirectoryInvariantPathRelativeToRootDirectory,
                    currentDirectoryInvariantPathRelativeToProjectDirectory == @"" ? childServerDsFilesStoreDirectoryName :
                        currentDirectoryInvariantPathRelativeToProjectDirectory + @"/" + childServerDsFilesStoreDirectoryName,
                    jobProgressInfo);
                newChildIndexedDBDirectoriesDictionary.Add(childIndexedDBDirectory.Name, childIndexedDBDirectory);                
            }
            foreach (var childIndexedDBDirectory in childIndexedDBDirectoriesDictionary.Values)
            {
                try
                {
                    await DeleteIndexedDBDirectoryAsync(projectDirectoryInvariantPathRelativeToRootDirectory, childIndexedDBDirectory);
                }
                catch
                {
                }
            }
            indexedDBDirectory.ChildIndexedDBDirectoriesDictionary = newChildIndexedDBDirectoriesDictionary.ToFrozenDictionary(StringComparer.InvariantCultureIgnoreCase);
        }        

        #region private functions

        //private static DsFilesStoreDirectory CreateProjectDsFilesStoreDirectoryObject(string projectName, string pathRelativeToRootDirectory)
        //{
        //    var dsFilesStoreDirectory = new DsFilesStoreDirectory();
        //    if (pathRelativeToRootDirectory != @"") // Not Root directory
        //    {
        //        dsFilesStoreDirectory.PathRelativeToRootDirectory = pathRelativeToRootDirectory;
        //    }

        //    if (filesAndDirectoriesIncludeLevel > 0)
        //    {
        //        filesAndDirectoriesIncludeLevel -= 1;
        //        foreach (DirectoryInfo childDirectoryInfo in currentDirectoryInfo.EnumerateDirectories())
        //        {
        //            string childPathRelativeToRootDirectory;
        //            if (pathRelativeToRootDirectory == @"")
        //                childPathRelativeToRootDirectory = childDirectoryInfo.Name;
        //            else
        //                childPathRelativeToRootDirectory = Path.Combine(pathRelativeToRootDirectory, childDirectoryInfo.Name);
        //            dsFilesStoreDirectory.ChildDsFilesStoreDirectoriesCollection.Add(CreateDsFilesStoreDirectoryObject(childDirectoryInfo, childPathRelativeToRootDirectory,
        //                filesAndDirectoriesIncludeLevel));
        //        }

        //        foreach (FileInfo fileInfo in currentDirectoryInfo.EnumerateFiles())
        //        {
        //            var dsFileInfo = new DsFilesStoreFile();
        //            dsFileInfo.Name = fileInfo.Name;
        //            dsFileInfo.LastModified = fileInfo.LastWriteTimeUtc;
        //            dsFilesStoreDirectory.DsFilesStoreFilesCollection.Add(dsFileInfo);
        //        }
        //    }

        //    return dsFilesStoreDirectory;
        //}

        private static async Task<IEnumerable<IndexedDBFile>> DownloadFilesAsync(            
            IDataAccessProvider utilityDataAccessProvider,
            string projectDirectoryInvariantPathRelativeToRootDirectory,
            List<string> fileInvariantPathRelativeToProjectDirectoryCollection)
        {
            List<IndexedDBFile> result = new();

            var returnData = await utilityDataAccessProvider.PassthroughAsync(
                "", 
                PassthroughConstants.LoadFiles, 
                Encoding.UTF8.GetBytes(CsvHelper.FormatForCsv(@",", fileInvariantPathRelativeToProjectDirectoryCollection.Select(n => projectDirectoryInvariantPathRelativeToRootDirectory + @"/" + n))));
            var loadFilesReply = SerializationHelper.CreateFromOwnedData(returnData, () => new LoadFilesReply());
            if (loadFilesReply is not null)
            {   
                foreach (var dsFilesStoreFileData in loadFilesReply.DsFilesStoreFileDatasCollection)
                {
                    if (dsFilesStoreFileData.InvariantPathRelativeToRootDirectory.StartsWith(projectDirectoryInvariantPathRelativeToRootDirectory))
                    {
                        string fileInvariantPathRelativeToProjectDirectory = dsFilesStoreFileData.InvariantPathRelativeToRootDirectory.Substring(projectDirectoryInvariantPathRelativeToRootDirectory.Length + 1);

                        string filePathRelativeToProjectDirectory = fileInvariantPathRelativeToProjectDirectory.Replace('/', Path.DirectorySeparatorChar);

                        IndexedDBFile indexedDBFile = new()
                        {
                            PhysicalPath = filePathRelativeToProjectDirectory,
                            LastModified = dsFilesStoreFileData.LastModified,
                            Length = dsFilesStoreFileData.FileData.LongLength
                        };
                        //     Creates a new file, writes the specified byte array to the file, and then closes
                        //     the file. If the target file already exists, it is overwritten.
                        try
                        {
                            await IndexedDBInterop.SaveFileAsync(
                                projectDirectoryInvariantPathRelativeToRootDirectory, 
                                indexedDBFile.PhysicalPath!, 
                                new Any(indexedDBFile.LastModified).ValueAsString(false),
                                dsFilesStoreFileData.FileData);                            
                            result.Add(indexedDBFile);
                        }
                        catch (Exception)
                        {
                            //Logger.LogError(ex, "Cannot write file: " + fileFullName);
                        }                        
                    }
                }
            }

            return result;
        }

        private static async Task DeleteIndexedDBDirectoryAsync(string projectDirectoryInvariantPathRelativeToRootDirectory, IndexedDBDirectory indexedDBDirectory)
        {
            foreach (var kvp in indexedDBDirectory.IndexedDBFilesDictionary)
            {
                await IndexedDBInterop.DeleteFileAsync(projectDirectoryInvariantPathRelativeToRootDirectory, kvp.Value.PhysicalPath!);
            }

            foreach (var kvp in indexedDBDirectory.ChildIndexedDBDirectoriesDictionary)
            {
                await DeleteIndexedDBDirectoryAsync(projectDirectoryInvariantPathRelativeToRootDirectory, kvp.Value);
            }
        }

        #endregion

        private class TempIndexedDBDirectory
        {
            #region public functions            

            /// <summary>        
            ///     Path relative to the root of the Files Store.
            ///     No '/' at the begin, no '/' at the end.
            ///     String.Empty for the Files Store root directory.
            /// </summary>
            public string? PhysicalPath { get; set; }

            public Dictionary<string, TempIndexedDBDirectory> ChildIndexedDBDirectoriesDictionary { get; } = new();

            public Dictionary<string, IndexedDBFile> IndexedDBFilesDictionary { get; set; } = new();

            /// <summary>
            /// </summary>
            public DateTimeOffset LastModified { get; set; }

            public long Length { get; set; }            

            #endregion
        }
    }
}
