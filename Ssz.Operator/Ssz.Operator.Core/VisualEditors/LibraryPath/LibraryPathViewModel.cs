using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Utils;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.VisualEditors.LibraryPath
{
    internal class LibraryPathViewModel : DisposableViewModelBase
    {
        #region construction and destruction

        public LibraryPathViewModel()
        {
            RecentFilesCollectionManager =
                new RecentFilesCollectionManager(
                    AppRegistryOptions.SimcodeSszOperatorSubKeyString + @"\" +
                    AppRegistryOptions.DsPagesAndDsShapesLibrariesSubKeyString, 10, 150, "");

            RecentFilesCollectionManager.Add(LocalLibraryString);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) RecentFilesCollectionManager.Dispose();

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public const string LocalLibraryString = @"local";

        public RecentFilesCollectionManager RecentFilesCollectionManager { get; }

        public DirectoryInfo? GetLibraryDirectoryInfo(string? libraryPath)
        {
            if (string.IsNullOrWhiteSpace(libraryPath)) return null;

            DirectoryInfo? libraryDirectoryInfo = null;

            if (libraryPath!.ToLower() == LocalLibraryString)
            {
                libraryDirectoryInfo = DsProject.GetStandardDsPagesAndDsShapesDirectory();

                if (libraryDirectoryInfo is null || !libraryDirectoryInfo.Exists)
                    //MessageBoxHelper.ShowInfo(Resources.StandardDsPagesAndDsShapesDirectoryError);
                    return null;
            }
            else
            {
                try
                {
                    libraryDirectoryInfo = new DirectoryInfo(libraryPath);
                }
                catch (Exception ex)
                {
                    DsProject.LoggersSet.Logger.LogError(ex, Resources.DsPagesAndDsShapesDirectoryError);
                }

                if (libraryDirectoryInfo is null || !libraryDirectoryInfo.Exists)
                {
                    MessageBoxHelper.ShowError(Resources.DsPagesAndDsShapesDirectoryError);
                    return null;
                }
            }

            RecentFilesCollectionManager.Add(libraryPath);

            return libraryDirectoryInfo;
        }

        #endregion
    }
}