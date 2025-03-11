using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsPageTypes;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.DsShapeViews;

namespace Ssz.Operator.Core.ControlsPlay
{
    public class PlayDrawingCanvas : Canvas, IDisposable
    {
        #region construction and destruction

        public PlayDrawingCanvas(DrawingBase drawing, Frame? frame)
        {
            PlayDrawingViewModel = new PlayDrawingViewModel(drawing);

            Width = drawing.Width;
            Height = drawing.Height;

            var underlyingContentControl = new ContentControl();
            underlyingContentControl.Bind(WidthProperty, new Binding
            {
                Path = "Bounds.Width",
                Source = this,
                Mode = BindingMode.OneWay
            });
            underlyingContentControl.Bind(HeightProperty, new Binding
            {
                Path = "Bounds.Height",
                Source = this,
                Mode = BindingMode.OneWay
            });
            Children.Add(underlyingContentControl);

            var dsPageDrawing = drawing as DsPageDrawing;
            if (dsPageDrawing is not null)
                SetUnderlyingContent(underlyingContentControl, dsPageDrawing);                

            //underlyingContentControl.//SnapsToDevicePixels = false;

            foreach (DsShapeBase dsShape in drawing.DsShapes)
            {
                var newDsShapeView = DsShapeViewFactory.New(dsShape, frame);
                if (newDsShapeView is null)
                    continue;
                newDsShapeView.Initialize(PlayDrawingViewModel);

                _dsShapeViewsList.Add(newDsShapeView);
                Children.Add(newDsShapeView);
            }

            PlayDrawingViewModel.DrawingUpdatingIsEnabled = true;
        }        

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                try
                {
                    Children.Clear();
                }
                catch
                {
                }

                foreach (DsShapeViewBase dsShapeView in _dsShapeViewsList) dsShapeView.Dispose();
                _dsShapeViewsList.Clear();

                PlayDrawingViewModel.Dispose();
            }

            Disposed = true;
        }

        ~PlayDrawingCanvas()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public PlayDrawingViewModel PlayDrawingViewModel { get; }

        public void ShapeViewsReInitialize()
        {
            foreach (DsShapeViewBase dsShapeView in _dsShapeViewsList)
            {
                dsShapeView.Initialize(PlayDrawingViewModel);
            }
        }

        #endregion

        #region private functions

        private async void SetUnderlyingContent(ContentControl underlyingContentControl, DsPageDrawing dsPageDrawing)
        {
            underlyingContentControl.Content = await dsPageDrawing.GetUnderlyingContentAsync();
        }

        #endregion        

        #region protected functions

        protected bool Disposed { get; private set; }

        #endregion  

        #region private fields

        private readonly List<DsShapeViewBase> _dsShapeViewsList = new();

        #endregion
    }
}