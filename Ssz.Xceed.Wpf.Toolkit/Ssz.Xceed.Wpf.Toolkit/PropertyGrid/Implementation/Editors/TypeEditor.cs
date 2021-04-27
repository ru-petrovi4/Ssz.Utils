/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Ssz.Xceed.Wpf.Toolkit.Primitives;

namespace Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors
{
    public abstract class TypeEditor<T> : ITypeEditor where T : FrameworkElement, new()
    {
        #region ITypeEditor Members

        public virtual FrameworkElement ResolveEditor(PropertyItem propertyItem)
        {
            Editor = CreateEditor();
            SetValueDependencyProperty();
            SetControlProperties();
            ResolveValueBinding(propertyItem);
            return Editor;
        }

        #endregion //ITypeEditor Members

        #region Properties

        protected T Editor { get; set; }

        protected DependencyProperty ValueProperty { get; set; }

        #endregion //Properties

        #region Methods

        protected virtual T CreateEditor()
        {
            return new();
        }

        protected virtual IValueConverter CreateValueConverter()
        {
            return null;
        }

        protected virtual void ResolveValueBinding(PropertyItem propertyItem)
        {
            var binding = new Binding("Value");
            binding.Source = propertyItem;
            if (Editor is InputBase) binding.UpdateSourceTrigger = UpdateSourceTrigger.LostFocus;
            else if (Editor is TextBox) binding.UpdateSourceTrigger = UpdateSourceTrigger.LostFocus;
            else binding.UpdateSourceTrigger = UpdateSourceTrigger.Default;
            binding.Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay;
            binding.Converter = CreateValueConverter();
            BindingOperations.SetBinding(Editor, ValueProperty, binding);

            binding = new Binding("IsValueEditorEnabled");
            binding.Source = propertyItem;
            binding.Mode = BindingMode.OneWay;
            BindingOperations.SetBinding(Editor, UIElement.IsEnabledProperty, binding);
        }

        protected virtual void SetControlProperties()
        {
            //TODO: implement in derived class
        }

        protected abstract void SetValueDependencyProperty();

        #endregion //Methods
    }
}