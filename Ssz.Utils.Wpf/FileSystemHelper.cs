using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Ssz.Utils.Wpf
{
    public static class FileSystemHelper
    {
        #region public functions

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool Compare(FileSystemInfo? left, FileSystemInfo? right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (ReferenceEquals(null, right)) return false;
            if (ReferenceEquals(null, left)) return false;
            return String.Equals(left.FullName, right.FullName, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="searchOption"></param>
        /// <param name="extensions"></param>
        /// <returns></returns>
        public static IEnumerable<FileInfo> GetFilesByExtensions(this DirectoryInfo dir, SearchOption searchOption,
            params string[] extensions)
        {            
            IEnumerable<FileInfo> files = dir.EnumerateFiles("*", searchOption);
            return files.Where(f => extensions.Contains(f.Extension, StringComparer.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dirPath"></param>
        /// <param name="throwIfFails"></param>
        /// <returns></returns>
        public static bool IsDirectoryWritable(string dirPath, bool throwIfFails = false)
        {
            try
            {
                using (FileStream fs = File.Create(
                    Path.Combine(
                        dirPath,
                        Path.GetRandomFileName()
                        ),
                    1,
                    FileOptions.DeleteOnClose)
                    )
                {
                }
                return true;
            }
            catch
            {
                if (throwIfFails)
                    throw;
                else
                    return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="filesToSelect"></param>
        public static void OpenFolderInExplorerAndSelectFiles(string folder, params string[] filesToSelect)
        {
            IntPtr dir = ILCreateFromPath(folder);

            var filesToSelectIntPtrs = new IntPtr[filesToSelect.Length];
            for (int i = 0; i < filesToSelect.Length; i++)
            {
                filesToSelectIntPtrs[i] = ILCreateFromPath(filesToSelect[i]);
            }

            SHOpenFolderAndSelectItems(dir, (uint) filesToSelect.Length, filesToSelectIntPtrs, 0);
            ReleaseComObject(dir);
            ReleaseComObject(filesToSelectIntPtrs);
        }

        #endregion

        #region private functions

        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern int SHOpenFolderAndSelectItems(
            IntPtr pidlFolder,
            uint cidl,
            [In, MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl,
            uint dwFlags);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr ILCreateFromPath([MarshalAs(UnmanagedType.LPTStr)] string pszPath);

        private static void ReleaseComObject(params object[] comObjs)
        {
            foreach (object obj in comObjs)
            {
                if (obj != null && Marshal.IsComObject(obj))
                    Marshal.ReleaseComObject(obj);
            }
        }

        #endregion
    }
}