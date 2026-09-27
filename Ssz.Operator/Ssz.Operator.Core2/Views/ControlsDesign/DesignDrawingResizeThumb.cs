using System;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ssz.Operator.Core.ControlsDesign
{
    /// <summary>
    ///     A handle on the edge of the drawing itself, for changing the size of the page.
    /// </summary>
    public class DesignDrawingResizeThumb : Thumb
    {
        #region construction and destruction

        public DesignDrawingResizeThumb()
        {
            // A handle of its own making: the themes of Avalonia leave a plain Thumb without one.
            Template = new FuncControlTemplate<DesignDrawingResizeThumb>((thumb, _) => new Rectangle
            {
                Fill = thumb.Background,
                Stroke = thumb.Foreground,
                StrokeThickness = 1.0
            });

            Background = new SolidColorBrush(Colors.Aqua);
            Foreground = new SolidColorBrush(Colors.Blue);
            DragDelta += ResizeThumbDragDelta;
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

        private void ResizeThumbDragDelta(object? sender, VectorEventArgs e)
        {
            var designerDrawingViewModel = DataContext as DesignDrawingViewModel;

            if (designerDrawingViewModel is not null)
            {
                switch (VerticalAlignment)
                {
                    case VerticalAlignment.Bottom:
                        designerDrawingViewModel.ResizeVerticalBottom(e.Vector.Y);
                        break;
                    case VerticalAlignment.Top:
                        designerDrawingViewModel.ResizeVerticalTop(e.Vector.Y);
                        break;
                }

                switch (HorizontalAlignment)
                {
                    case HorizontalAlignment.Left:
                        designerDrawingViewModel.ResizeHorizontalLeft(e.Vector.X);
                        break;
                    case HorizontalAlignment.Right:
                        designerDrawingViewModel.ResizeHorizontalRight(e.Vector.X);
                        break;
                }
            }

            e.Handled = true;
        }

        #endregion
    }
}
