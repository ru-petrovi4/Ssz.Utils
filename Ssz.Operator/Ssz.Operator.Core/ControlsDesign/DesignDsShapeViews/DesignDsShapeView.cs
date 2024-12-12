using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Ssz.Operator.Core.DsShapeViews;

namespace Ssz.Operator.Core.ControlsDesign
{
    [TemplatePart(Name = "PART_DragThumb", Type = typeof(DesignDsShapeViewDragThumb))]
    [TemplatePart(Name = "PART_ResizeDecorator", Type = typeof(Control))]
    [TemplatePart(Name = "PART_ContentPresenter", Type = typeof(ContentPresenter))]
    public class DesignDsShapeView : ContentControl, IDisposable
    {
        #region protected functions

        protected bool Disposed { get; private set; }

        #endregion

        #region construction and destruction

        public DesignDsShapeView(DsShapeViewBase dsShapeView, DesignDrawingCanvas designerDrawingCanvas)
        {
            DsShapeView = dsShapeView;
            DesignDrawingCanvas = designerDrawingCanvas;

            TransformGroup.Children.Add(ScaleTranform);
            TransformGroup.Children.Add(RotateTransform);
            RenderTransform = TransformGroup;

            DataContext = dsShapeView.DsShapeViewModel;
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        protected virtual void Dispose(bool disposing)
        {
            if (Disposed) return;

            //if (disposing)
            //{
            //    DsShapeView = null;
            //}            

            Disposed = true;
        }


        ~DesignDsShapeView()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public DesignDrawingCanvas DesignDrawingCanvas { get; }


        public DsShapeViewBase DsShapeView { get; }


        public DsShapeViewModel DsShapeViewModel => (DsShapeViewModel) DataContext;

        public readonly RotateTransform RotateTransform = new();
        public readonly ScaleTransform ScaleTranform = new();
        public readonly TransformGroup TransformGroup = new();

        public const double ResizeThumbThikness = 6.0;

        #endregion
    }
}