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

namespace Ssz.Xceed.Wpf.Toolkit.PropertyGrid
{
  /// <summary>
  ///     This Control is intended to be used in the template of the
  ///     PropertyItemBase and PropertyGrid classes to contain the
  ///     sub-children properties.
  /// </summary>
  public class PropertyItemsControl : ItemsControl
    {
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is PropertyItemBase;
        }


        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            RaisePreparePropertyItemEvent((PropertyItemBase) element, item);
        }

        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            RaiseClearPropertyItemEvent((PropertyItemBase) element, item);
            base.ClearContainerForItemOverride(element, item);
        }

        #region PreparePropertyItemEvent Attached Routed Event

        internal static readonly RoutedEvent PreparePropertyItemEvent =
            EventManager.RegisterRoutedEvent("PreparePropertyItem", RoutingStrategy.Bubble,
                typeof(PropertyItemEventHandler), typeof(PropertyItemsControl));

        internal event PropertyItemEventHandler PreparePropertyItem
        {
            add => AddHandler(PreparePropertyItemEvent, value);
            remove => RemoveHandler(PreparePropertyItemEvent, value);
        }

        private void RaisePreparePropertyItemEvent(PropertyItemBase propertyItem, object item)
        {
            RaiseEvent(new PropertyItemEventArgs(PreparePropertyItemEvent, this, propertyItem, item));
        }

        #endregion

        #region ClearPropertyItemEvent Attached Routed Event

        internal static readonly RoutedEvent ClearPropertyItemEvent =
            EventManager.RegisterRoutedEvent("ClearPropertyItem", RoutingStrategy.Bubble,
                typeof(PropertyItemEventHandler), typeof(PropertyItemsControl));

        internal event PropertyItemEventHandler ClearPropertyItem
        {
            add => AddHandler(ClearPropertyItemEvent, value);
            remove => RemoveHandler(ClearPropertyItemEvent, value);
        }

        private void RaiseClearPropertyItemEvent(PropertyItemBase propertyItem, object item)
        {
            RaiseEvent(new PropertyItemEventArgs(ClearPropertyItemEvent, this, propertyItem, item));
        }

        #endregion
    }
}