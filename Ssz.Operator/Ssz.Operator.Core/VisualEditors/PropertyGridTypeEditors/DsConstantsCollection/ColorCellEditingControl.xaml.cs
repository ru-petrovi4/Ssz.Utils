using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Ssz.Operator.Core.Constants;


using Ssz.Operator.Core.VisualEditors.ValueConverters;
using Ssz.Operator.Core.VisualEditors.Windows;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors.DsConstantsCollection
{
    public partial class ColorCellEditingControl : UserControl
    {
        #region construction and destruction

        public ColorCellEditingControl()
        {
            InitializeComponent();

            SetBinding(DsBrushProperty, new Binding
            {
                Path = new PropertyPath("Value"),
                Converter =
                    StringToSolidDsBrushConverter
                        .Instance
            });

            MainButton.SetBinding(ContentProperty, new Binding
            {
                Path = new PropertyPath(nameof(DsBrush)),
                Source = this,
                Converter =
                    DsBrushToContentConverter
                        .Instance
            });
        }

        #endregion

        #region private functions

        private void OnButtonClick(object? sender, RoutedEventArgs e)
        {
            var dialog = new BrushEditorDialog
            {
                Owner = Window.GetWindow(this),
                DsBrush = DsBrush
            };

            dialog.MainControl.GradientBrushTabItem.Visibility = Visibility.Collapsed;
            dialog.MainControl.BlinkingBrushTabItem.Visibility = Visibility.Collapsed;
            dialog.MainControl.ParamNumBrushTabItem.Visibility = Visibility.Collapsed;

            if (dialog.ShowDialog() == true)
            {
                string? dsBrushString = null;
                var dsBrush = dialog.DsBrush as DsBrushBase;
                if (dsBrush is not null)
                    dsBrushString = DsBrushValueSerializer.Instance.ConvertToString(dsBrush, null);
                ((DsConstantViewModel) DataContext).Value = dsBrushString ?? "";
            }
        }

        #endregion

        #region public functions

        public static readonly DependencyProperty DsBrushProperty = DependencyProperty.Register(
            "DsBrush",
            typeof(DsBrushBase),
            typeof(ColorCellEditingControl));

        public DsBrushBase DsBrush
        {
            get => (DsBrushBase) GetValue(DsBrushProperty);
            set => SetValue(DsBrushProperty, value);
        }

        #endregion
    }
}