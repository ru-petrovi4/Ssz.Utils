using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Ssz.Operator.Core.ControlsPlay.PanoramaPlay
{
    /// <summary>
    ///     The picture the panorama mesh is textured with, kept on the graphics card between the frames.
    ///     A page of a panorama is huge - four thousand pixels on its longest side - and handing a new
    ///     image of it to Skia on every change would upload all of it again, which costs far more than
    ///     drawing the page did. So the picture is held in a surface of its own, and a change only sends
    ///     the piece of it that changed.
    ///     The pieces are prepared on the thread the page is drawn on and are drawn into the surface on
    ///     the thread that renders, which is the only one that may touch the graphics context.
    /// </summary>
    internal sealed class PanoramaTexture : IDisposable
    {
        #region construction and destruction

        /// <summary>
        ///     Called from the thread of the page. The surface itself belongs to the render thread, so it
        ///     is left for the next render to let go of - or for the graphics context to take with it.
        /// </summary>
        public void Dispose()
        {
            lock (SyncRoot)
            {
                if (_disposed) return;
                _disposed = true;

                ClearPatches();
                ReleaseSurface();
            }
        }

        #endregion

        #region public functions

        public SKImageInfo Info { get; private set; }

        /// <summary>
        ///     Page thread: lets go of the picture, but keeps the texture usable for the next page - the
        ///     two viewports of a panorama take turns and are used again and again.
        /// </summary>
        public void Clear()
        {
            lock (SyncRoot)
            {
                Info = default;
                HasContent = false;
                ClearPatches();
                ReleaseSurface();
            }
        }

        /// <summary>
        ///     True once there is something to show.
        /// </summary>
        public bool HasContent { get; private set; }

        /// <summary>
        ///     Page thread: a page of another size starts a new surface.
        /// </summary>
        public void SetInfo(SKImageInfo info)
        {
            lock (SyncRoot)
            {
                if (Info == info) return;

                Info = info;
                HasContent = false;
                ClearPatches();
                ReleaseSurface();
            }
        }

        /// <summary>
        ///     Page thread: the piece of the picture that changed. The bitmap is handed over and is let
        ///     go of once it has been drawn into the surface.
        /// </summary>
        public void AddPatch(SKBitmap bitmap, SKRect destination)
        {
            bitmap.SetImmutable();
            var image = SKImage.FromBitmap(bitmap);

            lock (SyncRoot)
            {
                if (_disposed)
                {
                    image.Dispose();
                    bitmap.Dispose();
                    return;
                }

                _patches.Add((bitmap, image, destination));
                HasContent = true;
            }
        }

        /// <summary>
        ///     Render thread: applies what has changed and gives back the whole picture.
        /// </summary>
        public SKImage? GetImage(GRContext? grContext)
        {
            lock (SyncRoot)
            {
                ReleaseAbandonedSurfaces();

                if (_disposed || Info.Width <= 0 || Info.Height <= 0) return null;

                if (_patches.Count > 0)
                {
                    // The old picture is let go of first: a surface whose image is still held elsewhere
                    // is copied whole as soon as it is drawn into.
                    _image?.Dispose();
                    _image = null;

                    if (_surface is null)
                        _surface = CreateSurface(grContext, Info);

                    if (_surface is null)
                    {
                        ClearPatches();
                        return null;
                    }

                    using var paint = new SKPaint { BlendMode = SKBlendMode.Src, IsAntialias = false };
                    foreach (var patch in _patches)
                    {
                        _surface.Canvas.DrawImage(patch.Image, patch.Destination, paint);
                        patch.Image.Dispose();
                        patch.Bitmap.Dispose();
                    }

                    _patches.Clear();
                    _surface.Canvas.Flush();
                }

                if (_surface is null) return null;

                return _image ??= _surface.Snapshot();
            }
        }

        #endregion

        #region private functions

        private static SKSurface? CreateSurface(GRContext? grContext, SKImageInfo info)
        {
            try
            {
                // On the graphics card when there is one, and in main memory when the window is drawn
                // by the processor.
                return grContext is not null
                    ? SKSurface.Create(grContext, true, info) ?? SKSurface.Create(info)
                    : SKSurface.Create(info);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void ClearPatches()
        {
            foreach (var patch in _patches)
            {
                patch.Image.Dispose();
                patch.Bitmap.Dispose();
            }

            _patches.Clear();
        }

        private void ReleaseSurface()
        {
            _image?.Dispose();
            _image = null;

            if (_surface is null) return;

            // A surface of the graphics card may only be let go of on the thread that renders, so it
            // waits here for the next one to pass by.
            AbandonedSurfaces.Add(_surface);
            _surface = null;
        }

        private static void ReleaseAbandonedSurfaces()
        {
            if (AbandonedSurfaces.Count == 0) return;

            foreach (var surface in AbandonedSurfaces)
                try
                {
                    surface.Dispose();
                }
                catch (Exception)
                {
                }

            AbandonedSurfaces.Clear();
        }

        #endregion

        #region private fields

        private static readonly object SyncRoot = new();
        private static readonly List<SKSurface> AbandonedSurfaces = new();

        private readonly List<(SKBitmap Bitmap, SKImage Image, SKRect Destination)> _patches = new();
        private SKSurface? _surface;
        private SKImage? _image;
        private bool _disposed;

        #endregion
    }
}
