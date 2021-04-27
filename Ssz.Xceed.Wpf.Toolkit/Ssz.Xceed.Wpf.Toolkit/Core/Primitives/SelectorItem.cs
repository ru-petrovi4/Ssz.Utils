/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System.Windows;
using System.Windows.Controls;

namespace Ssz.Xceed.Wpf.Toolkit.Primitives
{
    public class SelectorItem : ContentControl
    {
        #region Constructors

        static SelectorItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SelectorItem),
                new FrameworkPropertyMetadata(typeof(SelectorItem)));
        }

        #endregion //Constructors

        #region Properties

        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register("IsSelected",
            typeof(bool), typeof(SelectorItem), new UIPropertyMetadata(false, OnIsSelectedChanged));

        public bool IsSelected
        {
            get => (bool) GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        private static void OnIsSelectedChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var selectorItem = o as SelectorItem;
            if (selectorItem != null)
                selectorItem.OnIsSelectedChanged((bool) e.OldValue, (bool) e.NewValue);
        }

        protected virtual void OnIsSelectedChanged(bool oldValue, bool newValue)
        {
            if (newValue)
                RaiseEvent(new RoutedEventArgs(Selector.SelectedEvent, this));
            else
                RaiseEvent(new RoutedEventArgs(Selector.UnSelectedEvent, this));
        }

        internal Selector ParentSelector => ItemsControl.ItemsControlFromItemContainer(this) as Selector;

        #endregion //Properties

        #region Events

        public static readonly RoutedEvent SelectedEvent = Selector.SelectedEvent.AddOwner(typeof(SelectorItem));
        public static readonly RoutedEvent UnselectedEvent = Selector.UnSelectedEvent.AddOwner(typeof(SelectorItem));

        #endregion
    }
}