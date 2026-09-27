using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Ssz.Operator.Core.MultiValueConverters;
using Res = Ssz.Operator.Core.Properties.Resources;

namespace Ssz.Operator.Core.VisualEditors
{
    /// <summary>
    ///     Writing the rules that decide what a shape is painted with for what the data source says.
    ///     Ported from the WPF editor.
    /// </summary>
    public partial class BrushConverterControl : UserControl
    {
        #region construction and destruction

        public BrushConverterControl()
        {
            InitializeComponent();

            DataSourceToUiConverterDataGrid.ItemsSource = _dataSourceToUiStatementViewModels;
        }

        #endregion

        #region public functions

        public ValueConverterBase? DsBrushConverter
        {
            get
            {
                if (_dataSourceToUiStatementViewModels.Count == 0) return null;

                var resultValueConverter = new DsBrushConverter();
                resultValueConverter.DataSourceToUiStatements.AddRange(
                    _dataSourceToUiStatementViewModels.Select(vm => new DsBrushStatement(vm)));
                return resultValueConverter;
            }
            set
            {
                var originalValueConverter = value as DsBrushConverter;
                if (originalValueConverter is null) return;

                foreach (DsBrushStatement statement in originalValueConverter.DataSourceToUiStatements)
                    _dataSourceToUiStatementViewModels.Add(new StatementViewModel(statement));
            }
        }

        #endregion

        #region private functions

        private void DataSourceToUiNewButtonClick(object? sender, RoutedEventArgs e)
        {
            _dataSourceToUiStatementViewModels.Add(new StatementViewModel((DsBrushStatement?) null)
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

            var converter = new DsBrushConverter();
            converter.DataSourceToUiStatements.AddRange(
                _dataSourceToUiStatementViewModels.Select(vm => new DsBrushStatement(vm)));

            DsProject.Instance.ExportObjectToXaml(converter);
        }

        private void LoadButtonOnClick(object? sender, RoutedEventArgs e)
        {
            var converter = DsProject.Instance.ImportObjectFromXaml() as DsBrushConverter;
            if (converter is null) return;

            _dataSourceToUiStatementViewModels.Clear();
            foreach (DsBrushStatement statement in converter.DataSourceToUiStatements)
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
