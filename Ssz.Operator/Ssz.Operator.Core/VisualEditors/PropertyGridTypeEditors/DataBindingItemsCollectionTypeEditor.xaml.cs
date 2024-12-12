using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

using Ssz.Utils.MonitoredUndo;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Implementation.Editors;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors
{
    public partial class DataBindingItemsCollectionTypeEditor : UserControl, ITypeEditor, IPropertyGridItem
    {
        #region construction and destruction

        public DataBindingItemsCollectionTypeEditor()
        {
            InitializeComponent();

            _editedCollection.CollectionChanged += EditedCollectionChanged;
        }

        #endregion

        #region public functions

        public FrameworkElement ResolveEditor(PropertyItem propertyItem)
        {
            _sourceCollection = (ObservableCollection<DataBindingItem>) propertyItem.Value;

            InitializeCollections();

            MainDataGrid.ItemsSource = _editedCollection;

            return this;
        }

        public bool RefreshForPropertyGridIsDisabled { get; set; }

        public void RefreshForPropertyGrid()
        {
            if (_sourceCollection is null) return;

            bool equals;

            if (_sourceCollectionCopy.Count != _sourceCollection.Count)
            {
                equals = false;
            }
            else
            {
                equals = true;
                for (var i = 0; i < _sourceCollectionCopy.Count; i += 1)
                {
                    var copy = _sourceCollectionCopy[i];
                    if (!copy.Equals(_sourceCollection[i]))
                    {
                        equals = false;
                        break;
                    }
                }
            }

            if (!equals)
            {
                InitializeCollections();
                return;
            }

            if (_editedCollection.Count != _sourceCollection.Count)
            {
                equals = false;
            }
            else
            {
                equals = true;
                for (var i = 0; i < _editedCollection.Count; i += 1)
                {
                    var edited = _editedCollection[i];
                    if (edited.IsEmpty()) continue;
                    if (!edited.DataBindingItem.Equals(_sourceCollection[i]))
                    {
                        equals = false;
                        break;
                    }
                }
            }

            if (equals) return;

            UndoService.Current.BeginChangeSetBatch("Data Source Items", true);

            for (var i = _sourceCollection.Count - 1; i >= 0; i--) _sourceCollection.RemoveAt(i);

            _sourceCollectionCopy.Clear();
            foreach (DataBindingItemViewModel dataBindingItemViewModel in _editedCollection)
            {
                if (dataBindingItemViewModel.IsEmpty()) continue;
                _sourceCollection.Add(
                    new DataBindingItem(dataBindingItemViewModel.DataBindingItem));
                _sourceCollectionCopy.Add(
                    new DataBindingItem(dataBindingItemViewModel.DataBindingItem));
            }

            UndoService.Current.EndChangeSetBatch();
        }

        public void EndEditInPropertyGrid()
        {
            MainDataGrid.CommitEdit(DataGridEditingUnit.Row, true);

            RefreshForPropertyGrid();
        }

        #endregion

        #region private functions

        private void InitializeCollections()
        {
            if (_sourceCollection is null) return;

            _sourceCollectionCopy.Clear();
            _editedCollection.Clear();
            foreach (DataBindingItem dataBindingItem in _sourceCollection)
            {
                _sourceCollectionCopy.Add(new DataBindingItem(dataBindingItem));
                _editedCollection.Add(new DataBindingItemViewModel(new DataBindingItem(dataBindingItem)));
            }
        }

        private void EditedCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            var index = 0;
            foreach (DataBindingItemViewModel dataBindingItemViewModel in _editedCollection)
            {
                dataBindingItemViewModel.Index = index;
                index += 1;
            }
        }

        #endregion

        #region private fields

        private ObservableCollection<DataBindingItem>? _sourceCollection;

        private readonly List<DataBindingItem> _sourceCollectionCopy = new();

        private readonly ObservableCollection<DataBindingItemViewModel> _editedCollection =
            new();

        #endregion
    }
}