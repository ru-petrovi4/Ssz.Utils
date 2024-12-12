using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Ssz.Operator.Core.MultiValueConverters;
using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.VisualEditors.ValueConverters;
using Ssz.Operator.Core.VisualEditors.Windows;
using Ssz.Utils.Wpf.WpfMessageBox;

namespace Ssz.Operator.Core.VisualEditors
{
    public partial class BrushConverterControl : UserControl
    {
        #region private fields

        private readonly ObservableCollection<StatementViewModel> _dataSourceToUiStatementViewModels =
            new();

        #endregion

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
                DataSourceToUiConverterDataGrid.CommitEdit();

                if (_dataSourceToUiStatementViewModels.Count == 0)
                {
                    return null;
                }

                var resultValueConverter = new DsBrushConverter();
                resultValueConverter.DataSourceToUiStatements.AddRange(
                    _dataSourceToUiStatementViewModels.Select(vm => new DsBrushStatement(vm)));
                return resultValueConverter;
            }
            set
            {
                var originalValueConverter = value as DsBrushConverter;
                if (originalValueConverter is not null)
                    foreach (DsBrushStatement statement in originalValueConverter.DataSourceToUiStatements)
                        _dataSourceToUiStatementViewModels.Add(new StatementViewModel(statement));
            }
        }

        #endregion

        #region private functions

        private void DataSourceToUiNewButtonClick(object? sender, RoutedEventArgs e)
        {
            DataSourceToUiConverterDataGrid.CommitEdit();
            var vm = new StatementViewModel((DsBrushStatement?) null)
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

            if (_dataSourceToUiStatementViewModels.Count == 0)
            {
                return;
            }

            var converter = new DsBrushConverter();
            converter.DataSourceToUiStatements.AddRange(
                _dataSourceToUiStatementViewModels.Select(vm => new DsBrushStatement(vm)));

            DsProject.Instance.ExportObjectToXaml(converter);
        }

        private void LoadButtonOnClick(object? sender, RoutedEventArgs e)
        {
            var converter = DsProject.Instance.ImportObjectFromXaml() as DsBrushConverter;
            if (converter is not null)
            {
                _dataSourceToUiStatementViewModels.Clear();
                foreach (DsBrushStatement statement in converter.DataSourceToUiStatements)
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

    public class BrushEditor : BrushTypeEditor
    {
        #region construction and destruction

        public BrushEditor()
        {
            MainButton.SetBinding(ContentProperty, new Binding
            {
                Path = new PropertyPath("ConstDsBrushOrParamNum"),
                Converter =
                    DsBrushToContentConverter
                        .Instance
            });
        }

        #endregion

        #region protected functions

        protected override void ButtonClick(object? sender, RoutedEventArgs e)
        {
            var dialog = new BrushEditorDialog
            {
                Owner = Window.GetWindow(this),
                DsBrush = ((StatementViewModel) DataContext).ConstDsBrushOrParamNum
            };

            if (dialog.ShowDialog() == true) ((StatementViewModel) DataContext).ConstDsBrushOrParamNum = dialog.DsBrush;
        }

        #endregion
    }
}