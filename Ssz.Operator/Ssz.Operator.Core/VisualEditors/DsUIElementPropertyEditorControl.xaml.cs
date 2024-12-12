using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;


using Ssz.Utils.Wpf.WpfMessageBox;
using Ssz.Utils;

namespace Ssz.Operator.Core.VisualEditors
{
    public partial class DsUIElementPropertyEditorControl : UserControl
    {
        #region construction and destruction

        public DsUIElementPropertyEditorControl(Type propertyInfoSupplierType)
        {
            InitializeComponent();

            _propertyInfoSupplier =
                Activator.CreateInstance(propertyInfoSupplierType) as DsUIElementPropertySupplier ??
                throw new InvalidOperationException();

            MainComboBox.SelectionChanged += MainComboBoxOnSelectionChanged;
            MainTextBox.TextChanged += MainTextBoxOnTextChanged;
        }

        #endregion

        #region public functions

        public DsUIElementProperty StyleInfo
        {
            get
            {
                _propertyInfo.TypeString = _propertyInfoSupplier.GetTypeString(MainTextBox.Text);
                if (StringHelper.CompareIgnoreCase(_propertyInfo.TypeString,
                    DsUIElementPropertySupplier.CustomTypeString))
                    _propertyInfo.CustomXamlString = MainTextBox.Text;
                else
                    _propertyInfo.CustomXamlString = "";

                return _propertyInfo;
            }
            set
            {
                _propertyInfo = value;

                _disableControlsChangedHandlers = true;

                MainComboBox.ItemsSource = _propertyInfoSupplier.GetTypesStrings();

                MainComboBox.Text = value.TypeString;
                MainTextBox.Text = _propertyInfoSupplier.GetPropertyXamlString(_propertyInfo, null);

                _disableControlsChangedHandlers = false;
            }
        }

        #endregion

        #region private functions

        private void MainTextBoxOnTextChanged(object? sender, TextChangedEventArgs textChangedEventArgs)
        {
            if (_disableControlsChangedHandlers) return;

            _disableControlsChangedHandlers = true;

            MainComboBox.Text = DsUIElementPropertySupplier.CustomTypeString;

            _disableControlsChangedHandlers = false;
        }

        private void MainComboBoxOnSelectionChanged(object? sender, SelectionChangedEventArgs selectionChangedEventArgs)
        {
            if (_disableControlsChangedHandlers) return;

            _disableControlsChangedHandlers = true;

            var newStyleType = (selectionChangedEventArgs.AddedItems ?? throw new InvalidOperationException())
                .OfType<string>().FirstOrDefault();
            if (string.IsNullOrWhiteSpace(newStyleType)) return;
            var oldStyleType = (selectionChangedEventArgs.RemovedItems ?? throw new InvalidOperationException())
                .OfType<string>().FirstOrDefault();

            if (StringHelper.CompareIgnoreCase(_propertyInfo.TypeString,
                DsUIElementPropertySupplier.CustomTypeString) && !string.IsNullOrWhiteSpace(MainTextBox.Text))
            {
                var messageBoxResult = WpfMessageBox.Show(Window.GetWindow(this),
                    Properties.Resources.ChangeControlStyleQuestion + " " + newStyleType + "?",
                    Properties.Resources.QuestionMessageBoxCaption,
                    WpfMessageBoxButton.OKCancel,
                    MessageBoxImage.Question);
                if (messageBoxResult != WpfMessageBoxResult.OK)
                {
                    Dispatcher.BeginInvoke(
                        (ThreadStart) delegate { MainComboBox.Text = DsUIElementPropertySupplier.CustomTypeString; });
                    return;
                }
            }

            if (StringHelper.CompareIgnoreCase(oldStyleType, DsUIElementPropertySupplier.CustomTypeString) &&
                !StringHelper.CompareIgnoreCase(newStyleType, DsUIElementPropertySupplier.CustomTypeString))
                _propertyInfo.CustomXamlString = MainTextBox.Text;

            _propertyInfo.TypeString = newStyleType;
            MainTextBox.Text = _propertyInfoSupplier.GetPropertyXamlString(_propertyInfo, null);

            _disableControlsChangedHandlers = false;
        }

        #endregion

        #region private fields

        private readonly DsUIElementPropertySupplier _propertyInfoSupplier;
        private bool _disableControlsChangedHandlers;

        private DsUIElementProperty _propertyInfo = new();

        #endregion
    }
}