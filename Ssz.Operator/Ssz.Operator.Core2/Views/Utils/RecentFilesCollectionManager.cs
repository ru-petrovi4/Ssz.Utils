using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Ssz.Operator.Core.ViewModels;

namespace Ssz.Operator.Core.Utils
{
    public class RecentFile
    {
        #region construction and destruction

        public RecentFile(string fullFileName, string fileNameToDisplay)
        {
            FullFileName = fullFileName;
            FileNameToDisplay = fileNameToDisplay;
        }

        #endregion

        #region public functions

        public string FullFileName { get; }

        public string FileNameToDisplay { get; }

        #endregion
    }

    /// <summary>
    ///     The projects the author opened last, newest first.
    ///     <para>
    ///         Ported from Ssz.Utils.Wpf. That version kept the list in the Windows registry and
    ///         shortened long paths with a shlwapi call; neither exists away from Windows, so the list
    ///         lives in a file beside the other settings of the application and the shortening is done
    ///         here. Where no such file can be written - the browser - the list simply lasts as long as
    ///         the session.
    ///     </para>
    /// </summary>
    public class RecentFilesCollectionManager : DisposableViewModelBase
    {
        #region construction and destruction

        /// <param name="settingsPath">
        ///     What the WPF version used as a registry path. It names the file the list is kept in, so
        ///     that two applications do not share one list.
        /// </param>
        public RecentFilesCollectionManager(string settingsPath, int maxNumberOfFiles, int maxDisplayLength,
            string? currentDirectory)
        {
            RecentFilesCollection = new ObservableCollection<RecentFile>();

            _settingsFileFullName = GetSettingsFileFullName(settingsPath);

            _maxNumberOfFiles = maxNumberOfFiles;

            _maxDisplayLength = maxDisplayLength;
            if (_maxDisplayLength < 10)
                _maxDisplayLength = 10;

            _currentDirectory = currentDirectory;

            LoadRecent();
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed) return;

            if (disposing) SaveRecent();

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public ObservableCollection<RecentFile> RecentFilesCollection { get; }

        public int MaxDisplayNameLength => _maxDisplayLength;

        public int MaxNumberOfFiles => _maxNumberOfFiles;

        public string? CurrentDirectory => _currentDirectory;

        /// <summary>
        ///     Adds a file to the list, or moves it to the front when it is already there.
        /// </summary>
        public void Add(string fullFileName)
        {
            if (String.IsNullOrWhiteSpace(fullFileName)) return;

            Remove(fullFileName);

            if (RecentFilesCollection.Count == _maxNumberOfFiles)
                RecentFilesCollection.RemoveAt(_maxNumberOfFiles - 1);

            RecentFilesCollection.Insert(0, new RecentFile(fullFileName, GetDisplayName(fullFileName)));

            SaveRecent();
        }

        /// <summary>
        ///     Drops a file from the list, which is what an editor does when opening it failed.
        /// </summary>
        public void Remove(string fullFileName)
        {
            for (var i = 0; i < RecentFilesCollection.Count; i += 1)
                if (String.Equals(RecentFilesCollection[i].FullFileName, fullFileName,
                        StringComparison.InvariantCultureIgnoreCase))
                {
                    RecentFilesCollection.RemoveAt(i);
                    SaveRecent();
                    return;
                }
        }

        public void ClearList()
        {
            RecentFilesCollection.Clear();

            SaveRecent();
        }

        /// <summary>
        ///     Shortens a path to fit, keeping the file name and the start of the path and putting an
        ///     ellipsis between them.
        /// </summary>
        public static string GetShortDisplayName(string longName, int maxLen)
        {
            if (longName.Length <= maxLen) return longName;

            var fileName = Path.GetFileName(longName);
            if (fileName.Length + 4 >= maxLen)
                return fileName.Length <= maxLen ? fileName : @"..." + fileName.Substring(fileName.Length - maxLen + 3);

            var headLength = maxLen - fileName.Length - 4;
            return longName.Substring(0, headLength) + @"...\" + fileName;
        }

        #endregion

        #region private functions

        private static string? GetSettingsFileFullName(string settingsPath)
        {
            try
            {
                var applicationDataPath =
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (String.IsNullOrEmpty(applicationDataPath)) return null;

                var directory = Path.Combine(applicationDataPath,
                    settingsPath.Replace('\\', Path.DirectorySeparatorChar).Trim(Path.DirectorySeparatorChar));
                return Path.Combine(directory, @"RecentFiles.txt");
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string GetDisplayName(string fullName)
        {
            try
            {
                var fileInfo = new FileInfo(fullName);

                if (fileInfo.DirectoryName == _currentDirectory)
                    return GetShortDisplayName(fileInfo.Name, _maxDisplayLength);
            }
            catch (Exception)
            {
            }

            return GetShortDisplayName(fullName, _maxDisplayLength);
        }

        private void LoadRecent()
        {
            RecentFilesCollection.Clear();

            if (_settingsFileFullName is null) return;

            try
            {
                if (!File.Exists(_settingsFileFullName)) return;

                foreach (var line in File.ReadAllLines(_settingsFileFullName)
                             .Where(l => !String.IsNullOrWhiteSpace(l))
                             .Take(_maxNumberOfFiles))
                    RecentFilesCollection.Add(new RecentFile(line, GetDisplayName(line)));
            }
            catch (Exception)
            {
                // A list of recently opened files is not worth failing a start over.
            }
        }

        private void SaveRecent()
        {
            if (_settingsFileFullName is null) return;

            try
            {
                var directory = Path.GetDirectoryName(_settingsFileFullName);
                if (!String.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

                File.WriteAllLines(_settingsFileFullName,
                    RecentFilesCollection.Select(rf => rf.FullFileName));
            }
            catch (Exception)
            {
            }
        }

        #endregion

        #region private fields

        private readonly string? _settingsFileFullName;
        private readonly int _maxNumberOfFiles;
        private readonly int _maxDisplayLength;
        private readonly string? _currentDirectory;

        #endregion
    }
}
