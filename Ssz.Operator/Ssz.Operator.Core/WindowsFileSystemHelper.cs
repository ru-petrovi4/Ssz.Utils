using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Ssz.Operator.Core
{
    public static class WindowsFileSystemHelper
    {
        #region public functions

        [DllImport("shell32.dll", ExactSpelling = true)]
        internal static extern int SHOpenFolderAndSelectItems(
            IntPtr pidlFolder,
            uint cidl,
            [In, MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl,
            uint dwFlags);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr ILCreateFromPath([MarshalAs(UnmanagedType.LPTStr)] string pszPath);

        public static void OpenFolderInExplorerAndSelectFiles(string? folder, params string[] filesToSelect)
        {
            if (String.IsNullOrEmpty(folder))
                return;
            IntPtr dir = ILCreateFromPath(folder!);

            var filesToSelectIntPtrs = new IntPtr[filesToSelect.Length];
            for (int i = 0; i < filesToSelect.Length; i += 1)
            {
                filesToSelectIntPtrs[i] = ILCreateFromPath(filesToSelect[i]);
            }

            SHOpenFolderAndSelectItems(dir, (uint) filesToSelect.Length, filesToSelectIntPtrs, 0);
            ReleaseComObject(dir);
            ReleaseComObject(filesToSelectIntPtrs);
        }

        #endregion

        #region private functions

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