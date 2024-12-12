using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Ssz.Operator.Core.MultiValueConverters;
using Ssz.Utils.Wpf.WpfMessageBox;

namespace Ssz.Operator.Core.VisualEditors
{
    public partial class XamlConverterControl : UserControl
    {
        #region private fields

        private readonly ObservableCollection<StatementViewModel> _dataSourceToUiStatementViewModels =
            new();

        #endregion

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
                DataSourceToUiConverterDataGrid.CommitEdit();

                if (_dataSourceToUiStatementViewModels.Count == 0) return null;

                var resultValueConverter = new XamlConverter();
                foreach (var s in _dataSourceToUiStatementViewModels.Select(vm =>
                {
                    var xamlStatement = new XamlStatement(true);
                    xamlStatement.Condition.ExpressionString = vm.Condition.ExpressionString;
                    var constXaml = vm.ConstXaml;
                    if (constXaml is not null)
                    {
                        var constXamlClone = (DsXaml) constXaml.Clone();
                        constXamlClone.ParentItem = constXaml.ParentItem;
                        constXaml = constXamlClone;
                    }

                    xamlStatement.ConstXaml = constXaml ?? new DsXaml();
                    return xamlStatement;
                }))
                    resultValueConverter.DataSourceToUiStatements.Add(s);

                return resultValueConverter;
            }
            set
            {
                var originalValueConverter = value as XamlConverter;
                if (originalValueConverter is not null)
                    foreach (XamlStatement statement in originalValueConverter.DataSourceToUiStatements)
                        _dataSourceToUiStatementViewModels.Add(new StatementViewModel(statement));
            }
        }

        #endregion

        #region private functions

        private void DataSourceToUiNewButtonClick(object? sender, RoutedEventArgs e)
        {
            DataSourceToUiConverterDataGrid.CommitEdit();
            var vm = new StatementViewModel((XamlStatement?) null)
            {
                Condition = {ExpressionString = "true"}
            };
            _dataSourceToUiStatementViewModels.Add(vm);
        }

        private void DataSourceToUiDeleteButtonClick(object? sender, RoutedEventArgs e)
        {
            if (DataSourceToUiCurentIndex >= 0)
                _dataSourceToUiStatementViewModels.RemoveAt(DataSourceToUiCurentIndex);
        }

        private void DataSourceToUiDownButtonClick(object? sender, RoutedEventArgs e)
        {
            DataSourceToUiConverterDataGrid.CommitEdit();
            var idx = DataSourceToUiCurentIndex;
            if (idx >= 0 && idx != _dataSourceToUiStatementViewModels.Count - 1)
                _dataSourceToUiStatementViewModels.Move(idx, idx + 1);
        }

        private void DataSourceToUiUpButtonClick(object? sender, RoutedEventArgs e)
        {
            DataSourceToUiConverterDataGrid.CommitEdit();
            var idx = DataSourceToUiCurentIndex;
            if (idx > 0)
                _dataSourceToUiStatementViewModels.Move(idx, idx - 1);
        }

        private void HelpButtonOnClick(object? sender, RoutedEventArgs e)
        {
            WpfMessageBox.Show(Properties.Resources.ConverterWindowHelp);
        }

        private void ClearButtonOnClick(object? sender, RoutedEventArgs e)
        {
            var messageBoxResult = WpfMessageBox.Show(Window.GetWindow(this),
                Properties.Resources.MessageAreYouSureToClearAllQuestion,
                Properties.Resources.QuestionMessageBoxCaption,
                WpfMessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);
            if (messageBoxResult != WpfMessageBoxResult.Yes) return;

            DataSourceToUiConverterDataGrid.CommitEdit();

            _dataSourceToUiStatementViewModels.Clear();
        }

        private void SaveButtonOnClick(object? sender, RoutedEventArgs e)
        {
            DataSourceToUiConverterDataGrid.CommitEdit();

            if (_dataSourceToUiStatementViewModels.Count == 0) return;
            var converter = new XamlConverter();
            foreach (var s in _dataSourceToUiStatementViewModels.Select(vm =>
            {
                var xamlStatement = new XamlStatement(true);
                xamlStatement.Condition.ExpressionString = vm.Condition.ExpressionString;
                var constXaml = vm.ConstXaml;
                if (constXaml is not null)
                {
                    var constXamlClone = (DsXaml) constXaml.Clone();
                    constXamlClone.ParentItem = constXaml.ParentItem;
                    constXaml = constXamlClone;
                }

                xamlStatement.ConstXaml = constXaml ?? new DsXaml();
                return xamlStatement;
            }))
                converter.DataSourceToUiStatements.Add(s);

            DsProject.Instance.ExportObjectToXaml(converter);
        }

        private void LoadButtonOnClick(object? sender, RoutedEventArgs e)
        {
            var converter = DsProject.Instance.ImportObjectFromXaml() as XamlConverter;
            if (converter is not null)
            {
                _dataSourceToUiStatementViewModels.Clear();
                foreach (XamlStatement statement in converter.DataSourceToUiStatements)
                    _dataSourceToUiStatementViewModels.Add(new StatementViewModel(statement));
            }
        }

        private void DataSourceToUiConverterDataGridCurrentCellChanged(object? sender, EventArgs e)
        {
            DataSourceToUiConverterDataGrid.CommitEdit();
        }

        private void DataSourceToUiConverterDataGridLostFocus(object? sender, RoutedEventArgs e)
        {
        }

        private int DataSourceToUiCurentIndex
        {
            get
            {
                if (DataSourceToUiConverterDataGrid.SelectedCells.Count > 0)
                    return
                        _dataSourceToUiStatementViewModels.IndexOf(
                            (StatementViewModel) DataSourceToUiConverterDataGrid.SelectedCells.First().Item);
                return -1;
            }
        }

        #endregion
    }
}