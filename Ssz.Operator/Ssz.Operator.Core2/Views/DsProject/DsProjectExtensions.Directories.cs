using System;
using System.Diagnostics;
using Ssz.Operator.Core.Utils;

namespace Ssz.Operator.Core
{
    public static partial class DsProjectExtensions
    {
        #region public functions

        public static void TryToLockDsProjectDirectory(this DsProject dsProject)
        {
            if (!dsProject.IsInitialized) return;

            try
            {
                /*
                var currentUserIdentity = WindowsIdentity.GetCurrent();

                var dsProjectDirectoryInfo = new DirectoryInfo(dsProject.DsProjectPath);
                DirectorySecurity dsProjectDirectorySecurity = dsProjectDirectoryInfo.GetAccessControl();
                dsProjectDirectorySecurity.AddAccessRule(new FileSystemAccessRule(currentUserIdentity.Name,
                    FileSystemRights.Delete, AccessControlType.Deny));
                dsProjectDirectoryInfo.SetAccessControl(dsProjectDirectorySecurity);
                */
            }
            catch (Exception)
            {
            }
        }

        public static void TryToUnlockDsProjectDirectory(this DsProject dsProject)
        {
            try
            {
                /*
                var currentUserIdentity = WindowsIdentity.GetCurrent();

                var dsProjectDirectoryInfo = new DirectoryInfo(dsProject.DsProjectPath);
                DirectorySecurity dsProjectDirectorySecurity = dsProjectDirectoryInfo.GetAccessControl();
                dsProjectDirectorySecurity.RemoveAccessRule(new FileSystemAccessRule(currentUserIdentity.Name,
                    FileSystemRights.Delete, AccessControlType.Deny));
                dsProjectDirectoryInfo.SetAccessControl(dsProjectDirectorySecurity);
                */
            }
            catch (Exception)
            {
            }
        }

        public static void ShowDsProjectDirectoryInExplorer(this DsProject dsProject)
        {
            if (!dsProject.IsInitialized) return;

            try
            {
                Process.Start("explorer.exe",
                    string.Format("/select,\"{0}\"", dsProject.DsProjectFileFullName));
            }
            catch (Exception)
            {
            }
        }

        public static void ShowLogFilesDirectoryInExplorer(this DsProject dsProject)
        { 
            //try
            //{
            //    Process.Start("explorer.exe",
            //        string.Format("/select,\"{0}\"", LogFileName));
            //}
            //catch (Exception)
            //{
            //}
        }

        public static void ShowDebugWindow(this DsProject dsProject)
        {
            DebugWindow.Instance.Activate();
        }

        #endregion
    }
}