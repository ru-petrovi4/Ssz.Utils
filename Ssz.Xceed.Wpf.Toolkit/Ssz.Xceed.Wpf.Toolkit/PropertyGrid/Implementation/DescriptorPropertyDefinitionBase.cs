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
using System.Windows.Input;
using System.Windows.Markup.Primitives;
using System.Windows.Media;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Commands;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Ssz.Xceed.Wpf.Toolkit.PropertyGrid
{
    public abstract class DescriptorPropertyDefinitionBase : DependencyObject
    {
        #region Initialization

        internal DescriptorPropertyDefinitionBase(bool isPropertyGridCategorized)
        {
            IsPropertyGridCategorized = isPropertyGridCategorized;
        }

        #endregion

        internal abstract PropertyDescriptor PropertyDescriptor { get; }

        public string Category { get; internal set; }

        public string CategoryValue { get; internal set; }

        public IEnumerable<CommandBinding> CommandBindings { get; private set; }

        public string DisplayName { get; internal set; }

        public string Description { get; internal set; }

        public int DisplayOrder { get; internal set; }

        public bool IsReadOnly { get; private set; }

        public IList<Type> NewItemTypes { get; private set; }

        public string PropertyName =>
            // A common property which is present in all selectedObjects will always have the same name.
            PropertyDescriptor.Name;

        public Type PropertyType => PropertyDescriptor.PropertyType;

        internal bool ExpandableAttribute
        {
            get => _expandableAttribute;
            set
            {
                _expandableAttribute = value;
                UpdateIsExpandable();
            }
        }

        internal string ValuePropertyPath { get; private set; }

        internal string IsValueEditorEnabledPropertyPath { get; private set; }

        internal bool IsPropertyGridCategorized { get; set; }

        #region Events

        public event EventHandler ContainerHelperInvalidated;

        #endregion

        public virtual void InitProperties()
        {
            ValuePropertyPath = ComputeValuePropertyPath();
            IsValueEditorEnabledPropertyPath = ComputeIsValueEditorEnabledPropertyPath();

            // Do "IsReadOnly" and PropertyName first since the others may need that value.
            IsReadOnly = ComputeIsReadOnly();
            Category = ComputeCategory();
            CategoryValue = ComputeCategoryValue();
            Description = ComputeDescription();
            DisplayName = ComputeDisplayName();
            DisplayOrder = ComputeDisplayOrder(IsPropertyGridCategorized);
            _expandableAttribute = ComputeExpandableAttribute();
            NewItemTypes = ComputeNewItemTypes();
            CommandBindings = new CommandBinding[]
                {new(PropertyItemCommands.ResetValue, ExecuteResetValueCommand, CanExecuteResetValueCommand)};

            UpdateIsExpandable();
            UpdateAdvanceOptions();

            var valueBinding = CreateValueBinding();
            BindingOperations.SetBinding(this, ValueProperty, valueBinding);
        }

        #region Members

        private bool _expandableAttribute;

        #endregion

        #region Virtual Methods

        protected virtual string ComputeCategory()
        {
            return null;
        }

        protected virtual string ComputeCategoryValue()
        {
            return null;
        }

        protected virtual string ComputeDescription()
        {
            return null;
        }

        protected virtual int ComputeDisplayOrder(bool isPropertyGridCategorized)
        {
            return int.MaxValue;
        }

        protected virtual bool ComputeExpandableAttribute()
        {
            return false;
        }

        protected virtual string ComputeValuePropertyPath()
        {
            return null;
        }

        protected virtual string ComputeIsValueEditorEnabledPropertyPath()
        {
            return null;
        }

        protected abstract bool ComputeIsExpandable();

        protected virtual IList<Type> ComputeNewItemTypes()
        {
            return null;
        }

        protected virtual bool ComputeIsReadOnly()
        {
            return false;
        }

        protected virtual bool ComputeCanResetValue()
        {
            return false;
        }

        protected virtual object ComputeAdvancedOptionsTooltip()
        {
            return null;
        }

        protected virtual void ResetValue()
        {
        }

        protected abstract BindingBase CreateValueBinding();

        #endregion

        #region Internal Methods

        internal abstract ObjectContainerHelperBase CreateContainerHelper(IPropertyContainer parent);

        internal void RaiseContainerHelperInvalidated()
        {
            if (ContainerHelperInvalidated is not null) ContainerHelperInvalidated(this, EventArgs.Empty);
        }

        internal virtual ITypeEditor CreateDefaultEditor()
        {
            return null;
        }

        internal virtual ITypeEditor CreateAttributeEditor()
        {
            return null;
        }

        internal void UpdateAdvanceOptionsForItem(MarkupObject markupObject, DependencyObject dependencyObject,
            DependencyPropertyDescriptor dpDescriptor,
            out object tooltip)
        {
            tooltip = StringConstants.AdvancedProperties;

            var isResource = false;
            var isDynamicResource = false;

            if (markupObject is not null)
            {
                var markupProperty = markupObject.Properties.Where(p => p.Name == PropertyName).FirstOrDefault();
                if (markupProperty is not null && markupProperty.PropertyType != typeof(object) &&
                    !markupProperty.PropertyType.IsEnum)
                {
                    //TODO: need to find a better way to determine if a StaticResource has been applied to any property not just a style
                    isResource = markupProperty.Value is Style;
                    isDynamicResource = markupProperty.Value is DynamicResourceExtension;
                }
            }

            if (isResource || isDynamicResource)
            {
                tooltip = StringConstants.Resource;
            }
            else
            {
                if (dependencyObject is not null && dpDescriptor is not null)
                {
                    if (BindingOperations.GetBindingExpressionBase(dependencyObject, dpDescriptor.DependencyProperty) !=
                        null)
                    {
                        tooltip = StringConstants.Databinding;
                    }
                    else
                    {
                        var bvs =
                            DependencyPropertyHelper
                                .GetValueSource(dependencyObject, dpDescriptor.DependencyProperty)
                                .BaseValueSource;

                        switch (bvs)
                        {
                            case BaseValueSource.Inherited:
                            case BaseValueSource.DefaultStyle:
                            case BaseValueSource.ImplicitStyleReference:
                                tooltip = StringConstants.Inheritance;
                                break;
                            case BaseValueSource.DefaultStyleTrigger:
                                break;
                            case BaseValueSource.Style:
                                tooltip = StringConstants.StyleSetter;
                                break;

                            case BaseValueSource.Local:
                                tooltip = StringConstants.Local;
                                break;
                        }
                    }
                }
            }
        }

        internal void UpdateAdvanceOptions()
        {
            // Only set the Tooltip. the Icon will be added in XAML based on the Tooltip.
            AdvancedOptionsTooltip = ComputeAdvancedOptionsTooltip();
        }

        internal void UpdateIsExpandable()
        {
            IsExpandable =
                ExpandableAttribute
                && ComputeIsExpandable();
        }

        internal void UpdateValueFromSource()
        {
            var bindingExpressionBase = BindingOperations.GetBindingExpressionBase(this, ValueProperty);
            if (bindingExpressionBase is not null) bindingExpressionBase.UpdateTarget();
        }

        internal object ComputeDescriptionForItem(object item)
        {
            var pd = item as PropertyDescriptor;

            //We do not simply rely on the "Description" property of PropertyDescriptor
            //since this value is cached by PropertyDescriptor and the localized version 
            //(e.g., LocalizedDescriptionAttribute) value can dynamicaly change.
            var descriptionAtt = PropertyGridUtilities.GetAttribute<DescriptionAttribute>(pd);
            return descriptionAtt is not null
                ? descriptionAtt.Description
                : pd.Description;
        }

        internal object ComputeNewItemTypesForItem(object item)
        {
            var pd = item as PropertyDescriptor;
            var attribute = PropertyGridUtilities.GetAttribute<NewItemTypesAttribute>(pd);

            return attribute is not null
                ? attribute.Types
                : null;
        }

        internal object ComputeDisplayOrderForItem(object item)
        {
            var pd = item as PropertyDescriptor;
            var list = pd.Attributes.OfType<PropertyOrderAttribute>().ToList();

            if (list.Count > 0)
            {
                ValidatePropertyOrderAttributes(list);

                if (IsPropertyGridCategorized)
                {
                    var attribute = list.FirstOrDefault(x => x.UsageContext == UsageContextEnum.Categorized
                                                             || x.UsageContext == UsageContextEnum.Both);
                    if (attribute is not null)
                        return attribute.Order;
                }
                else
                {
                    var attribute = list.FirstOrDefault(x => x.UsageContext == UsageContextEnum.Alphabetical
                                                             || x.UsageContext == UsageContextEnum.Both);
                    if (attribute is not null)
                        return attribute.Order;
                }
            }

            // Max Value. Properties with no order will be displayed last.
            return int.MaxValue;
        }

        internal object ComputeExpandableAttributeForItem(object item)
        {
            var pd = (PropertyDescriptor) item;
            var attribute = PropertyGridUtilities.GetAttribute<ExpandableObjectAttribute>(pd);
            return attribute is not null;
        }

        internal string ComputeValuePropertyPathForItem(object item)
        {
            var pd = (PropertyDescriptor) item;
            var attribute = PropertyGridUtilities.GetAttribute<ValuePropertyPathAttribute>(pd);
            return attribute is not null ? attribute.Path : null;
        }

        internal string ComputeIsValueEditorEnabledPropertyPathForItem(object item)
        {
            var pd = (PropertyDescriptor) item;
            var attribute = PropertyGridUtilities.GetAttribute<IsValueEditorEnabledPropertyPathAttribute>(pd);
            return attribute is not null ? attribute.Path : null;
        }

        internal int ComputeDisplayOrderInternal(bool isPropertyGridCategorized)
        {
            return ComputeDisplayOrder(isPropertyGridCategorized);
        }

        #endregion

        #region Private Methods

        private void ExecuteResetValueCommand(object sender, ExecutedRoutedEventArgs e)
        {
            if (ComputeCanResetValue())
                ResetValue();
        }

        private void CanExecuteResetValueCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ComputeCanResetValue();
        }

        private string ComputeDisplayName()
        {
            var displayName = PropertyDescriptor.DisplayName;
            var attribute = PropertyGridUtilities.GetAttribute<ParenthesizePropertyNameAttribute>(PropertyDescriptor);
            if (attribute is not null && attribute.NeedParenthesis) displayName = "(" + displayName + ")";

            return displayName;
        }

        private void ValidatePropertyOrderAttributes(List<PropertyOrderAttribute> list)
        {
            if (list.Count > 0)
            {
                var both = list.FirstOrDefault(x => x.UsageContext == UsageContextEnum.Both);
                if (both is not null && list.Count > 1)
                    Debug.Assert(false,
                        "A PropertyItem can't have more than 1 PropertyOrderAttribute when it has UsageContext : Both");
            }
        }

        #endregion

        #region AdvancedOptionsIcon (DP)

        public static readonly DependencyProperty AdvancedOptionsIconProperty =
            DependencyProperty.Register("AdvancedOptionsIcon", typeof(ImageSource),
                typeof(DescriptorPropertyDefinitionBase), new UIPropertyMetadata(null));

        public ImageSource AdvancedOptionsIcon
        {
            get => (ImageSource) GetValue(AdvancedOptionsIconProperty);
            set => SetValue(AdvancedOptionsIconProperty, value);
        }

        #endregion

        #region AdvancedOptionsTooltip (DP)

        public static readonly DependencyProperty AdvancedOptionsTooltipProperty =
            DependencyProperty.Register("AdvancedOptionsTooltip", typeof(object),
                typeof(DescriptorPropertyDefinitionBase), new UIPropertyMetadata(null));

        public object AdvancedOptionsTooltip
        {
            get => GetValue(AdvancedOptionsTooltipProperty);
            set => SetValue(AdvancedOptionsTooltipProperty, value);
        }

        #endregion //AdvancedOptionsTooltip

        #region IsExpandable (DP)

        public static readonly DependencyProperty IsExpandableProperty =
            DependencyProperty.Register("IsExpandable", typeof(bool), typeof(DescriptorPropertyDefinitionBase),
                new UIPropertyMetadata(false));

        public bool IsExpandable
        {
            get => (bool) GetValue(IsExpandableProperty);
            set => SetValue(IsExpandableProperty, value);
        }

        #endregion //IsExpandable

        #region Value Property (DP)

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(object),
            typeof(DescriptorPropertyDefinitionBase), new UIPropertyMetadata(null, OnValueChanged));

        public object Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private static void OnValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ((DescriptorPropertyDefinitionBase) o).OnValueChanged(e.OldValue, e.NewValue);
        }

        internal virtual void OnValueChanged(object oldValue, object newValue)
        {
            UpdateIsExpandable();
            UpdateAdvanceOptions();

            // Reset command also affected.
            CommandManager.InvalidateRequerySuggested();
        }

        #endregion //Value Property
    }
}