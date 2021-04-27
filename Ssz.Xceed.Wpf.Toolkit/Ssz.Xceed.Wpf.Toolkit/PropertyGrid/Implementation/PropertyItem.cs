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
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Ssz.Xceed.Wpf.Toolkit.PropertyGrid
{
    [TemplatePart(Name = "content", Type = typeof(ContentControl))]
    public class PropertyItem : CustomPropertyItem
    {
        private int _categoryOrder;

        #region Constructors

        internal PropertyItem(DescriptorPropertyDefinitionBase definition)
        {
            if (definition == null)
                throw new ArgumentNullException("definition");

            DescriptorDefinition = definition;
            ContainerHelper = definition.CreateContainerHelper(this);
            definition.ContainerHelperInvalidated += OnDefinitionContainerHelperInvalidated;
        }

        #endregion //Constructors

        #region Properties

        #region CategoryOrder

        public int CategoryOrder
        {
            get => _categoryOrder;
            internal set
            {
                if (_categoryOrder != value)
                {
                    _categoryOrder = value;
                    // Notify the parent helper since this property may affect ordering.
                    RaisePropertyChanged(() => CategoryOrder);
                }
            }
        }

        #endregion //CategoryOrder

        #region IsReadOnly

        /// <summary>
        ///     Identifies the IsReadOnly dependency property
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(PropertyItem),
                new UIPropertyMetadata(false));

        public bool IsReadOnly
        {
            get => (bool) GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        #endregion //IsReadOnly

        #region PropertyOrder

        public static readonly DependencyProperty PropertyOrderProperty =
            DependencyProperty.Register("PropertyOrder", typeof(int), typeof(PropertyItem), new UIPropertyMetadata(0));

        public int PropertyOrder
        {
            get => (int) GetValue(PropertyOrderProperty);
            set => SetValue(PropertyOrderProperty, value);
        }

        #endregion //PropertyOrder

        #region PropertyDescriptor

        public PropertyDescriptor PropertyDescriptor { get; internal set; }

        #endregion //PropertyDescriptor

        #region PropertyType

        public Type PropertyType =>
            PropertyDescriptor != null
                ? PropertyDescriptor.PropertyType
                : null;

        #endregion //PropertyType

        #region DescriptorDefinition

        public DescriptorPropertyDefinitionBase DescriptorDefinition { get; }

        #endregion DescriptorDefinition

        #region Instance

        public object Instance { get; internal set; }

        #endregion //Instance

        #endregion //Properties

        #region Methods

        protected override void OnIsExpandedChanged(bool oldValue, bool newValue)
        {
            if (newValue)
            {
                // This withholds the generation of all PropertyItem instances (recursively)
                // until the PropertyItem is expanded.
                var objectContainerHelper = ContainerHelper as ObjectContainerHelperBase;
                if (objectContainerHelper != null) objectContainerHelper.GenerateProperties();
            }
        }

        protected override void OnEditorChanged(FrameworkElement oldValue, FrameworkElement newValue)
        {
            if (oldValue != null)
                oldValue.DataContext = null;

            if (newValue != null)
                newValue.DataContext = this;
        }

        protected override object OnCoerceValueChanged(object baseValue)
        {
            // Propagate error from DescriptorPropertyDefinitionBase to PropertyItem.Value
            // to see the red error rectangle in the propertyGrid.
            var be = GetBindingExpression(ValueProperty);
            if (be != null && be.DataItem is DescriptorPropertyDefinitionBase)
            {
                var descriptor = be.DataItem as DescriptorPropertyDefinitionBase;
                if (Validation.GetHasError(descriptor))
                {
                    var errors = Validation.GetErrors(descriptor);
                    Validation.MarkInvalid(be, errors[0]);
                }
            }

            return baseValue;
        }

        protected override void OnValueChanged(object oldValue, object newValue)
        {
            base.OnValueChanged(oldValue, newValue);
        }

        private void OnDefinitionContainerHelperInvalidated(object sender, EventArgs e)
        {
            var helper = DescriptorDefinition.CreateContainerHelper(this);
            ContainerHelper = helper;
            if (IsExpanded) helper.GenerateProperties();
        }

        #endregion
    }
}