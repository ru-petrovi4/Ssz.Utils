using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Ssz.Operator.Core.ControlsDesign
{
    public class DesignDrawingResizeThumb : Thumb
    {
        #region construction and destruction

        public DesignDrawingResizeThumb()
        {
            Background = new SolidColorBrush(Colors.Aqua);
            Foreground = new SolidColorBrush(Colors.Blue);
            DragDelta += ResizeThumbDragDelta;
        }

        #endregion

        #region private functions

        private void ResizeThumbDragDelta(object? sender, DragDeltaEventArgs e)
        {
            var designerDrawingViewModel = DataContext as DesignDrawingViewModel;

            if (designerDrawingViewModel is not null)
            {
                switch (VerticalAlignment)
                {
                    case VerticalAlignment.Bottom:
                        designerDrawingViewModel.ResizeVerticalBottom(e.VerticalChange);
                        break;
                    case VerticalAlignment.Top:
                        designerDrawingViewModel.ResizeVerticalTop(e.VerticalChange);
                        break;
                }

                switch (HorizontalAlignment)
                {
                    case HorizontalAlignment.Left:
                        designerDrawingViewModel.ResizeHorizontalLeft(e.HorizontalChange);
                        break;
                    case HorizontalAlignment.Right:
                        designerDrawingViewModel.ResizeHorizontalRight(e.HorizontalChange);
                        break;
                }
            }

            e.Handled = true;
        }

        #endregion
    }
}