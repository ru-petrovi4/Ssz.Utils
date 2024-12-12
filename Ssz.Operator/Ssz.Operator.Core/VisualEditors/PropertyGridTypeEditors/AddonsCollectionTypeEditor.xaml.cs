using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.VisualEditors.Windows;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Implementation.Editors;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors
{
    public partial class AddonsCollectionTypeEditor : UserControl, ITypeEditor, IPropertyGridItem
    {
        #region private fields

        private ObservableCollection<AddonBase>? _addonCollection;

        #endregion

        #region construction and destruction

        public AddonsCollectionTypeEditor()
        {
            InitializeComponent();
        }

        #endregion

        #region private functions

        private void AddRemoveAddonsButtonOnClick(object? sender, RoutedEventArgs e)
        {
            if (!DsProject.Instance.IsInitialized) return;

            var dialog = new AddonsCollectionEditorDialog
            {
                Owner = Window.GetWindow(this)
            };

            dialog.ShowDialog();
        }

        #endregion

        #region public functions

        public FrameworkElement ResolveEditor(PropertyItem propertyItem)
        {
            _addonCollection = (ObservableCollection<AddonBase>) propertyItem.Value;

            MainDataGrid.ItemsSource = _addonCollection;
            ICollectionView dataView = CollectionViewSource.GetDefaultView(MainDataGrid.ItemsSource);
            dataView.SortDescriptions.Clear();
            dataView.SortDescriptions.Add(new SortDescription(nameof(AddonBase.Name), ListSortDirection.Ascending));
            dataView.Refresh();

            return this;
        }

        public bool RefreshForPropertyGridIsDisabled { get; set; }

        public void RefreshForPropertyGrid()
        {
        }

        public void EndEditInPropertyGrid()
        {
            MainDataGrid.ItemsSource = null;
        }

        #endregion
    }
}