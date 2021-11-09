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
    public class EditorTemplateDefinition : EditorDefinitionBase
    {
        protected sealed override FrameworkElement GenerateEditingElement(PropertyItemBase propertyItem)
        {
            return EditingTemplate is not null
                ? EditingTemplate.LoadContent() as FrameworkElement
                : null;
        }


        #region EditingTemplate

        public static readonly DependencyProperty EditingTemplateProperty =
            DependencyProperty.Register("EditingTemplate", typeof(DataTemplate), typeof(EditorTemplateDefinition),
                new UIPropertyMetadata(null));

        public DataTemplate EditingTemplate
        {
            get => (DataTemplate) GetValue(EditingTemplateProperty);
            set => SetValue(EditingTemplateProperty, value);
        }

        #endregion //EditingTemplate
    }
}