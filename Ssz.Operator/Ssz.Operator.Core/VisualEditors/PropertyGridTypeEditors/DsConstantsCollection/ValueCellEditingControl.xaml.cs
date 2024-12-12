using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors.DsConstantsCollection
{
    public partial class ValueCellEditingControl : UserControl
    {
        #region public functions

        public static readonly DependencyProperty ValueTypeProperty = DependencyProperty.Register(
            "ValueType",
            typeof(string),
            typeof(ValueCellEditingControl),
            new FrameworkPropertyMetadata(OnValueTypePropertyChanged));

        #endregion

        #region construction and destruction

        public ValueCellEditingControl()
        {
            InitializeComponent();

            OnValueTypePropertyChanged(null);

            SetBinding(ValueTypeProperty, new Binding
            {
                Path = new PropertyPath("Type")
            });
        }

        #endregion

        #region private functions

        private static void OnValueTypePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ValueCellEditingControl) d).OnValueTypePropertyChanged(e.NewValue as string);
        }

        private void OnValueTypePropertyChanged(string? newValue)
        {
            if (newValue is null) newValue = "";
            switch (newValue.ToUpperInvariant())
            {
                case "COLOR":
                    if (_colorCellEditingControl is null)
                        _colorCellEditingControl = new ColorCellEditingControl();
                    Content = _colorCellEditingControl;
                    break;
                default:
                    if (_filteredComboBoxCellEditingControl is null)
                        _filteredComboBoxCellEditingControl = new FilteredComboBoxCellEditingControl();
                    Content = _filteredComboBoxCellEditingControl;
                    break;
            }
        }

        #endregion

        #region private fields

        private ColorCellEditingControl? _colorCellEditingControl;
        private FilteredComboBoxCellEditingControl? _filteredComboBoxCellEditingControl;

        #endregion
    }
}