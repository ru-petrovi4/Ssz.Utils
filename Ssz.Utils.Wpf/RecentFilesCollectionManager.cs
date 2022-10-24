using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Ssz.Utils;
using Ssz.Utils.Wpf;
using Microsoft.Win32;

namespace Ssz.Utils.Wpf
{
    public class RecentFile
    {
        #region construction and destruction

        public RecentFile(string fileName, string fileNameToDisplay)
        {
            FullFileName = fileName;
            FileNameToDisplay = fileNameToDisplay;
        }

        #endregion

        #region public functions

        public string FullFileName { get; private set; }
        public string FileNameToDisplay { get; private set; }

        #endregion
    }

    /// <summary>
    ///     Recent manager - manages Most Recently Used Files list
    ///     for Windows Window application.
    /// </summary>
    public class RecentFilesCollectionManager : DisposableViewModelBase
    {
        #region construction and destruction

        public RecentFilesCollectionManager(string registryPath, int maxNumberOfFiles, int maxDisplayLength,
            string? currentDirectory)
        {
            // keep reference to owner window
            RecentFilesCollection = new ObservableCollection<RecentFile>();

            // keep Registry path adding Recent key to it
            _registryPath = registryPath;
            if (_registryPath.EndsWith("\\"))
                _registryPath += "Recent";
            else
                _registryPath += "\\Recent";

            _maxNumberOfFiles = maxNumberOfFiles;

            _maxDisplayLength = maxDisplayLength;
            if (_maxDisplayLength < 10)
                _maxDisplayLength = 10;

            // keep current directory in the time of initialization
            _currentDirectory = currentDirectory;

            // load Recent list from Registry
            LoadRecent();
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed) return;
            
            if (disposing)
            {
                try
                {
                    RegistryKey key = Registry.CurrentUser.CreateSubKey(_registryPath);

                    if (key is not null)
                    {
                        int n = RecentFilesCollection.Count;

                        int i;
                        for (i = 0; i < _maxNumberOfFiles; i++)
                        {
                            key.DeleteValue(RegEntryName + i.ToString(CultureInfo.InvariantCulture), false);
                        }

                        for (i = 0; i < n; i++)
                        {
                            key.SetValue(RegEntryName + i.ToString(CultureInfo.InvariantCulture),
                                RecentFilesCollection[i].FullFileName);
                        }
                    }
                }
                catch
                {
                    //Logger.Error("Saving Recent to Registry failed: " + ex.Message);
                }
            }

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Add file name to Recent list.
        ///     Call this function when file is opened successfully.
        ///     If file already exists in the list, it is moved to the first place.
        /// </summary>
        /// <param name="fullFileName">File Name</param>
        public void Add(string fullFileName)
        {
            if (String.IsNullOrWhiteSpace(fullFileName)) return;

            Remove(fullFileName);

            // if array has maximum length, remove last element
            if (RecentFilesCollection.Count == _maxNumberOfFiles)
                RecentFilesCollection.RemoveAt(_maxNumberOfFiles - 1);

            // add new file name to the start of array
            RecentFilesCollection.Insert(0, new RecentFile(fullFileName, GetDisplayName(fullFileName)));
        }

        /// <summary>
        ///     Remove file name from Recent list.
        ///     Call this function when File - Open operation failed.
        /// </summary>
        /// <param name="fullFileName">File Name</param>
        public void Remove(string fullFileName)
        {
            int i = 0;

            IEnumerator<RecentFile> myEnumerator = RecentFilesCollection.GetEnumerator();

            while (myEnumerator.MoveNext())
            {
                if (String.Equals(myEnumerator.Current.FullFileName, fullFileName, StringComparison.InvariantCultureIgnoreCase))
                {
                    RecentFilesCollection.RemoveAt(i);
                    return;
                }

                i++;
            }
        }

        public ObservableCollection<RecentFile> RecentFilesCollection { get; private set; }

