using System;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Media;
using Ssz.Operator.Core.DsShapeViews;

namespace Ssz.Operator.Core.ControlsDesign
{
    /// <summary>
    ///     The handle that moves the selected shapes around the drawing.
    ///     <para>
    ///         Ported from the WPF editor. There the drag handlers asked Keyboard.Modifiers what was
    ///         held; Avalonia's drag events carry no modifiers, so the ones of the press that began the
    ///         drag are remembered here.
    ///     </para>
    /// </summary>
    public class DesignDsShapeViewDragThumb : Thumb
    {
        #region construction and destruction

        public DesignDsShapeViewDragThumb()
        {
            // A sheet of nothing over the whole shape: it is what the pointer grabs, and what the
            // shape is right clicked through. The WPF editor drew it as a transparent rectangle too.
            Template = new FuncControlTemplate<DesignDsShapeViewDragThumb>((_, _) =>
                new Rectangle { Fill = Brushes.Transparent });

            DragStarted += OnDragStarted;
            DragCompleted += OnDragCompleted;
            DragDelta += OnDragDelta;
        }

        #endregion

        #region protected functions

        protected override Type StyleKeyOverride => typeof(Thumb);

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            _keyModifiersOnPress = e.KeyModifiers;

            base.OnPointerPressed(e);
        }

        #endregion

        #region private functions

        private void OnDragStarted(object? sender, VectorEventArgs e)
        {
            if (DesignDsShapeView.DsShapeViewModel.IsSelected &&
                (_keyModifiersOnPress & (KeyModifiers.Shift | KeyModifiers.Control)) == KeyModifiers.None)
            {
                _draggedDsShapeViews =
                    DesignDsShapeView.DesignDrawingCanvas.GetSelectedRootDsShapeViews();

                foreach (DsShapeViewBase dsShapeView in _draggedDsShapeViews)
                    dsShapeView.DsShapeViewModel.DsShape.RefreshForPropertyGridIsDisabled = true;
            }
        }

        private void OnDragCompleted(object? sender, VectorEventArgs e)
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

        private void OnDragDelta(object? sender, VectorEventArgs e)
        {
            if (_draggedDsShapeViews is null) return;

            Point originalDeltaVector =
                DesignDsShapeView.TransformGroup.Value.Transform(new Point(e.Vector.X, e.Vector.Y));

            var deltaHorizontal = originalDeltaVector.X;
            var deltaVertical = originalDeltaVector.Y;

            foreach (DsShapeViewBase dsShapeView in _draggedDsShapeViews)
            {
                var dsShape = dsShapeView.DsShapeViewModel.DsShape;

                Point p0 = dsShape.CenterInitialPositionNotRounded;

                dsShape.CenterInitialPosition = new Point(p0.X + deltaHorizontal,
                    p0.Y + deltaVertical);

                if (DesignDsProjectViewModel.Instance.DiscreteMode)
                {
                    var discreteModeStep = DesignDsProjectViewModel.Instance.DiscreteModeStep;

                    Rect rect = dsShape.GetBoundingRect();

                    rect = new Rect(
                        Math.Round(rect.X / discreteModeStep) * discreteModeStep,
                        Math.Round(rect.Y / discreteModeStep) * discreteModeStep,
                        Math.Round(rect.Width / discreteModeStep) * discreteModeStep,
                        Math.Round(rect.Height / discreteModeStep) * discreteModeStep);

                    dsShape.SetBoundingRect(rect);
                }
            }

            e.Handled = true;
        }

        private DesignDsShapeView DesignDsShapeView => (DesignDsShapeView) DataContext!;

        #endregion

        #region private fields

        private DsShapeViewBase[]? _draggedDsShapeViews;
        private KeyModifiers _keyModifiersOnPress;

        #endregion
    }
}
