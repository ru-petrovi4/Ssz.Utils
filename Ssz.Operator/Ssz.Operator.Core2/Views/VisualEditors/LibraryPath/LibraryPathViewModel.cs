using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.ViewModels;

namespace Ssz.Operator.Core.VisualEditors.LibraryPath
{
    /// <summary>
    ///     Which library of pages and shapes is being looked at, and the ones looked at before.
    /// </summary>
    public class LibraryPathViewModel : DisposableViewModelBase
    {
        #region construction and destruction

        public LibraryPathViewModel()
        {
            RecentFilesCollectionManager =
                new RecentFilesCollectionManager(
                    AppRegistryOptions.SszOperatorSubKeyString + @"\" +
                    AppRegistryOptions.DsPagesAndDsShapesLibrariesSubKeyString, 10, 150, @"");

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

        /// <returns>The library folder, or null when it is not there.</returns>
        public DirectoryInfo? GetLibraryDirectoryInfo(string? libraryPath)
        {
            if (String.IsNullOrWhiteSpace(libraryPath)) return null;

            DirectoryInfo? libraryDirectoryInfo;

            if (libraryPath!.ToLowerInvariant() == LocalLibraryString)
            {
                libraryDirectoryInfo = DsProject.GetStandardDsPagesAndDsShapesDirectory();

                if (libraryDirectoryInfo is null || !libraryDirectoryInfo.Exists)
                    return null;
            }
            else
            {
                libraryDirectoryInfo = null;
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
