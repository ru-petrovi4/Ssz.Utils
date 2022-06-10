using Microsoft.Extensions.Logging;
using Ssz.Utils.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    public class CsvDb
    {
        #region construction and destruction

        /// <summary>
        ///     userFriendlyLogger: Messages are localized. Priority is Information, Error, Warning.
        ///     If csvDbDirectoryInfo is null, directory is not used.
        ///     If !csvDbDirectoryInfo.Exists, directory is created.
        ///     If dispatcher is not null monitors csvDbDirectoryInfo
        ///     File names must be valid.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="userFriendlyLogger"></param>
        /// <param name="csvDbDirectoryInfo"></param>
        /// <param name="dispatcher"></param>
        public CsvDb(ILogger<CsvDb> logger, IUserFriendlyLogger? userFriendlyLogger = null, DirectoryInfo? csvDbDirectoryInfo = null, IDispatcher? dispatcher = null)
        {
            _logger = logger;
            UserFriendlyLogger = userFriendlyLogger;
            _csvDbDirectoryInfo = csvDbDirectoryInfo;
            _dispatcher = dispatcher;

            if (_csvDbDirectoryInfo is not null)
            {
                try
                {
                    // Creates all directories and subdirectories in the specified path unless they already exist.
                    Directory.CreateDirectory(_csvDbDirectoryInfo.FullName);
                    if (!_csvDbDirectoryInfo.Exists)
                        _csvDbDirectoryInfo = null;
                }
                catch
                {
                    _csvDbDirectoryInfo = null;
                }
            }

            if (_csvDbDirectoryInfo is not null)
            {
                _logger.LogInformation("CsvDb Created for: " + _csvDbDirectoryInfo.FullName);
                if (_dispatcher is not null)
                    try
                    {
                        _fileSystemWatcher.Created += FileSystemWatcherOnEventAsync;
                        _fileSystemWatcher.Changed += FileSystemWatcherOnEventAsync;
                        _fileSystemWatcher.Deleted += FileSystemWatcherOnEventAsync;
                        _fileSystemWatcher.Renamed += FileSystemWatcherOnEventAsync;
                        _fileSystemWatcher.Path = _csvDbDirectoryInfo.FullName;
                        _fileSystemWatcher.Filter = @"*.csv";
                        _fileSystemWatcher.IncludeSubdirectories = false;
                        _fileSystemWatcher.EnableRaisingEvents = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "AppSettings FilesStore directory error. Please, specify correct directory and restart service.");
                    }
            }

            LoadData();
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Messages are localized. Priority is Information, Error, Warning.
        /// </summary>
        public IUserFriendlyLogger? UserFriendlyLogger { get; set; }

        /// <summary>
        ///     FileName in Upper-Case.
        /// </summary>
        public event Action<CsvFileChangeAction, string>? CsvFileChanged;

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
            if (_csvDbDirectoryInfo is null) return;

            var newCsvFilesCollection = new Dictionary<string, CsvFile>();

            foreach (FileInfo fileInfo in _csvDbDirectoryInfo.GetFiles(@"*", SearchOption.TopDirectoryOnly).Where(f => f.Name.EndsWith(@".csv",
                StringComparison.InvariantCultureIgnoreCase)))
            {
                string fileNameUpper = fileInfo.Name.ToUpperInvariant();
                _csvFilesCollection.TryGetValue(fileNameUpper, out CsvFile? csvFile);
                if (csvFile is null)
                {
                    csvFile = new CsvFile { 
                        FileFullName = fileInfo.FullName, 
                        LastWriteTimeUtc = fileInfo.LastWriteTimeUtc,
                        DataIsChangedOnDisk = true
                    };                    

                    foreach (CsvFile oldCsvFile in _csvFilesCollection.Values)
                    {
                        if (oldCsvFile.IncludeFileNamesCollection.Contains(fileNameUpper))
                            oldCsvFile.DataIsChangedOnDisk = true;
                    }
                }
                else if (Path.GetFileNameWithoutExtension(csvFile.FileFullName) != Path.GetFileNameWithoutExtension(fileInfo.FullName) || // Strict compare
                    !FileSystemHelper.FileSystemTimeIsEquals(csvFile.LastWriteTimeUtc, fileInfo.LastWriteTimeUtc))
                {
                    csvFile.FileFullName = fileInfo.FullName;
                    csvFile.LastWriteTimeUtc = fileInfo.LastWriteTimeUtc;
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
                        CsvFileChanged?.Invoke(CsvFileChangeAction.Removed, Path.GetFileName(kvp.Value.FileFullName));
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
                    csvFile.DataIsChangedOnDisk = false;
                    csvFile.DataIsChangedByProgram = false;

                    // Notify about changed files
                    if (csvFile.MovedToNewCollection)
                        CsvFileChanged?.Invoke(CsvFileChangeAction.Updated, Path.GetFileName(kvp.Value.FileFullName));
                    else
                        CsvFileChanged?.Invoke(CsvFileChangeAction.Added, Path.GetFileName(kvp.Value.FileFullName));
                }
                csvFile.MovedToNewCollection = false;
            }            

            _csvFilesCollection = newCsvFilesCollection;
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

            if (_csvDbDirectoryInfo is not null)
                csvFile.FileFullName = Path.Combine(_csvDbDirectoryInfo.FullName, fileName);
            csvFile.IncludeFileNamesCollection.Clear();
            csvFile.Data = new CaseInsensitiveDictionary<List<string?>>();
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
            csvFile.Data = new CaseInsensitiveDictionary<List<string?>>();
            csvFile.DataIsChangedByProgram = true;
            return true;
        }

        public bool DeleteValues(string? fileName, string key)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return false;

            if (!_csvFilesCollection.TryGetValue(fileName!.ToUpperInvariant(), out CsvFile? csvFile)) return false;

            if (csvFile.Data == null)
                csvFile.Data = CsvHelper.LoadCsvFile(csvFile.FileFullName, true, null, UserFriendlyLogger, csvFile.IncludeFileNamesCollection);
            bool removed = csvFile.Data.Remove(key);
            if (removed)
                csvFile.DataIsChangedByProgram = true;
            return removed;
        }        

        /// <summary>
        ///     Files names in Upper-Case.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetFileNames()
        {
            return _csvFilesCollection.Values.Select(cf => Path.GetFileName(cf.FileFullName));
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
            if (csvFile.Data == null)
                csvFile.Data = CsvHelper.LoadCsvFile(csvFile.FileFullName, true, null, UserFriendlyLogger, csvFile.IncludeFileNamesCollection);
            return csvFile.Data;
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
            if (csvFile.Data == null)
                csvFile.Data = CsvHelper.LoadCsvFile(csvFile.FileFullName, true, null, UserFriendlyLogger, csvFile.IncludeFileNamesCollection);
            if (csvFile.Data.Count == 0) return 1;
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
            
            if (!_csvFilesCollection.TryGetValue(fileName!.ToUpperInvariant(), out CsvFile? csvFile)) return null;
            if (csvFile.Data == null)
                csvFile.Data = CsvHelper.LoadCsvFile(csvFile.FileFullName, true, null, UserFriendlyLogger, csvFile.IncludeFileNamesCollection);            
            if (!csvFile.Data.TryGetValue(key, out List<string?>? fileLine)) return null;
            return fileLine;
        }

        public string? GetValue(string? fileName, string key, int column)
        {
            if (string.IsNullOrWhiteSpace(fileName) || column < 0) return null;
            
            if (!_csvFilesCollection.TryGetValue(fileName!.ToUpperInvariant(), out CsvFile? csvFile)) return null;            
            if (csvFile.Data == null)
                csvFile.Data = CsvHelper.LoadCsvFile(csvFile.FileFullName, true, null, UserFriendlyLogger, csvFile.IncludeFileNamesCollection);
            if (!csvFile.Data.TryGetValue(key, out List<string?>? fileLine)) return null;
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
                if (_csvDbDirectoryInfo is not null)
                    csvFile.FileFullName = Path.Combine(_csvDbDirectoryInfo.FullName, fileName);                
                csvFile.Data = new CaseInsensitiveDictionary<List<string?>>();
                _csvFilesCollection.Add(fileNameUpper, csvFile);
            }

            if (csvFile.Data == null)
                csvFile.Data = CsvHelper.LoadCsvFile(csvFile.FileFullName, true, null, UserFriendlyLogger, csvFile.IncludeFileNamesCollection);            
            if (!csvFile.Data.TryGetValue(key, out List<string?>? fileLine))
            {
                fileLine = new List<string?> { key };
                csvFile.Data.Add(key, fileLine);
            }

            if (column >= fileLine.Count)
            {
                fileLine.Capacity = column + 1;
                fileLine.AddRange(Enumerable.Repeat<string?>(null, column + 1 - fileLine.Count));
            }

            if (column > 0) fileLine[column] = value;

            csvFile.DataIsChangedByProgram = true;
        }

        public void SetValues(string? fileName, IEnumerable<string?> values)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return;

            string fileNameUpper = fileName!.ToUpperInvariant();

            if (!fileNameUpper.EndsWith(@".CSV")) return;

            if (!_csvFilesCollection.TryGetValue(fileNameUpper, out CsvFile? csvFile))
            {
                csvFile = new CsvFile();
                if (_csvDbDirectoryInfo is not null)
                    csvFile.FileFullName = Path.Combine(_csvDbDirectoryInfo.FullName, fileName);                
                csvFile.Data = new CaseInsensitiveDictionary<List<string?>>();
                _csvFilesCollection.Add(fileNameUpper, csvFile);
            }

            var enumerator = values.GetEnumerator();
            if (!enumerator.MoveNext()) return;
            string key = enumerator.Current ?? @"";

            if (csvFile.Data == null)
                csvFile.Data = CsvHelper.LoadCsvFile(csvFile.FileFullName, true, null, UserFriendlyLogger, csvFile.IncludeFileNamesCollection);            
            if (!csvFile.Data.TryGetValue(key, out List<string?>? fileLine))
            {
                fileLine = new List<string?> { key };
                csvFile.Data.Add(key, fileLine);
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
        ///     Saves changed data to .csv files on disk.
        /// </summary>
        public void SaveData()
        {
            if (_csvDbDirectoryInfo is null) return;

            _fileSystemWatcher.EnableRaisingEvents = false;

            foreach (var kvp in _csvFilesCollection)
            {
                CsvFile csvFile = kvp.Value;
                if (csvFile.DataIsChangedByProgram)
                {
                    try
                    {
                        var isNewCsvFile = csvFile.LastWriteTimeUtc == DateTime.MinValue;

                        // If the file to be deleted does not exist, no exception is thrown.
                        File.Delete(csvFile.FileFullName); // For 'a' to 'A' changes in files names to work.
                        using (var writer = new StreamWriter(csvFile.FileFullName, false, new UTF8Encoding(true)))
                        {
                            foreach (var fileLine in csvFile.Data!.OrderBy(kvp => kvp.Key))
                                writer.WriteLine(CsvHelper.FormatForCsv(",", fileLine.Value.ToArray()));
                        }                                                
                        csvFile.LastWriteTimeUtc = File.GetLastWriteTimeUtc(csvFile.FileFullName);

                        if (isNewCsvFile)
                            CsvFileChanged?.Invoke(CsvFileChangeAction.Added, Path.GetFileName(kvp.Value.FileFullName));
                        else
                            CsvFileChanged?.Invoke(CsvFileChangeAction.Updated, Path.GetFileName(kvp.Value.FileFullName));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, Properties.Resources.CsvDb_CsvFileWritingError + " " + csvFile.FileFullName);
                    }

                    csvFile.DataIsChangedByProgram = false;
                }                
            }

            var t = FileSystemWatcherEnableRaisingEventsAsync();
        }

        /// <summary>
        ///     Saves changed data to .csv file on disk.
        /// </summary>
        public void SaveData(string? fileName)
        {
            if (_csvDbDirectoryInfo is null) return;            

            if (string.IsNullOrWhiteSpace(fileName)) return;

            string fileNameUpper = fileName!.ToUpperInvariant();

            if (!_csvFilesCollection.TryGetValue(fileNameUpper, out CsvFile? csvFile)) return;            

            if (csvFile.DataIsChangedByProgram)
            {
                _fileSystemWatcher.EnableRaisingEvents = false;

                try
                {
                    var isNewCsvFile = csvFile.LastWriteTimeUtc == DateTime.MinValue;

                    // If the file to be deleted does not exist, no exception is thrown.
                    File.Delete(csvFile.FileFullName); // For 'a' to 'A' changes in files names to work.
                    using (var writer = new StreamWriter(csvFile.FileFullName, false, new UTF8Encoding(true)))
                    {
                        foreach (var fileLine in csvFile.Data!.OrderBy(kvp => kvp.Key))
                            writer.WriteLine(CsvHelper.FormatForCsv(",", fileLine.Value.ToArray()));
                    }
                    csvFile.LastWriteTimeUtc = File.GetLastWriteTimeUtc(csvFile.FileFullName);
                    
                    if (isNewCsvFile)
                        CsvFileChanged?.Invoke(CsvFileChangeAction.Added, fileNameUpper);
                    else
                        CsvFileChanged?.Invoke(CsvFileChangeAction.Updated, fileNameUpper);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, Properties.Resources.CsvDb_CsvFileWritingError + " " + csvFile.FileFullName);
                }

                csvFile.DataIsChangedByProgram = false;

                var t = FileSystemWatcherEnableRaisingEventsAsync();
            }
        }

        #endregion

        #region private functions

        private async void FileSystemWatcherOnEventAsync(object sender, FileSystemEventArgs e)
        {
            if (_fileSystemWatcherOnEventIsProcessing) return;
            _fileSystemWatcherOnEventIsProcessing = true;
            await Task.Delay(1000);
            _fileSystemWatcherOnEventIsProcessing = false;

            if (_dispatcher is not null)
            {
                _dispatcher.BeginInvoke(ct =>
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

        private ILogger _logger;        

        private DirectoryInfo? _csvDbDirectoryInfo;

        private IDispatcher? _dispatcher;

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
            public string FileFullName = @"";

            public DateTime LastWriteTimeUtc = DateTime.MinValue;

            public CaseInsensitiveDictionary<List<string?>>? Data;

            /// <summary>            
            ///     File names in Upper-Case
            /// </summary>
            public List<string> IncludeFileNamesCollection = new();

            public bool DataIsChangedByProgram;

            public bool DataIsChangedOnDisk;

            public bool MovedToNewCollection;
        }
    }

    public enum CsvFileChangeAction
    {        
        Added,
        Updated,
        Removed
    }
}
