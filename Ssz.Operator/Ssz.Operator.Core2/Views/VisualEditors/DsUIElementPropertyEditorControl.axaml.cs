using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using Ssz.Utils;
using Res = Ssz.Operator.Core.Properties.Resources;

namespace Ssz.Operator.Core.VisualEditors
{
    /// <summary>
    ///     How a control of a shape looks: one of the styles the shape offers, or XAML the author
    ///     writes themselves.
    ///     <para>
    ///         Ported from the WPF editor. Picking a style over XAML the author wrote asks first, as
    ///         it did there; the answer is awaited and the list put back when it is no.
    ///     </para>
    /// </summary>
    public partial class DsUIElementPropertyEditorControl : UserControl
    {
        #region construction and destruction

        /// <summary>
        ///     For using in the previewer.
        /// </summary>
        public DsUIElementPropertyEditorControl()
        {
            InitializeComponent();

            _propertyInfoSupplier = null!;
        }

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
                _propertyInfo.TypeString = _propertyInfoSupplier.GetTypeString(MainTextBox.Text ?? @"");

                if (StringHelper.CompareIgnoreCase(_propertyInfo.TypeString,
                        DsUIElementPropertySupplier.CustomTypeString))
                    _propertyInfo.CustomXamlString = MainTextBox.Text ?? @"";
                else
                    _propertyInfo.CustomXamlString = @"";

                return _propertyInfo;
            }
            set
            {
                _propertyInfo = value;

                _disableControlsChangedHandlers = true;
                MainComboBox.ItemsSource = _propertyInfoSupplier.GetTypesStrings();
                MainComboBox.SelectedItem = value.TypeString;
                MainTextBox.Text = _propertyInfoSupplier.GetPropertyXamlString(_propertyInfo, null);
                _disableControlsChangedHandlers = false;
            }
        }

        #endregion

        #region private functions

        private void MainTextBoxOnTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (_disableControlsChangedHandlers) return;

            _disableControlsChangedHandlers = true;
            MainComboBox.SelectedItem = DsUIElementPropertySupplier.CustomTypeString;
            _disableControlsChangedHandlers = false;
        }

        private async void MainComboBoxOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_disableControlsChangedHandlers) return;

            var newStyleType = e.AddedItems.OfType<string>().FirstOrDefault();
            if (String.IsNullOrWhiteSpace(newStyleType)) return;

            var oldStyleType = e.RemovedItems.OfType<string>().FirstOrDefault();

            if (StringHelper.CompareIgnoreCase(_propertyInfo.TypeString,
                    DsUIElementPropertySupplier.CustomTypeString) &&
                !String.IsNullOrWhiteSpace(MainTextBox.Text))
                if (!await MessageBoxHelper.AskYesNoAsync(
                        Res.ChangeControlStyleQuestion + @" " + newStyleType + @"?"))
                {
                    // What the author wrote stays, and the list says so again.
                    Dispatcher.UIThread.Post(() =>
                    {
                        _disableControlsChangedHandlers = true;
                        MainComboBox.SelectedItem = DsUIElementPropertySupplier.CustomTypeString;
                        _disableControlsChangedHandlers = false;
                    });
                    return;
                }

            _disableControlsChangedHandlers = true;

            if (StringHelper.CompareIgnoreCase(oldStyleType, DsUIElementPropertySupplier.CustomTypeString) &&
                !StringHelper.CompareIgnoreCase(newStyleType, DsUIElementPropertySupplier.CustomTypeString))
                _propertyInfo.CustomXamlString = MainTextBox.Text ?? @"";

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
