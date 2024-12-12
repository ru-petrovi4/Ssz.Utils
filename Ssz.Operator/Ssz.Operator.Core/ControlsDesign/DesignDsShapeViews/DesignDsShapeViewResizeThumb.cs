using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.DsShapeViews;

namespace Ssz.Operator.Core.ControlsDesign
{
    public class DesignDsShapeViewResizeThumb : Thumb
    {
        #region private fields

        private DsShapeViewModel? _draggedDsShapeViewModel;

        #endregion

        #region construction and destruction

        public DesignDsShapeViewResizeThumb()
        {
            DragStarted += OnDragStarted;
            DragCompleted += OnDragCompleted;
            DragDelta += OnDragDelta;
        }

        #endregion

        #region private functions

        private void OnDragStarted(object? sender, DragStartedEventArgs dragStartedEventArgs)
        {
            if (DesignDsShapeView.DsShapeViewModel.DsShape.ResizeMode == DsShapeResizeMode.NoResize) return;

            _draggedDsShapeViewModel = DesignDsShapeView.DsShapeViewModel;

            _draggedDsShapeViewModel.DsShape.RefreshForPropertyGridIsDisabled = true;
        }

        private void OnDragCompleted(object? sender, DragCompletedEventArgs dragCompletedEventArgs)
        {
            if (_draggedDsShapeViewModel is null) return;

            _draggedDsShapeViewModel.DsShape.RefreshForPropertyGridIsDisabled = false;

            var complexDsShapeView = DesignDsShapeView.DsShapeView as ComplexDsShapeView;
            if (complexDsShapeView is not null) complexDsShapeView.UpdateModelLayer();

            _draggedDsShapeViewModel = null;
        }

        private void OnDragDelta(object? sender, DragDeltaEventArgs e)
        {
            if (_draggedDsShapeViewModel is null) return;

            var dsShape = _draggedDsShapeViewModel.DsShape;

            var minDeltaWidth = dsShape.GetMinDeltaWidth();
            var minDeltaHeight = dsShape.GetMinDeltaHeight();

            var pinnedPoint = new Point(0.0, 0.0);
            var notTransformedRect0 = dsShape.GetNotTransformedRect();
            double widthHeightRatio;
            if (dsShape.ResizeMode == DsShapeResizeMode.KeepAspectRatio)
                widthHeightRatio = notTransformedRect0.Width / notTransformedRect0.Height;
            else
                widthHeightRatio = double.NaN;
            double deltaHorizontal = 0;
            double deltaVertical = 0;
            if (dsShape.ResizeMode == DsShapeResizeMode.WidthAndHeight ||
                dsShape.ResizeMode == DsShapeResizeMode.WidthOnly ||
                dsShape.ResizeMode == DsShapeResizeMode.KeepAspectRatio)
                switch (HorizontalAlignment)
                {
                    case HorizontalAlignment.Left:
                        deltaHorizontal = -Math.Min(e.HorizontalChange, -minDeltaWidth);
                        if (!double.IsNaN(widthHeightRatio))
                            deltaVertical = deltaHorizontal / widthHeightRatio;
                        pinnedPoint.X = 1.0;
                        break;
                    case HorizontalAlignment.Right:
                        deltaHorizontal = Math.Max(e.HorizontalChange, minDeltaWidth);
                        if (!double.IsNaN(widthHeightRatio))
                            deltaVertical = deltaHorizontal / widthHeightRatio;
                        pinnedPoint.X = 0.0;
                        break;
                }

            if (dsShape.ResizeMode == DsShapeResizeMode.WidthAndHeight ||
                dsShape.ResizeMode == DsShapeResizeMode.HeightOnly ||
                dsShape.ResizeMode == DsShapeResizeMode.KeepAspectRatio)
                switch (VerticalAlignment)
                {
                    case VerticalAlignment.Top:
                        deltaVertical = -Math.Min(e.VerticalChange, -minDeltaHeight);
                        if (!double.IsNaN(widthHeightRatio))
                            deltaHorizontal = deltaVertical * widthHeightRatio;
                        pinnedPoint.Y = 1.0;
                        break;
                    case VerticalAlignment.Bottom:
                        deltaVertical = Math.Max(e.VerticalChange, minDeltaHeight);
                        if (!double.IsNaN(widthHeightRatio))
                            deltaHorizontal = deltaVertical * widthHeightRatio;
                        pinnedPoint.Y = 0.0;
                        break;
                }

            var newWidth = notTransformedRect0.Width + deltaHorizontal;
            var newHeight = notTransformedRect0.Height + deltaVertical;
            if (DesignDsProjectViewModel.Instance.DiscreteMode)
            {
                var discreteModeStep = DesignDsProjectViewModel.Instance.DiscreteModeStep;
                newWidth = Math.Round(newWidth / discreteModeStep) * discreteModeStep;
                newHeight = Math.Round(newHeight / discreteModeStep) * discreteModeStep;
                deltaHorizontal = newWidth - notTransformedRect0.Width;
                deltaVertical = newHeight - notTransformedRect0.Height;
            }

            dsShape.WidthInitial = newWidth;
            dsShape.HeightInitial = newHeight;

            _draggedDsShapeViewModel.SetGeometryEditingMode();

            var centerDeltaHorizontal =
                deltaHorizontal * (_draggedDsShapeViewModel.CenterRelativePosition.X - pinnedPoint.X);
            var centerDeltaVertical =
                deltaVertical * (_draggedDsShapeViewModel.CenterRelativePosition.Y - pinnedPoint.Y);

            var centerDeltaVector =
                DesignDsShapeView.TransformGroup.Transform(new Point(centerDeltaHorizontal, centerDeltaVertical));
            var x = _draggedDsShapeViewModel.CenterInitialPositionX;
            var y = _draggedDsShapeViewModel.CenterInitialPositionY;

            dsShape.CenterInitialPosition = new Point(x + centerDeltaVector.X, y + centerDeltaVector.Y);

            e.Handled = true;
        }

        private DesignDsShapeView DesignDsShapeView => (DesignDsShapeView) DataContext;

        #endregion
    }
}