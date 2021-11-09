/*************************************************************************************
   
   Toolkit for WPF

   Copyright (C) 2007-2018 Ssz.Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at https://xceed.com/xceed-toolkit-plus-for-wpf/

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Ssz.Xceed.Wpf.AvalonDock.Layout;

namespace Ssz.Xceed.Wpf.AvalonDock.Controls
{
    public class LayoutAnchorableControl : Control
    {
        #region Overrides

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            if (Model is not null)
                Model.IsActive = true;

            base.OnGotKeyboardFocus(e);
        }

        #endregion

        #region Constructors

        static LayoutAnchorableControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LayoutAnchorableControl),
                new FrameworkPropertyMetadata(typeof(LayoutAnchorableControl)));
            FocusableProperty.OverrideMetadata(typeof(LayoutAnchorableControl), new FrameworkPropertyMetadata(false));
        }

        #endregion

        #region Properties

        #region Model

        /// <summary>
        ///     Model Dependency Property
        /// </summary>
        public static readonly DependencyProperty ModelProperty = DependencyProperty.Register("Model",
            typeof(LayoutAnchorable), typeof(LayoutAnchorableControl),
            new FrameworkPropertyMetadata(null, OnModelChanged));

        /// <summary>
        ///     Gets or sets the Model property.  This dependency property
        ///     indicates the model attached to this view.
        /// </summary>
        public LayoutAnchorable Model
        {
            get => (LayoutAnchorable) GetValue(ModelProperty);
            set => SetValue(ModelProperty, value);
        }

        /// <summary>
        ///     Handles changes to the Model property.
        /// </summary>
        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LayoutAnchorableControl) d).OnModelChanged(e);
        }

        /// <summary>
        ///     Provides derived classes an opportunity to handle changes to the Model property.
        /// </summary>
        protected virtual void OnModelChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is not null) ((LayoutContent) e.OldValue).PropertyChanged -= Model_PropertyChanged;

            if (Model is not null)
            {
                Model.PropertyChanged += Model_PropertyChanged;
                SetLayoutItem(Model.Root.Manager.GetLayoutItemFromModel(Model));
            }
            else
            {
                SetLayoutItem(null);
            }
        }

        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsEnabled")
                if (Model is not null)
                {
                    IsEnabled = Model.IsEnabled;
                    if (!IsEnabled && Model.IsActive)
                        if (Model.Parent is not null && Model.Parent is LayoutAnchorablePane)
                            ((LayoutAnchorablePane) Model.Parent).SetNextSelectedIndex();
                }
        }

        #endregion

        #region LayoutItem

        /// <summary>
        ///     LayoutItem Read-Only Dependency Property
        /// </summary>
        private static readonly DependencyPropertyKey LayoutItemPropertyKey = DependencyProperty.RegisterReadOnly(
            "LayoutItem", typeof(LayoutItem), typeof(LayoutAnchorableControl),
            new FrameworkPropertyMetadata((LayoutItem) null));

        public static readonly DependencyProperty LayoutItemProperty = LayoutItemPropertyKey.DependencyProperty;

        /// <summary>
        ///     Gets the LayoutItem property.  This dependency property
        ///     indicates the LayoutItem attached to this tag item.
        /// </summary>
        public LayoutItem LayoutItem => (LayoutItem) GetValue(LayoutItemProperty);

        /// <summary>
        ///     Provides a secure method for setting the LayoutItem property.
        ///     This dependency property indicates the LayoutItem attached to this tag item.
        /// </summary>
        /// <param name="value">The new value for the property.</param>
        protected void SetLayoutItem(LayoutItem value)
        {
            SetValue(LayoutItemPropertyKey, value);
        }

        #endregion

        #endregion
    }
}