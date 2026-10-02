using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Platform.Storage;

namespace Ssz.Operator.Core
{
    /// <summary>
    ///     Picking a file to read or to write.
    ///     <para>
    ///         WPF's OpenFileDialog and SaveFileDialog are Windows only and block. Avalonia asks the
    ///         top level for its storage provider and answers a task, which is also what makes these
    ///         work in the browser.
    ///     </para>
    /// </summary>
    public static class FileDialogHelper
    {
        #region public functions

        /// <returns>The file the author chose, or null when the dialog was dismissed.</returns>
        public static async Task<IStorageFile?> OpenFileAsync(string title, params FilePickerFileType[] fileTypes)
        {
            TopLevel? topLevel = ClipboardHelper.GetTopLevel();
            if (topLevel is null) return null;

            IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = title,
                    AllowMultiple = false,
                    FileTypeFilter = fileTypes
                });

            return files.Count > 0 ? files[0] : null;
        }

        /// <summary>
        ///     Picking several files at once, which the editor needs wherever a list of drawings is
        ///     asked of the author.
        /// </summary>
        /// <param name="startDirectoryFullName">Where the dialog opens, when the folder can be reached.</param>
        public static async Task<IReadOnlyList<IStorageFile>> OpenFilesAsync(string title,
            string? startDirectoryFullName, params FilePickerFileType[] fileTypes)
        {
            TopLevel? topLevel = ClipboardHelper.GetTopLevel();
            if (topLevel is null) return new List<IStorageFile>();

            IStorageFolder? startFolder = null;
            if (!String.IsNullOrEmpty(startDirectoryFullName))
                try
                {
                    startFolder = await topLevel.StorageProvider.TryGetFolderFromPathAsync(startDirectoryFullName);
                }
                catch
                {
                    startFolder = null;
                }

            return await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = true,
                FileTypeFilter = fileTypes,
                SuggestedStartLocation = startFolder
            });
        }

        public static async Task<IStorageFile?> SaveFileAsync(string title, string? suggestedFileName,
            params FilePickerFileType[] fileTypes)
        {
            TopLevel? topLevel = ClipboardHelper.GetTopLevel();
            if (topLevel is null) return null;

            return await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = title,
                SuggestedFileName = suggestedFileName,
                FileTypeChoices = fileTypes
            });
        }

        /// <summary>
        ///     Asking for a file while the application has no window of its own yet, which is where
        ///     Play stands when it is started without a project.
        ///     <para>
        ///         A file dialog belongs to a top level, so one of no size is put up to ask from and
        ///         taken down afterwards. Windows opens its own dialog from it, and so do the desktops
        ///         of Linux through their portal.
        ///     </para>
        /// </summary>
        /// <returns>The full name of the file that was chosen, or null when the dialog was dismissed.</returns>
        public static async Task<string?> AskForFileFullNameAtStartupAsync(string title,
            params FilePickerFileType[] fileTypes)
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return null;

            Window? ownerWindow = desktop.MainWindow;
            Window? temporaryWindow = null;

            if (ownerWindow is null)
            {
                temporaryWindow = new Window
                {
                    Title = title,
                    Width = 1,
                    Height = 1,
                    WindowDecorations = WindowDecorations.None,
                    ShowInTaskbar = false,
                    ShowActivated = false,
                    Background = Brushes.Transparent,
                    TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent },
                    // A window is given a smallest size by the desktop it runs on, so it is put out
                    // of the way rather than made too small to notice.
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Position = OffScreenPosition
                };
                temporaryWindow.Show();
                temporaryWindow.Position = OffScreenPosition;
                ownerWindow = temporaryWindow;
            }

            try
            {
                IReadOnlyList<IStorageFile> files = await ownerWindow.StorageProvider.OpenFilePickerAsync(
                    new FilePickerOpenOptions
                    {
                        Title = title,
                        AllowMultiple = false,
                        FileTypeFilter = fileTypes
                    });

                // A file the operator reached through a portal may have no path on this machine, and
                // then there is nothing to open.
                return files.Count > 0 ? files[0].TryGetLocalPath() : null;
            }
            finally
            {
                temporaryWindow?.Close();
            }
        }

        /// <summary>
        ///     Where the window that a start up dialog belongs to is put, so that nothing of it is
        ///     seen on any screen.
        /// </summary>
        private static PixelPoint OffScreenPosition => new(-32000, -32000);

        public static FilePickerFileType DsProjectFileType =>
            new(Properties.OperatorUIResources.Play_DsProjectFileType)
            {
                Patterns = new[] { @"*" + DsProject.DsProjectFileExtension }
            };

        public static FilePickerFileType CsvFileType => new(@"*.csv")
        {
            Patterns = new[] { @"*.csv" }
        };

        public static FilePickerFileType AllFilesFileType => new(@"*.*")
        {
            Patterns = new[] { @"*.*" }
        };

        #endregion
    }
}
