﻿/*************************************************************************************

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
using System.ComponentModel;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Ssz.Xceed.Wpf.Toolkit.Core.Utilities;

namespace Ssz.Xceed.Wpf.Toolkit.PropertyGrid
{
    [TemplatePart(Name = PropertyGrid.PART_PropertyItemsControl, Type = typeof(PropertyItemsControl))]
    [TemplatePart(Name = PART_ValueContainer, Type = typeof(ContentControl))]
    public abstract class PropertyItemBase : Control, IPropertyContainer, INotifyPropertyChanged
    {
        internal const string PART_ValueContainer = "PART_ValueContainer";
        private ContainerHelperBase _containerHelper;

        #region Properties

        #region AdvancedOptionsIcon

        public static readonly DependencyProperty AdvancedOptionsIconProperty =
            DependencyProperty.Register("AdvancedOptionsIcon", typeof(ImageSource), typeof(PropertyItemBase),
                new UIPropertyMetadata(null));

        public ImageSource AdvancedOptionsIcon
        {
            get => (ImageSource) GetValue(AdvancedOptionsIconProperty);
            set => SetValue(AdvancedOptionsIconProperty, value);
        }

        #endregion //AdvancedOptionsIcon

        #region AdvancedOptionsTooltip

        public static readonly DependencyProperty AdvancedOptionsTooltipProperty =
            DependencyProperty.Register("AdvancedOptionsTooltip", typeof(object), typeof(PropertyItemBase),
                new UIPropertyMetadata(null));

        public object AdvancedOptionsTooltip
        {
            get => GetValue(AdvancedOptionsTooltipProperty);
            set => SetValue(AdvancedOptionsTooltipProperty, value);
        }

        #endregion //AdvancedOptionsTooltip


        #region Description

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(string), typeof(PropertyItemBase),
                new UIPropertyMetadata(null));

        public string Description
        {
            get => (string) GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        #endregion //Description

        #region DisplayName

        public static readonly DependencyProperty DisplayNameProperty =
            DependencyProperty.Register("DisplayName", typeof(string), typeof(PropertyItemBase),
                new UIPropertyMetadata(null));

        public string DisplayName
        {
            get => (string) GetValue(DisplayNameProperty);
            set => SetValue(DisplayNameProperty, value);
        }

        #endregion //DisplayName

        #region Editor

        public static readonly DependencyProperty EditorProperty = DependencyProperty.Register("Editor",
            typeof(FrameworkElement), typeof(PropertyItemBase), new UIPropertyMetadata(null, OnEditorChanged));

        public FrameworkElement Editor
        {
            get => (FrameworkElement) GetValue(EditorProperty);
            set => SetValue(EditorProperty, value);
        }

        private static void OnEditorChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var propertyItem = o as PropertyItem;
            if (propertyItem is not null)
                propertyItem.OnEditorChanged((FrameworkElement) e.OldValue, (FrameworkElement) e.NewValue);
        }

        protected virtual void OnEditorChanged(FrameworkElement oldValue, FrameworkElement newValue)
        {
        }

        #endregion //Editor

        #region IsExpanded

        public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register("IsExpanded",
            typeof(bool), typeof(PropertyItemBase), new UIPropertyMetadata(false, OnIsExpandedChanged));

        public bool IsExpanded
        {
            get => (bool) GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        private static void OnIsExpandedChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var propertyItem = o as PropertyItemBase;
            if (propertyItem is not null)
                propertyItem.OnIsExpandedChanged((bool) e.OldValue, (bool) e.NewValue);
        }

        protected virtual void OnIsExpandedChanged(bool oldValue, bool newValue)
        {
        }

        #endregion IsExpanded

        #region IsExpandable

        public static readonly DependencyProperty IsExpandableProperty =
            DependencyProperty.Register("IsExpandable", typeof(bool), typeof(PropertyItemBase),
                new UIPropertyMetadata(false));

        public bool IsExpandable
        {
            get => (bool) GetValue(IsExpandableProperty);
            set => SetValue(IsExpandableProperty, value);
        }

        #endregion //IsExpandable

        #region IsSelected

        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register("IsSelected",
            typeof(bool), typeof(PropertyItemBase), new UIPropertyMetadata(false, OnIsSelectedChanged));

        public bool IsSelected
        {
            get => (bool) GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        private static void OnIsSelectedChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var propertyItem = o as PropertyItemBase;
            if (propertyItem is not null)
                propertyItem.OnIsSelectedChanged((bool) e.OldValue, (bool) e.NewValue);
        }

        protected virtual void OnIsSelectedChanged(bool oldValue, bool newValue)
        {
            RaiseItemSelectionChangedEvent();
        }

        #endregion //IsSelected

        #region ParentElement

        /// <summary>
        ///     Gets the parent property grid element of this property.
        ///     A PropertyItemBase instance if this is a sub-element,
        ///     or the PropertyGrid itself if this is a first-level property.
        /// </summary>
        public FrameworkElement ParentElement => ParentNode as FrameworkElement;

        #endregion

        #region ParentNode

        internal IPropertyContainer ParentNode { get; set; }

        #endregion

        #region ValueContainer

        internal ContentControl ValueContainer { get; private set; }

        #endregion

        #region Level

        public int Level { get; internal set; }

        #endregion //Level

        #region Properties

        public IList Properties => _containerHelper.Properties;

        #endregion //Properties

        #region PropertyContainerStyle

        /// <summary>
        ///     Get the PropertyContainerStyle for sub items of this property.
        ///     It return the value defined on PropertyGrid.PropertyContainerStyle.
        /// </summary>
        public Style PropertyContainerStyle =>
            ParentNode is not null
                ? ParentNode.PropertyContainerStyle
                : null;

        #endregion

        #region ContainerHelper

        internal ContainerHelperBase ContainerHelper
        {
            get => _containerHelper;
            set
            {
                if (value is null)
                    throw new ArgumentNullException("value");

                _containerHelper = value;
                // Properties property relies on the "Properties" property of the helper
                // class. Raise a property-changed event.
                RaisePropertyChanged(() => Properties);
            }
        }

        #endregion

        #endregion //Properties

        #region Events

        #region ItemSelectionChanged

        internal static readonly RoutedEvent ItemSelectionChangedEvent = EventManager.RegisterRoutedEvent(
            "ItemSelectionChangedEvent", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PropertyItemBase));

        private void RaiseItemSelectionChangedEvent()
        {
            RaiseEvent(new RoutedEventArgs(ItemSelectionChangedEvent));
        }

        #endregion

        #region PropertyChanged event

        public event PropertyChangedEventHandler PropertyChanged;

        internal void RaisePropertyChanged<TMember>(Expression<Func<TMember>> propertyExpression)
        {
            this.Notify(PropertyChanged, propertyExpression);
        }

        internal void RaisePropertyChanged(string name)
        {
            this.Notify(PropertyChanged, name);
        }

        #endregion

        #endregion //Events

        #region Constructors

        static PropertyItemBase()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PropertyItemBase),
                new FrameworkPropertyMetadata(typeof(PropertyItemBase)));
        }

        internal PropertyItemBase()
        {
            _containerHelper = new ObjectContainerHelper(this, null);
            GotFocus += PropertyItemBase_GotFocus;
            AddHandler(PropertyItemsControl.PreparePropertyItemEvent,
                new PropertyItemEventHandler(OnPreparePropertyItemInternal));
            AddHandler(PropertyItemsControl.ClearPropertyItemEvent,
                new PropertyItemEventHandler(OnClearPropertyItemInternal));
        }

        #endregion //Constructors

        #region Event Handlers

        private void OnPreparePropertyItemInternal(object sender, PropertyItemEventArgs args)
        {
            // This is the callback of the PreparePropertyItem comming from the template PropertyItemControl.
            args.PropertyItem.Level = Level + 1;
            _containerHelper.PrepareChildrenPropertyItem(args.PropertyItem, args.Item);

            args.Handled = true;
        }

        private void OnClearPropertyItemInternal(object sender, PropertyItemEventArgs args)
        {
            _containerHelper.ClearChildrenPropertyItem(args.PropertyItem, args.Item);
            // This is the callback of the PreparePropertyItem comming from the template PropertyItemControl.
            args.PropertyItem.Level = 0;

            args.Handled = true;
        }

        #endregion //Event Handlers

        #region Methods

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _containerHelper.ChildrenItemsControl =
                GetTemplateChild(PropertyGrid.PART_PropertyItemsControl) as PropertyItemsControl;
            ValueContainer = GetTemplateChild(PART_ValueContainer) as ContentControl;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            IsSelected = true;
            if (!IsKeyboardFocusWithin) Focus();
            // Handle the event; otherwise, the possible 
            // parent property item will select itself too.
            e.Handled = true;
        }

        private void PropertyItemBase_GotFocus(object sender, RoutedEventArgs e)
        {
            IsSelected = true;
            // Handle the event; otherwise, the possible 
            // parent property item will select itself too.
            e.Handled = true;
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            var propertyItem = e.OldValue as PropertyItem;
            if (propertyItem is not null) BindingOperations.ClearAllBindings(propertyItem.DescriptorDefinition);

            // First check that the raised property is actually a real CLR property.
            // This could be something else like an Attached DP.
            if (ReflectionHelper.IsPublicInstanceProperty(GetType(), e.Property.Name))
                RaisePropertyChanged(e.Property.Name);
        }

        #endregion //Methods

        #region IPropertyContainer Members

        Style IPropertyContainer.PropertyContainerStyle => PropertyContainerStyle;

        EditorDefinitionCollection IPropertyContainer.EditorDefinitions => ParentNode.EditorDefinitions;

        PropertyDefinitionCollection IPropertyContainer.PropertyDefinitions => null;

        ContainerHelperBase IPropertyContainer.ContainerHelper => ContainerHelper;

        bool IPropertyContainer.IsCategorized => false;

        bool IPropertyContainer.AutoGenerateProperties => true;

        FilterInfo IPropertyContainer.FilterInfo => new();

        #endregion
    }
}