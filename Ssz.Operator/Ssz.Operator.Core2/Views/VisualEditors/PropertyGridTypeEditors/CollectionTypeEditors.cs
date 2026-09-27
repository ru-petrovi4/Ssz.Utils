using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.VisualEditors.Windows;
using Res = Ssz.Operator.Core.Properties.Resources;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors
{
    /// <summary>
    ///     The addons a project asks for: what they are called, and the way to change the list.
    /// </summary>
    public class AddonsCollectionTypeEditor : UserControl, ITypeEditor, IPropertyGridItem
    {
        #region construction and destruction

        public AddonsCollectionTypeEditor()
        {
            _mainDataGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                HeadersVisibility = DataGridHeadersVisibility.Row,
                CanUserResizeColumns = true,
                CanUserSortColumns = false,
                IsReadOnly = true,
                MaxHeight = 200
            };
            _mainDataGrid.Columns.Add(new DataGridTextColumn
            {
                Binding = new Binding(nameof(AddonBase.Name)),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            var button = new Button
            {
                Width = 160,
                Margin = new Thickness(0, 5, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Content = Res.AddRemoveAddonsButtonText
            };
            button.Click += AddRemoveAddonsButtonOnClick;

            var grid = new Grid { RowDefinitions = new RowDefinitions(@"*,Auto") };
            grid.Children.Add(_mainDataGrid);
            Grid.SetRow(button, 1);
            grid.Children.Add(button);

            Content = grid;
        }

        #endregion

        #region public functions

        public Control ResolveEditor(PropertyItemViewModel propertyItem)
        {
            DataContext = propertyItem;

            _addonCollection = propertyItem.Value as ObservableCollection<AddonBase>;
            if (_addonCollection is not null)
                _mainDataGrid.ItemsSource = _addonCollection.OrderBy(a => a.Name).ToArray();

            return this;
        }

        public bool RefreshForPropertyGridIsDisabled { get; set; }

        public void RefreshForPropertyGrid()
        {
        }

        public void EndEditInPropertyGrid()
        {
            _mainDataGrid.ItemsSource = null;
        }

        #endregion

        #region private functions

        private async void AddRemoveAddonsButtonOnClick(object? sender, RoutedEventArgs e)
        {
            if (!DsProject.Instance.IsInitialized) return;
            if (TopLevel.GetTopLevel(this) is not Window ownerWindow) return;

            var dialog = new AddonsCollectionEditorDialog
            {
                Title = Res.AddRemoveAddonsButtonText
            };
            await dialog.ShowDialog(ownerWindow);

            if (_addonCollection is not null)
                _mainDataGrid.ItemsSource = _addonCollection.OrderBy(a => a.Name).ToArray();
        }

        #endregion

        #region private fields

        private readonly DataGrid _mainDataGrid;
        private ObservableCollection<AddonBase>? _addonCollection;

        #endregion
    }

    /// <summary>
    ///     What a shape reads from the data source: one row per item, with its kind, its identifier and
    ///     the value to use until the server answers.
    ///     <para>
    ///         Ported from the WPF editor, including the way it works: the rows are edited on copies
    ///         and written back into the shape only once they differ, so that a shape being typed into
    ///         is not rebuilt on every keystroke.
    ///     </para>
    /// </summary>
    public class DataBindingItemsCollectionTypeEditor : UserControl, ITypeEditor, IPropertyGridItem
    {
        #region construction and destruction

        public DataBindingItemsCollectionTypeEditor()
        {
            _mainDataGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                CanUserResizeColumns = true,
                CanUserSortColumns = false,
                MaxHeight = 200
            };

            _mainDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = @"N",
                IsReadOnly = true,
                Binding = new Binding(nameof(DataBindingItemViewModel.Index))
            });
            _mainDataGrid.Columns.Add(new DataGridTemplateColumn
            {
                Header = Res.DataItemType,
                CellTemplate = new FuncDataTemplate<DataBindingItemViewModel>((_, _) =>
                {
                    var comboBox = new ComboBox
                    {
                        ItemsSource = Enum.GetValues<DataSourceType>(),
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    };
                    comboBox.Bind(SelectingItemsControl.SelectedItemProperty,
                        new Binding(nameof(DataBindingItemViewModel.Type)) { Mode = BindingMode.TwoWay });
                    return comboBox;
                })
            });
            _mainDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = Res.DataItemId,
                Binding = new Binding(nameof(DataBindingItemViewModel.IdString)) { Mode = BindingMode.TwoWay },
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
            _mainDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = Res.DataItemDefaultValue,
                Binding = new Binding(nameof(DataBindingItemViewModel.DefaultValue)) { Mode = BindingMode.TwoWay }
            });

            var addButton = new Button
            {
                Width = 30,
                Content = @"+",
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            addButton.Click += (sender, e) => _editedCollection.Add(new DataBindingItemViewModel());

            var removeButton = new Button
            {
                Width = 30,
                Margin = new Thickness(5, 0, 0, 0),
                Content = @"X",
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            removeButton.Click += (sender, e) =>
            {
                if (_mainDataGrid.SelectedItem is DataBindingItemViewModel vm) _editedCollection.Remove(vm);
            };

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 0)
            };
            buttons.Children.Add(addButton);
            buttons.Children.Add(removeButton);

            var grid = new Grid { RowDefinitions = new RowDefinitions(@"*,Auto") };
            grid.Children.Add(_mainDataGrid);
            Grid.SetRow(buttons, 1);
            grid.Children.Add(buttons);

            Content = grid;

            _editedCollection.CollectionChanged += EditedCollectionChanged;
        }

        #endregion

        #region public functions

        public Control ResolveEditor(PropertyItemViewModel propertyItem)
        {
            DataContext = propertyItem;

            _sourceCollection = propertyItem.Value as ObservableCollection<DataBindingItem>;
            InitializeCollections();
            _mainDataGrid.ItemsSource = _editedCollection;

            return this;
        }

        public bool RefreshForPropertyGridIsDisabled { get; set; }

        /// <summary>
        ///     Writes the rows back into the shape, but only when they say something different from
        ///     what it already holds.
        /// </summary>
        public void RefreshForPropertyGrid()
        {
            if (_sourceCollection is null) return;

            if (!SameAs(_sourceCollectionCopy, _sourceCollection))
            {
                // Something other than this editor changed the shape; the rows follow it.
                InitializeCollections();
                return;
            }

            var equals = _editedCollection.Count == _sourceCollection.Count;
            if (equals)
                for (var i = 0; i < _editedCollection.Count; i += 1)
                {
                    DataBindingItemViewModel edited = _editedCollection[i];
                    if (edited.IsEmpty()) continue;
                    if (!edited.DataBindingItem.Equals(_sourceCollection[i]))
                    {
                        equals = false;
                        break;
                    }
                }

            if (equals) return;

            for (var i = _sourceCollection.Count - 1; i >= 0; i--) _sourceCollection.RemoveAt(i);
            _sourceCollectionCopy.Clear();

            foreach (DataBindingItemViewModel dataBindingItemViewModel in _editedCollection)
            {
                if (dataBindingItemViewModel.IsEmpty()) continue;

                _sourceCollection.Add(new DataBindingItem(dataBindingItemViewModel.DataBindingItem));
                _sourceCollectionCopy.Add(new DataBindingItem(dataBindingItemViewModel.DataBindingItem));
            }
        }

        public void EndEditInPropertyGrid()
        {
            RefreshForPropertyGrid();
        }

        #endregion

        #region private functions

        private static bool SameAs(List<DataBindingItem> copy, ObservableCollection<DataBindingItem> source)
        {
            if (copy.Count != source.Count) return false;

            for (var i = 0; i < copy.Count; i += 1)
                if (!copy[i].Equals(source[i]))
                    return false;

            return true;
        }

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

        private readonly DataGrid _mainDataGrid;
        private ObservableCollection<DataBindingItem>? _sourceCollection;
        private readonly List<DataBindingItem> _sourceCollectionCopy = new();
        private readonly ObservableCollection<DataBindingItemViewModel> _editedCollection = new();

        #endregion
    }

    /// <summary>
    ///     A list of objects of one kind, edited as a table in a dialog.
    /// </summary>
    public class SameTypeCloneableObjectsListTypeEditor : DialogTypeEditorBase
    {
        #region protected functions

        protected override async Task OnButtonClickAsync()
        {
            if (PropertyItem.Value is null) return;

            var dialog = new SameTypeCloneableObjectsListEditorDialog
            {
                Collection = PropertyItem.Value
            };

            if (await ShowDialogAsync(dialog)) PropertyItem.Value = dialog.Collection;
        }

        #endregion
    }

    /// <summary>
    ///     A collection of objects that need not all be of one kind; each is edited on its own.
    /// </summary>
    public class MiscTypeCloneableObjectsCollectionTypeEditor : DialogTypeEditorBase
    {
        #region protected functions

        protected override async Task OnButtonClickAsync()
        {
            if (PropertyItem.Value is not IList originalCollection) return;

            // The WPF editor opened the toolkit's collection dialog here. There is none in Avalonia,
            // so the items are edited one after another, which is what that dialog amounted to.
            foreach (ICloneable item in originalCollection.OfType<ICloneable>().ToArray())
            {
                ICloneable? edited =
                    await CloneableObjectPropertiesDialog.ShowDialogAsync(OwnerWindow, item);
                if (edited is null) continue;

                var index = originalCollection.IndexOf(item);
                if (index >= 0) originalCollection[index] = edited;
            }

            PropertyItem.NotifyValueChanged();
        }

        #endregion
    }

    /// <summary>
    ///     The look of a control of a shape, chosen from the styles that shape offers.
    /// </summary>
    public class DsUIElementPropertyTypeEditor<T> : DialogTypeEditorBase
        where T : DsUIElementPropertySupplier
    {
        #region protected functions

        protected override async Task OnButtonClickAsync()
        {
            if (PropertyItem.Value is not DsUIElementProperty original) return;

            var dialog = new DsUIElementPropertyEditorDialog(typeof(T))
            {
                StyleInfo = (DsUIElementProperty) original.Clone()
            };

            if (await ShowDialogAsync(dialog)) PropertyItem.Value = dialog.StyleInfo;
        }

        #endregion
    }
}
