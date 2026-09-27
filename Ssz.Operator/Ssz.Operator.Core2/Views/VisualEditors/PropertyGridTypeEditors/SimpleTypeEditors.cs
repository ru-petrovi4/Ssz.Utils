using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors
{
    /// <summary>
    ///     A property edited as text, which is what most of them are.
    /// </summary>
    public class TextBoxEditor : UserControl, ITypeEditor
    {
        #region public functions

        public Control ResolveEditor(PropertyItemViewModel propertyItem)
        {
            DataContext = propertyItem;

            var textBox = new TextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 120,
                IsReadOnly = propertyItem.IsReadOnly
            };
            textBox.Bind(TextBox.TextProperty, new Binding(nameof(PropertyItemViewModel.ValueAsString))
            {
                Source = propertyItem,
                Mode = BindingMode.TwoWay
            });

            Content = textBox;
            return this;
        }

        #endregion
    }

    /// <summary>
    ///     A property the author may read but not change.
    /// </summary>
    public class TextBlockEditor : UserControl, ITypeEditor
    {
        #region public functions

        public Control ResolveEditor(PropertyItemViewModel propertyItem)
        {
            DataContext = propertyItem;

            var textBox = new TextBox
            {
                BorderThickness = new Thickness(0),
                IsReadOnly = true,
                Foreground = Brushes.Gray
            };
            textBox.Bind(TextBox.TextProperty, new Binding(nameof(PropertyItemViewModel.ValueAsString))
            {
                Source = propertyItem,
                Mode = BindingMode.OneWay
            });

            Content = textBox;
            return this;
        }

        #endregion
    }

    /// <summary>
    ///     A property that is either on or off.
    /// </summary>
    public class CheckBoxEditor : UserControl, ITypeEditor
    {
        #region public functions

        public Control ResolveEditor(PropertyItemViewModel propertyItem)
        {
            DataContext = propertyItem;

            var checkBox = new CheckBox
            {
                Margin = new Thickness(5, 0, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                IsEnabled = !propertyItem.IsReadOnly
            };
            checkBox.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(PropertyItemViewModel.Value))
            {
                Source = propertyItem,
                Mode = BindingMode.TwoWay
            });

            Content = checkBox;
            return this;
        }

        #endregion
    }

    /// <summary>
    ///     A plain colour, picked from the wheel.
    /// </summary>
    public class ColorEditor : UserControl, ITypeEditor
    {
        #region public functions

        public Control ResolveEditor(PropertyItemViewModel propertyItem)
        {
            DataContext = propertyItem;

            var colorPicker = new ColorPicker
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                IsEnabled = !propertyItem.IsReadOnly
            };
            colorPicker.Bind(ColorPicker.ColorProperty, new Binding(nameof(PropertyItemViewModel.Value))
            {
                Source = propertyItem,
                Mode = BindingMode.TwoWay
            });

            Content = colorPicker;
            return this;
        }

        #endregion
    }
}
