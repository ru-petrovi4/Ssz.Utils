/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors
{
    public abstract class ComboBoxEditor : TypeEditor<ComboBox>
    {
        protected override void SetValueDependencyProperty()
        {
            ValueProperty = Selector.SelectedItemProperty;
        }

        protected override ComboBox CreateEditor()
        {
            return new PropertyGridEditorComboBox();
        }

        protected override void ResolveValueBinding(PropertyItem propertyItem)
        {
            SetItemsSource(propertyItem);
            base.ResolveValueBinding(propertyItem);
        }

        protected abstract IEnumerable CreateItemsSource(PropertyItem propertyItem);

        private void SetItemsSource(PropertyItem propertyItem)
        {
            Editor.ItemsSource = CreateItemsSource(propertyItem);
        }
    }

    public class PropertyGridEditorComboBox : ComboBox
    {
        static PropertyGridEditorComboBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PropertyGridEditorComboBox),
                new FrameworkPropertyMetadata(typeof(PropertyGridEditorComboBox)));
        }
    }
}