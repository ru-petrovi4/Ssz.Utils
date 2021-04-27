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

namespace Ssz.Xceed.Wpf.Toolkit.PropertyGrid
{
    public abstract class EditorDefinitionBase : PropertyDefinitionBase
    {
        internal EditorDefinitionBase()
        {
        }

        internal FrameworkElement GenerateEditingElementInternal(PropertyItemBase propertyItem)
        {
            return GenerateEditingElement(propertyItem);
        }

        protected virtual FrameworkElement GenerateEditingElement(PropertyItemBase propertyItem)
        {
            return null;
        }

        internal void UpdateProperty(FrameworkElement element, DependencyProperty elementProp,
            DependencyProperty definitionProperty)
        {
            var currentValue = GetValue(definitionProperty);
            var localValue = ReadLocalValue(definitionProperty);
            // Avoid setting values if it does not affect anything 
            // because setting a local value may prevent a style setter from being active.
            if (localValue != DependencyProperty.UnsetValue
                || currentValue != element.GetValue(elementProp))
                element.SetValue(elementProp, currentValue);
            else
                element.ClearValue(elementProp);
        }
    }
}