        /// <summary>
        ///     Maximum length of displayed file name in menu (default is 40).
        ///     Set this property to change default value (optional).
        /// </summary>
        public int MaxDisplayNameLength
        {
            get { return _maxDisplayLength; }
        }

        /// <summary>
        ///     Maximum length of Recent list (default is 10).
        ///     Set this property to change default value (optional).
        /// </summary>
        public int MaxNumberOfFiles
        {
            get { return _maxNumberOfFiles; }
        }

        /// <summary>
        ///     Set current directory.
        ///     Default value is program current directory which is set when
        ///     Initialize function is called.
        ///     Set this property to change default value (optional)
        ///     after call to Initialize.
        /// </summary>
        public string? CurrentDirectory
        {
            get { return _currentDirectory; }
        }

        public void ClearList()
        {
            RecentFilesCollection.Clear();

            try
            {
                RegistryKey key = Registry.CurrentUser.CreateSubKey(_registryPath);

                if (key is not null)
                {
                    int n = RecentFilesCollection.Count;

                    int i;
                    for (i = 0; i < _maxNumberOfFiles; i++)
                    {
                        key.DeleteValue(RegEntryName + i.ToString(CultureInfo.InvariantCulture), false);
                    }
                }
            }
            catch
            {
                //Logger.Error("Saving Recent to Registry failed: " + ex.Message);
            }
        }

        /// <summary>
        ///     Truncate a path to fit within a certain number of characters
        ///     by replacing path components with ellipses.
        ///     This solution is provided by CodeProject and GotDotNet C# expert
        ///     Richard Deeming.
        /// </summary>
        /// <param name="longName">Long file name</param>
        /// <param name="maxLen">Maximum length</param>
        /// <returns>Truncated file name</returns>
        public static string GetShortDisplayName(string longName, int maxLen)
        {
            var pszOut = new StringBuilder(maxLen + maxLen + 2); // for safety

            if (PathCompactPathEx(pszOut, longName, maxLen, 0))
            {
                return pszOut.ToString();
            }
            return longName;
        }



        #endregion

        #region private functions

        [DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
        private static extern bool PathCompactPathEx(
            StringBuilder pszOut,
            string pszPath,
            int cchMax,
            int reserved);

        /// <summary>
        ///     Get display file name from full name.
        /// </summary>
        /// <param name="fullName">Full file name</param>
        /// <returns>Short display name</returns>
        private string GetDisplayName(string fullName)
        {
            // if file is in current directory, show only file name
            var fileInfo = new FileInfo(fullName);

            if (fileInfo.DirectoryName == _currentDirectory)
                return GetShortDisplayName(fileInfo.Name, _maxDisplayLength);

            return GetShortDisplayName(fullName, _maxDisplayLength);
        }

        /// <summary>
        ///     Load Recent list from Registry.
        ///     Called from Initialize.
        /// </summary>
        private void LoadRecent()
        {
            try
            {
                RecentFilesCollection.Clear();

                RegistryKey? key = Registry.CurrentUser.OpenSubKey(_registryPath);

                if (key is not null)
                {
                    for (int i = 0; i < _maxNumberOfFiles; i++)
                    {
                        string sKey = RegEntryName + i.ToString(CultureInfo.InvariantCulture);

                        var s = key.GetValue(sKey, "") as string;

                        if (s is null || s.Length == 0)
                            break;

                        RecentFilesCollection.Add(new RecentFile(s, GetDisplayName(s)));
                    }
                }
            }
            catch
            {
                //Logger.Error("Loading Recent from Registry failed: " + ex.Message);
            }
        }

        #endregion

        #region private fields

        private readonly string _registryPath; // Registry path to keep Recent list

        private readonly int _maxNumberOfFiles; // maximum number of files in Recent list

        private readonly int _maxDisplayLength; // maximum length of file name for display

        private readonly string? _currentDirectory; // current directory

        private const string RegEntryName = "file"; // entry name to keep Recent (file0, file1...)

        #endregion
    }
}