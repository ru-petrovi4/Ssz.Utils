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

        public CsvDb(ILogger<CsvDb>? logger = null, DirectoryInfo? csvDbDirectoryInfo = null)
        {
            _logger = logger;
            _csvDbDirectoryInfo = csvDbDirectoryInfo;
        }

        #endregion

        #region public functions

        public void Clear()
        {
            _csvDbData.Clear();
            _changedFileNames.Clear();
        }

        /// <summary>
        ///     Loads data from .csv files on disk.
        ///     resultWarnings are localized.
        /// </summary>
        /// <param name="resultWarnings"></param>
        public void LoadData(ILogger? loadLogger = null)
        {
            Clear();

            if (_csvDbDirectoryInfo == null || !_csvDbDirectoryInfo.Exists) return;

            var logger = loadLogger ?? _logger;

            foreach (FileInfo file in _csvDbDirectoryInfo.GetFiles(@"*.csv", SearchOption.TopDirectoryOnly))
                try
                {
                    string fileName = file.Name;
                    _csvDbData[fileName] = CsvHelper.LoadCsvFile(file.FullName, true, null, logger);
                }
                catch (Exception ex)
                {                   
                    if (logger != null)
                        logger.LogError(ex, Properties.Resources.CsvDb_CsvFileReadingError + " " + file.FullName);                    
                }
        }


        public bool FileExists(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return false;

            return _csvDbData.ContainsKey(fileName);
        }


        public bool FileClear(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return false;

            CaseInsensitiveDictionary<List<string?>>? fileData;
            if (!_csvDbData.TryGetValue(fileName, out fileData)) return false;
            fileData.Clear();
            return true;
        }


        public IEnumerable<string> GetFileNames()
        {
            return _csvDbData.Keys;
        }


        public CaseInsensitiveDictionary<List<string?>> GetValues(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return new CaseInsensitiveDictionary<List<string?>>();

            CaseInsensitiveDictionary<List<string?>>? fileData;
            if (!_csvDbData.TryGetValue(fileName, out fileData)) return new CaseInsensitiveDictionary<List<string?>>();
            return fileData;
        }


        public List<string?> GetValues(string? fileName, string key)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return new List<string?>();

            CaseInsensitiveDictionary<List<string?>>? fileData;
            if (!_csvDbData.TryGetValue(fileName, out fileData)) return new List<string?>();
            List<string?>? fileLine;
            if (!fileData.TryGetValue(key, out fileLine)) return new List<string?>();
            return fileLine;
        }


        public string? GetValue(string? fileName, string key, int column)
        {
            if (string.IsNullOrWhiteSpace(fileName) || column < 0) return null;

            CaseInsensitiveDictionary<List<string?>>? fileData;
            if (!_csvDbData.TryGetValue(fileName, out fileData)) return null;
            List<string?>? fileLine;
            if (!fileData.TryGetValue(key, out fileLine)) return null;
            if (column >= fileLine.Count) return null;
            return fileLine[column];
        }


        public void SetValue(string? fileName, string key, int column, string? value)
        {
            if (string.IsNullOrWhiteSpace(fileName) || 
                !fileName.EndsWith(".csv", StringComparison.InvariantCultureIgnoreCase) || column < 0) return;

            CaseInsensitiveDictionary<List<string?>>? fileData;
            if (!_csvDbData.TryGetValue(fileName, out fileData))
            {
                fileData = new CaseInsensitiveDictionary<List<string?>>();
                _csvDbData[fileName] = fileData;
            }

            List<string?>? fileLine;
            if (!fileData.TryGetValue(key, out fileLine))
            {
                fileLine = new List<string?> { key };
                fileData[key] = fileLine;
            }

            if (column >= fileLine.Count)
            {
                fileLine.Capacity = column + 1;
                fileLine.AddRange(Enumerable.Repeat<string?>(null, column + 1 - fileLine.Count));
            }

            if (column > 0) fileLine[column] = value;

            _changedFileNames.Add(fileName);
        }

        /// <summary>
        ///     Saves data to .csv files on disk.
        /// </summary>
        public void SaveData()
        {
            if (_csvDbDirectoryInfo == null) return;

            if (!_csvDbDirectoryInfo.Exists) _csvDbDirectoryInfo.Create();

            foreach (string fileName in _changedFileNames)
            {
                string fileFullName = _csvDbDirectoryInfo.FullName + @"\" + fileName;
                try
                {
                    using (var writer = new StreamWriter(fileFullName, false, new UTF8Encoding(true)))
                    {
                        foreach (var fileLine in _csvDbData[fileName].OrderBy(kvp => kvp.Key))
                            writer.WriteLine(CsvHelper.FormatForCsv(",", fileLine.Value.ToArray()));
                    }
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                        _logger.LogError(ex, Properties.Resources.CsvDb_CsvFileWritingError + " " + fileFullName);                   
                }
            }

            _changedFileNames.Clear();
        }

        #endregion

        #region private fields

        private ILogger<CsvDb>? _logger;
        private DirectoryInfo? _csvDbDirectoryInfo;

        /// <summary>
        ///     File names end with .csv
        /// </summary>
        private readonly CaseInsensitiveDictionary<CaseInsensitiveDictionary<List<string?>>> _csvDbData =
            new();

        /// <summary>
        ///     File names end with .csv
        /// </summary>
        private readonly HashSet<string> _changedFileNames = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

        #endregion
    }
}
