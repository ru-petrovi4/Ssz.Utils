using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;

namespace Ssz.Operator.Core
{
    /// <summary>
    ///     Getting a drawing out of the editor on paper.
    ///     <para>
    ///         WPF had a print dialog that took a visual and sent it to a printer. Avalonia has no
    ///         printing at all, and in the browser there is nothing to print to either, so the drawing
    ///         is rendered to a picture the author saves and prints from wherever they like.
    ///     </para>
    /// </summary>
    public static class PrintHelper
    {
        #region public functions

        public static async Task SaveDrawingAsImageAsync(Control control, string suggestedName)
        {
            try
            {
                byte[]? bytes = PreviewImageHelper.CreatePreviewImageBytes(control,
                    Math.Max(control.Bounds.Width, 1), Math.Max(control.Bounds.Height, 1));
                if (bytes is null) return;

                IStorageFile? storageFile = await FileDialogHelper.SaveFileAsync(
                    suggestedName,
                    suggestedName + @".png",
                    new FilePickerFileType(@"*.png") { Patterns = new[] { @"*.png" } },
                    FileDialogHelper.AllFilesFileType);
                if (storageFile is null) return;

                await using Stream stream = await storageFile.OpenWriteAsync();
                await stream.WriteAsync(bytes);
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, @"Saving the drawing as a picture failed.");
            }
        }

        #endregion
    }
}
