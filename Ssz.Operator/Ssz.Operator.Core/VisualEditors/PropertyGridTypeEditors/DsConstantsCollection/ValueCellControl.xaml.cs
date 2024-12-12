using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors.DsConstantsCollection
{
    public partial class ValueCellControl : Grid
    {
        #region public functions

        public static readonly DependencyProperty ValueTypeProperty = DependencyProperty.Register(
            "ValueType",
            typeof(string),
            typeof(ValueCellControl),
            new FrameworkPropertyMetadata(null, OnValueTypePropertyChanged));

        #endregion

        #region construction and destruction

        public ValueCellControl()
        {
            InitializeComponent();

            _backgroundHintTextBlock = new TextBlock();
            _backgroundHintTextBlock.Foreground = Brushes.Gray;
            _backgroundHintTextBlock.Text = Properties.Resources.ConstantValueIsInherited;
            _backgroundHintTextBlock.SetBinding(VisibilityProperty, new Binding
            {
                Path = new PropertyPath(@"BackgroundHintVisibility")
            });

            SetBinding(ValueTypeProperty, new Binding
            {
                Path = new PropertyPath(@"Type")
            });
        }

        #endregion

        #region private functions

        private static void OnValueTypePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ValueCellControl) d).OnValueTypePropertyChanged(e.NewValue as string);
        }

        private void OnValueTypePropertyChanged(string? newValue)
        {
            if (newValue is null) newValue = "";
            switch ((newValue ?? @"").ToUpperInvariant())
            {
                case "COLOR":
                    if (_colorCellControl is null) _colorCellControl = new ColorCellControl();
                    Children.Clear();
                    Children.Add(_backgroundHintTextBlock);
                    Children.Add(_colorCellControl);
                    break;
                default:
                    if (_mainTextBlock is null)
                    {
                        _mainTextBlock = new TextBlock();
                        _mainTextBlock.SetBinding(TextBlock.TextProperty, new Binding
                        {
                            Path = new PropertyPath("Value")
                        });
                        _mainTextBlock.SetBinding(TextBlock.ForegroundProperty, new Binding
                        {
                            Path = new PropertyPath("Foreground")
                        });
                        _mainTextBlock.SetBinding(ToolTipProperty, new Binding
                        {
                            Path = new PropertyPath("ToolTip")
                        });
                    }

                    Children.Clear();
                    Children.Add(_backgroundHintTextBlock);
                    Children.Add(_mainTextBlock);
                    break;
            }
        }

        #endregion

        #region private fields

        private readonly TextBlock _backgroundHintTextBlock;
        private ColorCellControl? _colorCellControl;
        private TextBlock? _mainTextBlock;

        #endregion
    }
}