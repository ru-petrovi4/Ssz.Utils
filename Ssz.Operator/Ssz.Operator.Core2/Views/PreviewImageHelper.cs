using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;

namespace Ssz.Operator.Core
{
    /// <summary>
    ///     Makes the small picture a drawing is listed with.
    ///     <para>
    ///         The WPF version put the control in a hidden window when it was not on screen, rendered it
    ///         to a bitmap and encoded that. Avalonia renders a control to a render target bitmap
    ///         without a window at all, but only at the size the control has, so the result is then
    ///         drawn scaled into the preview.
    ///     </para>
    /// </summary>
    public static class PreviewImageHelper
    {
        #region public functions

        public static byte[]? CreatePreviewImageBytes(Control? control, double previewWidth, double previewHeight)
        {
            if (control is null) return null;

            try
            {
                Size size = control.Bounds.Size;
                if (size.Width < 1 || size.Height < 1)
                {
                    // A control that was never laid out has no size of its own yet.
                    control.Measure(Size.Infinity);
                    control.Arrange(new Rect(control.DesiredSize));
                    size = control.Bounds.Size;
                }

                if (size.Width < 1 || size.Height < 1) return null;

                using var drawingBitmap = new RenderTargetBitmap(
                    new PixelSize((int) Math.Ceiling(size.Width), (int) Math.Ceiling(size.Height)),
                    new Vector(96, 96));
                drawingBitmap.Render(control);

                // Fit the drawing into the preview, keeping its proportions and centring what is left.
                var scale = Math.Min(previewWidth / size.Width, previewHeight / size.Height);
                var scaledWidth = size.Width * scale;
                var scaledHeight = size.Height * scale;
                var destinationRect = new Rect((previewWidth - scaledWidth) / 2,
                    (previewHeight - scaledHeight) / 2, scaledWidth, scaledHeight);

                using var previewBitmap = new RenderTargetBitmap(
                    new PixelSize((int) Math.Ceiling(previewWidth), (int) Math.Ceiling(previewHeight)),
                    new Vector(96, 96));
                using (DrawingContext context = previewBitmap.CreateDrawingContext())
                {
                    context.DrawImage(drawingBitmap, new Rect(0, 0, size.Width, size.Height), destinationRect);
                }

                using var stream = new MemoryStream();
                previewBitmap.Save(stream);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogWarning(ex, @"Creating a preview image failed.");
                return null;
            }
        }

        #endregion
    }
}
