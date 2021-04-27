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
using System.Windows;
using System.Windows.Data;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Ssz.Xceed.Wpf.Toolkit.PropertyGrid
{
    internal class DescriptorPropertyDefinition : DescriptorPropertyDefinitionBase
    {
        #region Constructor

        internal DescriptorPropertyDefinition(PropertyDescriptor propertyDescriptor, object selectedObject,
            bool isPropertyGridCategorized)
            : base(isPropertyGridCategorized)
        {
            if (propertyDescriptor == null)
                throw new ArgumentNullException("propertyDescriptor");

            if (selectedObject == null)
                throw new ArgumentNullException("selectedObject");

            PropertyDescriptor = propertyDescriptor;
            SelectedObject = selectedObject;
            _dpDescriptor = DependencyPropertyDescriptor.FromProperty(propertyDescriptor);
            //_markupObject = MarkupWriter.GetMarkupObjectFor( SelectedObject );
        }

        #endregion

        #region Private Methods

        private T GetAttribute<T>() where T : Attribute
        {
            return PropertyGridUtilities.GetAttribute<T>(PropertyDescriptor);
        }

        #endregion //Private Methods

        #region Members

        private readonly DependencyPropertyDescriptor _dpDescriptor;

        #endregion

        #region Custom Properties

        internal override PropertyDescriptor PropertyDescriptor { get; }

        private object SelectedObject { get; }

        #endregion

        #region Override Methods

        internal override ObjectContainerHelperBase CreateContainerHelper(IPropertyContainer parent)
        {
            return new ObjectContainerHelper(parent, Value);
        }

        internal override void OnValueChanged(object oldValue, object newValue)
        {
            base.OnValueChanged(oldValue, newValue);
            RaiseContainerHelperInvalidated();
        }

        protected override BindingBase CreateValueBinding()
        {
            //Bind the value property with the source object.
            var binding = new Binding(PropertyDescriptor.Name)
            {
                Source = SelectedObject,
                Mode = IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay,
                ValidatesOnDataErrors = true,
                ValidatesOnExceptions = true
            };

            return binding;
        }

        protected override bool ComputeIsReadOnly()
        {
            return PropertyDescriptor.IsReadOnly ||
                   PropertyDescriptor.Attributes.OfType<ReadOnlyInEditorAttribute>().Any();
        }

        internal override ITypeEditor CreateDefaultEditor()
        {
            return PropertyGridUtilities.CreateDefaultEditor(PropertyDescriptor.PropertyType,
                PropertyDescriptor.Converter);
        }

        protected override bool ComputeCanResetValue()
        {
            return PropertyDescriptor.CanResetValue(SelectedObject)
                   && !IsReadOnly;
        }

        protected override object ComputeAdvancedOptionsTooltip()
        {
            object tooltip;
            UpdateAdvanceOptionsForItem(null, SelectedObject as DependencyObject, _dpDescriptor, out tooltip);

            return tooltip;
        }

        protected override string ComputeCategory()
        {
            return PropertyDescriptor.Category;
        }

        protected override string ComputeCategoryValue()
        {
            return PropertyDescriptor.Category;
        }

        protected override bool ComputeExpandableAttribute()
        {
            return (bool) ComputeExpandableAttributeForItem(PropertyDescriptor);
        }

        protected override string ComputeValuePropertyPath()
        {
            return ComputeValuePropertyPathForItem(PropertyDescriptor);
        }

        protected override string ComputeIsValueEditorEnabledPropertyPath()
        {
            return ComputeIsValueEditorEnabledPropertyPathForItem(PropertyDescriptor);
        }

        protected override bool ComputeIsExpandable()
        {
            return Value != null;
        }

        protected override IList<Type> ComputeNewItemTypes()
        {
            return (IList<Type>) ComputeNewItemTypesForItem(PropertyDescriptor);
        }

        protected override string ComputeDescription()
        {
            return (string) ComputeDescriptionForItem(PropertyDescriptor);
        }

        protected override int ComputeDisplayOrder(bool isPropertyGridCategorized)
        {
            IsPropertyGridCategorized = isPropertyGridCategorized;
            return (int) ComputeDisplayOrderForItem(PropertyDescriptor);
        }

        protected override void ResetValue()
        {
            PropertyDescriptor.ResetValue(SelectedObject);
        }

        internal override ITypeEditor CreateAttributeEditor()
        {
            var editorAttribute = GetAttribute<EditorAttribute>();
            if (editorAttribute != null)
            {
                var type = Type.GetType(editorAttribute.EditorTypeName);

                // If the editor does not have any public parameterless constructor, forget it.
                if (typeof(ITypeEditor).IsAssignableFrom(type)
                    && type.GetConstructor(new Type[0]) != null)
                {
                    var instance = Activator.CreateInstance(type) as ITypeEditor;
                    Debug.Assert(instance != null, "Type was expected to be ITypeEditor with public constructor.");
                    if (instance != null)
                        return instance;
                }
            }

            var itemsSourceAttribute = GetAttribute<ItemsSourceAttribute>();
            if (itemsSourceAttribute != null)
                return new ItemsSourceAttributeEditor(itemsSourceAttribute);

            return null;
        }

        #endregion
    }
}