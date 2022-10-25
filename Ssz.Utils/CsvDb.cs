using Microsoft.Extensions.Logging;
using Ssz.Utils.Dispatcher;
using Ssz.Utils.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    public class CsvDb : IDispatcherObject
    {
        #region construction and destruction

        /// <summary>
        ///     userFriendlyLogger: Messages are localized. Priority is Information, Error, Warning.
        ///     If csvDbDirectoryInfo is null, directory is not used.
        ///     If !csvDbDirectoryInfo.Exists, directory is created.
        ///     If callbackDispatcher is not null, monitors csvDbDirectoryInfo for changes.        
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="userFriendlyLogger"></param>
        /// <param name="csvDbDirectoryInfo"></param>
        /// <param name="dispatcher">Dispatcher for all callbacks</param>
        public CsvDb(ILogger<CsvDb> logger, IUserFriendlyLogger? userFriendlyLogger = null, DirectoryInfo? csvDbDirectoryInfo = null, IDispatcher? dispatcher = null)
        {
            LoggersSet = new LoggersSet<CsvDb>(logger, userFriendlyLogger);            
            CsvDbDirectoryInfo = csvDbDirectoryInfo;
            Dispatcher = dispatcher;

            if (CsvDbDirectoryInfo is not null)
            {
                try
                {
                    // Creates all directories and subdirectories in the specified path unless they already exist.
                    Directory.CreateDirectory(CsvDbDirectoryInfo.FullName);
                    if (!CsvDbDirectoryInfo.Exists)
                        CsvDbDirectoryInfo = null;
                }
                catch
                {
                    CsvDbDirectoryInfo = null;
                }
            }

            if (CsvDbDirectoryInfo is not null)
            {
                LoggersSet.Logger.LogInformation("CsvDb Created for: " + CsvDbDirectoryInfo.FullName);
                if (Dispatcher is not null)
                    try
                    {
                        _fileSystemWatcher.Created += FileSystemWatcherOnEventAsync;
                        _fileSystemWatcher.Changed += FileSystemWatcherOnEventAsync;
                        _fileSystemWatcher.Deleted += FileSystemWatcherOnEventAsync;
                        _fileSystemWatcher.Renamed += FileSystemWatcherOnEventAsync;
                        _fileSystemWatcher.Path = CsvDbDirectoryInfo.FullName;
                        _fileSystemWatcher.Filter = @"*.csv";
                        _fileSystemWatcher.IncludeSubdirectories = false;
                        _fileSystemWatcher.EnableRaisingEvents = true;
                    }
                    catch (Exception ex)
                    {
                        LoggersSet.Logger.LogWarning(ex, "AppSettings FilesStore directory error. Please, specify correct directory and restart service.");
                    }
            }

            LoadData();
        }

        #endregion

        #region public functions

        public LoggersSet<CsvDb> LoggersSet { get; }

        /// <summary>
        ///     Existing directory info or null
        /// </summary>
        public DirectoryInfo? CsvDbDirectoryInfo { get; }

        /// <summary>
        ///     Dispatcher for all callbacks
        /// </summary>
        public IDispatcher? Dispatcher { get; }        

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<CsvFileChangedEventArgs>? CsvFileChanged;

        public bool EnableRaisingEvents
        {
            get
            {
                return _fileSystemWatcher.EnableRaisingEvents;
            }
            set
            {
                if (!value)
                {
                    _fileSystemWatcher.EnableRaisingEvents = false;
                }
                else
                {
                    var t = FileSystemWatcherEnableRaisingEventsAsync();
                }
            }
        }

        public void Clear()
        {
            _csvFilesCollection.Clear();            
        }

        /// <summary>
        ///     Loads data from .csv files on disk.        
        ///     Data is loaded in constructor.
        ///     Data is loaded when directory content changes, if dispatcher in consructor is not null.
        /// </summary>
        public void LoadData()
        {
            if (CsvDbDirectoryInfo is null) return;

            List<CsvFileChangedEventArgs> eventArgsList = new();

            var newCsvFilesCollection = new Dictionary<string, CsvFile>();

            foreach (FileInfo fileInfo in CsvDbDirectoryInfo.GetFiles(@"*", SearchOption.TopDirectoryOnly).Where(f => f.Name.EndsWith(@".csv",
                StringComparison.InvariantCultureIgnoreCase)))
            {
                string fileNameUpper = fileInfo.Name.ToUpperInvariant();
                _csvFilesCollection.TryGetValue(fileNameUpper, out CsvFile? csvFile);
                if (csvFile is null)
                {
                    csvFile = new CsvFile { 
                        FileName = fileInfo.Name,
                        FileInfo = fileInfo,
                        DataIsChangedOnDisk = true
                    };                    

                    foreach (CsvFile oldCsvFile in _csvFilesCollection.Values)
                    {
                        if (oldCsvFile.IncludeFileNamesCollection.Contains(fileNameUpper))
                            oldCsvFile.DataIsChangedOnDisk = true;
                    }
                }
                else if (Path.GetFileNameWithoutExtension(csvFile.FileName) != Path.GetFileNameWithoutExtension(fileInfo.Name) || // Strict compare
                    csvFile.FileInfo is null ||
                    !FileSystemHelper.FileSystemTimeIsEquals(csvFile.FileInfo.LastWriteTimeUtc, fileInfo.LastWriteTimeUtc))
                {
                    csvFile.FileName = fileInfo.Name;
                    csvFile.FileInfo = fileInfo;
                    csvFile.DataIsChangedOnDisk = true;

                    foreach (CsvFile oldCsvFile in _csvFilesCollection.Values)
                    {
                        if (oldCsvFile.IncludeFileNamesCollection.Contains(fileNameUpper))
                            oldCsvFile.DataIsChangedOnDisk = true;
                    }

                    csvFile.MovedToNewCollection = true;
                }
                newCsvFilesCollection.Add(fileNameUpper, csvFile);                
            }

            foreach (var kvp in _csvFilesCollection)
            {
                if (!kvp.Value.MovedToNewCollection)
                {
                    if (kvp.Value.DataIsChangedByProgram)
                    {
                        newCsvFilesCollection.Add(kvp.Key, kvp.Value);                        
                    }
                    else
                    {
                        // Notify about deleted files
                        eventArgsList.Add(new CsvFileChangedEventArgs 
                            {
                                CsvFileChangeAction = CsvFileChangeAction.Removed,
                                CsvFileName = kvp.Value.FileName 
                            });
                    }                    
                }
            }

            foreach (var kvp in newCsvFilesCollection)
            {
                CsvFile csvFile = kvp.Value;                
                if (csvFile.DataIsChangedOnDisk)
                {
                    csvFile.IncludeFileNamesCollection.Clear();
                    csvFile.FileData = null;
                    csvFile.DataIsChangedOnDisk = false;
                    csvFile.DataIsChangedByProgram = false;

                    // Notify about changed files
                    if (csvFile.MovedToNewCollection)
                        eventArgsList.Add(new CsvFileChangedEventArgs
                            {
                                CsvFileChangeAction = CsvFileChangeAction.Updated,
                                CsvFileName = kvp.Value.FileName
                            });
                    else
                        eventArgsList.Add(new CsvFileChangedEventArgs
                            {
                                CsvFileChangeAction = CsvFileChangeAction.Added,
                                CsvFileName = kvp.Value.FileName
                            });
                }
                csvFile.MovedToNewCollection = false;
            }            

            _csvFilesCollection = newCsvFilesCollection;
            
            foreach (var eventArgs in eventArgsList)
                CsvFileChanged?.Invoke(this, eventArgs);
        }

        /// <summary>
        ///     Clears existing file, if any.
        ///     Returns true, if succeeded.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public bool FileCreate(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return false;

            string fileNameUpper = fileName!.ToUpperInvariant();
            if (!_csvFilesCollection.TryGetValue(fileNameUpper, out CsvFile? csvFile))
            {
                csvFile = new CsvFile();
                _csvFilesCollection.Add(fileNameUpper, csvFile);
            }

            if (CsvDbDirectoryInfo is not null)
                csvFile.FileName = fileName;
            csvFile.IncludeFileNamesCollection.Clear();
            csvFile.FileData = new CaseInsensitiveDictionary<List<string?>>();
            csvFile.DataIsChangedByProgram = true;            

            return true;
        }

        public bool FileExists(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return false;

            return _csvFilesCollection.ContainsKey(fileName!.ToUpperInvariant());
        }

        /// <summary>
        ///     Returns true, if succeeded.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public bool FileClear(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return false;
            
            if (!_csvFilesCollection.TryGetValue(fileName!.ToUpperInvariant(), out CsvFile? csvFile)) return false;
            csvFile.IncludeFileNamesCollection.Clear();
            csvFile.FileData = new CaseInsensitiveDictionary<List<string?>>();
            csvFile.DataIsChangedByProgram = true;
            return true;
        }

        public bool DeleteValues(string? fileName, string key)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return false;

            if (!_csvFilesCollection.TryGetValue(fileName!.ToUpperInvariant(), out CsvFile? csvFile)) return false;

            EnsureDataIsLoaded(csvFile);

            bool removed = csvFile.FileData!.Remove(key);
            if (removed)
                csvFile.DataIsChangedByProgram = true;
            return removed;
        }        

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetFileNames()
        {
            return _csvFilesCollection.Values.Select(cf => cf.FileName);
        }

        public IEnumerable<FileInfo> GetFileInfos()
        {
            if (CsvDbDirectoryInfo is null)
                return new FileInfo[0];

            return _csvFilesCollection.Values.Select(cf => cf.FileInfo).Where(fi => fi is not null).OfType<FileInfo>();
        }

        /// <summary>
        ///     List.Count >= 1, List[0] is not null
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public CaseInsensitiveDictionary<List<string?>> GetValues(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return new CaseInsensitiveDictionary<List<string?>>();
            
            if (!_csvFilesCollection.TryGetValue(fileName!.ToUpperInvariant(), out CsvFile? csvFile)) return new CaseInsensitiveDictionary<List<string?>>();

            EnsureDataIsLoaded(csvFile);
            
            return csvFile.FileData!;
        }

        /// <summary>
        ///     Returns new uint key, starting from 1.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public uint GetNewKey(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return 1;

            if (!_csvFilesCollection.TryGetValue(fileName!.ToUpperInvariant(), out CsvFile? csvFile))
                return 1;

            EnsureDataIsLoaded(csvFile);
            
            if (csvFile.FileData!.Count == 0) return 1;
            return csvFile.FileData.Keys.Max(k => new Any(k).ValueAsUInt32(false)) + 1;
        }

        /// <summary>
        ///     null or List.Count >= 1, List[0] is not null
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public List<string?>? GetValues(string? fileName, string key)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return null;
            
            if (!_csvFilesCollection.TryGetValue(fileName!.ToUpperInvariant(), out CsvFile? csvFile)) return null;

            EnsureDataIsLoaded(csvFile);

            if (!csvFile.FileData!.TryGetValue(key, out List<string?>? fileLine)) return null;
            return fileLine;
        }

        public string? GetValue(string? fileName, string key, int column)
        {
            if (string.IsNullOrWhiteSpace(fileName) || column < 0) return null;
            
            if (!_csvFilesCollection.TryGetValue(fileName!.ToUpperInvariant(), out CsvFile? csvFile)) return null;

            EnsureDataIsLoaded(csvFile);

            if (!csvFile.FileData!.TryGetValue(key, out List<string?>? fileLine)) return null;
            if (column >= fileLine.Count) return null;
            return fileLine[column];
        }

        public static string? GetValue(CaseInsensitiveDictionary<List<string?>> data, string key, int column)
        {
            if (column < 0) return null;
            if (!data.TryGetValue(key, out List<string?>? fileLine)) return null;
            if (column >= fileLine.Count) return null;
            return fileLine[column];
        }

        public static string? GetValue(List<string?> fileLine, int column)
        {
            if (column < 0 || column >= fileLine.Count) return null;
            return fileLine[column];
        }

        public void SetValue(string? fileName, string key, int column, string? value)
        {
            if (string.IsNullOrWhiteSpace(fileName) || column < 0) return;

            string fileNameUpper = fileName!.ToUpperInvariant();

            if (!fileNameUpper.EndsWith(@".CSV")) return;
            
            if (!_csvFilesCollection.TryGetValue(fileNameUpper, out CsvFile? csvFile))
            {
                csvFile = new CsvFile();                
                csvFile.FileName = fileName;                
                csvFile.FileData = new CaseInsensitiveDictionary<List<string?>>();
                _csvFilesCollection.Add(fileNameUpper, csvFile);
            }

            EnsureDataIsLoaded(csvFile);

            if (!csvFile.FileData!.TryGetValue(key, out List<string?>? fileLine))
            {
                fileLine = new List<string?> { key };
                csvFile.FileData.Add(key, fileLine);
            }

            if (column >= fileLine.Count)
            {
                fileLine.Capacity = column + 1;
                fileLine.AddRange(Enumerable.Repeat<string?>(null, column + 1 - fileLine.Count));
            }

            if (column > 0) fileLine[column] = value;

            csvFile.DataIsChangedByProgram = true;
        }

        /// <summary>
        ///     Appends valuesLine to existing data.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="valuesLine"></param>
        public void SetValues(string? fileName, IEnumerable<string?> valuesLine)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return;

            string fileNameUpper = fileName!.ToUpperInvariant();

            if (!fileNameUpper.EndsWith(@".CSV")) return;

            if (!_csvFilesCollection.TryGetValue(fileNameUpper, out CsvFile? csvFile))
            {
                csvFile = new CsvFile();
                csvFile.FileName = fileName;                
                csvFile.FileData = new CaseInsensitiveDictionary<List<string?>>();
                _csvFilesCollection.Add(fileNameUpper, csvFile);
            }

            var enumerator = valuesLine.GetEnumerator();
            if (!enumerator.MoveNext()) return;
            string key = enumerator.Current ?? @"";

            EnsureDataIsLoaded(csvFile);

            if (!csvFile.FileData!.TryGetValue(key, out List<string?>? fileLine))
            {
                fileLine = new List<string?> { key };
                csvFile.FileData.Add(key, fileLine);
            }
            else
            {
                fileLine.Clear();
                fileLine.Add(key);
            }

            while (enumerator.MoveNext())
            {
                fileLine.Add(enumerator.Current);
            }

            csvFile.DataIsChangedByProgram = true;
        }

        /// <summary>
        ///     Appends values to existing data.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="values"></param>
        public void SetValues(string? fileName, IEnumerable<IEnumerable<string?>> values)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return;

            string fileNameUpper = fileName!.ToUpperInvariant();

            if (!fileNameUpper.EndsWith(@".CSV")) return;

            if (!_csvFilesCollection.TryGetValue(fileNameUpper, out CsvFile? csvFile))
            {
                csvFile = new CsvFile();
                csvFile.FileName = fileName;
                csvFile.FileData = new CaseInsensitiveDictionary<List<string?>>();
                _csvFilesCollection.Add(fileNameUpper, csvFile);
            }

            foreach (var valuesLine in values)
            {
                var enumerator = valuesLine.GetEnumerator();
                if (!enumerator.MoveNext()) return;
                string key = enumerator.Current ?? @"";

                EnsureDataIsLoaded(csvFile);

                if (!csvFile.FileData!.TryGetValue(key, out List<string?>? fileLine))
                {
                    fileLine = new List<string?> { key };
                    csvFile.FileData.Add(key, fileLine);
                }
                else
                {
                    fileLine.Clear();
                    fileLine.Add(key);
                }

                while (enumerator.MoveNext())
                {
                    fileLine.Add(enumerator.Current);
                }
            }            

            csvFile.DataIsChangedByProgram = true;
        }

        /// <summary>
        ///     Saves changed data to .csv files on disk.
        /// </summary>
        public void SaveData()
        {
            if (CsvDbDirectoryInfo is null) return;

            List<CsvFileChangedEventArgs> eventArgsList = new();

            _fileSystemWatcher.EnableRaisingEvents = false;

            foreach (var kvp in _csvFilesCollection)
            {
                CsvFile csvFile = kvp.Value;
                if (csvFile.DataIsChangedByProgram)
                {
                    string fileFullName = Path.Combine(CsvDbDirectoryInfo.FullName, csvFile.FileName);
                    try
                    {
                        var isNewCsvFile = csvFile.FileInfo is null;
                        
                        // If the file to be deleted does not exist, no exception is thrown.
                        File.Delete(fileFullName); // For 'a' to 'A' changes in files names to work.
                        using (var writer = new StreamWriter(fileFullName, false, new UTF8Encoding(true)))
                        {
                            foreach (var fileLine in csvFile.FileData!.OrderBy(kvp => kvp.Key))
                                writer.WriteLine(CsvHelper.FormatForCsv(",", fileLine.Value.ToArray()));
                        }                                                
                        csvFile.FileInfo = new FileInfo(fileFullName);

                        if (isNewCsvFile)
                            eventArgsList.Add(new CsvFileChangedEventArgs
                                {
                                    CsvFileChangeAction = CsvFileChangeAction.Added,
                                    CsvFileName = kvp.Value.FileName
                                });
                        else
                            eventArgsList.Add(new CsvFileChangedEventArgs
                                {
                                    CsvFileChangeAction = CsvFileChangeAction.Updated,
                                    CsvFileName = kvp.Value.FileName
                                });
                    }
                    catch (Exception ex)
                    {
                        LoggersSet.Logger.LogError(ex, Properties.Resources.CsvDb_CsvFileWritingError + " " + fileFullName);
                    }

                    csvFile.DataIsChangedByProgram = false;
                }                
            }

            var t = FileSystemWatcherEnableRaisingEventsAsync();

            foreach (var eventArgs in eventArgsList)
                CsvFileChanged?.Invoke(this, eventArgs);
        }

        /// <summary>
        ///     Saves changed data to .csv file on disk (only this file).
        /// </summary>
        public void SaveData(string? fileName)
        {
            if (CsvDbDirectoryInfo is null) return;            

            if (string.IsNullOrWhiteSpace(fileName)) return;            

            string fileNameUpper = fileName!.ToUpperInvariant();

            if (!_csvFilesCollection.TryGetValue(fileNameUpper, out CsvFile? csvFile)) return;

            List<CsvFileChangedEventArgs> eventArgsList = new();

            if (csvFile.DataIsChangedByProgram)
            {
                _fileSystemWatcher.EnableRaisingEvents = false;

                string fileFullName = Path.Combine(CsvDbDirectoryInfo.FullName, csvFile.FileName);
                try
                {
                    var isNewCsvFile = csvFile.FileInfo is null;

                    // If the file to be deleted does not exist, no exception is thrown.
                    File.Delete(fileFullName); // For 'a' to 'A' changes in files names to work.
                    using (var writer = new StreamWriter(fileFullName, false, new UTF8Encoding(true)))
                    {
                        foreach (var fileLine in csvFile.FileData!.OrderBy(kvp => kvp.Key))
                            writer.WriteLine(CsvHelper.FormatForCsv(",", fileLine.Value.ToArray()));
                    }
                    csvFile.FileInfo = new FileInfo(fileFullName);
                    
                    if (isNewCsvFile)
                        eventArgsList.Add(new CsvFileChangedEventArgs
                            {
                                CsvFileChangeAction = CsvFileChangeAction.Added,
                                CsvFileName = csvFile.FileName
                            });                    
                    else
                        eventArgsList.Add(new CsvFileChangedEventArgs
                            {
                                CsvFileChangeAction = CsvFileChangeAction.Updated,
                                CsvFileName = csvFile.FileName
                            });                    
                }
                catch (Exception ex)
                {
                    LoggersSet.Logger.LogError(ex, Properties.Resources.CsvDb_CsvFileWritingError + " " + fileFullName);
                }

                csvFile.DataIsChangedByProgram = false;

                var t = FileSystemWatcherEnableRaisingEventsAsync();

                foreach (var eventArgs in eventArgsList)
                    CsvFileChanged?.Invoke(this, eventArgs);
            }
        }

        #endregion

        #region private functions

        private void EnsureDataIsLoaded(CsvFile csvFile)
        {
            if (csvFile.FileData is null)
            {
                if (CsvDbDirectoryInfo is not null)
                    csvFile.FileData = CsvHelper.LoadCsvFile(Path.Combine(CsvDbDirectoryInfo.FullName, csvFile.FileName), true, null, LoggersSet.UserFriendlyLogger, csvFile.IncludeFileNamesCollection);
                else
                    csvFile.FileData = new CaseInsensitiveDictionary<List<string?>>();
            }
        }

        private async void FileSystemWatcherOnEventAsync(object sender, FileSystemEventArgs e)
        {
            if (_fileSystemWatcherOnEventIsProcessing) return;
            _fileSystemWatcherOnEventIsProcessing = true;
            await Task.Delay(1000);
            _fileSystemWatcherOnEventIsProcessing = false;

            if (Dispatcher is not null)
            {
                Dispatcher.BeginInvoke(ct =>
                {
                    this.LoadData();                    
                });
            }            
        }

        private async Task FileSystemWatcherEnableRaisingEventsAsync()
        {
            while (true)
            {
                try
                {
                    _fileSystemWatcher.EnableRaisingEvents = true;
                    break;
                }
                catch
                {
                    await Task.Delay(1000);
                }
            }
        }

        #endregion

        #region private fields

        /// <summary>
        ///     [File name with .CSV extension in Upper-Case, CsvFile]
        /// </summary>
        private Dictionary<string, CsvFile> _csvFilesCollection =
            new();        

        private readonly FileSystemWatcher _fileSystemWatcher = new();

        private volatile bool _fileSystemWatcherOnEventIsProcessing;

        #endregion

        private class CsvFile
        {
            public string FileName = @"";

            public FileInfo? FileInfo;

            public CaseInsensitiveDictionary<List<string?>>? FileData;

            /// <summary>            
            ///     File names in Upper-Case
            /// </summary>
            public List<string> IncludeFileNamesCollection = new();

            public bool DataIsChangedByProgram;

            public bool DataIsChangedOnDisk;

            public bool MovedToNewCollection;
        }
    }

    public class CsvFileChangedEventArgs : EventArgs
    {        
        public CsvFileChangeAction CsvFileChangeAction { get; set; }

        public string CsvFileName { get; set; } = @"";
    }

    public enum CsvFileChangeAction
    {        
        Added,
        Updated,
        Removed
    }
}
