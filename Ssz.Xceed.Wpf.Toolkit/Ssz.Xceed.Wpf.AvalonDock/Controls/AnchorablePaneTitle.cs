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
    public class AnchorablePaneTitle : Control
    {
        #region Members

        private bool _isMouseDown;

        #endregion

        #region Constructors

        static AnchorablePaneTitle()
        {
            IsHitTestVisibleProperty.OverrideMetadata(typeof(AnchorablePaneTitle), new FrameworkPropertyMetadata(true));
            FocusableProperty.OverrideMetadata(typeof(AnchorablePaneTitle), new FrameworkPropertyMetadata(false));
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AnchorablePaneTitle),
                new FrameworkPropertyMetadata(typeof(AnchorablePaneTitle)));
        }

        #endregion

        #region Properties

        #region Model

        /// <summary>
        ///     Model Dependency Property
        /// </summary>
        public static readonly DependencyProperty ModelProperty = DependencyProperty.Register("Model",
            typeof(LayoutAnchorable), typeof(AnchorablePaneTitle),
            new FrameworkPropertyMetadata(null, _OnModelChanged));

        /// <summary>
        ///     Gets or sets the Model property.  This dependency property
        ///     indicates model attached to this view.
        /// </summary>
        public LayoutAnchorable Model
        {
            get => (LayoutAnchorable) GetValue(ModelProperty);
            set => SetValue(ModelProperty, value);
        }

        private static void _OnModelChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((AnchorablePaneTitle) sender).OnModelChanged(e);
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
        }

        #endregion

        #region LayoutItem

        /// <summary>
        ///     LayoutItem Read-Only Dependency Property
        /// </summary>
        private static readonly DependencyPropertyKey LayoutItemPropertyKey = DependencyProperty.RegisterReadOnly(
            "LayoutItem", typeof(LayoutItem), typeof(AnchorablePaneTitle),
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

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) _isMouseDown = false;

            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            if (_isMouseDown && e.LeftButton == MouseButtonState.Pressed)
            {
                var pane = this.FindVisualAncestor<LayoutAnchorablePaneControl>();
                if (pane is not null)
                {
                    var paneModel = pane.Model as LayoutAnchorablePane;
                    var manager = paneModel.Root.Manager;

                    manager.StartDraggingFloatingWindowForPane(paneModel);
                }
                else
                {
                    var model = Model;
                    if (model is not null)
                    {
                        var root = model.Root;
                        if (root is not null)
                        {
                            var manager = root.Manager;
                            if (manager is not null) manager.StartDraggingFloatingWindowForContent(model);
                        }
                    }
                }
            }

            _isMouseDown = false;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            if (!e.Handled)
            {
                var attachFloatingWindow = false;
                var parentFloatingWindow = Model.FindParent<LayoutAnchorableFloatingWindow>();
                if (parentFloatingWindow is not null)
                    attachFloatingWindow =
                        parentFloatingWindow.Descendents().OfType<LayoutAnchorablePane>().Count() == 1;

                if (attachFloatingWindow)
                {
                    //the pane is hosted inside a floating window that contains only an anchorable pane so drag the floating window itself
                    var floatingWndControl =
                        Model.Root.Manager.FloatingWindows.Single(fwc => fwc.Model == parentFloatingWindow);
                    floatingWndControl.AttachDrag(false);
                }
                else
                {
                    _isMouseDown = true; //normal drag
                }
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            _isMouseDown = false;
            base.OnMouseLeftButtonUp(e);

            if (Model is not null)
                Model.IsActive = true; //FocusElementManager.SetFocusOnLastElement(Model);
        }

        #endregion

        #region Private Methods

        private void OnHide()
        {
            Model.Hide();
        }

        private void OnToggleAutoHide()
        {
            Model.ToggleAutoHide();
        }

        #endregion
    }
}