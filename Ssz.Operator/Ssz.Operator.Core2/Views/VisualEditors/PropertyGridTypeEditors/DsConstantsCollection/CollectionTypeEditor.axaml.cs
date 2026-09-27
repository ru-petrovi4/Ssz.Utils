using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Ssz.Operator.Core.Constants;
using Ssz.Utils;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors.DsConstantsCollection
{
    /// <summary>
    ///     The constants of a shape or a page, shown as a table. Only their values are the author's to
    ///     change here; what constants there are comes from the shape itself.
    ///     <para>
    ///         Ported from the WPF editor. Copying and pasting a column of values, which is how a page
    ///         of many shapes is filled in, works as it did.
    ///     </para>
    /// </summary>
    public partial class CollectionTypeEditor : UserControl, ITypeEditor, IPropertyGridItem
    {
        #region construction and destruction

        public CollectionTypeEditor()
        {
            InitializeComponent();
        }

        #endregion

        #region public functions

        public Control ResolveEditor(PropertyItemViewModel propertyItem)
        {
            DataContext = propertyItem;

            if (propertyItem.Value is ObservableCollection<DsConstant> sourceCollection)
            {
                _dsConstantsCollectionViewModel = new DsConstantsCollectionViewModel(sourceCollection);
                MainDataGrid.ItemsSource = _dsConstantsCollectionViewModel.EditedCollection;
            }

            AddRemovePanel.IsVisible = WithAddRemove;

            return this;
        }

        public bool RefreshForPropertyGridIsDisabled { get; set; }

        public void RefreshForPropertyGrid()
        {
            _dsConstantsCollectionViewModel?.Refresh();
        }

        public void EndEditInPropertyGrid()
        {
            RefreshForPropertyGrid();
        }

        #endregion

        #region protected functions

        /// <summary>
        ///     Whether the author may add constants of their own, which only the project and the
        ///     drawings allow.
        /// </summary>
        protected virtual bool WithAddRemove => false;

        #endregion

        #region private functions

        private void AddButtonOnClick(object? sender, RoutedEventArgs e)
        {
            _dsConstantsCollectionViewModel?.EditedCollection.Add(new DsConstantViewModel());
        }

        private void RemoveButtonOnClick(object? sender, RoutedEventArgs e)
        {
            if (_dsConstantsCollectionViewModel is null) return;
            if (MainDataGrid.SelectedItem is not DsConstantViewModel vm) return;

            _dsConstantsCollectionViewModel.EditedCollection.Remove(vm);
        }

        private async void CopyMenuItemOnClick(object? sender, RoutedEventArgs e)
        {
            if (MainDataGrid.SelectedItem is not DsConstantViewModel vm) return;

            await ClipboardHelper.SetTextAsync(vm.Value);
        }

        /// <summary>
        ///     Pastes a column of values down the rows from the selected one.
        /// </summary>
        private async void PasteMenuItemOnClick(object? sender, RoutedEventArgs e)
        {
            if (_dsConstantsCollectionViewModel is null) return;

            var text = await ClipboardHelper.GetTextAsync();
            if (String.IsNullOrEmpty(text)) return;

            ObservableCollection<DsConstantViewModel> editedCollection =
                _dsConstantsCollectionViewModel.EditedCollection;

            var rowIndex = MainDataGrid.SelectedItem is DsConstantViewModel selected
                ? editedCollection.IndexOf(selected)
                : 0;
            if (rowIndex < 0) rowIndex = 0;

            foreach (var line in text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                if (rowIndex >= editedCollection.Count) break;

                // A line of several fields fills only this column, as the WPF editor did for a
                // single-column selection.
                editedCollection[rowIndex].Value = CsvHelper.ParseCsvLine(@",", line).FirstOrDefault() ?? line;
                rowIndex += 1;
            }

            RefreshForPropertyGrid();
        }

        private void DeleteMenuItemOnClick(object? sender, RoutedEventArgs e)
        {
            if (MainDataGrid.SelectedItem is not DsConstantViewModel vm) return;

            vm.Value = @"";

            RefreshForPropertyGrid();
        }

        #endregion

        #region private fields

        private DsConstantsCollectionViewModel? _dsConstantsCollectionViewModel;

        #endregion
    }

    /// <summary>
    ///     The same table, where the author may also add and remove constants.
    /// </summary>
    public class CollectionWithAddRemoveTypeEditor : CollectionTypeEditor
    {
        #region protected functions

        protected override bool WithAddRemove => true;

        #endregion
    }
}
