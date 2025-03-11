using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DsShapes;

namespace Ssz.Operator.Core.DsShapeViews
{
    public class WindowDragDsShapeView : DsShapeViewBase
    {
        #region construction and destruction

        public WindowDragDsShapeView(WindowDragDsShape dsShape, ControlsPlay.Frame? frame)
            : base(dsShape, frame)
        {
            if (VisualDesignMode)
            {
                DesignModeTextBlock = new TextBlock();
                DesignModeTextBlock.Text = WindowDragDsShape.DsShapeTypeNameToDisplay;
                Border = new Border();
                Border.Opacity = 0.3;
                Border.BorderThickness = new Thickness(1);
                Border.BorderBrush = Brushes.White;
                Border.Background = Brushes.Gray;
                Border.Padding = new Thickness(5, 1, 5, 1);
                Border.Child =
                    new Viewbox
                    {
                        Stretch = Stretch.Uniform,
                        Child = DesignModeTextBlock
                    };
                Content = Border;
            }
            else
            {
                Border = new Border();
                Border.BorderThickness = new Thickness(0);
                Border.Background = Brushes.Transparent;
                Border.PointerPressed += PlayBorderOnPointerPressed;
                Content = Border;
            }
        }

        private void PlayBorderOnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            //if (e. == MouseButton.Left)
            {
                var window = Frame is not null ? Frame.PlayWindow as Window : null;
                if (window is not null) 
                    window.BeginMoveDrag(e);
            }
        }

        #endregion

        #region protected functions

        protected TextBlock? DesignModeTextBlock { get; }

        protected Border Border { get; }

        #endregion
    }
}