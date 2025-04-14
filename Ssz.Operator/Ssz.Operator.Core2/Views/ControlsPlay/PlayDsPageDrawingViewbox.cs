using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.Utils;

namespace Ssz.Operator.Core.ControlsPlay
{
    public sealed class PlayDsPageDrawingViewbox : Border, IDisposable
    {
        #region construction and destruction

        public PlayDsPageDrawingViewbox(DsPageDrawing dsPageDrawing, Frame? frame)
        {
            Background = dsPageDrawing.ComputeDsPageBackgroundBrush();

            var dsPageStretchMode = dsPageDrawing.ComputeDsPageStretchMode();
            Stretch stretch;
            if (dsPageStretchMode == DsPageStretchMode.Default) 
                stretch = Stretch.Uniform;
            else 
                stretch = (Stretch) dsPageStretchMode;

            var dsPageHorizontalAlignment = dsPageDrawing.ComputeDsPageHorizontalAlignment();
            HorizontalAlignment horizontalAlignment;
            if (dsPageHorizontalAlignment == DsPageHorizontalAlignment.Default)
                horizontalAlignment = HorizontalAlignment.Center;
            else 
                horizontalAlignment = TreeHelper.GetHorizontalAlignment(dsPageHorizontalAlignment);

            var dsPageVerticalAlignment = dsPageDrawing.ComputeDsPageVerticalAlignment();
            VerticalAlignment verticalAlignment;
            if (dsPageVerticalAlignment == DsPageVerticalAlignment.Default)
                verticalAlignment = VerticalAlignment.Center;
            else 
                verticalAlignment = TreeHelper.GetVerticalAlignment(dsPageVerticalAlignment);

            _playDsPageDrawingCanvas = new PlayDsPageDrawingCanvas(dsPageDrawing, frame);

            if (stretch == Stretch.None)
                Child = new ScrollViewer
                {
                    Content = _playDsPageDrawingCanvas,
                    HorizontalAlignment = horizontalAlignment,
                    VerticalAlignment = verticalAlignment,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                };
            else
                Child = new Viewbox
                {
                    Child = _playDsPageDrawingCanvas,
                    HorizontalAlignment = horizontalAlignment,
                    VerticalAlignment = verticalAlignment,
                    Stretch = stretch
                };
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing) _playDsPageDrawingCanvas.Dispose();

            _disposed = true;
        }        

        ~PlayDsPageDrawingViewbox()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public PlayDrawingViewModel PlayDrawingViewModel => _playDsPageDrawingCanvas.PlayDrawingViewModel;

        public void ShapeViewsReInitialize()
        {
            _playDsPageDrawingCanvas.ShapeViewsReInitialize();
        }

        #endregion

        #region private functions

        private bool _disposed;

        private readonly PlayDsPageDrawingCanvas _playDsPageDrawingCanvas;

        #endregion
    }
}