using Microsoft.Extensions.Logging;
using Ssz.Utils.Dispatcher;
using Ssz.Utils.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    /// <summary>
    ///     Only the thread that the Dispatcher was created on may access the CsvDb directly. 
    ///     To access a CsvDb from a thread other than the thread the CsvDb was created on,
    ///     call BeginInvoke or BeginInvokeEx on the Dispatcher the CsvDb is associated with.
    /// </summary>
    public class CsvDb : IDispatcherObject
    {
        #region construction and destruction

        /// <summary>
        ///     userFriendlyLogger: Messages are localized. Priority is Information, Error, Warning.
        ///     If csvDbDirectoryInfo is null, directory is not used.
        ///     If !csvDbDirectoryInfo.Exists, directory is created.
        ///     If dispatcher is not null, monitors csvDbDirectoryInfo for changes.        
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="userFriendlyLogger"></param>
        /// <param name="csvDbDirectoryInfo"></param>
        /// <param name="dispatcher"></param>
        public CsvDb(ILogger<CsvDb> logger, IUserFriendlyLogger? userFriendlyLogger = null, DirectoryInfo? csvDbDirectoryInfo = null, IDispatcher? dispatcher = null)
        {
            LoggersSet = new LoggersSet<CsvDb>(logger, userFriendlyLogger);            
            CsvDbDirectoryInfo = csvDbDirectoryInfo;
            Dispatcher = dispatcher;

            if (CsvDbDirectoryInfo is not null && CsvDbDirectoryInfo.Exists)
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

            if (CsvDbDirectoryInfo is not null && CsvDbDirectoryInfo.Exists)
            {
                try
                {
                    string file = Path.Combine(CsvDbDirectoryInfo.FullName, Guid.NewGuid().ToString().ToLower());
                    File.CreateText(file).Close();
                    CsvDbDirectoryIsCaseInsensistve = File.Exists(file.ToUpper());
                    File.Delete(file);                    
                    CsvDbDirectoryHasWriteAccess = true;
                }
                catch
                {                      
                }

                LoggersSet.Logger.LogDebug("CsvDb Created for: " + CsvDbDirectoryInfo.FullName);
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

            if (CsvDbDirectoryIsCaseInsensistve)
                _csvFilesCollection = new CaseInsensitiveDictionary<CsvFile>();
            else
                _csvFilesCollection = new Dictionary<string, CsvFile>();

            LoadData();
        }

        #endregion

        #region public functions

        public LoggersSet<CsvDb> LoggersSet { get; }

        /// <summary>
        ///     Existing directory info or null
        /// </summary>
        public DirectoryInfo? CsvDbDirectoryInfo { get; }

        public bool CsvDbDirectoryIsCaseInsensistve { get; } = true;

        public bool CsvDbDirectoryHasWriteAccess { get; }

        /// <summary>
        ///     To access a CsvDb from a thread other than the thread the CsvDb was created on,
        ///     call BeginInvoke or BeginInvokeEx on this Dispatcher.
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
            if (CsvDbDirectoryInfo is null || !CsvDbDirectoryInfo.Exists) 
                return;

            List<CsvFileChangedEventArgs> eventArgsList = new();

            Dictionary<string, CsvFile> newCsvFilesCollection;
            if (CsvDbDirectoryIsCaseInsensistve)
                newCsvFilesCollection = new CaseInsensitiveDictionary<CsvFile>();
            else
                newCsvFilesCollection = new Dictionary<string, CsvFile>();

            foreach (FileInfo fileInfo in CsvDbDirectoryInfo.GetFiles(@"*", SearchOption.TopDirectoryOnly).Where(f => f.Name.EndsWith(@".csv",
                StringComparison.InvariantCultureIgnoreCase)))
            {                
                _csvFilesCollection.TryGetValue(fileInfo.Name, out CsvFile? csvFile);
                if (csvFile is null)
                {
                    csvFile = new CsvFile
                    {
                        FileName = fileInfo.Name,
                        OnDiskFileInfo = fileInfo,                        
                        DataIsChangedOnDisk = true
                    };

                    foreach (CsvFile oldCsvFile in _csvFilesCollection.Values)
                    {
                        if (oldCsvFile.IncludeFileNamesCollection.Contains(fileInfo.Name))
                            oldCsvFile.DataIsChangedOnDisk = true;
                    }
                }
                else
                {
                    if (Path.GetFileNameWithoutExtension(csvFile.FileName) != Path.GetFileNameWithoutExtension(fileInfo.Name) || // Strict compare
                        csvFile.Data_OnDiskFileInfo_LastWriteTimeUtc is null ||
                        csvFile.Data_OnDiskFileInfo_LastWriteTimeUtc.Value != fileInfo.LastWriteTimeUtc)
                    {
                        csvFile.FileName = fileInfo.Name;
                        csvFile.OnDiskFileInfo = fileInfo;                        
                        csvFile.DataIsChangedOnDisk = true;

                        foreach (CsvFile oldCsvFile in _csvFilesCollection.Values)
                        {
                            if (oldCsvFile.IncludeFileNamesCollection.Contains(fileInfo.Name))
                                oldCsvFile.DataIsChangedOnDisk = true;
                        }                        
                    }

                    csvFile.MovedToNewCollection = true;
                }
                newCsvFilesCollection.Add(fileInfo.Name, csvFile);                
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
                    csvFile.Data = null;
                    csvFile.Data_OnDiskFileInfo_LastWriteTimeUtc = null;
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
            
            if (!_csvFilesCollection.TryGetValue(fileName!, out CsvFile? csvFile))
            {
                csvFile = new CsvFile();
                _csvFilesCollection.Add(fileName!, csvFile);
            }

            if (CsvDbDirectoryInfo is not null && CsvDbDirectoryInfo.Exists)
                csvFile.FileName = fileName!;
            csvFile.IncludeFileNamesCollection.Clear();
            csvFile.Data = new CaseInsensitiveDictionary<List<string?>>();
            csvFile.Data_OnDiskFileInfo_LastWriteTimeUtc = null;                     
            csvFile.DataIsChangedByProgram = true;            

            return true;
        }

        public bool FileExists(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return false;

            return _csvFilesCollection.ContainsKey(fileName!);
        }

        /// <summary>
        ///     Returns true, if succeeded.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public bool FileClear(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return false;
            
            if (!_csvFilesCollection.TryGetValue(fileName!, out CsvFile? csvFile)) return false;
            csvFile.IncludeFileNamesCollection.Clear();
            csvFile.Data = new CaseInsensitiveDictionary<List<string?>>();
            csvFile.Data_OnDiskFileInfo_LastWriteTimeUtc = null;            
            csvFile.DataIsChangedByProgram = true;
            return true;
        }

        public bool DeleteValues(string? fileName, string key)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return false;

            if (!_csvFilesCollection.TryGetValue(fileName!, out CsvFile? csvFile)) return false;

            EnsureDataIsLoaded(csvFile);

            bool removed = csvFile.Data!.Remove(key);
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
            if (CsvDbDirectoryInfo is null || !CsvDbDirectoryInfo.Exists)
                return new FileInfo[0];

            return _csvFilesCollection.Values.Select(cf => cf.OnDiskFileInfo).Where(fi => fi is not null).OfType<FileInfo>();
        }

        /// <summary>
        ///     List.Count >= 1, List[0] is not null
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public CaseInsensitiveDictionary<List<string?>> GetData(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return new CaseInsensitiveDictionary<List<string?>>();
            
            if (!_csvFilesCollection.TryGetValue(fileName!, out CsvFile? csvFile)) return new CaseInsensitiveDictionary<List<string?>>();

            EnsureDataIsLoaded(csvFile);
            
            return csvFile.Data!;
        }

        /// <summary>
        ///     Returns new uint key, starting from 1.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public uint GetNewKey(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return 1;

            if (!_csvFilesCollection.TryGetValue(fileName!, out CsvFile? csvFile))
                return 1;

            EnsureDataIsLoaded(csvFile);
            
            if (csvFile.Data!.Count == 0) return 1;
            return csvFile.Data.Keys.Max(k => new Any(k).ValueAsUInt32(false)) + 1;
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
            
            if (!_csvFilesCollection.TryGetValue(fileName!, out CsvFile? csvFile)) return null;

            EnsureDataIsLoaded(csvFile);

            if (!csvFile.Data!.TryGetValue(key, out List<string?>? values)) return null;
            return values;
        }

        public string? GetValue(string? fileName, string key, int column)
        {
            if (string.IsNullOrWhiteSpace(fileName) || column < 0) return null;
            
            if (!_csvFilesCollection.TryGetValue(fileName!, out CsvFile? csvFile)) return null;

            EnsureDataIsLoaded(csvFile);

            if (!csvFile.Data!.TryGetValue(key, out List<string?>? values)) return null;
            if (column >= values.Count) return null;
            return values[column];
        }

        public static string? GetValue(CaseInsensitiveDictionary<List<string?>> data, string key, int column)
        {
            if (column < 0) return null;
            if (!data.TryGetValue(key, out List<string?>? values)) return null;
            if (column >= values.Count) return null;
            return values[column];
        }

        public static string? GetValue(List<string?> values, int column)
        {
            if (column < 0 || column >= values.Count) return null;
            return values[column];
        }

        public void SetValue(string? fileName, string key, int column, string? value)
        {
            if (string.IsNullOrWhiteSpace(fileName) || column < 0) return;

            if (!fileName!.EndsWith(@".csv", StringComparison.InvariantCultureIgnoreCase)) return;
            
            if (!_csvFilesCollection.TryGetValue(fileName!, out CsvFile? csvFile))
            {
                csvFile = new CsvFile();                
                csvFile.FileName = fileName!;                
                csvFile.Data = new CaseInsensitiveDictionary<List<string?>>();
                csvFile.Data_OnDiskFileInfo_LastWriteTimeUtc = null;                              
                _csvFilesCollection.Add(fileName!, csvFile);
            }

            EnsureDataIsLoaded(csvFile);

            if (!csvFile.Data!.TryGetValue(key, out List<string?>? values))
            {
                values = new List<string?> { key };
                csvFile.Data.Add(key, values);
            }

            if (column >= values.Count)
            {
                values.Capacity = column + 1;
                values.AddRange(Enumerable.Repeat<string?>(null, column + 1 - values.Count));
            }

            if (column > 0) values[column] = value;

            csvFile.DataIsChangedByProgram = true;
        }

        /// <summary>
        ///     Appends values to existing data.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="values"></param>
        public void SetValues(string? fileName, IEnumerable<string?> values)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return;

            if (!fileName!.EndsWith(@".csv", StringComparison.InvariantCultureIgnoreCase)) return;

            if (!_csvFilesCollection.TryGetValue(fileName!, out CsvFile? csvFile))
            {
                csvFile = new CsvFile();
                csvFile.FileName = fileName!;
                csvFile.Data = new CaseInsensitiveDictionary<List<string?>>();
                csvFile.Data_OnDiskFileInfo_LastWriteTimeUtc = null;                             
                _csvFilesCollection.Add(fileName!, csvFile);
            }

            var enumerator = values.GetEnumerator();
            if (!enumerator.MoveNext()) return;
            string key = enumerator.Current ?? @"";

            EnsureDataIsLoaded(csvFile);

            if (!csvFile.Data!.TryGetValue(key, out List<string?>? existingValues))
            {
                existingValues = new List<string?> { key };
                csvFile.Data.Add(key, existingValues);
            }
            else
            {
                existingValues.Clear();
                existingValues.Add(key);
            }

            while (enumerator.MoveNext())
            {
                existingValues.Add(enumerator.Current);
            }

            csvFile.DataIsChangedByProgram = true;
        }

        /// <summary>        
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool FileEquals(string? fileName, IEnumerable<IEnumerable<string?>> data)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return false;            

            if (!fileName!.EndsWith(@".csv", StringComparison.InvariantCultureIgnoreCase)) return false;

            var valuesList = data.ToList();

            if (!_csvFilesCollection.TryGetValue(fileName!, out CsvFile? csvFile))
                return valuesList.Count == 0;

            EnsureDataIsLoaded(csvFile);

            if (csvFile.Data!.Count != valuesList.Count)
                return false;

            StringBuilder valuesString = new();
            foreach (var values in valuesList)
            {
                valuesString.Append(CsvHelper.FormatForCsv(",", values));                
            }

            StringBuilder fileDataString = new();
            foreach (var kvp in csvFile.Data.OrderBy(i => i.Key))
            {
                fileDataString.Append(CsvHelper.FormatForCsv(",", kvp.Value));                
            }

            return valuesString.ToString() == fileDataString.ToString();
        }

        /// <summary>
        ///     Appends values to existing data.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="data"></param>
        public void SetData(string? fileName, IEnumerable<IEnumerable<string?>> data)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return;            

            if (!fileName!.EndsWith(@".csv", StringComparison.InvariantCultureIgnoreCase)) return;

            if (!_csvFilesCollection.TryGetValue(fileName!, out CsvFile? csvFile))
            {
                csvFile = new CsvFile();
                csvFile.FileName = fileName!;
                csvFile.Data = new CaseInsensitiveDictionary<List<string?>>();
                csvFile.Data_OnDiskFileInfo_LastWriteTimeUtc = null;                           
                _csvFilesCollection.Add(fileName!, csvFile);
            }
            else
            {
                EnsureDataIsLoaded(csvFile);
            }

            foreach (var values in data)
            {
                var enumerator = values.GetEnumerator();
                if (!enumerator.MoveNext()) return;
                string key = enumerator.Current ?? @"";

                if (!csvFile.Data!.TryGetValue(key, out List<string?>? existingValues))
                {
                    existingValues = new List<string?> { key };
                    csvFile.Data.Add(key, existingValues);
                }
                else
                {
                    existingValues.Clear();
                    existingValues.Add(key);
                }

                while (enumerator.MoveNext())
                {
                    existingValues.Add(enumerator.Current);
                }
            }

            csvFile.DataIsChangedByProgram = true;
        }

        /// <summary>
        ///     Saves changed data to .csv files on disk.
        /// </summary>
        public void SaveData()
        {
            if (CsvDbDirectoryInfo is null || !CsvDbDirectoryInfo.Exists) 
                return;

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
                        var isNewCsvFile = csvFile.OnDiskFileInfo is null;
                        
                        // If the file to be deleted does not exist, no exception is thrown.
                        File.Delete(fileFullName); // For 'a' to 'A' changes in files names to work.
                        using (var writer = new StreamWriter(fileFullName, false, new UTF8Encoding(true)))
                        {
                            foreach (var kvp2 in csvFile.Data!.OrderBy(i => i.Key))
                                writer.WriteLine(CsvHelper.FormatForCsv(",", kvp2.Value));
                        }
                        var fi = new FileInfo(fileFullName);
                        csvFile.OnDiskFileInfo = fi;
                        csvFile.Data_OnDiskFileInfo_LastWriteTimeUtc = fi.LastWriteTimeUtc;

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
            if (CsvDbDirectoryInfo is null || !CsvDbDirectoryInfo.Exists) 
                return;            

            if (string.IsNullOrWhiteSpace(fileName)) return;

            if (!_csvFilesCollection.TryGetValue(fileName!, out CsvFile? csvFile)) return;

            List<CsvFileChangedEventArgs> eventArgsList = new();

            if (csvFile.DataIsChangedByProgram)
            {
                _fileSystemWatcher.EnableRaisingEvents = false;

                string fileFullName = Path.Combine(CsvDbDirectoryInfo.FullName, csvFile.FileName);
                try
                {
                    var isNewCsvFile = csvFile.OnDiskFileInfo is null;

                    // If the file to be deleted does not exist, no exception is thrown.
                    File.Delete(fileFullName); // For 'a' to 'A' changes in files names to work.
                    using (var writer = new StreamWriter(fileFullName, false, new UTF8Encoding(true)))
                    {
                        foreach (var values in csvFile.Data!.OrderBy(kvp => kvp.Key))
                            writer.WriteLine(CsvHelper.FormatForCsv(",", values.Value.ToArray()));
                    }
                    var fi = new FileInfo(fileFullName);
                    csvFile.Data_OnDiskFileInfo_LastWriteTimeUtc = fi.LastWriteTimeUtc;
                    csvFile.OnDiskFileInfo = fi;                    
                    
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
            if (csvFile.Data is null)
            {
                if (CsvDbDirectoryInfo is not null && CsvDbDirectoryInfo.Exists)
                {
                    using var fileFullNameScope = LoggersSet.UserFriendlyLogger.BeginScope((Properties.Resources.FileNameScopeName, csvFile.FileName));
                    csvFile.IncludeFileNamesCollection.Clear();
                    string fileFullName = Path.Combine(CsvDbDirectoryInfo.FullName, csvFile.FileName);
                    csvFile.Data = CsvHelper.LoadCsvFile(fileFullName, true, null, LoggersSet.UserFriendlyLogger, csvFile.IncludeFileNamesCollection);
                    var fi = new FileInfo(fileFullName);
                    csvFile.Data_OnDiskFileInfo_LastWriteTimeUtc = fi.LastWriteTimeUtc;
                    csvFile.OnDiskFileInfo = fi;                    
                }
                else
                {
                    csvFile.Data = new CaseInsensitiveDictionary<List<string?>>();
                    csvFile.Data_OnDiskFileInfo_LastWriteTimeUtc = null;
                }   
            }
        }

        private async void FileSystemWatcherOnEventAsync(object sender, FileSystemEventArgs e)
        {
            if (_fileSystemWatcherOnEventIsProcessing) return;
            _fileSystemWatcherOnEventIsProcessing = true;
            //await Task.Delay(1000); // Some problems with this line 
            await Task.Run(() => Thread.Sleep(1000)); // Working
            _fileSystemWatcherOnEventIsProcessing = false;

            Dispatcher!.BeginInvoke(ct =>
            {
                LoadData();
            });
        }

        private async Task FileSystemWatcherEnableRaisingEventsAsync()
        {
            for (int i = 0; i < 10; i += 1)
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
        ///     [File name with .csv extensione, CsvFile]
        /// </summary>
        private Dictionary<string, CsvFile> _csvFilesCollection;        

        private readonly FileSystemWatcher _fileSystemWatcher = new();

        private volatile bool _fileSystemWatcherOnEventIsProcessing;

        #endregion

        private class CsvFile
        {
            public string FileName = @"";

            public FileInfo? OnDiskFileInfo;

            public DateTime? Data_OnDiskFileInfo_LastWriteTimeUtc;

            public CaseInsensitiveDictionary<List<string?>>? Data;            

            /// <summary>            
            ///     File names
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
