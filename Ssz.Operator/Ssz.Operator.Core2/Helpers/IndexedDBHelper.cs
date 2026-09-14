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
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Ssz.Operator.Core
{
    public static class IndexedDBHelper
    {
        #region public functions        

        /// <summary>   
        ///     <para>!!! Warning: always '/' as path separator !!!</para>
        ///     <para>Path relative to the root of the Files Store.</para>
        ///     <para>No '/' at the begin, no '/' at the end.</para>
        ///     <para>String.Empty for the Files Store root directory.</para>
        /// </summary>
        public static async Task<IndexedDBFileProvider> CreateFileProviderAsync(string projectDirectoryInvariantPathRelativeToRootDirectory)
        {
            TempIndexedDBDirectory rootTempIndexedDBDirectory = new()
            {
                PhysicalPath = @""
            };

#if !TEST_BROWSER_IN_DESKTOP
            if (!OperatingSystem.IsBrowser())
                throw new InvalidOperationException();

            await IndexedDBInterop.InitializeInteropAsync();
            await IndexedDBInterop.InitializeAsync(projectDirectoryInvariantPathRelativeToRootDirectory);            

            var r = (object[])(await IndexedDBInterop.GetFileInfosAsync(projectDirectoryInvariantPathRelativeToRootDirectory));
            foreach (System.Runtime.InteropServices.JavaScript.JSObject jSObject in r)
            {
                string filePathRelativeToProjectDirectory = jSObject.GetPropertyAsString(@"id") ?? @"";
                DateTimeOffset indexedDBFileLastModified = new Ssz.Utils.Any(jSObject.GetPropertyAsString(@"fileInfo")).ValueAs<DateTimeOffset>(false);
#else
            var pathInTempDirectory = GetPathInTempDirectory(projectDirectoryInvariantPathRelativeToRootDirectory);
            Directory.CreateDirectory(pathInTempDirectory);
            foreach (FileInfo cacheFileInfo in (new DirectoryInfo(pathInTempDirectory)).GetFiles("*", SearchOption.AllDirectories))
            {
                string filePathRelativeToProjectDirectory = Path.GetRelativePath(pathInTempDirectory, cacheFileInfo.FullName);
                DateTimeOffset indexedDBFileLastModified = new DateTimeOffset(cacheFileInfo.LastWriteTimeUtc);
#endif

                var indexedDBFile = new IndexedDBFile
                {
                    ProjectDirectoryInvariantPathRelativeToRootDirectory = projectDirectoryInvariantPathRelativeToRootDirectory,
                    PhysicalPath = filePathRelativeToProjectDirectory,
                    LastModified = indexedDBFileLastModified
                };
                var parts = filePathRelativeToProjectDirectory.Split(Path.DirectorySeparatorChar);

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

        /// <summary>
        ///     A project cannot hold two different files with the same name and the exact same
        ///     modification time, so that pair identifies the content. Identifying a file by it
        ///     lets a file repeated in several directories be downloaded and stored just once.
        /// </summary>
        public static string GetContentId(string name, DateTimeOffset lastModified)
        {
            return name + @"|" + new Ssz.Utils.Any(lastModified.UtcTicks).ValueAsString(false);
        }

        public static async Task DownloadFilesStoreDirectoryAsync(
            IndexedDBDirectory indexedDBDirectory,
            DsFilesStoreDirectory serverDsFilesStoreDirectory,
            IDataAccessProvider utilityDataAccessProvider,
            string projectDirectoryInvariantPathRelativeToRootDirectory,
            string currentDirectoryInvariantPathRelativeToProjectDirectory,
            JobProgressInfo jobProgressInfo)
        {
            CachedContents cachedContents = await CachedContents.CreateAsync(projectDirectoryInvariantPathRelativeToRootDirectory);

            await DownloadFilesStoreDirectoryAsync(
                indexedDBDirectory,
                serverDsFilesStoreDirectory,
                utilityDataAccessProvider,
                projectDirectoryInvariantPathRelativeToRootDirectory,
                currentDirectoryInvariantPathRelativeToProjectDirectory,
                jobProgressInfo,
                cachedContents);

            await cachedContents.DeleteUnusedContentsAsync(projectDirectoryInvariantPathRelativeToRootDirectory);
        }

        private static async Task DownloadFilesStoreDirectoryAsync(
            IndexedDBDirectory indexedDBDirectory,
            DsFilesStoreDirectory serverDsFilesStoreDirectory,
            IDataAccessProvider utilityDataAccessProvider,
            string projectDirectoryInvariantPathRelativeToRootDirectory,
            string currentDirectoryInvariantPathRelativeToProjectDirectory,
            JobProgressInfo jobProgressInfo,
            CachedContents cachedContents)
        {            
            var indexedDBFilesDictionary = indexedDBDirectory.IndexedDBFilesDictionary.ToDictionary(StringComparer.InvariantCultureIgnoreCase);
            Dictionary<string, IndexedDBFile> newIndexedDBFilesDictionary = new(indexedDBFilesDictionary.Count);
            List<string> fileInvariantPathsToDownload = new();
            foreach (var serverDsFilesStoreFile in serverDsFilesStoreDirectory.DsFilesStoreFilesCollection)
            {
                string contentId = GetContentId(serverDsFilesStoreFile.Name, serverDsFilesStoreFile.LastModified);
                cachedContents.UsedContentIds.Add(contentId);

                bool dowload = false;
                indexedDBFilesDictionary.Remove(serverDsFilesStoreFile.Name, out IndexedDBFile? existingIndexedDBFile);
                if (existingIndexedDBFile is not null)
                {
                    if (!FileSystemHelper.FileSystemTimeIsEquals(existingIndexedDBFile.LastModified.UtcDateTime, serverDsFilesStoreFile.LastModified.UtcDateTime))
                    {
                        try
                        {
#if !TEST_BROWSER_IN_DESKTOP
                            await IndexedDBInterop.DeleteFileAsync(projectDirectoryInvariantPathRelativeToRootDirectory, existingIndexedDBFile.PhysicalPath!);
#else
                            File.Delete(Path.Combine(GetPathInTempDirectory(projectDirectoryInvariantPathRelativeToRootDirectory), existingIndexedDBFile.PhysicalPath!));
#endif

                            dowload = true;
                        }
                        catch (Exception)
                        {
                            //Logger.LogError(ex, "Delete file error: " + existingFileInfo.FullName);
                        }
                    }
                    else if (cachedContents.Contains(contentId))
                    {
                        newIndexedDBFilesDictionary.Add(existingIndexedDBFile.Name, existingIndexedDBFile);
                    }
                    else
                    {
                        // The path entry survived but its content did not - fetch it again.
                        dowload = true;
                    }
                }
                else
                {
                    dowload = true;
                }

                if (dowload)
                {
                    string fileInvariantPathRelativeToProjectDirectory =
                        currentDirectoryInvariantPathRelativeToProjectDirectory == @"" ?
                            serverDsFilesStoreFile.Name :
                            currentDirectoryInvariantPathRelativeToProjectDirectory + @"/" + serverDsFilesStoreFile.Name;

                    if (cachedContents.Contains(contentId))
                    {
                        // The very same content is already stored for another path: point this
                        // path at it instead of downloading and storing a second copy.
                        var sharedIndexedDBFile = new IndexedDBFile
                        {
                            ProjectDirectoryInvariantPathRelativeToRootDirectory = projectDirectoryInvariantPathRelativeToRootDirectory,
                            PhysicalPath = fileInvariantPathRelativeToProjectDirectory.Replace('/', Path.DirectorySeparatorChar),
                            LastModified = serverDsFilesStoreFile.LastModified
                        };
#if !TEST_BROWSER_IN_DESKTOP
                        await IndexedDBInterop.SaveFileAsync(
                            projectDirectoryInvariantPathRelativeToRootDirectory,
                            sharedIndexedDBFile.PhysicalPath!,
                            new Ssz.Utils.Any(sharedIndexedDBFile.LastModified).ValueAsString(false),
                            contentId,
                            null);
#endif
                        newIndexedDBFilesDictionary.Add(sharedIndexedDBFile.Name, sharedIndexedDBFile);
                        await ReportProgressAsync(jobProgressInfo, 1);
                    }
                    else
                    {
                        fileInvariantPathsToDownload.Add(fileInvariantPathRelativeToProjectDirectory);
                    }
                }
                else
                {
                    // Already in the cache, nothing to fetch for it.
                    await ReportProgressAsync(jobProgressInfo, 1);
                }
            }

            // One request per batch instead of one per file: against a remote server the round
            // trip dominates, and a project easily has hundreds of small files.
            for (int batchStartIndex = 0; batchStartIndex < fileInvariantPathsToDownload.Count; batchStartIndex += DownloadFilesBatchSize)
            {
                List<string> batch = fileInvariantPathsToDownload
                    .GetRange(batchStartIndex, Math.Min(DownloadFilesBatchSize, fileInvariantPathsToDownload.Count - batchStartIndex));

                foreach (var indexedDBFile in await DownloadFilesAsync(
                    utilityDataAccessProvider,
                    projectDirectoryInvariantPathRelativeToRootDirectory,
                    batch,
                    cachedContents))
                {
                    newIndexedDBFilesDictionary.Add(indexedDBFile.Name, indexedDBFile);
                }

                await ReportProgressAsync(jobProgressInfo, batch.Count);
            }
            foreach (var fileInfo in indexedDBFilesDictionary.Values)
            {
                try
                {
#if !TEST_BROWSER_IN_DESKTOP
                    await IndexedDBInterop.DeleteFileAsync(projectDirectoryInvariantPathRelativeToRootDirectory, fileInfo.PhysicalPath!);
#else
                    File.Delete(Path.Combine(GetPathInTempDirectory(projectDirectoryInvariantPathRelativeToRootDirectory), fileInfo.PhysicalPath!));
#endif
                    
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
                    jobProgressInfo,
                    cachedContents);
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

#if TEST_BROWSER_IN_DESKTOP
        /// <summary>   
        ///     <para>!!! Warning: always '/' as path separator !!!</para>
        ///     <para>Path relative to the root of the Files Store.</para>
        ///     <para>No '/' at the begin, no '/' at the end.</para>
        ///     <para>String.Empty for the Files Store root directory.</para>
        /// </summary>
        public static string GetPathInTempDirectory(string invariantPathRelativeToRootDirectory)
        {
            invariantPathRelativeToRootDirectory = invariantPathRelativeToRootDirectory.Replace('/', Path.DirectorySeparatorChar);
            var pathInTempDirectory = Path.Combine(Path.GetTempPath(), invariantPathRelativeToRootDirectory);            
            return pathInTempDirectory;
        }

#endif

        #endregion

        #region private functions

        /// <summary>
        ///     How many files one LoadFiles request asks for. The transport splits big replies
        ///     into several gRPC messages on its own, so the limit here is the memory taken by
        ///     one reply: the file sizes are not known in advance, and a project may well hold
        ///     multi-megabyte images.
        /// </summary>
        private const int DownloadFilesBatchSize = 25;

        /// <summary>
        ///     Counts processed files and pushes the progress out at most five times a second.
        /// </summary>
        private static async Task ReportProgressAsync(JobProgressInfo jobProgressInfo, int processedFilesCount)
        {
            jobProgressInfo.ProgressCurrentValue += processedFilesCount;
            if (jobProgressInfo.Stopwatch.ElapsedMilliseconds > 200)
            {
                jobProgressInfo.Stopwatch.Restart();
                await jobProgressInfo.JobProgress.SetJobProgressAsync(
                    jobProgressInfo.GetProgressPercent(),
                    null,
                    null,
                    StatusCodes.Good);
            }
        }

        /// <summary>
        ///     Tracks which file contents the browser store holds and which ones the project still
        ///     refers to, so nothing is downloaded or kept twice.
        /// </summary>
        private class CachedContents
        {
            public static async Task<CachedContents> CreateAsync(string projectDirectoryInvariantPathRelativeToRootDirectory)
            {
                CachedContents cachedContents = new();
#if !TEST_BROWSER_IN_DESKTOP
                foreach (object contentId in (object[])(await IndexedDBInterop.GetContentIdsAsync(projectDirectoryInvariantPathRelativeToRootDirectory)))
                    cachedContents._storedContentIds.Add(new Ssz.Utils.Any(contentId).ValueAsString(false));
#endif
                await Task.CompletedTask;
                return cachedContents;
            }

            /// <summary>
            ///     Contents the project refers to. Everything else is dropped at the end.
            /// </summary>
            public HashSet<string> UsedContentIds { get; } = new(StringComparer.Ordinal);

            public bool Contains(string contentId)
            {
                return _storedContentIds.Contains(contentId);
            }

            public void Add(string contentId)
            {
                _storedContentIds.Add(contentId);
            }

            /// <summary>
            ///     Drops contents no file of the project points at any more: files deleted on the
            ///     server, and older versions of files that have been replaced.
            /// </summary>
            public async Task DeleteUnusedContentsAsync(string projectDirectoryInvariantPathRelativeToRootDirectory)
            {
#if !TEST_BROWSER_IN_DESKTOP
                foreach (string contentId in _storedContentIds.Where(contentId => !UsedContentIds.Contains(contentId)).ToList())
                {
                    try
                    {
                        await IndexedDBInterop.DeleteContentAsync(projectDirectoryInvariantPathRelativeToRootDirectory, contentId);
                        _storedContentIds.Remove(contentId);
                    }
                    catch (Exception)
                    {
                    }
                }
#endif
                await Task.CompletedTask;
            }

            private readonly HashSet<string> _storedContentIds = new(StringComparer.Ordinal);
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
            List<string> fileInvariantPathRelativeToProjectDirectoryCollection,
            CachedContents cachedContents)
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
                            ProjectDirectoryInvariantPathRelativeToRootDirectory = projectDirectoryInvariantPathRelativeToRootDirectory,
                            PhysicalPath = filePathRelativeToProjectDirectory,
                            LastModified = dsFilesStoreFileData.LastModified,
                            Length = dsFilesStoreFileData.FileData.LongLength
                        };
                        //     Creates a new file, writes the specified byte array to the file, and then closes
                        //     the file. If the target file already exists, it is overwritten.
                        try
                        {
#if !TEST_BROWSER_IN_DESKTOP
                            await IndexedDBInterop.SaveFileAsync(
                                projectDirectoryInvariantPathRelativeToRootDirectory,
                                indexedDBFile.PhysicalPath!,
                                new Ssz.Utils.Any(indexedDBFile.LastModified).ValueAsString(false),
                                GetContentId(indexedDBFile.Name, indexedDBFile.LastModified),
                                dsFilesStoreFileData.FileData);
#else
                            string cacheFileFullName = Path.Combine(GetPathInTempDirectory(projectDirectoryInvariantPathRelativeToRootDirectory), indexedDBFile.PhysicalPath!);
                            Directory.CreateDirectory(Path.GetDirectoryName(cacheFileFullName)!);
                            await File.WriteAllBytesAsync(cacheFileFullName, dsFilesStoreFileData.FileData);
                            File.SetLastWriteTimeUtc(cacheFileFullName, indexedDBFile.LastModified.DateTime);
#endif

                            // Another path with the same name and modification time now reuses
                            // this content instead of downloading it again.
                            cachedContents.Add(GetContentId(indexedDBFile.Name, indexedDBFile.LastModified));

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
#if !TEST_BROWSER_IN_DESKTOP
                await IndexedDBInterop.DeleteFileAsync(projectDirectoryInvariantPathRelativeToRootDirectory, kvp.Value.PhysicalPath!);
#else
                File.Delete(Path.Combine(GetPathInTempDirectory(projectDirectoryInvariantPathRelativeToRootDirectory), kvp.Value.PhysicalPath!));
#endif
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
