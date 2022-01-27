using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Ssz.Utils.Wpf
{
    [TemplatePart(Name = "PART_ItemsHolder", Type = typeof (Panel))]
    public class TabControlWithViewCache : TabControl
    {
        #region construction and destruction

        public TabControlWithViewCache()
        {
            // This is necessary so that we get the initial databound selected item
            ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Get the ItemsHolder and generate any children
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _itemsHolderPanel = GetTemplateChild("PART_ItemsHolder") as Panel;
            UpdateSelectedItem();
        }

        #endregion

        #region protected functions

        /// <summary>
        ///     When the items change we remove any generated panel children and add any new ones as necessary
        /// </summary>
        /// <param name="e"></param>
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            if (_itemsHolderPanel == null)
                return;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    _itemsHolderPanel.Children.Clear();
                    break;

                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        foreach (object item in e.OldItems)
                        {
                            ContentPresenter? cp = FindChildContentPresenter(item);
                            if (cp is not null)
                                _itemsHolderPanel.Children.Remove(cp);
                        }
                    }

                    // Don't do anything with new items because we don't want to
                    // create visuals that aren't being shown

                    UpdateSelectedItem();
                    break;

                case NotifyCollectionChangedAction.Replace:
                    throw new NotImplementedException("Replace not implemented yet");
            }
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            UpdateSelectedItem();
        }

        protected TabItem? GetSelectedTabItem()
        {
            object? selectedItem = base.SelectedItem;
            if (selectedItem is null)
                return null;

            var item = selectedItem as TabItem;
            if (item == null)
                item = base.ItemContainerGenerator.ContainerFromIndex(base.SelectedIndex) as TabItem;

            return item;
        }

        #endregion

        #region private functions

        /// <summary>
        ///     If containers are done, generate the selected item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ItemContainerGenerator_StatusChanged(object? sender, EventArgs e)
        {
            if (ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                ItemContainerGenerator.StatusChanged -= ItemContainerGenerator_StatusChanged;
                UpdateSelectedItem();
            }
        }

        private void UpdateSelectedItem()
        {
            if (_itemsHolderPanel == null)
                return;

            // Generate a ContentPresenter if necessary
            TabItem? item = GetSelectedTabItem();
            if (item is not null)
                CreateChildContentPresenter(item);

            // show the right child
			foreach (ContentPresenter child in _itemsHolderPanel.Children)
			{
				var tab = child.Tag as TabItem;
				if (tab != null)
				{
					child.Visibility = tab.IsSelected ? Visibility.Visible : Visibility.Collapsed;
				}
			}
        }

        private ContentPresenter? CreateChildContentPresenter(object? item)
        {
            if (item is null)
                return null;

            ContentPresenter? cp = FindChildContentPresenter(item);

            if (cp is not null)
                return cp;

            // the actual child to be added.  cp.Tag is a reference to the TabItem
            cp = new ContentPresenter();
            if (item is TabItem tabItem)
                cp.Content = tabItem.Content;
            else
                cp.Content = item;            
            cp.ContentTemplate = SelectedContentTemplate;
            cp.ContentTemplateSelector = SelectedContentTemplateSelector;
            cp.ContentStringFormat = SelectedContentStringFormat;
            cp.Visibility = Visibility.Collapsed;
            cp.Tag = (item is TabItem) ? item : (ItemContainerGenerator.ContainerFromItem(item));
            _itemsHolderPanel!.Children.Add(cp);
            return cp;
        }

        private ContentPresenter? FindChildContentPresenter(object? data)
        {
            if (data is TabItem tabItem)
                data = tabItem.Content;

            if (data is null)
                return null;

            if (_itemsHolderPanel is null)
                return null;

            foreach (ContentPresenter cp in _itemsHolderPanel.Children)
            {
                if (cp.Content == data)
                    return cp;
            }

            return null;
        }

        #endregion

        #region private fields

        private Panel? _itemsHolderPanel;

        #endregion
    }
}