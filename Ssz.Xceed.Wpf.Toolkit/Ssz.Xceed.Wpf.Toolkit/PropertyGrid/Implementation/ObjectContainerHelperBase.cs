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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Ssz.Xceed.Wpf.Toolkit.Core.Utilities;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Ssz.Xceed.Wpf.Toolkit.PropertyGrid
{
    internal abstract class ObjectContainerHelperBase : ContainerHelperBase
    {
        // This is needed to work around the ItemsControl behavior.
        // When ItemsControl is preparing its containers, it appears 
        // that calling Refresh() on the CollectionView bound to
        // the ItemsSource prevents the items from being displayed.
        // This patch is to avoid such a behavior.
        private bool _isPreparingItemFlag;

        public ObjectContainerHelperBase(IPropertyContainer propertyContainer)
            : base(propertyContainer)
        {
            PropertyItems = new PropertyItemCollection(new ObservableCollection<PropertyItem>());
            UpdateFilter();
            UpdateCategorization();
        }

        public override IList Properties => PropertyItems;

        private PropertyItem DefaultProperty
        {
            get
            {
                PropertyItem defaultProperty = null;
                var defaultName = GetDefaultPropertyName();
                if (defaultName is not null)
                    defaultProperty = PropertyItems
                        .FirstOrDefault(prop => Equals(defaultName, prop.PropertyDescriptor.Name));

                return defaultProperty;
            }
        }


        protected PropertyItemCollection PropertyItems { get; }

        public override PropertyItemBase ContainerFromItem(object item)
        {
            if (item is null)
                return null;
            // Exception case for ObjectContainerHelperBase. The "Item" may sometimes
            // be identified as a string representing the property name or
            // the PropertyItem itself.
            Debug.Assert(item is PropertyItem || item is string);

            var propertyItem = item as PropertyItem;
            if (propertyItem is not null)
                return propertyItem;


            var propertyStr = item as string;
            if (propertyStr is not null)
                return PropertyItems.FirstOrDefault(prop => propertyStr == prop.PropertyDescriptor.Name);

            return null;
        }

        public override object ItemFromContainer(PropertyItemBase container)
        {
            // Since this call is only used to update the PropertyGrid.SelectedProperty property,
            // return the PropertyName.
            var propertyItem = container as PropertyItem;
            if (propertyItem is null)
                return null;

            return propertyItem.PropertyDescriptor.Name;
        }

        public override void UpdateValuesFromSource()
        {
            foreach (var item in PropertyItems)
            {
                item.DescriptorDefinition.UpdateValueFromSource();
                item.ContainerHelper.UpdateValuesFromSource();
            }
        }

        public void GenerateProperties()
        {
            if (PropertyItems.Count == 0) RegenerateProperties();
        }

        protected override void OnFilterChanged()
        {
            UpdateFilter();
        }

        protected override void OnCategorizationChanged()
        {
            UpdateCategorization();
        }

        protected override void OnAutoGeneratePropertiesChanged()
        {
            RegenerateProperties();
        }

        protected override void OnEditorDefinitionsChanged()
        {
            RegenerateProperties();
        }

        protected override void OnPropertyDefinitionsChanged()
        {
            RegenerateProperties();
        }


        private void UpdateFilter()
        {
            var filterInfo = PropertyContainer.FilterInfo;

            PropertyItems.FilterPredicate = filterInfo.Predicate
                                            ?? PropertyItemCollection.CreateFilter(filterInfo.InputString);
        }

        private void UpdateCategorization()
        {
            PropertyItems.UpdateCategorization(ComputeCategoryGroupDescription(), PropertyContainer.IsCategorized);
        }

        private GroupDescription ComputeCategoryGroupDescription()
        {
            if (!PropertyContainer.IsCategorized)
                return null;
            return new PropertyGroupDescription(PropertyItemCollection.CategoryPropertyName);
        }

        private string GetCategoryGroupingPropertyName()
        {
            var propGroup = ComputeCategoryGroupDescription() as PropertyGroupDescription;
            return propGroup is not null ? propGroup.PropertyName : null;
        }

        private void OnChildrenPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (IsItemOrderingProperty(e.PropertyName)
                || GetCategoryGroupingPropertyName() == e.PropertyName)
                // Refreshing the view while Containers are generated will throw an exception
                if (ChildrenItemsControl.ItemContainerGenerator.Status != GeneratorStatus.GeneratingContainers
                    && !_isPreparingItemFlag)
                    PropertyItems.RefreshView();
        }

        protected abstract string GetDefaultPropertyName();

        protected abstract IEnumerable<PropertyItem> GenerateSubPropertiesCore();

        private void RegenerateProperties()
        {
            var subProperties = GenerateSubPropertiesCore();

            var uiParent = PropertyContainer as UIElement;

            foreach (var propertyItem in subProperties) InitializePropertyItem(propertyItem);

            //Remove the event callback from the previous children (if any)
            foreach (var propertyItem in PropertyItems) propertyItem.PropertyChanged -= OnChildrenPropertyChanged;


            PropertyItems.UpdateItems(subProperties);

            //Add the event callback to the new childrens
            foreach (var propertyItem in PropertyItems) propertyItem.PropertyChanged += OnChildrenPropertyChanged;

            // Update the selected property on the property grid only.
            var propertyGrid = PropertyContainer as PropertyGrid;
            if (propertyGrid is not null) propertyGrid.SelectedPropertyItem = DefaultProperty;
        }

        protected static List<PropertyDescriptor> GetPropertyDescriptors(object instance)
        {
            PropertyDescriptorCollection descriptors;

            var tc = TypeDescriptor.GetConverter(instance);
            if (tc is null || !tc.GetPropertiesSupported())
            {
                if (instance is ICustomTypeDescriptor)
                    descriptors = ((ICustomTypeDescriptor) instance).GetProperties();
                else
                    descriptors = TypeDescriptor.GetProperties(instance.GetType());
            }
            else
            {
                descriptors = tc.GetProperties(instance);
            }

            return descriptors is not null
                ? descriptors.Cast<PropertyDescriptor>().ToList()
                : null;
        }

        internal void InitializeDescriptorDefinition(
            DescriptorPropertyDefinitionBase descriptorDef,
            PropertyDefinition propertyDefinition)
        {
            if (descriptorDef is null)
                throw new ArgumentNullException("descriptorDef");

            if (propertyDefinition is null)
                return;

            // Values defined on PropertyDefinition have priority on the attributes
            if (propertyDefinition is not null)
            {
                if (propertyDefinition.Category is not null)
                {
                    descriptorDef.Category = propertyDefinition.Category;
                    descriptorDef.CategoryValue = propertyDefinition.Category;
                }

                if (propertyDefinition.Description is not null) descriptorDef.Description = propertyDefinition.Description;

                if (propertyDefinition.DisplayName is not null) descriptorDef.DisplayName = propertyDefinition.DisplayName;

                if (propertyDefinition.DisplayOrder is not null)
                    descriptorDef.DisplayOrder = propertyDefinition.DisplayOrder.Value;

                if (propertyDefinition.IsExpandable is not null)
                    descriptorDef.ExpandableAttribute = propertyDefinition.IsExpandable.Value;
            }
        }

        private void InitializePropertyItem(PropertyItem propertyItem)
        {
            BindingOperations.ClearAllBindings(propertyItem);
            var pd = propertyItem.DescriptorDefinition;
            propertyItem.PropertyDescriptor = pd.PropertyDescriptor;

            if (string.IsNullOrWhiteSpace(pd.ValuePropertyPath)) propertyItem.IsReadOnly = pd.IsReadOnly;
            else propertyItem.IsReadOnly = false;

            propertyItem.DisplayName = pd.DisplayName;
            propertyItem.Description = pd.Description;
            propertyItem.Category = pd.Category;
            propertyItem.PropertyOrder = pd.DisplayOrder;

            //These properties can vary with the value. They need to be bound.
            SetupDefinitionBinding(propertyItem, PropertyItemBase.IsExpandableProperty, pd, () => pd.IsExpandable,
                BindingMode.OneWay);
            SetupDefinitionBinding(propertyItem, PropertyItemBase.AdvancedOptionsIconProperty, pd,
                () => pd.AdvancedOptionsIcon, BindingMode.OneWay);
            SetupDefinitionBinding(propertyItem, PropertyItemBase.AdvancedOptionsTooltipProperty, pd,
                () => pd.AdvancedOptionsTooltip, BindingMode.OneWay);
            if (!string.IsNullOrWhiteSpace(pd.ValuePropertyPath))
            {
                if (pd.PropertyType.GetProperty(pd.ValuePropertyPath).CanWrite)
                    SetupDefinitionBinding(propertyItem, CustomPropertyItem.ValueProperty, pd.Value,
                        pd.ValuePropertyPath,
                        BindingMode.TwoWay);
                else
                    SetupDefinitionBinding(propertyItem, CustomPropertyItem.ValueProperty, pd.Value,
                        pd.ValuePropertyPath,
                        BindingMode.OneWay);
            }
            else
            {
                SetupDefinitionBinding(propertyItem, CustomPropertyItem.ValueProperty, pd, () => pd.Value,
                    BindingMode.TwoWay);
            }

            if (!string.IsNullOrWhiteSpace(pd.IsValueEditorEnabledPropertyPath))
                SetupDefinitionBinding(propertyItem, CustomPropertyItem.IsValueEditorEnabledProperty, pd.Value,
                    pd.IsValueEditorEnabledPropertyPath, BindingMode.OneWay);

            if (pd.CommandBindings is not null)
                foreach (var commandBinding in pd.CommandBindings)
                    propertyItem.CommandBindings.Add(commandBinding);
        }

        private void SetupDefinitionBinding<T>(
            PropertyItem propertyItem,
            DependencyProperty itemProperty,
            DescriptorPropertyDefinitionBase pd,
            Expression<Func<T>> definitionProperty,
            BindingMode bindingMode)
        {
            var sourceProperty = ReflectionHelper.GetPropertyOrFieldName(definitionProperty);
            SetupDefinitionBinding(propertyItem, itemProperty, pd, sourceProperty, bindingMode);
        }

        private void SetupDefinitionBinding(
            PropertyItem propertyItem,
            DependencyProperty itemProperty,
            object source,
            string sourceProperty,
            BindingMode bindingMode)
        {
            var binding = new Binding(sourceProperty)
            {
                Source = source,
                Mode = bindingMode
            };

            propertyItem.SetBinding(itemProperty, binding);
        }

        private FrameworkElement GenerateChildrenEditorElement(PropertyItem propertyItem)
        {
            FrameworkElement editorElement = null;
            var pd = propertyItem.DescriptorDefinition;
            object definitionKey = null;
            var definitionKeyAsType = definitionKey as Type;

            var editor = pd.CreateAttributeEditor();
            if (editor is not null)
                editorElement = editor.ResolveEditor(propertyItem);


            if (editorElement is null && definitionKey is null)
                editorElement = GenerateCustomEditingElement(propertyItem.PropertyDescriptor.Name, propertyItem);

            if (editorElement is null && definitionKeyAsType is null)
                editorElement = GenerateCustomEditingElement(propertyItem.PropertyType, propertyItem);

            if (editorElement is null)
            {
                if (pd.IsReadOnly)
                    editor = new TextBlockEditor();

                // Fallback: Use a default type editor.
                if (editor is null)
                    editor = definitionKeyAsType is not null
                        ? PropertyGridUtilities.CreateDefaultEditor(definitionKeyAsType, null)
                        : pd.CreateDefaultEditor();

                Debug.Assert(editor is not null);

                editorElement = editor.ResolveEditor(propertyItem);
            }

            return editorElement;
        }

        internal PropertyDefinition GetPropertyDefinition(PropertyDescriptor descriptor)
        {
            PropertyDefinition def = null;

            var propertyDefs = PropertyContainer.PropertyDefinitions;
            if (propertyDefs is not null)
            {
                def = propertyDefs[descriptor.Name];
                if (def is null) def = propertyDefs.GetRecursiveBaseTypes(descriptor.PropertyType);
            }

            return def;
        }


        public override void PrepareChildrenPropertyItem(PropertyItemBase propertyItem, object item)
        {
            _isPreparingItemFlag = true;
            base.PrepareChildrenPropertyItem(propertyItem, item);

            if (propertyItem.Editor is null)
            {
                var editor = GenerateChildrenEditorElement((PropertyItem) propertyItem);
                if (editor is not null)
                {
                    // Tag the editor as generated to know if we should clear it.
                    SetIsGenerated(editor, true);
                    propertyItem.Editor = editor;
                }
            }

            _isPreparingItemFlag = false;
        }

        public override void ClearChildrenPropertyItem(PropertyItemBase propertyItem, object item)
        {
            if (propertyItem.Editor is not null
                && GetIsGenerated(propertyItem.Editor))
                propertyItem.Editor = null;

            base.ClearChildrenPropertyItem(propertyItem, item);
        }

        public override Binding CreateChildrenDefaultBinding(PropertyItemBase propertyItem)
        {
            var binding = new Binding("Value");
            binding.Mode = ((PropertyItem) propertyItem).IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay;
            return binding;
        }

        protected static string GetDefaultPropertyName(object instance)
        {
            var attributes = TypeDescriptor.GetAttributes(instance);
            var defaultPropertyAttribute = (DefaultPropertyAttribute) attributes[typeof(DefaultPropertyAttribute)];
            return defaultPropertyAttribute is not null ? defaultPropertyAttribute.Name : null;
        }

        private static bool IsItemOrderingProperty(string propertyName)
        {
            return propertyName == PropertyItemCollection.DisplayNamePropertyName
                   || propertyName == PropertyItemCollection.CategoryOrderPropertyName
                   || propertyName == PropertyItemCollection.PropertyOrderPropertyName;
        }
    }
}