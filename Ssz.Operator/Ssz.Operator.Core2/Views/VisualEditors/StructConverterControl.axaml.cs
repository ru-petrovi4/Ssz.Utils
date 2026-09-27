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
    ///     Writing the rules that turn what the data source says into what the shape shows, and what
    ///     the author does into what is written back.
    ///     <para>
    ///         Ported from the WPF editor rule for rule. The grids commit as they are typed in, so the
    ///         explicit commits the WPF version needed around every button are gone.
    ///     </para>
    /// </summary>
    public partial class StructConverterControl : UserControl
    {
        #region construction and destruction

        /// <summary>
        ///     For using in the previewer.
        /// </summary>
        public StructConverterControl() : this(true, true)
        {
        }

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
                if (_dataSourceToUiStatementViewModels.Count == 0 &&
                    _uiToDataSourceStatementViewModels.Count == 0)
                    return null;

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
                if (originalLocalizedConverter is null) return;

                foreach (TextStatement statement in originalLocalizedConverter.DataSourceToUiStatements)
                    _dataSourceToUiStatementViewModels.Add(new StatementViewModel(statement));
                foreach (TextStatement statement in originalLocalizedConverter.UiToDataSourceStatements)
                    _uiToDataSourceStatementViewModels.Add(new StatementViewModel(statement));
            }
        }

        #endregion

        #region private functions

        private void DataSourceToUiNewButtonClick(object? sender, RoutedEventArgs e)
        {
            var vm = new StatementViewModel((TextStatement?) null)
            {
                Condition = { ExpressionString = @"true" }
            };
            if (vm.Value is null) throw new InvalidOperationException();
            vm.Value.ExpressionString = @"d[0]";
            _dataSourceToUiStatementViewModels.Add(vm);
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

        private void UiToDataSourceNewButtonClick(object? sender, RoutedEventArgs e)
        {
            var vm = new StatementViewModel((TextStatement?) null)
            {
                Condition = { ExpressionString = @"true" }
            };
            if (vm.Value is null) throw new InvalidOperationException();
            vm.Value.ExpressionString = @"0";
            _uiToDataSourceStatementViewModels.Add(vm);
        }

        private void UiToDataSourceDeleteButtonClick(object? sender, RoutedEventArgs e)
        {
            if (UiToDataSourceCurrentIndex >= 0)
                _uiToDataSourceStatementViewModels.RemoveAt(UiToDataSourceCurrentIndex);
        }

        private void UiToDataSourceDownButtonClick(object? sender, RoutedEventArgs e)
        {
            var idx = UiToDataSourceCurrentIndex;
            if (idx >= 0 && idx != _uiToDataSourceStatementViewModels.Count - 1)
                _uiToDataSourceStatementViewModels.Move(idx, idx + 1);
        }

        private void UiToDataSourceUpButtonClick(object? sender, RoutedEventArgs e)
        {
            var idx = UiToDataSourceCurrentIndex;
            if (idx > 0) _uiToDataSourceStatementViewModels.Move(idx, idx - 1);
        }

        private void HelpButtonOnClick(object? sender, RoutedEventArgs e)
        {
            MessageBoxHelper.ShowInfo(Res.ConverterWindowHelp + Environment.NewLine +
                                      Environment.NewLine + Res.ConverterWindowHelp2);
        }

        private async void ClearButtonOnClick(object? sender, RoutedEventArgs e)
        {
            if (await MessageBoxHelper.AskYesNoCancelAsync(
                    Res.MessageAreYouSureToClearAllQuestion) != true) return;

            _dataSourceToUiStatementViewModels.Clear();
            _uiToDataSourceStatementViewModels.Clear();
        }

        private void SaveButtonOnClick(object? sender, RoutedEventArgs e)
        {
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
            if (localizedConverter is null) return;

            _dataSourceToUiStatementViewModels.Clear();
            _uiToDataSourceStatementViewModels.Clear();
            foreach (TextStatement statement in localizedConverter.DataSourceToUiStatements)
                _dataSourceToUiStatementViewModels.Add(new StatementViewModel(statement));
            foreach (TextStatement statement in localizedConverter.UiToDataSourceStatements)
                _uiToDataSourceStatementViewModels.Add(new StatementViewModel(statement));
        }

        private int DataSourceToUiCurrentIndex =>
            DataSourceToUiConverterDataGrid.SelectedItem is StatementViewModel vm
                ? _dataSourceToUiStatementViewModels.IndexOf(vm)
                : -1;

        private int UiToDataSourceCurrentIndex =>
            UiToDataSourceConverterDataGrid.SelectedItem is StatementViewModel vm
                ? _uiToDataSourceStatementViewModels.IndexOf(vm)
                : -1;

        #endregion

        #region private fields

        private readonly ObservableCollection<StatementViewModel> _dataSourceToUiStatementViewModels = new();

        private readonly ObservableCollection<StatementViewModel> _uiToDataSourceStatementViewModels = new();

        #endregion
    }
}
