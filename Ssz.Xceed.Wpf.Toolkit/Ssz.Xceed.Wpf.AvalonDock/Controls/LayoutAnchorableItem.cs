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
using System.Windows.Data;
using System.Windows.Input;
using Ssz.Xceed.Wpf.AvalonDock.Commands;
using Ssz.Xceed.Wpf.AvalonDock.Layout;

namespace Ssz.Xceed.Wpf.AvalonDock.Controls
{
    public class LayoutAnchorableItem : LayoutItem
    {
        #region Constructors

        internal LayoutAnchorableItem()
        {
        }

        #endregion

        #region Private Methods

        private void _anchorable_IsVisibleChanged(object sender, EventArgs e)
        {
            if (_anchorable is not null && _anchorable.Root is not null)
                if (_visibilityReentrantFlag.CanEnter)
                    using (_visibilityReentrantFlag.Enter())
                    {
                        if (_anchorable.IsVisible)
                            Visibility = Visibility.Visible;
                        else
                            Visibility = Visibility.Hidden;
                    }
        }

        #endregion

        #region Members

        private LayoutAnchorable _anchorable;
        private ICommand _defaultHideCommand;
        private ICommand _defaultAutoHideCommand;
        private ICommand _defaultDockCommand;
        private readonly ReentrantFlag _visibilityReentrantFlag = new();

        #endregion

        #region Properties

        #region HideCommand

        /// <summary>
        ///     HideCommand Dependency Property
        /// </summary>
        public static readonly DependencyProperty HideCommandProperty = DependencyProperty.Register("HideCommand",
            typeof(ICommand), typeof(LayoutAnchorableItem),
            new FrameworkPropertyMetadata(null, OnHideCommandChanged, CoerceHideCommandValue));

        /// <summary>
        ///     Gets or sets the HideCommand property.  This dependency property
        ///     indicates the command to execute when an anchorable is hidden.
        /// </summary>
        public ICommand HideCommand
        {
            get => (ICommand) GetValue(HideCommandProperty);
            set => SetValue(HideCommandProperty, value);
        }

