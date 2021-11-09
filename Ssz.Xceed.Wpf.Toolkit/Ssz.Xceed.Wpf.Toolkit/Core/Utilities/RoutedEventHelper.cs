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
using System.Windows;

namespace Ssz.Xceed.Wpf.Toolkit.Core.Utilities
{
    internal static class RoutedEventHelper
    {
        internal static void RaiseEvent(DependencyObject target, RoutedEventArgs args)
        {
            if (target is UIElement)
                (target as UIElement).RaiseEvent(args);
            else if (target is ContentElement) (target as ContentElement).RaiseEvent(args);
        }

        internal static void AddHandler(DependencyObject element, RoutedEvent routedEvent, Delegate handler)
        {
            var uie = element as UIElement;
            if (uie is not null)
            {
                uie.AddHandler(routedEvent, handler);
            }
            else
            {
                var ce = element as ContentElement;
                if (ce is not null) ce.AddHandler(routedEvent, handler);
            }
        }

        internal static void RemoveHandler(DependencyObject element, RoutedEvent routedEvent, Delegate handler)
        {
            var uie = element as UIElement;
            if (uie is not null)
            {
                uie.RemoveHandler(routedEvent, handler);
            }
            else
            {
                var ce = element as ContentElement;
                if (ce is not null) ce.RemoveHandler(routedEvent, handler);
            }
        }
    }
}