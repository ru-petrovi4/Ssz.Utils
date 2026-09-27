using System;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.DsShapeViews;

namespace Ssz.Operator.Core.ControlsDesign
{
    /// <summary>
    ///     One of the handles around a selected shape. Which edge or corner it sits on is its own
    ///     alignment inside the decorator, and that is what decides which way the shape grows.
    /// </summary>
    public class DesignDsShapeViewResizeThumb : Thumb
    {
        #region construction and destruction

        public DesignDsShapeViewResizeThumb()
        {
            // A handle of its own making: the themes of Avalonia leave a plain Thumb without one, and
            // WPF drew it as the small square the author knows.
            Template = new FuncControlTemplate<DesignDsShapeViewResizeThumb>((_, _) => new Rectangle
            {
                Fill = Brushes.White,
                Stroke = Brushes.Black,
                StrokeThickness = 1.0
            });

            DragStarted += OnDragStarted;
            DragCompleted += OnDragCompleted;
            DragDelta += OnDragDelta;
        }

        #endregion

        #region protected functions
        /// <summary>
        ///     A handle looks like any other thumb. Avalonia gives a derived control no theme of its
        ///     own, so this one says to be styled as the thumb it is - otherwise it has no template at
        ///     all and nothing is drawn.
        /// </summary>
        protected override Type StyleKeyOverride => typeof(Thumb);

        #endregion

        #region private functions

        private void OnDragStarted(object? sender, VectorEventArgs e)
        {
            if (DesignDsShapeView.DsShapeViewModel.DsShape.ResizeMode == DsShapeResizeMode.NoResize) return;

            _draggedDsShapeViewModel = DesignDsShapeView.DsShapeViewModel;

            _draggedDsShapeViewModel.DsShape.RefreshForPropertyGridIsDisabled = true;
        }

        private void OnDragCompleted(object? sender, VectorEventArgs e)
        {
            if (_draggedDsShapeViewModel is null) return;

            _draggedDsShapeViewModel.DsShape.RefreshForPropertyGridIsDisabled = false;

            var complexDsShapeView = DesignDsShapeView.DsShapeView as ComplexDsShapeView;
            if (complexDsShapeView is not null) complexDsShapeView.UpdateModelLayer();

            _draggedDsShapeViewModel = null;
        }

        private void OnDragDelta(object? sender, VectorEventArgs e)
        {
            if (_draggedDsShapeViewModel is null) return;

            var dsShape = _draggedDsShapeViewModel.DsShape;

            var minDeltaWidth = dsShape.GetMinDeltaWidth();
            var minDeltaHeight = dsShape.GetMinDeltaHeight();

            var pinnedPointX = 0.0;
            var pinnedPointY = 0.0;
            Rect notTransformedRect0 = dsShape.GetNotTransformedRect();
            double widthHeightRatio;
            if (dsShape.ResizeMode == DsShapeResizeMode.KeepAspectRatio)
                widthHeightRatio = notTransformedRect0.Width / notTransformedRect0.Height;
            else
                widthHeightRatio = Double.NaN;
            double deltaHorizontal = 0;
            double deltaVertical = 0;
            if (dsShape.ResizeMode == DsShapeResizeMode.WidthAndHeight ||
                dsShape.ResizeMode == DsShapeResizeMode.WidthOnly ||
                dsShape.ResizeMode == DsShapeResizeMode.KeepAspectRatio)
                switch (HorizontalAlignment)
                {
                    case HorizontalAlignment.Left:
                        deltaHorizontal = -Math.Min(e.Vector.X, -minDeltaWidth);
                        if (!Double.IsNaN(widthHeightRatio))
                            deltaVertical = deltaHorizontal / widthHeightRatio;
                        pinnedPointX = 1.0;
                        break;
                    case HorizontalAlignment.Right:
                        deltaHorizontal = Math.Max(e.Vector.X, minDeltaWidth);
                        if (!Double.IsNaN(widthHeightRatio))
                            deltaVertical = deltaHorizontal / widthHeightRatio;
                        pinnedPointX = 0.0;
                        break;
                }

            if (dsShape.ResizeMode == DsShapeResizeMode.WidthAndHeight ||
                dsShape.ResizeMode == DsShapeResizeMode.HeightOnly ||
                dsShape.ResizeMode == DsShapeResizeMode.KeepAspectRatio)
                switch (VerticalAlignment)
                {
                    case VerticalAlignment.Top:
                        deltaVertical = -Math.Min(e.Vector.Y, -minDeltaHeight);
                        if (!Double.IsNaN(widthHeightRatio))
                            deltaHorizontal = deltaVertical * widthHeightRatio;
                        pinnedPointY = 1.0;
                        break;
                    case VerticalAlignment.Bottom:
                        deltaVertical = Math.Max(e.Vector.Y, minDeltaHeight);
                        if (!Double.IsNaN(widthHeightRatio))
                            deltaHorizontal = deltaVertical * widthHeightRatio;
                        pinnedPointY = 0.0;
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
                deltaHorizontal * (_draggedDsShapeViewModel.CenterRelativePosition.X - pinnedPointX);
            var centerDeltaVertical =
                deltaVertical * (_draggedDsShapeViewModel.CenterRelativePosition.Y - pinnedPointY);

            Point centerDeltaVector =
                DesignDsShapeView.TransformGroup.Value.Transform(
                    new Point(centerDeltaHorizontal, centerDeltaVertical));
            var x = _draggedDsShapeViewModel.CenterInitialPositionX;
            var y = _draggedDsShapeViewModel.CenterInitialPositionY;

            dsShape.CenterInitialPosition = new Point(x + centerDeltaVector.X, y + centerDeltaVector.Y);

            e.Handled = true;
        }

        private DesignDsShapeView DesignDsShapeView => (DesignDsShapeView) DataContext!;

        #endregion

        #region private fields

        private DsShapeViewModel? _draggedDsShapeViewModel;

        #endregion
    }
}
