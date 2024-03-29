/*************************************************************************************
   
   Toolkit for WPF

   Copyright (C) 2007-2018 Ssz.Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at https://xceed.com/xceed-toolkit-plus-for-wpf/

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Ssz.Xceed.Wpf.AvalonDock.Layout;

namespace Ssz.Xceed.Wpf.AvalonDock.Controls
{
    public class LayoutAnchorableTabItem : Control
    {
        #region Members

        private bool _isMouseDown;
        private static LayoutAnchorableTabItem _draggingItem;
        private static bool _cancelMouseLeave;

        #endregion

        #region Constructors

        static LayoutAnchorableTabItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LayoutAnchorableTabItem),
                new FrameworkPropertyMetadata(typeof(LayoutAnchorableTabItem)));
        }

        #endregion

        #region Properties

        #region Model

        /// <summary>
        ///     Model Dependency Property
        /// </summary>
        public static readonly DependencyProperty ModelProperty = DependencyProperty.Register("Model",
            typeof(LayoutContent), typeof(LayoutAnchorableTabItem),
            new FrameworkPropertyMetadata(null, OnModelChanged));

        /// <summary>
        ///     Gets or sets the Model property.  This dependency property
        ///     indicates model attached to the anchorable tab item.
        /// </summary>
        public LayoutContent Model
        {
            get => (LayoutContent) GetValue(ModelProperty);
            set => SetValue(ModelProperty, value);
        }

        /// <summary>
        ///     Handles changes to the Model property.
        /// </summary>
        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LayoutAnchorableTabItem) d).OnModelChanged(e);
        }

        /// <summary>
        ///     Provides derived classes an opportunity to handle changes to the Model property.
        /// </summary>
        protected virtual void OnModelChanged(DependencyPropertyChangedEventArgs e)
        {
            if (Model is not null)
                SetLayoutItem(Model.Root.Manager.GetLayoutItemFromModel(Model));
            else
                SetLayoutItem(null);
            //UpdateLogicalParent();
        }

        #endregion

        #region LayoutItem

        /// <summary>
        ///     LayoutItem Read-Only Dependency Property
        /// </summary>
        private static readonly DependencyPropertyKey LayoutItemPropertyKey = DependencyProperty.RegisterReadOnly(
            "LayoutItem", typeof(LayoutItem), typeof(LayoutAnchorableTabItem),
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

        #region Overrides

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            _isMouseDown = true;
            _draggingItem = this;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                _isMouseDown = false;
                _draggingItem = null;
            }
            else
            {
                _cancelMouseLeave = false;
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            _isMouseDown = false;

            base.OnMouseLeftButtonUp(e);

            Model.IsActive = true;
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            if (_isMouseDown && e.LeftButton == MouseButtonState.Pressed)
                // drag the item if the mouse leave is not canceled.
                // Mouse leave should be canceled when selecting a new tab to prevent automatic undock when Panel size is Auto.
                _draggingItem = !_cancelMouseLeave ? this : null;

            _isMouseDown = false;
            _cancelMouseLeave = false;
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);

            if (_draggingItem is not null
                && _draggingItem != this
                && e.LeftButton == MouseButtonState.Pressed)
            {
                var model = Model;
                var container = model.Parent;
                var containerPane = model.Parent as ILayoutPane;

                if (containerPane is LayoutAnchorablePane && !((LayoutAnchorablePane) containerPane).CanRepositionItems)
                    return;
                if (containerPane.Parent is not null && containerPane.Parent is LayoutAnchorablePaneGroup &&
                    !((LayoutAnchorablePaneGroup) containerPane.Parent).CanRepositionItems)
                    return;

                var childrenList = container.Children.ToList();
                containerPane.MoveChild(childrenList.IndexOf(_draggingItem.Model), childrenList.IndexOf(model));
            }
        }

        protected override void OnPreviewGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnPreviewGotKeyboardFocus(e);
        }

        #endregion

        #region Internal Methods

        internal static bool IsDraggingItem()
        {
            return _draggingItem is not null;
        }

        internal static LayoutAnchorableTabItem GetDraggingItem()
        {
            return _draggingItem;
        }

        internal static void ResetDraggingItem()
        {
            _draggingItem = null;
        }

        internal static void CancelMouseLeave()
        {
            _cancelMouseLeave = true;
        }

        #endregion
    }
}