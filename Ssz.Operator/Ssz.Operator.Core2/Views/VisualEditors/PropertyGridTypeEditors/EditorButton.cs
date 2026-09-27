using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors
{
    /// <summary>
    ///     The button a property row shows when its value is edited in a dialog: the value itself on
    ///     the left, and on the right the arrow that says a dialog opens.
    ///     <para>
    ///         Ported from the WPF editor's EditorButtonStyle.
    ///     </para>
    /// </summary>
    public class EditorButton : Button
    {
        #region construction and destruction

        public EditorButton()
        {
            MinHeight = 22;
            // The WPF button was painted white, which is also what makes the whole of it answer to
            // the pointer rather than only what is drawn on it.
            Background = Brushes.White;
            Foreground = Brushes.Black;
            Padding = new Thickness(2, 0, 0, 0);
            BorderThickness = new Thickness(0);
            HorizontalAlignment = HorizontalAlignment.Stretch;
            HorizontalContentAlignment = HorizontalAlignment.Left;
            VerticalContentAlignment = VerticalAlignment.Center;

            Template = new FuncControlTemplate<EditorButton>((button, scope) =>
            {
                var grid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions(@"*,Auto")
                };

                var border = new Border
                {
                    Name = @"PART_Border",
                    [!BackgroundProperty] = button[!BackgroundProperty],
                    [!BorderBrushProperty] = button[!BorderBrushProperty],
                    [!BorderThicknessProperty] = button[!BorderThicknessProperty],
                    [!PaddingProperty] = button[!PaddingProperty]
                };
                var contentPresenter = new ContentPresenter
                {
                    Name = @"PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = button[!ContentProperty],
                    [!ContentPresenter.ContentTemplateProperty] = button[!ContentTemplateProperty],
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };
                border.Child = contentPresenter;
                contentPresenter.RegisterInNameScope(scope);
                border.RegisterInNameScope(scope);
                grid.Children.Add(border);

                // The same little arrow the WPF button carried, drawn as its path.
                var arrow = new Path
                {
                    Width = 7,
                    Height = 4,
                    Margin = new Thickness(5),
                    Fill = Brushes.Black,
                    VerticalAlignment = VerticalAlignment.Center,
                    Data = Geometry.Parse(ArrowGeometry)
                };
                Grid.SetColumn(arrow, 1);
                grid.Children.Add(arrow);

                return grid;
            });
        }

        #endregion

        #region private fields

        private const string ArrowGeometry =
            @"M 0,1 C0,1 0,0 0,0 0,0 3,0 3,0 3,0 3,1 3,1 3,1 4,1 4,1 4,1 4,0 4,0 4,0 7,0 7,0 7,0 7,1 7,1 " +
            @"7,1 6,1 6,1 6,1 6,2 6,2 6,2 5,2 5,2 5,2 5,3 5,3 5,3 4,3 4,3 4,3 4,4 4,4 4,4 3,4 3,4 3,4 3,3 " +
            @"3,3 3,3 2,3 2,3 2,3 2,2 2,2 2,2 1,2 1,2 1,2 1,1 1,1 1,1 0,1 0,1 z";

        #endregion
    }
}
