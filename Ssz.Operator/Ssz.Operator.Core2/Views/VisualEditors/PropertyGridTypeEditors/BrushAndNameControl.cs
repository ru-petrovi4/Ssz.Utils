using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.Utils;
using Ssz.Utils;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors
{
    /// <summary>
    ///     What a brush looks like in one row of the property grid: a swatch of it over a chequerboard,
    ///     so that transparency shows, and its name beside it.
    ///     <para>
    ///         A brush that comes from a constant has no colour to show until the project is run, so
    ///         only the constant itself is named.
    ///     </para>
    /// </summary>
    public class BrushAndNameControl : UserControl
    {
        #region construction and destruction

        public BrushAndNameControl(DsBrushBase dsBrush)
        {
            var border = new Border
            {
                Background = CheckerBrush,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(2, 1, 4, 1),
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 20,
                Height = 20
            };
            var rectangle = new Rectangle();
            border.Child = rectangle;

            var textBlock = new TextBlock { VerticalAlignment = VerticalAlignment.Center };

            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            stackPanel.Children.Add(border);
            stackPanel.Children.Add(textBlock);
            Content = stackPanel;

            var solidDsBrush = dsBrush as SolidDsBrush;
            if (solidDsBrush is not null && ConstantsHelper.ContainsQuery(solidDsBrush.ColorString))
            {
                border.IsVisible = false;
                textBlock.Text = solidDsBrush.ColorString;
                return;
            }

            var blinkingDsBrush = dsBrush as BlinkingDsBrush;
            if (blinkingDsBrush is not null &&
                (ConstantsHelper.ContainsQuery(blinkingDsBrush.FirstColorString) ||
                 ConstantsHelper.ContainsQuery(blinkingDsBrush.SecondColorString)))
            {
                border.IsVisible = false;
                textBlock.Text = blinkingDsBrush.FirstColorString + @";" + blinkingDsBrush.SecondColorString;
                return;
            }

            if (solidDsBrush is not null)
                textBlock.Text = ObsoleteAnyHelper.ConvertTo<string>(solidDsBrush.Color, false);

            rectangle.Fill = dsBrush.GetBrush(null);
        }

        #endregion

        #region public functions

        /// <summary>
        ///     The chequerboard a partly transparent brush is shown over.
        /// </summary>
        public static IBrush CheckerBrush { get; } = CreateCheckerBrush();

        #endregion

        #region private functions

        private static IBrush CreateCheckerBrush()
        {
            var drawingGroup = new DrawingGroup();
            drawingGroup.Children.Add(new GeometryDrawing
            {
                Brush = Brushes.White,
                Geometry = new RectangleGeometry(new Rect(0, 0, 100, 100))
            });

            var darkSquares = new GeometryGroup();
            darkSquares.Children.Add(new RectangleGeometry(new Rect(0, 0, 50, 50)));
            darkSquares.Children.Add(new RectangleGeometry(new Rect(50, 50, 50, 50)));
            drawingGroup.Children.Add(new GeometryDrawing
            {
                Brush = Brushes.LightGray,
                Geometry = darkSquares
            });

            return new DrawingBrush(drawingGroup)
            {
                DestinationRect = new RelativeRect(0, 0, 10, 10, RelativeUnit.Absolute),
                TileMode = TileMode.Tile
            };
        }

        #endregion
    }
}
