using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Ssz.Operator.Core.MultiValueConverters;
using Ssz.Utils.Wpf.WpfMessageBox;

namespace Ssz.Operator.Core.VisualEditors
{
    public partial class StructConverterControl : UserControl
    {
        #region construction and destruction

        public StructConverterControl(bool showDataSourceToUiGrid, bool showUiToDataSourceGrid)
        {
            InitializeComponent();

            DataSourceToUiConverterDataGrid.ItemsSource = _dataSourceToUiStatementViewModels;
            UiToDataSourceConverterDataGrid.ItemsSource = _uiToDataSourceStatementViewModels;

            if (!showDataSourceToUiGrid) DataSourceToUiGrid.IsEnabled = false;
            if (!showUiToDataSourceGrid) UiToDataSourceGrid.IsEnabled = false;
        }

        #endregion

        #region public functions

        public ValueConverterBase? LocalizedConverter
        {
            get
            {
                DataSourceToUiConverterDataGrid.CommitEdit();

                if (_dataSourceToUiStatementViewModels.Count == 0 && _uiToDataSourceStatementViewModels.Count == 0)
                {
                    return null;
                }

                var resultLocalizedConverter = new LocalizedConverter();
                resultLocalizedConverter.DataSourceToUiStatements.AddRange(
                    _dataSourceToUiStatementViewModels.Select(vm => new TextStatement(vm)));
                resultLocalizedConverter.UiToDataSourceStatements.AddRange(
                    _uiToDataSourceStatementViewModels.Select(vm => new TextStatement(vm)));
                return resultLocalizedConverter;
            }
            set
            {
                var originalLocalizedConverter = value as LocalizedConverter;
                if (originalLocalizedConverter is not null)
                {
                    foreach (TextStatement statement in originalLocalizedConverter.DataSourceToUiStatements)
                        _dataSourceToUiStatementViewModels.Add(new StatementViewModel(statement));
                    foreach (TextStatement statement in originalLocalizedConverter.UiToDataSourceStatements)
                        _uiToDataSourceStatementViewModels.Add(new StatementViewModel(statement));
                }
            }
        }

        #endregion

        #region private functions

        private void DataSourceToUiNewButtonClick(object? sender, RoutedEventArgs e)
        {
            DataSourceToUiConverterDataGrid.CommitEdit();
            var vm = new StatementViewModel((TextStatement?) null)
            {
                Condition = {ExpressionString = @"true"}
            };
            if (vm.Value is null) throw new InvalidOperationException();
            vm.Value.ExpressionString = @"d[0]";
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

        private void DataSourceToUiConverterDataGridCurrentCellChanged(object? sender, EventArgs e)
        {
            DataSourceToUiConverterDataGrid.CommitEdit();
        }

        private void DataSourceToUiConverterDataGridLostFocus(object? sender, RoutedEventArgs e)
        {
        }

        private void UiToDataSourceNewButtonClick(object? sender, RoutedEventArgs e)
        {
            UiToDataSourceConverterDataGrid.CommitEdit();
            var vm = new StatementViewModel((TextStatement?) null)
            {
                Condition = {ExpressionString = "true"}
            };
            if (vm.Value is null) throw new InvalidOperationException();
            vm.Value.ExpressionString = @"0";
            _uiToDataSourceStatementViewModels.Add(vm);
        }

        private void UiToDataSourceDeleteButtonClick(object? sender, RoutedEventArgs e)
        {
            if (UiToDataSourceCurentIndex >= 0)
                _uiToDataSourceStatementViewModels.RemoveAt(UiToDataSourceCurentIndex);
        }

        private void UiToDataSourceDownButtonClick(object? sender, RoutedEventArgs e)
        {
            UiToDataSourceConverterDataGrid.CommitEdit();
            var idx = UiToDataSourceCurentIndex;
            if (idx >= 0 && idx != _uiToDataSourceStatementViewModels.Count - 1)
                _uiToDataSourceStatementViewModels.Move(idx, idx + 1);
        }

        private void UiToDataSourceUpButtonClick(object? sender, RoutedEventArgs e)
        {
            UiToDataSourceConverterDataGrid.CommitEdit();
            var idx = UiToDataSourceCurentIndex;
            if (idx > 0)
                _uiToDataSourceStatementViewModels.Move(idx, idx - 1);
        }

        private void HelpButtonOnClick(object? sender, RoutedEventArgs e)
        {
            WpfMessageBox.Show(
                Properties.Resources.ConverterWindowHelp + "\n\n" + Properties.Resources.ConverterWindowHelp2,
                Properties.Resources.ConverterHelp);
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
            _uiToDataSourceStatementViewModels.Clear();
        }

        private void SaveButtonOnClick(object? sender, RoutedEventArgs e)
        {
            DataSourceToUiConverterDataGrid.CommitEdit();

            if (_dataSourceToUiStatementViewModels.Count == 0 && _uiToDataSourceStatementViewModels.Count == 0)
            {
            }

            var localizedConverter = new LocalizedConverter();
            localizedConverter.DataSourceToUiStatements.AddRange(
                _dataSourceToUiStatementViewModels.Select(vm => new TextStatement(vm)));
            localizedConverter.UiToDataSourceStatements.AddRange(
                _uiToDataSourceStatementViewModels.Select(vm => new TextStatement(vm)));

            DsProject.Instance.ExportObjectToXaml(localizedConverter);
        }

        private void LoadButtonOnClick(object? sender, RoutedEventArgs e)
        {
            var localizedConverter = DsProject.Instance.ImportObjectFromXaml() as LocalizedConverter;
            if (localizedConverter is not null)
            {
                _dataSourceToUiStatementViewModels.Clear();
                _uiToDataSourceStatementViewModels.Clear();
                foreach (TextStatement statement in localizedConverter.DataSourceToUiStatements)
                    _dataSourceToUiStatementViewModels.Add(new StatementViewModel(statement));
                foreach (TextStatement statement in localizedConverter.UiToDataSourceStatements)
                    _uiToDataSourceStatementViewModels.Add(new StatementViewModel(statement));
            }
        }

        private void UiToDataSourceConverterDataGridCurrentCellChanged(object? sender, EventArgs e)
        {
            UiToDataSourceConverterDataGrid.CommitEdit();
        }

        private void UiToDataSourceConverterDataGridLostFocus(object? sender, RoutedEventArgs e)
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

        private int UiToDataSourceCurentIndex
        {
            get
            {
                if (UiToDataSourceConverterDataGrid.SelectedCells.Count > 0)
                    return
                        _uiToDataSourceStatementViewModels.IndexOf(
                            (StatementViewModel) UiToDataSourceConverterDataGrid.SelectedCells.First().Item);
                return -1;
            }
        }

        #endregion

        #region private fields

        private readonly ObservableCollection<StatementViewModel> _dataSourceToUiStatementViewModels =
            new();

        private readonly ObservableCollection<StatementViewModel> _uiToDataSourceStatementViewModels =
            new();

        #endregion
    }
}