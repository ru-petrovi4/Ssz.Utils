using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Ssz.Operator.Core
{
    /// <summary>
    ///     Showing the author where a file is on their machine.
    ///     <para>
    ///         Ported from Ssz.Utils.Wpf's WindowsExplorerHelper, which called shell32 to open Explorer
    ///         with the files selected. That is what happens on Windows here too; on the other desktops
    ///         the file manager is asked to open the directory, which is as close as they get, and in
    ///         the browser there is nothing to open.
    ///     </para>
    /// </summary>
    public static class FileLocationHelper
    {
        #region public functions

        public static void OpenFolderAndSelectFiles(string? folder, params string[] filesToSelect)
        {
            if (String.IsNullOrEmpty(folder)) return;

            try
            {
                if (OperatingSystem.IsWindows())
                {
                    OpenInExplorerAndSelect(folder, filesToSelect);
                    return;
                }

                if (OperatingSystem.IsBrowser()) return;

                OpenDirectory(folder);
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogWarning(ex, @"Showing the files in the file manager failed.");
            }
        }

        #endregion

        #region private functions

        private static void OpenInExplorerAndSelect(string folder, string[] filesToSelect)
        {
            IntPtr dir = ILCreateFromPath(folder);

            var filesToSelectIntPtrs = new IntPtr[filesToSelect.Length];
            for (var i = 0; i < filesToSelect.Length; i++)
                filesToSelectIntPtrs[i] = ILCreateFromPath(filesToSelect[i]);

            SHOpenFolderAndSelectItems(dir, (uint) filesToSelect.Length, filesToSelectIntPtrs, 0);

            ILFree(dir);
            foreach (IntPtr pidl in filesToSelectIntPtrs) ILFree(pidl);
        }

        private static void OpenDirectory(string folder)
        {
            var fileName = OperatingSystem.IsMacOS() ? @"open" : @"xdg-open";
            using var process = new Process();
            process.StartInfo.FileName = fileName;
            process.StartInfo.ArgumentList.Add(folder);
            process.StartInfo.UseShellExecute = false;
            process.Start();
        }

        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern int SHOpenFolderAndSelectItems(
            IntPtr pidlFolder,
            uint cidl,
            [In] [MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl,
            uint dwFlags);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr ILCreateFromPath([MarshalAs(UnmanagedType.LPTStr)] string pszPath);

        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern void ILFree(IntPtr pidl);

        #endregion
    }
}
