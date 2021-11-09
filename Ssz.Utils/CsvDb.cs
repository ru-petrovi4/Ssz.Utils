using Microsoft.Extensions.Logging;
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
        ///     If csvDbDirectoryInfo == null, directory is not used.
        ///     If !csvDbDirectoryInfo.Exists, directory is created.
        ///     If dispatcher != null monitors csvDbDirectoryInfo
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="userFriendlyLogger"></param>
        /// <param name="csvDbDirectoryInfo"></param>
        /// <param name="dispatcher"></param>
        public CsvDb(ILogger<CsvDb>? logger = null, ILogger? userFriendlyLogger = null, DirectoryInfo? csvDbDirectoryInfo = null, IDispatcher? dispatcher = null)
        {
            _logger = logger;
            UserFriendlyLogger = userFriendlyLogger;
            _csvDbDirectoryInfo = csvDbDirectoryInfo;
            _dispatcher = dispatcher;

            if (_csvDbDirectoryInfo != null)
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

            if (_csvDbDirectoryInfo != null)
            {
                if (_dispatcher != null)
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
        public ILogger? UserFriendlyLogger { get; set; }

        /// <summary>
        ///     FileName in Upper-Case.
        /// </summary>
        public event Action<string>? CsvFileChanged;

        public void Clear()
        {
            _csvFilesCollection.Clear();            
        }

        /// <summary>
        ///     Loads data from .csv files on disk.        
        ///     Data is loaded in constructor.
        ///     Data is loaded when directory changes, if dispatcher in consructor != null.
        /// </summary>
        public void LoadData()
        {
            if (_csvDbDirectoryInfo == null) return;

            var newCsvFilesCollection = new Dictionary<string, CsvFile>();

            foreach (FileInfo fileInfo in _csvDbDirectoryInfo.GetFiles(@"*.csv", SearchOption.TopDirectoryOnly))
            {
                string fileNameUpper = fileInfo.Name.ToUpperInvariant();
                _csvFilesCollection.TryGetValue(fileNameUpper, out CsvFile? csvFile);
                if (csvFile == null)
                {
                    csvFile = new CsvFile { FileFullName = fileInfo.FullName, LastWriteTimeUtc = fileInfo.LastWriteTimeUtc };
                    csvFile.DataIsChangedOnDisk = true;

                    foreach (CsvFile oldCsvFile in _csvFilesCollection.Values)
                    {
                        if (oldCsvFile.IncludeFileNamesCollection.Contains(fileNameUpper))
                            oldCsvFile.DataIsChangedOnDisk = true;
                    }

                    CsvFileChanged?.Invoke(fileNameUpper);
                }
                else if (csvFile.LastWriteTimeUtc != fileInfo.LastWriteTimeUtc) // Strict compare, because DateTimes from one source.
                {
                    csvFile.LastWriteTimeUtc = fileInfo.LastWriteTimeUtc;
                    csvFile.DataIsChangedOnDisk = true;

                    foreach (CsvFile oldCsvFile in _csvFilesCollection.Values)
                    {
                        if (oldCsvFile.IncludeFileNamesCollection.Contains(fileNameUpper))
                            oldCsvFile.DataIsChangedOnDisk = true;
                    }

                    CsvFileChanged?.Invoke(fileNameUpper);
                }
                newCsvFilesCollection.Add(fileNameUpper, csvFile);
                csvFile.AddedToNewCollection = true;
            }

            foreach (var kvp in _csvFilesCollection)
            {
                if (!kvp.Value.AddedToNewCollection)
                {
                    if (kvp.Value.DataIsChangedByProgram)
                    {
                        newCsvFilesCollection.Add(kvp.Key, kvp.Value);                        
                    }
                    else
                    {
                        CsvFileChanged?.Invoke(kvp.Key);
                    }                    
                }
            }

            foreach (CsvFile csvFile in newCsvFilesCollection.Values)
            {
                csvFile.AddedToNewCollection = false;
                if (csvFile.DataIsChangedOnDisk)
                {
                    try
                    {
                        csvFile.IncludeFileNamesCollection = new List<string>();
                        csvFile.Data = CsvHelper.LoadCsvFile(csvFile.FileFullName, true, null, UserFriendlyLogger, csvFile.IncludeFileNamesCollection);
                    }
                    catch (Exception ex)
                    {
                        if (UserFriendlyLogger != null)
                            UserFriendlyLogger.LogError(ex, Properties.Resources.CsvDb_CsvFileReadingError + " " + csvFile.FileFullName);
                    }
                    csvFile.DataIsChangedOnDisk = false;
                    csvFile.DataIsChangedByProgram = false;
                }
            }            

            _csvFilesCollection = newCsvFilesCollection;
        }        

        public bool FileExists(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return false;

            return _csvFilesCollection.ContainsKey(fileName.ToUpperInvariant());
        }

        public bool FileClear(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return false;
            
            if (!_csvFilesCollection.TryGetValue(fileName.ToUpperInvariant(), out CsvFile? csvFile)) return false;
            csvFile.Data.Clear();
            csvFile.DataIsChangedByProgram = true;
            return true;
        }

        public bool DeleteValues(string? fileName, string key)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return false;

            if (!_csvFilesCollection.TryGetValue(fileName.ToUpperInvariant(), out CsvFile? csvFile)) return false;
                  
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
            return _csvFilesCollection.Keys;
        }

        /// <summary>
        ///     List.Count >= 1, List[0] != null
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public CaseInsensitiveDictionary<List<string?>> GetValues(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return new CaseInsensitiveDictionary<List<string?>>();
            
            if (!_csvFilesCollection.TryGetValue(fileName.ToUpperInvariant(), out CsvFile? csvFile)) return new CaseInsensitiveDictionary<List<string?>>();
            return csvFile.Data;
        }

        /// <summary>
        ///     null or List.Count >= 1, List[0] != null
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public List<string?>? GetValues(string? fileName, string key)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return null;
            
            if (!_csvFilesCollection.TryGetValue(fileName.ToUpperInvariant(), out CsvFile? csvFile)) return null;
            List<string?>? fileLine;
            if (!csvFile.Data.TryGetValue(key, out fileLine)) return null;
            return fileLine;
        }

        public string? GetValue(string? fileName, string key, int column)
        {
            if (string.IsNullOrWhiteSpace(fileName) || column < 0) return null;
            
            if (!_csvFilesCollection.TryGetValue(fileName.ToUpperInvariant(), out CsvFile? csvFile)) return null;
            List<string?>? fileLine;
            if (!csvFile.Data.TryGetValue(key, out fileLine)) return null;
            if (column >= fileLine.Count) return null;
            return fileLine[column];
        }

        public void SetValue(string? fileName, string key, int column, string? value)
        {
            if (string.IsNullOrWhiteSpace(fileName) || column < 0) return;

            string fileNameUpper = fileName.ToUpperInvariant();

            if (!fileNameUpper.EndsWith(@".CSV")) return;
            
            if (!_csvFilesCollection.TryGetValue(fileNameUpper, out CsvFile? csvFile))
            {
                csvFile = new CsvFile();                
                if (_csvDbDirectoryInfo != null)
                    csvFile.FileFullName = Path.Combine(_csvDbDirectoryInfo.FullName, fileName);
                csvFile.IncludeFileNamesCollection = new List<string>();
                csvFile.Data = new CaseInsensitiveDictionary<List<string?>>();
                _csvFilesCollection.Add(fileNameUpper, csvFile);
            }

            List<string?>? fileLine;
            if (!csvFile.Data.TryGetValue(key, out fileLine))
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

            string fileNameUpper = fileName.ToUpperInvariant();

            if (!fileNameUpper.EndsWith(@".CSV")) return;

            if (!_csvFilesCollection.TryGetValue(fileNameUpper, out CsvFile? csvFile))
            {
                csvFile = new CsvFile();
                if (_csvDbDirectoryInfo != null)
                    csvFile.FileFullName = Path.Combine(_csvDbDirectoryInfo.FullName, fileName);
                csvFile.IncludeFileNamesCollection = new List<string>();
                csvFile.Data = new CaseInsensitiveDictionary<List<string?>>();
                _csvFilesCollection.Add(fileNameUpper, csvFile);
            }

            var enumerator = values.GetEnumerator();
            if (!enumerator.MoveNext()) return;
            string key = enumerator.Current ?? @"";

            List<string?>? fileLine;
            if (!csvFile.Data.TryGetValue(key, out fileLine))
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
            if (_csvDbDirectoryInfo == null) return;

            _fileSystemWatcher.EnableRaisingEvents = false;

            foreach (var kvp in _csvFilesCollection)
            {
                CsvFile csvFile = kvp.Value;
                if (csvFile.DataIsChangedByProgram)
                {
                    try
                    {
                        using (var writer = new StreamWriter(csvFile.FileFullName, false, new UTF8Encoding(true)))
                        {
                            foreach (var fileLine in csvFile.Data.OrderBy(kvp => kvp.Key))
                                writer.WriteLine(CsvHelper.FormatForCsv(",", fileLine.Value.ToArray()));
                        }
                        csvFile.LastWriteTimeUtc = File.GetLastWriteTimeUtc(csvFile.FileFullName);
                        CsvFileChanged?.Invoke(kvp.Key);
                    }
                    catch (Exception ex)
                    {
                        if (_logger != null)
                            _logger.LogError(ex, Properties.Resources.CsvDb_CsvFileWritingError + " " + csvFile.FileFullName);
                    }

                    csvFile.DataIsChangedByProgram = false;
                }                
            }

            _fileSystemWatcher.EnableRaisingEvents = true;
        }

        #endregion

        #region private functions

        private async void FileSystemWatcherOnEventAsync(object sender, FileSystemEventArgs e)
        {
            if (_fileSystemWatcherOnEventIsProcessing) return;
            _fileSystemWatcherOnEventIsProcessing = true;
            await Task.Delay(1000);
            _fileSystemWatcherOnEventIsProcessing = false;

            _dispatcher!.BeginInvoke((Action<System.Threading.CancellationToken>)(ct =>
            {
                this.LoadData();
            }));
        }

        #endregion

        #region private fields        

        private ILogger<CsvDb>? _logger;        

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

            public DateTime LastWriteTimeUtc;

            public CaseInsensitiveDictionary<List<string?>> Data = null!;

            /// <summary>
            ///     File names in Upper-Case
            /// </summary>
            public List<string> IncludeFileNamesCollection = null!;

            public bool DataIsChangedByProgram;

            public bool DataIsChangedOnDisk;

            public bool AddedToNewCollection;
        }
    }
}
