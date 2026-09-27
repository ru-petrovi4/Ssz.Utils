using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Ssz.Operator.Core
{
    /// <summary>
    ///     Picking a directory.
    ///     <para>
    ///         The WPF editor used the folder picker of the Windows API Code Pack, which is Windows
    ///         only. Avalonia asks the top level for its storage provider, which also works elsewhere.
    ///     </para>
    /// </summary>
    public static class FolderDialogHelper
    {
        #region public functions

        /// <returns>The folder the author chose, or null when the dialog was dismissed.</returns>
        public static async Task<IStorageFolder?> OpenFolderAsync(string title)
        {
            TopLevel? topLevel = ClipboardHelper.GetTopLevel();
            if (topLevel is null) return null;

            IReadOnlyList<IStorageFolder> folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions
                {
                    Title = title,
                    AllowMultiple = false
                });

            return folders.Count > 0 ? folders[0] : null;
        }

        #endregion
    }
}
