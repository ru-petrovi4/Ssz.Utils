using System;
using System.Diagnostics;
using System.IO;
using Ssz.Operator.Core.Drawings;
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

        public static void PrepareCloseDsProjectDirectory(this DsProject dsProject)
        {
            try
            {
                var projectFileInfo = new FileInfo(dsProject.DsProjectFileFullName!);                
                if (String.Equals(projectFileInfo.Extension, DsProject.DsProjectFileExtension, StringComparison.InvariantCultureIgnoreCase) &&
                        projectFileInfo.Extension != DsProject.DsProjectFileExtension)
                {
                    string fixedProjectFileFullName =
                            Path.Combine(
                                dsProject.DsProjectPath,
                                Path.GetFileNameWithoutExtension(dsProject.DsProjectFileFullName!) + DsProject.DsProjectFileExtension);
                    File.Move(projectFileInfo.FullName, fixedProjectFileFullName + "_");
                    File.Move(fixedProjectFileFullName + "_", fixedProjectFileFullName);
                }

                foreach (FileInfo fi in dsProject.DsPagesDirectoryInfo!.GetFiles(@"*", SearchOption.TopDirectoryOnly))
                {
                    if (String.Equals(fi.Extension, DsProject.DsPageFileExtension, StringComparison.InvariantCultureIgnoreCase) &&
                            fi.Extension != DsProject.DsPageFileExtension)
                    {
                        string directoryFullName = DrawingBase.GetDrawingFilesDirectoryFullName(fi.FullName);
                        string fixedFileFullName =
                            Path.Combine(
                                Path.GetDirectoryName(fi.FullName)!,
                                Path.GetFileNameWithoutExtension(fi.FullName) + DsProject.DsPageFileExtension);
                        string fixedDirectoryFullName = DrawingBase.GetDrawingFilesDirectoryFullName(fixedFileFullName);
                        File.Move(fi.FullName, fixedFileFullName + "_");                        
                        File.Move(fixedFileFullName + "_", fixedFileFullName);
                        if (Directory.Exists(directoryFullName))
                        {
                            Directory.Move(directoryFullName, fixedDirectoryFullName + "_");
                            Directory.Move(fixedDirectoryFullName + "_", fixedDirectoryFullName);
                        }
                    }
                }

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