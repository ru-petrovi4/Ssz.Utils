/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using ItemCollection = Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes.ItemCollection;

namespace Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors
{
    public class ItemsSourceAttributeEditor : TypeEditor<ComboBox>
    {
        private readonly ItemsSourceAttribute _attribute;

        public ItemsSourceAttributeEditor(ItemsSourceAttribute attribute)
        {
            _attribute = attribute;
        }

        protected override void SetValueDependencyProperty()
        {
            ValueProperty = ComboBox.TextProperty;
        }

        protected override ComboBox CreateEditor()
        {
            return new PropertyGridEditorComboBox();
        }

        protected override void ResolveValueBinding(PropertyItem propertyItem)
        {
            SetItemsSource();
            base.ResolveValueBinding(propertyItem);
            if (!Editor.IsEditable)
            {
                var index = 0;
                foreach (var item in (ItemCollection) Editor.ItemsSource)
                {
                    if (Equals(item.Value, propertyItem.Value))
                    {
                        Editor.SelectedIndex = index;
                        break;
                    }

                    index++;
                }
            }
        }

        protected override IValueConverter CreateValueConverter()
        {
            if (_attribute.IsEditable) return null;
            return new EditorValueConverter((ItemCollection) Editor.ItemsSource);
        }

        protected override void SetControlProperties()
        {
            Editor.DisplayMemberPath = "DisplayName";
            Editor.SelectedValuePath = "Value";
            Editor.IsEditable = _attribute.IsEditable;
            if (Editor.IsEditable) Editor.SelectionChanged += OnSelectionChanged;
            Editor.IsTextSearchEnabled = false;
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Editor.SelectedIndex == -1) return;
            var text = Editor.SelectedValue.ToString();
            Editor.Dispatcher.BeginInvoke(
                new Action(
                    () =>
                    {
                        Editor.SelectedItem = null;
                        Editor.SetValue(ComboBox.TextProperty, text);
                    })
            );
        }

        private void SetItemsSource()
        {
            Editor.ItemsSource = CreateItemsSource();
        }

        private IEnumerable CreateItemsSource()
        {
            var instance = Activator.CreateInstance(_attribute.Type);
            return (instance as IItemsSource).GetValues();
        }

        private class EditorValueConverter : IValueConverter
        {
            private readonly ItemCollection _itemCollection;

            public EditorValueConverter(ItemCollection itemCollection)
            {
                _itemCollection = itemCollection;
            }

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                foreach (var item in _itemCollection)
                    if (Equals(item.Value, value))
                        return item.DisplayName;
                return DependencyProperty.UnsetValue;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                var valueString = value as string;
                foreach (var item in _itemCollection)
                    if (Equals(item.DisplayName, valueString))
                        return item.Value;
                return Binding.DoNothing;
            }
        }
    }
}