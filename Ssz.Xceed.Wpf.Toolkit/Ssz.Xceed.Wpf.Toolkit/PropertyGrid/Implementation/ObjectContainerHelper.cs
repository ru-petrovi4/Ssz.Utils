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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Ssz.Xceed.Wpf.Toolkit.PropertyGrid
{
    internal class ObjectContainerHelper : ObjectContainerHelperBase
    {
        public ObjectContainerHelper(IPropertyContainer propertyContainer, object selectedObject)
            : base(propertyContainer)
        {
            SelectedObject = selectedObject;
        }

        private object SelectedObject { get; }

        protected override string GetDefaultPropertyName()
        {
            var selectedObject = SelectedObject;
            return selectedObject != null ? GetDefaultPropertyName(SelectedObject) : null;
        }

        protected override IEnumerable<PropertyItem> GenerateSubPropertiesCore()
        {
            var propertyItems = new List<PropertyItem>();

            if (SelectedObject != null)
                try
                {
                    var descriptors = GetPropertyDescriptors(SelectedObject);
                    foreach (var descriptor in descriptors)
                    {
                        var propertyDef = GetPropertyDefinition(descriptor);
                        var isBrowsable = descriptor.IsBrowsable && PropertyContainer.AutoGenerateProperties;
                        if (propertyDef != null) isBrowsable = propertyDef.IsBrowsable.GetValueOrDefault(isBrowsable);
                        if (isBrowsable) propertyItems.Add(CreatePropertyItem(descriptor, propertyDef));
                    }
                }
                catch (Exception e)
                {
                    //TODO: handle this some how
                    Debug.WriteLine("Property creation failed");
                    Debug.WriteLine(e.StackTrace);
                }

            return propertyItems;
        }


        private PropertyItem CreatePropertyItem(PropertyDescriptor property, PropertyDefinition propertyDef)
        {
            var definition =
                new DescriptorPropertyDefinition(property, SelectedObject, PropertyContainer.IsCategorized);
            definition.InitProperties();

            InitializeDescriptorDefinition(definition, propertyDef);

            var propertyItem = new PropertyItem(definition);
            Debug.Assert(SelectedObject != null);
            propertyItem.Instance = SelectedObject;
            propertyItem.CategoryOrder = GetCategoryOrder(definition.CategoryValue);

            return propertyItem;
        }

        private int GetCategoryOrder(object categoryValue)
        {
            Debug.Assert(SelectedObject != null);

            if (categoryValue == null)
                return int.MaxValue;

            var order = int.MaxValue;
            var selectedObject = SelectedObject;
            var orderAttributes = selectedObject != null
                ? (CategoryOrderAttribute[]) selectedObject.GetType()
                    .GetCustomAttributes(typeof(CategoryOrderAttribute), true)
                : new CategoryOrderAttribute[0];

            var orderAttribute = orderAttributes
                .FirstOrDefault(a => Equals(a.CategoryValue, categoryValue));

            if (orderAttribute != null) order = orderAttribute.Order;

            return order;
        }
    }
}