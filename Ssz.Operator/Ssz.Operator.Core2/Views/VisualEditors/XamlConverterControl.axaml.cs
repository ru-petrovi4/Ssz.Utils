using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Ssz.Operator.Core.MultiValueConverters;
using Res = Ssz.Operator.Core.Properties.Resources;

namespace Ssz.Operator.Core.VisualEditors
{
    /// <summary>
    ///     Writing the rules that decide which piece of content a shape shows for what the data source
    ///     says. Ported from the WPF editor.
    /// </summary>
    public partial class XamlConverterControl : UserControl
    {
        #region construction and destruction

        public XamlConverterControl()
        {
            InitializeComponent();

            DataSourceToUiConverterDataGrid.ItemsSource = _dataSourceToUiStatementViewModels;
        }

        #endregion

        #region public functions

        public ValueConverterBase? XamlConverter
        {
            get
            {
                if (_dataSourceToUiStatementViewModels.Count == 0) return null;

                var resultValueConverter = new XamlConverter();
                foreach (XamlStatement statement in _dataSourceToUiStatementViewModels.Select(ToStatement))
                    resultValueConverter.DataSourceToUiStatements.Add(statement);
                return resultValueConverter;
            }
            set
            {
                var originalValueConverter = value as XamlConverter;
                if (originalValueConverter is null) return;

                foreach (XamlStatement statement in originalValueConverter.DataSourceToUiStatements)
                    _dataSourceToUiStatementViewModels.Add(new StatementViewModel(statement));
            }
        }

        #endregion

        #region private functions

        /// <summary>
        ///     The content of a rule is copied out, so that the rule and the row that made it do not
        ///     go on sharing it.
        /// </summary>
        private static XamlStatement ToStatement(StatementViewModel vm)
        {
            var xamlStatement = new XamlStatement(true);
            xamlStatement.Condition.ExpressionString = vm.Condition.ExpressionString;

            DsXaml? constXaml = vm.ConstXaml;
            if (constXaml is not null)
            {
                var constXamlClone = (DsXaml) constXaml.Clone();
                constXamlClone.ParentItem = constXaml.ParentItem;
                constXaml = constXamlClone;
            }

            xamlStatement.ConstXaml = constXaml ?? new DsXaml();
            return xamlStatement;
        }

        private void DataSourceToUiNewButtonClick(object? sender, RoutedEventArgs e)
        {
            _dataSourceToUiStatementViewModels.Add(new StatementViewModel((XamlStatement?) null)
            {
                Condition = { ExpressionString = @"true" }
            });
        }

        private void DataSourceToUiDeleteButtonClick(object? sender, RoutedEventArgs e)
        {
            if (DataSourceToUiCurrentIndex >= 0)
                _dataSourceToUiStatementViewModels.RemoveAt(DataSourceToUiCurrentIndex);
        }

        private void DataSourceToUiDownButtonClick(object? sender, RoutedEventArgs e)
        {
            var idx = DataSourceToUiCurrentIndex;
            if (idx >= 0 && idx != _dataSourceToUiStatementViewModels.Count - 1)
                _dataSourceToUiStatementViewModels.Move(idx, idx + 1);
        }

        private void DataSourceToUiUpButtonClick(object? sender, RoutedEventArgs e)
        {
            var idx = DataSourceToUiCurrentIndex;
            if (idx > 0) _dataSourceToUiStatementViewModels.Move(idx, idx - 1);
        }

        private void HelpButtonOnClick(object? sender, RoutedEventArgs e)
        {
            MessageBoxHelper.ShowInfo(Res.ConverterWindowHelp);
        }

        private async void ClearButtonOnClick(object? sender, RoutedEventArgs e)
        {
            if (await MessageBoxHelper.AskYesNoCancelAsync(
                    Res.MessageAreYouSureToClearAllQuestion) != true) return;

            _dataSourceToUiStatementViewModels.Clear();
        }

        private void SaveButtonOnClick(object? sender, RoutedEventArgs e)
        {
            if (_dataSourceToUiStatementViewModels.Count == 0) return;

            var converter = new XamlConverter();
            foreach (XamlStatement statement in _dataSourceToUiStatementViewModels.Select(ToStatement))
                converter.DataSourceToUiStatements.Add(statement);

            DsProject.Instance.ExportObjectToXaml(converter);
        }

        private void LoadButtonOnClick(object? sender, RoutedEventArgs e)
        {
            var converter = DsProject.Instance.ImportObjectFromXaml() as XamlConverter;
            if (converter is null) return;

            _dataSourceToUiStatementViewModels.Clear();
            foreach (XamlStatement statement in converter.DataSourceToUiStatements)
                _dataSourceToUiStatementViewModels.Add(new StatementViewModel(statement));
        }

        private int DataSourceToUiCurrentIndex =>
            DataSourceToUiConverterDataGrid.SelectedItem is StatementViewModel vm
                ? _dataSourceToUiStatementViewModels.IndexOf(vm)
                : -1;

        #endregion

        #region private fields

        private readonly ObservableCollection<StatementViewModel> _dataSourceToUiStatementViewModels = new();

        #endregion
    }
}