        /// <summary>
        ///     Handles changes to the HideCommand property.
        /// </summary>
        private static void OnHideCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LayoutAnchorableItem) d).OnHideCommandChanged(e);
        }

        /// <summary>
        ///     Provides derived classes an opportunity to handle changes to the HideCommand property.
        /// </summary>
        protected virtual void OnHideCommandChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        /// <summary>
        ///     Coerces the HideCommand value.
        /// </summary>
        private static object CoerceHideCommandValue(DependencyObject d, object value)
        {
            return value;
        }

        private bool CanExecuteHideCommand(object parameter)
        {
            if (LayoutElement is null)
                return false;
            return _anchorable.CanHide;
        }

        private void ExecuteHideCommand(object parameter)
        {
            if (_anchorable is not null && _anchorable.Root is not null && _anchorable.Root.Manager is not null)
                _anchorable.Root.Manager._ExecuteHideCommand(_anchorable);
        }

        #endregion

        #region AutoHideCommand

        /// <summary>
        ///     AutoHideCommand Dependency Property
        /// </summary>
        public static readonly DependencyProperty AutoHideCommandProperty = DependencyProperty.Register(
            "AutoHideCommand", typeof(ICommand), typeof(LayoutAnchorableItem),
            new FrameworkPropertyMetadata(null, OnAutoHideCommandChanged, CoerceAutoHideCommandValue));

        /// <summary>
        ///     Gets or sets the AutoHideCommand property.  This dependency property
        ///     indicates the command to execute when user click the auto hide button.
        /// </summary>
        /// <remarks>By default this command toggles auto hide state for an anchorable.</remarks>
        public ICommand AutoHideCommand
        {
            get => (ICommand) GetValue(AutoHideCommandProperty);
            set => SetValue(AutoHideCommandProperty, value);
        }

        /// <summary>
        ///     Handles changes to the AutoHideCommand property.
        /// </summary>
        private static void OnAutoHideCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LayoutAnchorableItem) d).OnAutoHideCommandChanged(e);
        }

        /// <summary>
        ///     Provides derived classes an opportunity to handle changes to the AutoHideCommand property.
        /// </summary>
        protected virtual void OnAutoHideCommandChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        /// <summary>
        ///     Coerces the AutoHideCommand value.
        /// </summary>
        private static object CoerceAutoHideCommandValue(DependencyObject d, object value)
        {
            return value;
        }

        private bool CanExecuteAutoHideCommand(object parameter)
        {
            if (LayoutElement is null)
                return false;

            if (LayoutElement.FindParent<LayoutAnchorableFloatingWindow>() is not null)
                return false; //is floating

            return _anchorable.CanAutoHide;
        }

        private void ExecuteAutoHideCommand(object parameter)
        {
            if (_anchorable is not null && _anchorable.Root is not null && _anchorable.Root.Manager is not null)
                _anchorable.Root.Manager._ExecuteAutoHideCommand(_anchorable);
        }

        #endregion

        #region DockCommand

        /// <summary>
        ///     DockCommand Dependency Property
        /// </summary>
        public static readonly DependencyProperty DockCommandProperty = DependencyProperty.Register("DockCommand",
            typeof(ICommand), typeof(LayoutAnchorableItem),
            new FrameworkPropertyMetadata(null, OnDockCommandChanged, CoerceDockCommandValue));

        /// <summary>
        ///     Gets or sets the DockCommand property.  This dependency property
        ///     indicates the command to execute when user click the Dock button.
        /// </summary>
        /// <remarks>By default this command moves the anchorable inside the container pane which previously hosted the object.</remarks>
        public ICommand DockCommand
        {
            get => (ICommand) GetValue(DockCommandProperty);
            set => SetValue(DockCommandProperty, value);
        }

        /// <summary>
        ///     Handles changes to the DockCommand property.
        /// </summary>
        private static void OnDockCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LayoutAnchorableItem) d).OnDockCommandChanged(e);
        }

        /// <summary>
        ///     Provides derived classes an opportunity to handle changes to the DockCommand property.
        /// </summary>
        protected virtual void OnDockCommandChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        /// <summary>
        ///     Coerces the DockCommand value.
        /// </summary>
        private static object CoerceDockCommandValue(DependencyObject d, object value)
        {
            return value;
        }

        private bool CanExecuteDockCommand(object parameter)
        {
            if (LayoutElement is null)
                return false;
            return LayoutElement.FindParent<LayoutAnchorableFloatingWindow>() is not null;
        }

        private void ExecuteDockCommand(object parameter)
        {
            LayoutElement.Root.Manager._ExecuteDockCommand(_anchorable);
        }

        #endregion

        #region CanHide

        /// <summary>
        ///     CanHide Dependency Property
        /// </summary>
        public static readonly DependencyProperty CanHideProperty = DependencyProperty.Register("CanHide", typeof(bool),
            typeof(LayoutAnchorableItem), new FrameworkPropertyMetadata(true,
                OnCanHideChanged));

        /// <summary>
        ///     Gets or sets the CanHide property.  This dependency property
        ///     indicates if user can hide the anchorable item.
        /// </summary>
        public bool CanHide
        {
            get => (bool) GetValue(CanHideProperty);
            set => SetValue(CanHideProperty, value);
        }

        /// <summary>
        ///     Handles changes to the CanHide property.
        /// </summary>
        private static void OnCanHideChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LayoutAnchorableItem) d).OnCanHideChanged(e);
        }

        /// <summary>
        ///     Provides derived classes an opportunity to handle changes to the CanHide property.
        /// </summary>
        protected virtual void OnCanHideChanged(DependencyPropertyChangedEventArgs e)
        {
            if (_anchorable is not null)
                _anchorable.CanHide = (bool) e.NewValue;
        }

        #endregion

        #endregion

        #region Overrides

        internal override void Attach(LayoutContent model)
        {
            _anchorable = model as LayoutAnchorable;
            _anchorable.IsVisibleChanged += _anchorable_IsVisibleChanged;

            base.Attach(model);
        }

        internal override void Detach()
        {
            _anchorable.IsVisibleChanged -= _anchorable_IsVisibleChanged;
            _anchorable = null;
            base.Detach();
        }

        protected override bool CanExecuteDockAsDocumentCommand()
        {
            var canExecute = base.CanExecuteDockAsDocumentCommand();
            if (canExecute && _anchorable is not null)
                return _anchorable.CanDockAsTabbedDocument;

            return canExecute;
        }

        protected override void Close()
        {
            if (_anchorable.Root is not null && _anchorable.Root.Manager is not null)
            {
                var dockingManager = _anchorable.Root.Manager;
                dockingManager._ExecuteCloseCommand(_anchorable);
            }
        }

        protected override void InitDefaultCommands()
        {
            _defaultHideCommand = new RelayCommand(p => ExecuteHideCommand(p), p => CanExecuteHideCommand(p));
            _defaultAutoHideCommand =
                new RelayCommand(p => ExecuteAutoHideCommand(p), p => CanExecuteAutoHideCommand(p));
            _defaultDockCommand = new RelayCommand(p => ExecuteDockCommand(p), p => CanExecuteDockCommand(p));

            base.InitDefaultCommands();
        }

        protected override void ClearDefaultBindings()
        {
            if (HideCommand == _defaultHideCommand)
                BindingOperations.ClearBinding(this, HideCommandProperty);
            if (AutoHideCommand == _defaultAutoHideCommand)
                BindingOperations.ClearBinding(this, AutoHideCommandProperty);
            if (DockCommand == _defaultDockCommand)
                BindingOperations.ClearBinding(this, DockCommandProperty);

            base.ClearDefaultBindings();
        }

        protected override void SetDefaultBindings()
        {
            if (HideCommand is null)
                HideCommand = _defaultHideCommand;
            if (AutoHideCommand is null)
                AutoHideCommand = _defaultAutoHideCommand;
            if (DockCommand is null)
                DockCommand = _defaultDockCommand;

            Visibility = _anchorable.IsVisible ? Visibility.Visible : Visibility.Hidden;
            base.SetDefaultBindings();
        }

        protected override void OnVisibilityChanged()
        {
            if (_anchorable is not null && _anchorable.Root is not null)
                if (_visibilityReentrantFlag.CanEnter)
                    using (_visibilityReentrantFlag.Enter())
                    {
                        if (Visibility == Visibility.Hidden)
                            _anchorable.Hide(false);
                        else if (Visibility == Visibility.Visible)
                            _anchorable.Show();
                    }

            base.OnVisibilityChanged();
        }

        #endregion
    }
}