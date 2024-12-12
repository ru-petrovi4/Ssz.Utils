using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using Ssz.Operator.Core.Utils;
using Ssz.Utils.Wpf.WpfMessageBox;
using Ssz.Utils;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.VisualEditors
{
    public partial class SameTypeCloneableObjectsListEditorControl : UserControl
    {
        #region construction and destruction

        public SameTypeCloneableObjectsListEditorControl()
        {
            InitializeComponent();

            MainDataGrid.AutoGeneratingColumn += OnAutoGeneratingColumn;

            Unloaded += (sender, args) => _cancellationTokenSource.Cancel();
        }

        #endregion

        #region public functions

        public object Collection
        {
            get
            {
                if (_itemType is null) throw new InvalidOperationException();

                var result = Activator.CreateInstance(
                    typeof(List<>).MakeGenericType(_itemType)) as IList;
                if (result is null) throw new InvalidOperationException();
                foreach (var item in MainDataGrid.ItemsSource) result.Add(item);
                return result;
            }
            set
            {
                _itemType = value.GetType().GetGenericArguments().First();

                var itemsSource = Activator.CreateInstance(
                    typeof(ReferenceEqualityList<>).MakeGenericType(_itemType)) as IList;
                if (itemsSource is null) throw new InvalidOperationException();
                foreach (var item in (IList) value) itemsSource.Add(((ICloneable) item).Clone());
                MainDataGrid.ItemsSource = itemsSource;
            }
        }

        #endregion

        #region private functions

        private void OnAutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            var propertyDescriptor = e.PropertyDescriptor as PropertyDescriptor;
            if (propertyDescriptor is null)
                return;

            if (!propertyDescriptor.IsBrowsable)
            {
                e.Cancel = true;
                return;
            }

            e.Column.Header = propertyDescriptor.DisplayName;
        }

        private async void PasteOnExecutedAsync(object? sender, ExecutedRoutedEventArgs e)
        {
            BusyIndicator.IsBusy = true;

            await Task.Delay(100);

            List<List<string?>> data = ClipboardHelper.ParseClipboardData();

            if (data.Count == 0) return;

            var hasAddedNewRow = false;

            var minRowIndex = ((IList) MainDataGrid.ItemsSource).IndexOf(MainDataGrid.SelectedItem);
            if (minRowIndex == -1) minRowIndex = MainDataGrid.Items.Count - 1;
            var maxRowIndex = MainDataGrid.Items.Count - 1;
            const int minColumnDisplayIndex = 0;
            var maxColumnDisplayIndex = MainDataGrid.Columns.Count - 1;

            for (var i = 0; i < data.Count; i += 1)
            {
                var rowIndex = minRowIndex + i;
                if (rowIndex > maxRowIndex) break;

                for (var j = 0; j < data[i]?.Count; j += 1)
                {
                    var columnIndex = minColumnDisplayIndex + j;
                    if (columnIndex > maxColumnDisplayIndex) break;

                    DataGridColumn column = MainDataGrid.ColumnFromDisplayIndex(columnIndex);
                    column.OnPastingCellClipboardContent(MainDataGrid.Items[rowIndex],
                        data[i][j]);
                }

                if (rowIndex == maxRowIndex)
                {
                    maxRowIndex += 1;
                    hasAddedNewRow = true;
                }
            }

            if (hasAddedNewRow)
            {
                MainDataGrid.UnselectAll();
                MainDataGrid.UnselectAllCells();
            }

            BusyIndicator.IsBusy = false;
        }

        private async void ImportFromCsvButtonOnClickAsync(object? sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Csv files (*.csv)|*.csv|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true)
                return;

            BusyIndicator.IsBusy = true;

            if (_itemType is null) throw new InvalidOperationException();
            var itemsSource = Activator.CreateInstance(
                typeof(ReferenceEqualityList<>).MakeGenericType(_itemType)) as IList;
            if (itemsSource is null) throw new InvalidOperationException();

            await Task.Run(() =>
            {
                using (Stream stream = File.OpenRead(dialog.FileName))
                {
                    using (var parser = new TextFieldParser(stream) {TextFieldType = FieldType.Delimited})
                    {
                        parser.SetDelimiters(",");

                        var fieldNames = parser.ReadFields();
                        if (fieldNames is null || fieldNames.Length == 0)
                            return;

                        while (!parser.EndOfData)
                        {
                            var fieldValues = parser.ReadFields();
                            if (fieldValues is null || fieldValues.Length == 0)
                                continue;

                            var newItem = Activator.CreateInstance(_itemType);
                            if (newItem is null) throw new InvalidOperationException();
                            for (var i = 0; i < fieldNames.Length; i += 1)
                            {
                                if (i >= fieldValues.Length) break;
                                var propertyInfo = newItem.GetType().GetProperty(fieldNames[i]);
                                if (propertyInfo is null)
                                    continue;

                                object convertedValue =
                                    ObsoleteAnyHelper.ConvertTo(fieldValues[i], propertyInfo.PropertyType, false);

                                propertyInfo.SetValue(newItem, convertedValue);
                            }

                            itemsSource.Add(newItem);
                        }

                        parser.Close();
                    }
                }
            });

            MainDataGrid.ItemsSource = itemsSource;

            BusyIndicator.IsBusy = false;
        }

        private async void ExportToCsvButtonOnClickAsync(object? sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Csv files (*.csv)|*.csv|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true)
                return;

            BusyIndicator.IsBusy = true;

            await Task.Run(() =>
            {
                using (Stream stream = dialog.OpenFile())
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    var browsableProperties = BrowsableProperties.ToArray();
                    // Header
                    writer.WriteLine(string.Join(",", browsableProperties.Select(property => property.Name)));

                    foreach (object item in MainDataGrid.ItemsSource)
                    {
                        object localItem = item;
                        writer.WriteLine(
                            CsvHelper.FormatForCsv(",",
                                browsableProperties.Select(
                                        property => ObsoleteAnyHelper.ConvertTo<string>(property.GetValue(localItem), false))
                                    .ToArray()));
                    }
                }
            });

            BusyIndicator.IsBusy = false;
        }

        private void ClearAllButtonOnClick(object? sender, RoutedEventArgs e)
        {
            if (_itemType is null) throw new InvalidOperationException();
            var messageBoxResult = WpfMessageBox.Show(Window.GetWindow(this),
                Properties.Resources.MessageAreYouSureToClearAllQuestion,
                Properties.Resources.QuestionMessageBoxCaption,
                WpfMessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);
            if (messageBoxResult != WpfMessageBoxResult.Yes) return;

            MainDataGrid.ItemsSource = Activator.CreateInstance(
                typeof(ReferenceEqualityList<>).MakeGenericType(_itemType)) as IList;
        }

        private void FindButtonOnClick(object? sender, RoutedEventArgs e)
        {
            _textToFind = Interaction.InputBox(Properties.Resources.FindDialogLabel,
                Properties.Resources.FindButtonText, _textToFind);
            FindNext();
        }

        private void FindNextButtonOnClick(object? sender, RoutedEventArgs e)
        {
            FindNext();
        }

        private void FindNext()
        {
            if (string.IsNullOrEmpty(_textToFind)) return;
            var startIndex = 0;
            if (MainDataGrid.SelectedItem is not null)
                startIndex = ((IList) MainDataGrid.ItemsSource).IndexOf(MainDataGrid.SelectedItem) + 1;
            var browsableProperties = BrowsableProperties.ToArray();
            MainDataGrid.SelectedItem = MainDataGrid.ItemsSource.OfType<object>()
                .Skip(startIndex)
                .FirstOrDefault(i => string.Join(",", browsableProperties.Select(
                        property => ObsoleteAnyHelper.ConvertTo<string>(property.GetValue(i), false)))
                    .IndexOf(_textToFind) >= 0);
            if (MainDataGrid.SelectedItem is not null)
            {
                MainDataGrid.ScrollIntoView(MainDataGrid.SelectedItem);
                MainDataGrid.Focus();
            }
        }

        private IEnumerable<PropertyDescriptor> BrowsableProperties
        {
            get
            {
                if (_itemType is null) throw new InvalidOperationException();
                return TypeDescriptor.GetProperties(_itemType)
                    .OfType<PropertyDescriptor>()
                    .Where(item => item.IsBrowsable);
            }
        }

        #endregion

        #region private fields

        private Type? _itemType;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private string _textToFind = @"";

        #endregion
    }
}