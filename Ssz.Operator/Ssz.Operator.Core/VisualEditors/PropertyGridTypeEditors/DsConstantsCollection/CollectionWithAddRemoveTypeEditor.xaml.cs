using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Ssz.Operator.Core.Constants;
using Ssz.Utils.Wpf;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Implementation.Editors;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors.DsConstantsCollection
{
    public partial class CollectionWithAddRemoveTypeEditor : UserControl, ITypeEditor,
        IPropertyGridItem
    {
        #region private fields

        private DsConstantsCollectionViewModel? _dsConstantsCollectionViewModel;

        #endregion

        #region construction and destruction

        public CollectionWithAddRemoveTypeEditor()
        {
            InitializeComponent();
        }

        #endregion

        #region public functions

        public FrameworkElement ResolveEditor(PropertyItem propertyItem)
        {
            _dsConstantsCollectionViewModel =
                new DsConstantsCollectionViewModel((ObservableCollection<DsConstant>) propertyItem.Value);

            MainDataGrid.ItemsSource = _dsConstantsCollectionViewModel.EditedCollection;

            return this;
        }

        public bool RefreshForPropertyGridIsDisabled { get; set; }

        public void RefreshForPropertyGrid()
        {
            if (_dsConstantsCollectionViewModel is not null) _dsConstantsCollectionViewModel.Refresh();
        }

        public void EndEditInPropertyGrid()
        {
            MainDataGrid.CommitEdit(DataGridEditingUnit.Row, true);

            RefreshForPropertyGrid();
        }

        #endregion

        #region private functions

        private void PasteOnExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            var selectedCells = MainDataGrid.SelectedCells;
            if (selectedCells is null) return;

            var currentCell = selectedCells.FirstOrDefault();
            if (currentCell == default) return;

           List<List<string?>> data = ClipboardHelper.ParseClipboardData();
            if (data.Count == 0) return;

            var hasAddedNewRow = false;

            var minRowIndex = MainDataGrid.Items.IndexOf(currentCell.Item);
            if (minRowIndex == -1) minRowIndex = MainDataGrid.Items.Count;
            var maxRowIndex = MainDataGrid.Items.Count - 1;
            var minColumnIndex = MainDataGrid.Columns.IndexOf(currentCell.Column);
            if (minColumnIndex == -1) minColumnIndex = MainDataGrid.Columns.Count;
            var maxColumnIndex = MainDataGrid.Columns.Count - 1;

            for (var i = 0; i < data.Count; i += 1)
            {
                var rowIndex = minRowIndex + i;
                if (rowIndex > maxRowIndex) break;

                for (var j = 0; j < data[i].Count; j += 1)
                {
                    var columnIndex = minColumnIndex + j;
                    if (columnIndex > maxColumnIndex) break;

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
        }

        private void DeleteOnExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            var selectedCells = MainDataGrid.SelectedCells;
            if (selectedCells is null) return;

            var rowsIndexesToDelete = new List<int>();
            foreach (var selectedCell in selectedCells)
            {
                var columnIndex = MainDataGrid.Columns.IndexOf(selectedCell.Column);
                if (columnIndex == 0)
                {
                    var rowIndex = MainDataGrid.Items.IndexOf(selectedCell.Item);
                    rowsIndexesToDelete.Add(rowIndex);
                }
                else if (columnIndex == 1)
                {
                    ((DsConstantViewModel) selectedCell.Item).Value = "";
                }
                else if (columnIndex == 2)
                {
                    ((DsConstantViewModel) selectedCell.Item).Type = "";
                }
                else if (columnIndex == 3)
                {
                    ((DsConstantViewModel) selectedCell.Item).Desc = "";
                }
            }

            if (_dsConstantsCollectionViewModel is not null)
                foreach (var rowIndex in rowsIndexesToDelete.OrderByDescending(i => i))
                    _dsConstantsCollectionViewModel.EditedCollection.RemoveAt(rowIndex);
        }

        #endregion
    }
}