using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Ssz.Operator.Core.Utils;
using Ssz.Utils;
using Res = Ssz.Operator.Core.Properties.Resources;

namespace Ssz.Operator.Core.VisualEditors
{
    /// <summary>
    ///     A list of objects of one kind, shown as a table: one row per object, one column per property
    ///     it has.
    ///     <para>
    ///         Ported from the WPF editor, including reading and writing the list as CSV and finding
    ///         text in it. It is edited on copies, so the list is only replaced when the dialog closes.
    ///     </para>
    /// </summary>
    public partial class SameTypeCloneableObjectsListEditorControl : UserControl
    {
        #region construction and destruction

        public SameTypeCloneableObjectsListEditorControl()
        {
            InitializeComponent();

            MainDataGrid.AutoGeneratingColumn += OnAutoGeneratingColumn;
        }

        #endregion

        #region public functions

        public object Collection
        {
            get
            {
                if (_itemType is null) throw new InvalidOperationException();

                var result = Activator.CreateInstance(typeof(List<>).MakeGenericType(_itemType)) as IList;
                if (result is null) throw new InvalidOperationException();

                foreach (object item in (IEnumerable) MainDataGrid.ItemsSource!) result.Add(item);
                return result;
            }
            set
            {
                _itemType = value.GetType().GetGenericArguments().First();

                IList itemsSource = NewItemsSource();
                foreach (object item in (IList) value) itemsSource.Add(((ICloneable) item).Clone());
                MainDataGrid.ItemsSource = itemsSource;
            }
        }

        #endregion

        #region private functions

        private IList NewItemsSource()
        {
            if (_itemType is null) throw new InvalidOperationException();

            var itemsSource = Activator.CreateInstance(
                typeof(ReferenceEqualityList<>).MakeGenericType(_itemType)) as IList;
            if (itemsSource is null) throw new InvalidOperationException();
            return itemsSource;
        }

        /// <summary>
        ///     A property the shapes do not show is not a column, and the rest are headed as the author
        ///     knows them.
        /// </summary>
        private void OnAutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (_itemType is null) return;

            // Avalonia names the property rather than handing over its descriptor, so it is looked up.
            PropertyDescriptor? propertyDescriptor =
                TypeDescriptor.GetProperties(_itemType).Find(e.PropertyName, false);
            if (propertyDescriptor is null) return;

            if (!propertyDescriptor.IsBrowsable)
            {
                e.Cancel = true;
                return;
            }

            e.Column.Header = propertyDescriptor.DisplayName;
        }

        private async void CopyMenuItemOnClick(object? sender, RoutedEventArgs e)
        {
            if (MainDataGrid.SelectedItem is null) return;

            PropertyDescriptor[] browsableProperties = BrowsableProperties.ToArray();
            object selectedItem = MainDataGrid.SelectedItem;

            await ClipboardHelper.SetTextAsync(CsvHelper.FormatForCsv(@",",
                browsableProperties.Select(property => property.GetValue(selectedItem)).ToArray()));
        }

        /// <summary>
        ///     Pastes rows of text over the rows from the selected one down, column for column.
        /// </summary>
        private async void PasteMenuItemOnClick(object? sender, RoutedEventArgs e)
        {
            if (_itemType is null) return;

            var text = await ClipboardHelper.GetTextAsync();
            if (String.IsNullOrEmpty(text)) return;

            var itemsSource = (IList) MainDataGrid.ItemsSource!;

            var rowIndex = MainDataGrid.SelectedItem is not null
                ? itemsSource.IndexOf(MainDataGrid.SelectedItem)
                : itemsSource.Count;
            if (rowIndex < 0) rowIndex = itemsSource.Count;

            PropertyDescriptor[] browsableProperties = BrowsableProperties.ToArray();

            foreach (var line in text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                if (String.IsNullOrWhiteSpace(line)) continue;

                string?[] fieldValues = CsvHelper.ParseCsvLine(@",", line);

                object? item;
                if (rowIndex < itemsSource.Count)
                {
                    item = itemsSource[rowIndex];
                }
                else
                {
                    item = Activator.CreateInstance(_itemType);
                    if (item is null) break;
                    itemsSource.Add(item);
                }

                for (var i = 0; i < browsableProperties.Length && i < fieldValues.Length; i += 1)
                {
                    PropertyDescriptor property = browsableProperties[i];
                    if (property.IsReadOnly) continue;

                    try
                    {
                        property.SetValue(item,
                            ObsoleteAnyHelper.ConvertTo(fieldValues[i], property.PropertyType, false));
                    }
                    catch (Exception)
                    {
                        // A value the property refuses leaves it as it was.
                    }
                }

                rowIndex += 1;
            }

            // The grid is told to read the list again, which a plain list cannot say for itself.
            IList newItemsSource = NewItemsSource();
            foreach (object item in itemsSource) newItemsSource.Add(item);
            MainDataGrid.ItemsSource = newItemsSource;
        }

