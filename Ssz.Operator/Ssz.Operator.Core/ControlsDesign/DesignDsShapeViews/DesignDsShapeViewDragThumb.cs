using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Ssz.Operator.Core.DsShapeViews;

namespace Ssz.Operator.Core.ControlsDesign
{
    public class DesignDsShapeViewDragThumb : Thumb
    {
        #region private fields

        private DsShapeViewBase[]? _draggedDsShapeViews;

        #endregion

        #region construction and destruction

        public DesignDsShapeViewDragThumb()
        {
            DragStarted += OnDragStarted;
            DragCompleted += OnDragCompleted;
            DragDelta += OnDragDelta;
        }

        #endregion

        #region private functions

        private void OnDragStarted(object? sender, DragStartedEventArgs dragStartedEventArgs)
        {
            if (DesignDsShapeView.DsShapeViewModel.IsSelected &&
                (Keyboard.Modifiers & (ModifierKeys.Shift | ModifierKeys.Control)) == ModifierKeys.None)
            {
                _draggedDsShapeViews =
                    DesignDsShapeView.DesignDrawingCanvas.GetSelectedRootDsShapeViews();

                foreach (DsShapeViewBase dsShapeView in _draggedDsShapeViews)
                    dsShapeView.DsShapeViewModel.DsShape.RefreshForPropertyGridIsDisabled = true;
            }
        }

        private void OnDragCompleted(object? sender, DragCompletedEventArgs dragCompletedEventArgs)
        {
            if (_draggedDsShapeViews is null) return;

            foreach (DsShapeViewBase dsShapeView in _draggedDsShapeViews)
            {
                dsShapeView.DsShapeViewModel.DsShape.RefreshForPropertyGridIsDisabled = false;

                var complexDsShapeView = dsShapeView as ComplexDsShapeView;
                if (complexDsShapeView is not null) complexDsShapeView.UpdateModelLayer();
            }

            _draggedDsShapeViews = null;
        }

        private void OnDragDelta(object? sender, DragDeltaEventArgs e)
        {
            if (_draggedDsShapeViews is null) return;

            var originalDeltaVector =
                DesignDsShapeView.TransformGroup.Transform(new Point(e.HorizontalChange, e.VerticalChange));

            var deltaHorizontal = originalDeltaVector.X;
            var deltaVertical = originalDeltaVector.Y;

            foreach (DsShapeViewBase dsShapeView in _draggedDsShapeViews)
            {
                var dsShape = dsShapeView.DsShapeViewModel.DsShape;

                var p0 = dsShape.CenterInitialPositionNotRounded;
                ;

                dsShape.CenterInitialPosition = new Point(p0.X + deltaHorizontal,
                    p0.Y + deltaVertical);

                if (DesignDsProjectViewModel.Instance.DiscreteMode)
                {
                    var discreteModeStep = DesignDsProjectViewModel.Instance.DiscreteModeStep;

                    var rect = dsShape.GetBoundingRect();

                    rect.X = Math.Round(rect.X / discreteModeStep) * discreteModeStep;
                    rect.Y = Math.Round(rect.Y / discreteModeStep) * discreteModeStep;
                    rect.Width = Math.Round(rect.Width / discreteModeStep) * discreteModeStep;
                    rect.Height = Math.Round(rect.Height / discreteModeStep) * discreteModeStep;

                    dsShape.SetBoundingRect(rect);
                }
            }

            /*
            if (designerDrawingCanvas.DesignDrawingViewModel is DesignDsShapeDrawingViewModel)
            {
                var containingRect = new Rect(0, 0, 0, 0);

                foreach (
                    DsShapeViewModel dsShapeViewModel in designerDrawingCanvas.DsShapeViewModels)
                {
                    containingRect = Rect.Union(containingRect,
                        new Rect(dsShapeViewModel.LeftNotTransformed, dsShapeViewModel.DesignTop,
                            dsShapeViewModel.WidthInitial, dsShapeViewModel.HeightInitial));
                }

                //if (containingRect.Right > designerDrawingCanvas.DesignDrawingViewModel.Width)
                designerDrawingCanvas.DesignDrawingViewModel.Width = containingRect.Right;
                //if (containingRect.Bottom > designerDrawingCanvas.DesignDrawingViewModel.Height)
                designerDrawingCanvas.DesignDrawingViewModel.Height = containingRect.Bottom;
                //designerDrawingCanvas.InvalidateMeasure();
            }*/

            e.Handled = true;
        }

        private DesignDsShapeView DesignDsShapeView => (DesignDsShapeView) DataContext;

        #endregion
    }
}