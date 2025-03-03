using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
            underlyingContentControl.SetBinding(WidthProperty, new Binding
            {
                Path = new PropertyPath("ActualWidth"),
                Source = this,
                Mode = BindingMode.OneWay
            });
            underlyingContentControl.SetBinding(HeightProperty, new Binding
            {
                Path = new PropertyPath("ActualHeight"),
                Source = this,
                Mode = BindingMode.OneWay
            });
            Children.Add(underlyingContentControl);

            var dsPageDrawing = drawing as DsPageDrawing;
            if (dsPageDrawing is not null) underlyingContentControl.Content = dsPageDrawing.GetUnderlyingContent();

            underlyingContentControl.SnapsToDevicePixels = false;

            foreach (DsShapeBase dsShape in drawing.DsShapes)
            {
                var newDsShapeView = DsShapeViewFactory.New(dsShape, frame);
                if (newDsShapeView is null) continue;
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

                foreach (DsShapeViewBase dsShapeView in _dsShapeViewsList)
                {
                    dsShapeView.Dispose();
                }
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

        public void DsShapeViewsReInitialize()
        {
            foreach (DsShapeViewBase dsShapeView in _dsShapeViewsList)
            {
                dsShapeView.Initialize(PlayDrawingViewModel);
            }
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