        private async void ImportFromCsvButtonOnClickAsync(object? sender, RoutedEventArgs e)
        {
            if (_itemType is null) return;

            IStorageFile? file = await FileDialogHelper.OpenFileAsync(Res.ImportFromCsvButtonText,
                FileDialogHelper.CsvFileType, FileDialogHelper.AllFilesFileType);
            if (file is null) return;

            var path = file.TryGetLocalPath();
            if (String.IsNullOrEmpty(path)) return;

            BusyIndicator.IsBusy = true;
            BusyIndicator.Text1 = Res.ImportFromCsvButtonText;

            IList itemsSource = NewItemsSource();
            Type itemType = _itemType;

            try
            {
                await Task.Run(() =>
                {
                    using (var reader = new StreamReader(File.OpenRead(path), Encoding.UTF8))
                    {
                        var header = reader.ReadLine();
                        if (String.IsNullOrEmpty(header)) return;

                        string?[] fieldNames = CsvHelper.ParseCsvLine(@",", header);
                        if (fieldNames.Length == 0) return;

                        while (reader.ReadLine() is { } line)
                        {
                            if (String.IsNullOrWhiteSpace(line)) continue;

                            string?[] fieldValues = CsvHelper.ParseCsvLine(@",", line);
                            if (fieldValues.Length == 0) continue;

                            object? newItem = Activator.CreateInstance(itemType);
                            if (newItem is null) throw new InvalidOperationException();

                            for (var i = 0; i < fieldNames.Length; i += 1)
                            {
                                if (i >= fieldValues.Length) break;

                                System.Reflection.PropertyInfo? propertyInfo =
                                    itemType.GetProperty(fieldNames[i] ?? @"");
                                if (propertyInfo is null) continue;

                                object convertedValue = ObsoleteAnyHelper.ConvertTo(fieldValues[i],
                                    propertyInfo.PropertyType, false);
                                propertyInfo.SetValue(newItem, convertedValue);
                            }

                            itemsSource.Add(newItem);
                        }
                    }
                });

                MainDataGrid.ItemsSource = itemsSource;
            }
            finally
            {
                BusyIndicator.IsBusy = false;
            }
        }

        private async void ExportToCsvButtonOnClickAsync(object? sender, RoutedEventArgs e)
        {
            IStorageFile? file = await FileDialogHelper.SaveFileAsync(Res.ExportToCsvButtonText, null,
                FileDialogHelper.CsvFileType, FileDialogHelper.AllFilesFileType);
            if (file is null) return;

            var path = file.TryGetLocalPath();
            if (String.IsNullOrEmpty(path)) return;

            BusyIndicator.IsBusy = true;
            BusyIndicator.Text1 = Res.ExportToCsvButtonText;

            PropertyDescriptor[] browsableProperties = BrowsableProperties.ToArray();
            object[] items = ((IEnumerable) MainDataGrid.ItemsSource!).OfType<object>().ToArray();

            try
            {
                await Task.Run(() =>
                {
                    using (var writer = new StreamWriter(File.Create(path), Encoding.UTF8))
                    {
                        writer.WriteLine(String.Join(@",",
                            browsableProperties.Select(property => property.Name)));

                        foreach (object item in items)
                            writer.WriteLine(CsvHelper.FormatForCsv(@",",
                                browsableProperties.Select(property => property.GetValue(item)).ToArray()));
                    }
                });
            }
            finally
            {
                BusyIndicator.IsBusy = false;
            }
        }

        private async void ClearAllButtonOnClick(object? sender, RoutedEventArgs e)
        {
            if (_itemType is null) return;

            if (await MessageBoxHelper.AskYesNoCancelAsync(
                    Res.MessageAreYouSureToClearAllQuestion) != true) return;

            MainDataGrid.ItemsSource = NewItemsSource();
        }

        private async void FindButtonOnClick(object? sender, RoutedEventArgs e)
        {
            _textToFind = await InputBoxHelper.ShowAsync(Res.FindDialogLabel, Res.FindButtonText, _textToFind)
                          ?? _textToFind;
            FindNext();
        }

        private void FindNextButtonOnClick(object? sender, RoutedEventArgs e)
        {
            FindNext();
        }

        private void BusyIndicatorOnStopped(object? sender, EventArgs e)
        {
            BusyIndicator.IsBusy = false;
        }

        private void FindNext()
        {
            if (String.IsNullOrEmpty(_textToFind)) return;

            var itemsSource = (IList) MainDataGrid.ItemsSource!;

            var startIndex = 0;
            if (MainDataGrid.SelectedItem is not null)
                startIndex = itemsSource.IndexOf(MainDataGrid.SelectedItem) + 1;

            PropertyDescriptor[] browsableProperties = BrowsableProperties.ToArray();

            object? found = itemsSource.OfType<object>()
                .Skip(startIndex)
                .FirstOrDefault(i => String.Join(@",", browsableProperties.Select(
                        property => ObsoleteAnyHelper.ConvertTo<string>(property.GetValue(i), false)))
                    .IndexOf(_textToFind, StringComparison.CurrentCulture) >= 0);
            if (found is null) return;

            MainDataGrid.SelectedItem = found;
            MainDataGrid.ScrollIntoView(found, null);
            MainDataGrid.Focus();
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
        private string _textToFind = @"";

        #endregion
    }
}
