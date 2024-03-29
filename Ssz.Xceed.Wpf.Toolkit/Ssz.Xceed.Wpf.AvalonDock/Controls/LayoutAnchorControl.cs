/*************************************************************************************
   
   Toolkit for WPF

   Copyright (C) 2007-2018 Ssz.Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at https://xceed.com/xceed-toolkit-plus-for-wpf/

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Ssz.Xceed.Wpf.AvalonDock.Layout;

namespace Ssz.Xceed.Wpf.AvalonDock.Controls
{
    public class LayoutAnchorControl : Control, ILayoutControl
    {
        #region Members

        private readonly LayoutAnchorable _model;
        private DispatcherTimer _openUpTimer;

        #endregion

        #region Constructors

        static LayoutAnchorControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LayoutAnchorControl),
                new FrameworkPropertyMetadata(typeof(LayoutAnchorControl)));
            IsHitTestVisibleProperty.AddOwner(typeof(LayoutAnchorControl), new FrameworkPropertyMetadata(true));
        }

        internal LayoutAnchorControl(LayoutAnchorable model)
        {
            _model = model;
            _model.IsActiveChanged += _model_IsActiveChanged;
            _model.IsSelectedChanged += _model_IsSelectedChanged;

            SetSide(_model.FindParent<LayoutAnchorSide>().Side);
        }

        #endregion

        #region Properties

        #region Model

        public ILayoutElement Model => _model;

        #endregion

        #region Side

        /// <summary>
        ///     Side Read-Only Dependency Property
        /// </summary>
        private static readonly DependencyPropertyKey SidePropertyKey = DependencyProperty.RegisterReadOnly("Side",
            typeof(AnchorSide), typeof(LayoutAnchorControl),
            new FrameworkPropertyMetadata(AnchorSide.Left));

        public static readonly DependencyProperty SideProperty = SidePropertyKey.DependencyProperty;

        /// <summary>
        ///     Gets the Side property.  This dependency property
        ///     indicates the anchor side of the control.
        /// </summary>
        public AnchorSide Side => (AnchorSide) GetValue(SideProperty);

        /// <summary>
        ///     Provides a secure method for setting the Side property.
        ///     This dependency property indicates the anchor side of the control.
        /// </summary>
        /// <param name="value">The new value for the property.</param>
        protected void SetSide(AnchorSide value)
        {
            SetValue(SidePropertyKey, value);
        }

        #endregion

        #endregion

        #region Private Methods

        private void _model_IsSelectedChanged(object sender, EventArgs e)
        {
            if (!_model.IsAutoHidden)
            {
                _model.IsSelectedChanged -= _model_IsSelectedChanged;
            }
            else if (_model.IsSelected)
            {
                _model.Root.Manager.ShowAutoHideWindow(this);
                _model.IsSelected = false;
            }
        }

        private void _model_IsActiveChanged(object sender, EventArgs e)
        {
            if (!_model.IsAutoHidden)
                _model.IsActiveChanged -= _model_IsActiveChanged;
            else if (_model.IsActive)
                _model.Root.Manager.ShowAutoHideWindow(this);
        }

        private void _openUpTimer_Tick(object sender, EventArgs e)
        {
            _openUpTimer.Tick -= _openUpTimer_Tick;
            _openUpTimer.Stop();
            _openUpTimer = null;
            _model.Root.Manager.ShowAutoHideWindow(this);
        }

        #endregion

        #region Overrides

        //protected override void OnVisualParentChanged(DependencyObject oldParent)
        //{
        //    base.OnVisualParentChanged(oldParent);

        //    var contentModel = _model;

        //    if (oldParent is not null && contentModel is not null && contentModel.Content is UIElement)
        //    {
        //        var oldParentPaneControl = oldParent.FindVisualAncestor<LayoutAnchorablePaneControl>();
        //        if (oldParentPaneControl is not null)
        //        {
        //            ((ILogicalChildrenContainer)oldParentPaneControl).InternalRemoveLogicalChild(contentModel.Content);
        //        }
        //    }

        //    if (contentModel.Content is not null && contentModel.Content is UIElement)
        //    {
        //        var oldLogicalParentPaneControl = LogicalTreeHelper.GetParent(contentModel.Content as UIElement)
        //            as ILogicalChildrenContainer;
        //        if (oldLogicalParentPaneControl is not null)
        //            oldLogicalParentPaneControl.InternalRemoveLogicalChild(contentModel.Content);
        //    }

        //    if (contentModel is not null && contentModel.Content is not null && contentModel.Root is not null && contentModel.Content is UIElement)
        //    {
        //        ((ILogicalChildrenContainer)contentModel.Root.Manager).InternalAddLogicalChild(contentModel.Content);
        //    }
        //}


        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (!e.Handled)
            {
                _model.Root.Manager.ShowAutoHideWindow(this);
                _model.IsActive = true;
            }
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);

            if (!e.Handled)
            {
                _openUpTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle);
                _openUpTimer.Interval = TimeSpan.FromMilliseconds(400);
                _openUpTimer.Tick += _openUpTimer_Tick;
                _openUpTimer.Start();
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            if (_openUpTimer is not null)
            {
                _openUpTimer.Tick -= _openUpTimer_Tick;
                _openUpTimer.Stop();
                _openUpTimer = null;
            }

            base.OnMouseLeave(e);
        }

        #endregion
    }
}