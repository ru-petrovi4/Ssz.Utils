/*************************************************************************************
   
   Toolkit for WPF

   Copyright (C) 2007-2018 Ssz.Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at https://xceed.com/xceed-toolkit-plus-for-wpf/

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Ssz.Xceed.Wpf.AvalonDock.Controls
{
    public class DropDownButton : ToggleButton
    {
        #region Constructors

        public DropDownButton()
        {
            Unloaded += DropDownButton_Unloaded;
        }

        #endregion

        #region Overrides

        protected override void OnClick()
        {
            if (DropDownContextMenu is not null)
            {
                //IsChecked = true;
                DropDownContextMenu.PlacementTarget = this;
                DropDownContextMenu.Placement = PlacementMode.Bottom;
                DropDownContextMenu.DataContext = DropDownContextMenuDataContext;
                DropDownContextMenu.Closed += OnContextMenuClosed;
                DropDownContextMenu.IsOpen = true;
            }

            base.OnClick();
        }

        #endregion

        #region Properties

        #region DropDownContextMenu

        /// <summary>
        ///     DropDownContextMenu Dependency Property
        /// </summary>
        public static readonly DependencyProperty DropDownContextMenuProperty = DependencyProperty.Register(
            "DropDownContextMenu", typeof(ContextMenu), typeof(DropDownButton),
            new FrameworkPropertyMetadata(null, OnDropDownContextMenuChanged));

        /// <summary>
        ///     Gets or sets the DropDownContextMenu property.  This dependency property
        ///     indicates drop down menu to show up when user click on an anchorable menu pin.
        /// </summary>
        public ContextMenu DropDownContextMenu
        {
            get => (ContextMenu) GetValue(DropDownContextMenuProperty);
            set => SetValue(DropDownContextMenuProperty, value);
        }

        /// <summary>
        ///     Handles changes to the DropDownContextMenu property.
        /// </summary>
        private static void OnDropDownContextMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DropDownButton) d).OnDropDownContextMenuChanged(e);
        }

        /// <summary>
        ///     Provides derived classes an opportunity to handle changes to the DropDownContextMenu property.
        /// </summary>
        protected virtual void OnDropDownContextMenuChanged(DependencyPropertyChangedEventArgs e)
        {
            var oldContextMenu = e.OldValue as ContextMenu;
            if (oldContextMenu is not null && IsChecked.GetValueOrDefault())
                oldContextMenu.Closed -= OnContextMenuClosed;
        }

        #endregion

        #region DropDownContextMenuDataContext

        /// <summary>
        ///     DropDownContextMenuDataContext Dependency Property
        /// </summary>
        public static readonly DependencyProperty DropDownContextMenuDataContextProperty = DependencyProperty.Register(
            "DropDownContextMenuDataContext", typeof(object), typeof(DropDownButton),
            new FrameworkPropertyMetadata((object) null));

        /// <summary>
        ///     Gets or sets the DropDownContextMenuDataContext property.  This dependency property
        ///     indicates data context to set for drop down context menu.
        /// </summary>
        public object DropDownContextMenuDataContext
        {
            get => GetValue(DropDownContextMenuDataContextProperty);
            set => SetValue(DropDownContextMenuDataContextProperty, value);
        }

        #endregion

        #endregion

        #region Private Methods

        private void OnContextMenuClosed(object sender, RoutedEventArgs e)
        {
            //Debug.Assert(IsChecked.GetValueOrDefault());
            var ctxMenu = sender as ContextMenu;
            ctxMenu.Closed -= OnContextMenuClosed;
            IsChecked = false;
        }

        private void DropDownButton_Unloaded(object sender, RoutedEventArgs e)
        {
            // When changing theme, Unloaded event is called, erasing the DropDownContextMenu.
            // Prevent this on theme changes.
            if (IsLoaded) DropDownContextMenu = null;
        }

        #endregion
    }
}