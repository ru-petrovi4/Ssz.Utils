using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Binding = System.Windows.Data.Binding;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using UserControl = System.Windows.Controls.UserControl;

namespace Ssz.Operator.Core.ControlsCommon.Trends
{
    public partial class TrendsInfoTableControl : UserControl
    {
        #region construction and destruction

        public TrendsInfoTableControl()
        {
            InitializeComponent();

            MainDataGrid.SetBinding(Selector.SelectedItemProperty,
                new Binding {Source = this, Path = new PropertyPath("SelectedItem")});
        }

        #endregion

        #region public functions

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register("SelectedItem",
            typeof(object),
            typeof(
                TrendsInfoTableControl));

        public ObservableCollection<Trend>? TrendItemViewsCollection
        {
            get => MainDataGrid.ItemsSource as ObservableCollection<Trend>;
            set
            {
                MainDataGrid.ItemsSource = value;
                if (MainDataGrid.Items.Count > 0)
                    MainDataGrid.SelectedIndex = 0;
            }
        }

        public object? SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        #endregion

        #region private functions

        private void CheckBoxOnPreviewMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            if (sender is null) return;

            var checkBox = (CheckBox) sender;
            checkBox.IsChecked = !checkBox.IsChecked;

            BindingExpression bindingExpression = checkBox.GetBindingExpression(ToggleButton.IsCheckedProperty);
            if (bindingExpression is not null)
                bindingExpression.UpdateSource();

            e.Handled = true;
        }

        private void ChooseColorButtonClick(object? sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            using (var colorDialog = new ColorDialog())
            {
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    var color = colorDialog.Color;
                    if (btn is not null)
                    {
                        btn.Background =
                            new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));

                        BindingExpression bindingExpression = btn.GetBindingExpression(BackgroundProperty);
                        if (bindingExpression is not null)
                            bindingExpression.UpdateSource();
                    }
                }
            }
        }

        #endregion
    }
}