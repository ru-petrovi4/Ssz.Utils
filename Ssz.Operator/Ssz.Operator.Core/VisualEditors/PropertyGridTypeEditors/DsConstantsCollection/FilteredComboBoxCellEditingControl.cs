using System;
using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Ssz.Operator.Core.Constants;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors.DsConstantsCollection
{
    public class FilteredComboBoxCellEditingControl : ComboBox
    {
        #region construction and destruction

        public FilteredComboBoxCellEditingControl()
        {
            IsEditable = true;
            IsTextSearchEnabled = false;

            var itemFactory = new FrameworkElementFactory(typeof(TextBlock));
            itemFactory.SetBinding(TextBlock.TextProperty, new Binding
            {
                Path = new PropertyPath("ValueToDisplay")
            });
            itemFactory.SetBinding(TextBlock.ForegroundProperty, new Binding
            {
                Path = new PropertyPath("Foreground")
            });
            ItemTemplate = new DataTemplate
            {
                VisualTree = itemFactory
            };

            SetBinding(ItemsSourceProperty,
                new Binding
                {
                    Path = new PropertyPath("ValuesItemsSource")
                });
            SetBinding(TextProperty,
                new Binding
                {
                    Path = new PropertyPath("Value")
                });

            SelectionChanged += (sender, args) =>
            {
                var thisControl = (FilteredComboBoxCellEditingControl) sender;
                if (thisControl.SelectedIndex == -1) return;
                var textBox = thisControl.EditableTextBox;
                _selected = thisControl.SelectedValue as ConstantValueViewModel;
                if (textBox is null) return;
                var text = textBox.Text;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    SelectedIndex = -1;
                    textBox.Text = text;
                    textBox.SelectAll();
                }));
            };
            DropDownClosed += (sender, args) =>
            {
                var thisControl = (FilteredComboBoxCellEditingControl?) sender;
                if (thisControl is null || _selected is null) return;
                var textBox = thisControl.EditableTextBox;
                if (textBox is null) return;
                textBox.Text = _selected.Value;
                textBox.SelectAll();
                thisControl.RefreshFilter();
            };

            Loaded += (sender, args) =>
            {
                TextBox textBox = ((FilteredComboBoxCellEditingControl) sender).EditableTextBox;
                textBox.SelectAll();
                textBox.Focus();
            };
        }

        #endregion

        #region public functions

        public TextBox EditableTextBox => GetTemplateChild("PART_EditableTextBox") as TextBox ??
                                          throw new InvalidOperationException();

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

            _selected = null;

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
            var textBox = EditableTextBox;
            string text = textBox.Text ?? "";
            string newFilter;
            var selectionStart = textBox.SelectionStart;
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
            var vm = value as ConstantValueViewModel;
            // No filter, no text
            if (vm is null) return false;

            // No text, no filter
            if (_filter.Length == 0) return true;

            // Case insensitive search
            return vm.Value.IndexOf(_filter) >= 0;
        }

        #endregion

        #region private fields

        private string _filter = string.Empty;

        private ConstantValueViewModel? _selected;

        #endregion
    }
}