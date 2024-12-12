using System;
using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Ssz.Operator.Core.ControlsPlay.PanoramaPlay.Map3D
{
    public class TagsFilteredComboBox : ComboBox
    {
        #region private fields

        private string _filter = string.Empty;

        #endregion

        #region construction and destruction

        public TagsFilteredComboBox()
        {
            IsEditable = true;
            IsTextSearchEnabled = false;

            var itemFactory = new FrameworkElementFactory(typeof(TextBlock));
            itemFactory.SetBinding(TextBlock.TextProperty, new Binding
            {
                Path = new PropertyPath("ValueToDisplay")
            });
            ItemTemplate = new DataTemplate
            {
                VisualTree = itemFactory
            };

            SelectionChanged += (sender, args) =>
            {
                var thisControl = (TagsFilteredComboBox) sender;
                if (thisControl.SelectedIndex == -1)
                {
                    var currentItemChanged = CurrentItemChanged;
                    if (currentItemChanged is not null) currentItemChanged(null);
                    return;
                }

                var tagViewModel = (TagViewModel) thisControl.SelectedValue;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    thisControl.SelectedIndex = -1;
                    thisControl.Text = tagViewModel.Tag;
                    var myTextBox = thisControl.EditableTextBox;
                    if (myTextBox is not null) myTextBox.SelectAll();
                    thisControl.RefreshFilter();

                    var currentItemChanged = thisControl.CurrentItemChanged;
                    if (currentItemChanged is not null) currentItemChanged(tagViewModel);
                }));
            };
        }

        #endregion

        #region public functions

        public TextBox? EditableTextBox => GetTemplateChild("PART_EditableTextBox") as TextBox;

        public event Action<TagViewModel?>? CurrentItemChanged;

        #endregion

        #region protected functions

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            if (newValue is not null)
            {
                ICollectionView view = CollectionViewSource.GetDefaultView(newValue);
                view.Filter = FilterPredicate;
            }

            if (oldValue is not null)
            {
                ICollectionView view = CollectionViewSource.GetDefaultView(oldValue);
                view.Filter = null;
            }

            base.OnItemsSourceChanged(oldValue, newValue);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (e.Key == Key.Down)
            {
                IsDropDownOpen = true;
                return;
            }

            RefreshFilter();
        }

        #endregion

        #region private functions

        private void RefreshFilter()
        {
            string text = Text ?? "";
            var myTextBox = EditableTextBox;
            if (myTextBox is null) return;

            string newFilter;
            var selectionStart = myTextBox.SelectionStart;
            if (selectionStart <= 0 || text.Length == 0)
                newFilter = "";
            else if (selectionStart >= text.Length)
                newFilter = text;
            else
                newFilter = text.Substring(0, selectionStart);

            if (newFilter != _filter)
            {
                _filter = newFilter;
                // Clear the filter if the text is empty,
                // apply the filter if the text is long enough
                if (_filter.Length == 0 || _filter.Length >= 3)
                    if (ItemsSource is not null)
                    {
                        ICollectionView view = CollectionViewSource.GetDefaultView(ItemsSource);
                        view.Refresh();
                    }
            }
        }


        private bool FilterPredicate(object value)
        {
            var tvm = value as TagViewModel;
            // No filter, no text
            if (tvm is null) return false;

            // No text, no filter
            if (_filter.Length == 0) return true;

            // Case insensitive search
            return tvm.ValueToDisplay.IndexOf(_filter) >= 0;
        }

        #endregion
    }
}