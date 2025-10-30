using Microsoft.Extensions.FileProviders;
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
        ///     If csvDbDirectoryFullName is null, directory is not used.
        ///     If !csvDbDirectoryInfo.Exists, directory is created.
        ///     If dispatcher is not null, monitors csvDbDirectoryInfo for changes.        
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="userFriendlyLogger"></param>
        /// <param name="csvDbDirectoryFullName"></param>
        /// <param name="csvDbFileProvider"></param>
        /// <param name="dispatcher"></param>
        public CsvDb(
            ILogger<CsvDb> logger, 
            IUserFriendlyLogger? userFriendlyLogger = null, 
            string? csvDbDirectoryFullName = null,
            IFileProvider? csvDbFileProvider = null,
            IDispatcher? dispatcher = null)
        {
            LoggersSet = new LoggersSet<CsvDb>(logger, userFriendlyLogger);
            CsvDbDirectoryFullName = csvDbDirectoryFullName;            
            CsvDbFileProvider = csvDbFileProvider;
            Dispatcher = dispatcher;

            CsvFileNameIsCaseSensistve = true;
            CsvDbDirectoryHasWriteAccess = false;

            if (!String.IsNullOrEmpty(CsvDbDirectoryFullName) && CsvDbFileProvider is null)
            {
                var csvDbDirectoryInfo = new DirectoryInfo(CsvDbDirectoryFullName);
                try
                {
                    // Creates all directories and subdirectories in the specified path unless they already exist.
                    Directory.CreateDirectory(csvDbDirectoryInfo.FullName);
                    if (!csvDbDirectoryInfo.Exists)
                        csvDbDirectoryInfo = null;
                }
                catch
                {
                    csvDbDirectoryInfo = null;
                }
                CsvDbDirectoryInfo = csvDbDirectoryInfo;

                if (CsvDbDirectoryInfo is not null)
                {
                    try
                    {
                        string file = Path.Combine(CsvDbDirectoryInfo.FullName, Guid.NewGuid().ToString().ToLower());
                        File.CreateText(file).Close();
                        CsvFileNameIsCaseSensistve = !File.Exists(file.ToUpper());
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
                            _fileSystemWatcher = new();
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
                            LoggersSet.Logger.LogWarning(ex, "CsvDb FileSystemWatcher error.");
                        }
                }
            }                     

            if (!CsvFileNameIsCaseSensistve)
                _csvFilesCollection = new Dictionary<string, CsvFile>(StringComparer.InvariantCultureIgnoreCase);
            else
                _csvFilesCollection = new Dictionary<string, CsvFile>();

            LoadCsvFileInfos();
        }

        #endregion

        #region public functions

        public LoggersSet<CsvDb> LoggersSet { get; }

        public string? CsvDbDirectoryFullName { get; }

        /// <summary>
        ///     Existing directory info or null
        /// </summary>
        public DirectoryInfo? CsvDbDirectoryInfo { get; }

        /// <summary>
        ///     
        /// </summary>
        public IFileProvider? CsvDbFileProvider { get; }

        public bool CsvFileNameIsCaseSensistve { get; }

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
                return _fileSystemWatcher?.EnableRaisingEvents ?? false;
            }
            set
            {
                if (!value)
                {
                    if (_fileSystemWatcher is not null)
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
        ///     Loads info about .csv files on disk.      
        ///     Is called in constructor.
        ///     Is called when directory content changes, if dispatcher in consructor is not null.
        /// </summary>
        public void LoadCsvFileInfos()
        {            
            List<CsvFileChangedEventArgs> eventArgsList = new();

            Dictionary<string, CsvFile> newCsvFilesCollection;
            if (!CsvFileNameIsCaseSensistve)
                newCsvFilesCollection = new Dictionary<string, CsvFile>(StringComparer.InvariantCultureIgnoreCase);
            else
                newCsvFilesCollection = new Dictionary<string, CsvFile>();
            
            if (CsvDbFileProvider is not null) // Always process first
            {
                foreach (IFileInfo fileInfo in CsvDbFileProvider.GetDirectoryContents(CsvDbDirectoryFullName ?? @"").Where(f => !f.IsDirectory && f.Name.EndsWith(@".csv",
                    StringComparison.InvariantCultureIgnoreCase)))
                {
                    _csvFilesCollection.TryGetValue(fileInfo.Name, out CsvFile? csvFile);
                    if (csvFile is null)
                    {
                        csvFile = new CsvFile
                        {
                            FileName = fileInfo.Name,
                            //OnDiskFileInfo = fileInfo,
                            Temp_NeedResetData = true
                        };

                        foreach (CsvFile oldCsvFile in _csvFilesCollection.Values)
                        {
                            if (oldCsvFile.IncludeFileNamesCollection.Contains(fileInfo.Name))
                                oldCsvFile.Temp_NeedResetData = true;
                        }
                    }
                    else
                    {
                        if (Path.GetFileNameWithoutExtension(csvFile.FileName) != Path.GetFileNameWithoutExtension(fileInfo.Name) || // Strict compare
                            csvFile.Data_FileInfo_LastModifiedUtc is null ||
                            csvFile.Data_FileInfo_LastModifiedUtc.Value != fileInfo.LastModified)
                        {
                            csvFile.FileName = fileInfo.Name;
                            //csvFile.OnDiskFileInfo = fileInfo;
                            csvFile.Temp_NeedResetData = true;

                            foreach (CsvFile oldCsvFile in _csvFilesCollection.Values)
                            {
                                if (oldCsvFile.IncludeFileNamesCollection.Contains(fileInfo.Name))
                                    oldCsvFile.Temp_NeedResetData = true;
                            }
                        }

                        csvFile.Temp_FileUpdated = true;
                    }
                    newCsvFilesCollection.Add(fileInfo.Name, csvFile);
                }
            }
            else if (CsvDbDirectoryInfo?.Exists == true)
            {
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
                            Temp_NeedResetData = true
                        };

                        foreach (CsvFile oldCsvFile in _csvFilesCollection.Values)
                        {
                            if (oldCsvFile.IncludeFileNamesCollection.Contains(fileInfo.Name))
                                oldCsvFile.Temp_NeedResetData = true;
                        }
                    }
                    else
                    {
                        if (Path.GetFileNameWithoutExtension(csvFile.FileName) != Path.GetFileNameWithoutExtension(fileInfo.Name) || // Strict compare
                            csvFile.Data_FileInfo_LastModifiedUtc is null ||
                            csvFile.Data_FileInfo_LastModifiedUtc.Value != fileInfo.LastWriteTimeUtc)
                        {
                            csvFile.FileName = fileInfo.Name;
                            csvFile.OnDiskFileInfo = fileInfo;
                            csvFile.Temp_NeedResetData = true;

                            foreach (CsvFile oldCsvFile in _csvFilesCollection.Values)
                            {
                                if (oldCsvFile.IncludeFileNamesCollection.Contains(fileInfo.Name))
                                    oldCsvFile.Temp_NeedResetData = true;
                            }
                        }

                        csvFile.Temp_FileUpdated = true;
                    }
                    newCsvFilesCollection.Add(fileInfo.Name, csvFile);
                }
            }

            foreach (var kvp in _csvFilesCollection)
            {
                if (!kvp.Value.Temp_FileUpdated)
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
                if (csvFile.Temp_NeedResetData)
                {
                    csvFile.IncludeFileNamesCollection.Clear();
                    csvFile.Data = null;
                    csvFile.Data_FileInfo_LastModifiedUtc = null;
                    csvFile.Temp_NeedResetData = false;
                    csvFile.DataIsChangedByProgram = false;

                    // Notify about changed files
                    if (csvFile.Temp_FileUpdated)
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
                csvFile.Temp_FileUpdated = false;
            }            

            _csvFilesCollection = newCsvFilesCollection;
            
            foreach (var eventArgs in eventArgsList)
                CsvFileChanged?.Invoke(this, eventArgs);
        }

        /// <summary>
        ///     Optional method for loading all csv files data in advance. 
        ///     If ths method is not called, csv files data are loaded only when needed.
        /// </summary>
        /// <returns></returns>
        public async Task EnsureCsvFilesDataIsLoadedAsync()
        {
            foreach (var csvFile in _csvFilesCollection.Values.ToArray())
            {
                if (csvFile.Data is null)
                {
                    if (CsvDbFileProvider is not null) // Always process first
                    {
                        using var fileFullNameScope = LoggersSet.UserFriendlyLogger.BeginScope((Properties.Resources.FileNameScopeName, csvFile.FileName));
                        csvFile.IncludeFileNamesCollection.Clear();
                        string fileFullName = Path.Combine(CsvDbDirectoryFullName ?? @"", csvFile.FileName);
                        csvFile.Data = await CsvHelper.LoadCsvFileAsync(fileFullName, true, CsvDbFileProvider, null, LoggersSet.UserFriendlyLogger, csvFile.IncludeFileNamesCollection);
                        csvFile.Data_FileInfo_LastModifiedUtc = CsvDbFileProvider.GetFileInfo(fileFullName).LastModified.UtcDateTime;
                        //csvFile.OnDiskFileInfo = fi;
                    }
                    else if (CsvDbDirectoryInfo?.Exists == true)
                    {
                        using var fileFullNameScope = LoggersSet.UserFriendlyLogger.BeginScope((Properties.Resources.FileNameScopeName, csvFile.FileName));
                        csvFile.IncludeFileNamesCollection.Clear();
                        string fileFullName = Path.Combine(CsvDbDirectoryInfo.FullName, csvFile.FileName);
                        csvFile.Data = CsvHelper.LoadCsvFile(fileFullName, true, null, LoggersSet.UserFriendlyLogger, csvFile.IncludeFileNamesCollection);
                        var fi = new FileInfo(fileFullName);
                        csvFile.Data_FileInfo_LastModifiedUtc = fi.LastWriteTimeUtc;
                        csvFile.OnDiskFileInfo = fi;
                    }                    
                    else
                    {
                        csvFile.Data = new CaseInsensitiveOrderedDictionary<List<string?>>();
                        csvFile.Data_FileInfo_LastModifiedUtc = null;
                    }
                }
            }            
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

            if (CsvDbDirectoryHasWriteAccess)
                csvFile.FileName = fileName!;
            csvFile.IncludeFileNamesCollection.Clear();
            csvFile.Data = new CaseInsensitiveOrderedDictionary<List<string?>>();
            csvFile.Data_FileInfo_LastModifiedUtc = null;                     
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
            csvFile.Data = new CaseInsensitiveOrderedDictionary<List<string?>>();
            csvFile.Data_FileInfo_LastModifiedUtc = null;            
            csvFile.DataIsChangedByProgram = true;
            return true;
        }

        public bool DeleteValues(string? fileName, string key)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return false;

            if (!_csvFilesCollection.TryGetValue(fileName!, out CsvFile? csvFile)) return false;

            EnsureCsvFileDataIsLoaded(csvFile);

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
        public CaseInsensitiveOrderedDictionary<List<string?>> GetData(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return new CaseInsensitiveOrderedDictionary<List<string?>>();
            
            if (!_csvFilesCollection.TryGetValue(fileName!, out CsvFile? csvFile)) return new CaseInsensitiveOrderedDictionary<List<string?>>();

            EnsureCsvFileDataIsLoaded(csvFile);
            
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

            EnsureCsvFileDataIsLoaded(csvFile);
            
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

            EnsureCsvFileDataIsLoaded(csvFile);

            if (!csvFile.Data!.TryGetValue(key, out List<string?>? values)) return null;
            return values;
        }

        public string? GetValue(string? fileName, string key, int column)
        {
            if (string.IsNullOrWhiteSpace(fileName) || column < 0) 
                return null;
            
            if (!_csvFilesCollection.TryGetValue(fileName!, out CsvFile? csvFile)) return null;

            EnsureCsvFileDataIsLoaded(csvFile);

            if (!csvFile.Data!.TryGetValue(key, out List<string?>? values)) return null;
            if (column >= values.Count) return null;
            return values[column];
        }

        public static string? GetValue(CaseInsensitiveOrderedDictionary<List<string?>> data, string key, int column)
        {
            if (column < 0) return null;
            if (!data.TryGetValue(key, out List<string?>? values)) return null;
            if (column >= values.Count) return null;
            return values[column];
        }

        public static string? GetValue(List<string?> values, int column)
        {
            if (column < 0 || column >= values.Count) 
                return null;
            return values[column];
        }

        public void SetValue(string? fileName, string key, int column, string? value)
        {
            if (string.IsNullOrWhiteSpace(fileName) || column < 1) 
                return;

            if (!fileName!.EndsWith(@".csv", StringComparison.InvariantCultureIgnoreCase)) return;
            
            if (!_csvFilesCollection.TryGetValue(fileName!, out CsvFile? csvFile))
            {
                csvFile = new CsvFile();                
                csvFile.FileName = fileName!;                
                csvFile.Data = new CaseInsensitiveOrderedDictionary<List<string?>>();
                csvFile.Data_FileInfo_LastModifiedUtc = null;                              
                _csvFilesCollection.Add(fileName!, csvFile);
            }

            EnsureCsvFileDataIsLoaded(csvFile);

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

            if (values[column] != value)
            {
                values[column] = value;

                csvFile.DataIsChangedByProgram = true;
            }
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
                csvFile.Data = new CaseInsensitiveOrderedDictionary<List<string?>>();
                csvFile.Data_FileInfo_LastModifiedUtc = null;                             
                _csvFilesCollection.Add(fileName!, csvFile);
            }

            var enumerator = values.GetEnumerator();
            if (!enumerator.MoveNext()) return;
            string key = enumerator.Current ?? @"";

            EnsureCsvFileDataIsLoaded(csvFile);

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

            EnsureCsvFileDataIsLoaded(csvFile);

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
                csvFile.Data = new CaseInsensitiveOrderedDictionary<List<string?>>();
                csvFile.Data_FileInfo_LastModifiedUtc = null;                           
                _csvFilesCollection.Add(fileName!, csvFile);
            }
            else
            {
                EnsureCsvFileDataIsLoaded(csvFile);
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
            if (!CsvDbDirectoryHasWriteAccess)
                return;

            if (CsvDbDirectoryInfo?.Exists == true)
            {
                List<CsvFileChangedEventArgs> eventArgsList = new();

                if (_fileSystemWatcher is not null)
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
                            csvFile.Data_FileInfo_LastModifiedUtc = fi.LastWriteTimeUtc;

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
        }

        /// <summary>
        ///     Saves changed data to .csv file on disk (only this file).
        /// </summary>
        public void SaveData(string? fileName)
        {
            if (!CsvDbDirectoryHasWriteAccess || String.IsNullOrWhiteSpace(fileName))
                return;

            if (CsvDbDirectoryInfo?.Exists == true)
            {
                if (!_csvFilesCollection.TryGetValue(fileName!, out CsvFile? csvFile)) return;

                List<CsvFileChangedEventArgs> eventArgsList = new();

                if (csvFile.DataIsChangedByProgram)
                {
                    if (_fileSystemWatcher is not null)
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
                        csvFile.Data_FileInfo_LastModifiedUtc = fi.LastWriteTimeUtc;
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
        }

        #endregion

        #region private functions

        private void EnsureCsvFileDataIsLoaded(CsvFile csvFile)
        {
            if (csvFile.Data is null)
            {
                if (CsvDbFileProvider is not null)
                {
                    throw new NotImplementedException("Call EnsureCsvFilesDataIsLoadedAsync() after CsvDb creation to avoid this error.");
                }
                else if (CsvDbDirectoryInfo?.Exists == true)
                {
                    using var fileFullNameScope = LoggersSet.UserFriendlyLogger.BeginScope((Properties.Resources.FileNameScopeName, csvFile.FileName));
                    csvFile.IncludeFileNamesCollection.Clear();
                    string fileFullName = Path.Combine(CsvDbDirectoryInfo.FullName, csvFile.FileName);
                    csvFile.Data = CsvHelper.LoadCsvFile(fileFullName, true, null, LoggersSet.UserFriendlyLogger, csvFile.IncludeFileNamesCollection);
                    var fi = new FileInfo(fileFullName);
                    csvFile.Data_FileInfo_LastModifiedUtc = fi.LastWriteTimeUtc;
                    csvFile.OnDiskFileInfo = fi;                    
                }                
                else
                {
                    csvFile.Data = new CaseInsensitiveOrderedDictionary<List<string?>>();
                    csvFile.Data_FileInfo_LastModifiedUtc = null;
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
                LoadCsvFileInfos();
            });
        }

        private async Task FileSystemWatcherEnableRaisingEventsAsync()
        {
            if (_fileSystemWatcher is null)
                return;

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

        private readonly FileSystemWatcher? _fileSystemWatcher;

        private volatile bool _fileSystemWatcherOnEventIsProcessing;

        #endregion

        private class CsvFile
        {
            public string FileName = @"";

            public FileInfo? OnDiskFileInfo;

            /// <summary>
            ///     On disk, or in FileProvider
            /// </summary>
            public DateTime? Data_FileInfo_LastModifiedUtc;

            public CaseInsensitiveOrderedDictionary<List<string?>>? Data;            

            /// <summary>            
            ///     File names
            /// </summary>
            public List<string> IncludeFileNamesCollection = new();

            public bool DataIsChangedByProgram;

            public bool Temp_NeedResetData;

            public bool Temp_FileUpdated;
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
