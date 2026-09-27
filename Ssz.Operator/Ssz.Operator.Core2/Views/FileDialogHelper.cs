using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
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
