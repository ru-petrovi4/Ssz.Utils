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
using System.Windows;
using System.Windows.Media;

namespace Ssz.Xceed.Wpf.Toolkit.Core.Utilities
{
    public static class VisualTreeHelperEx
    {
        public static DependencyObject FindAncestorByType(DependencyObject element, Type type, bool specificTypeOnly)
        {
            if (element == null)
                return null;

            if (specificTypeOnly
                ? element.GetType() == type
                : element.GetType() == type || element.GetType().IsSubclassOf(type))
                return element;

            return FindAncestorByType(VisualTreeHelper.GetParent(element), type, specificTypeOnly);
        }

        public static T FindAncestorByType<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) return default;
            if (depObj is T) return (T) depObj;

            var parent = default(T);

            parent = FindAncestorByType<T>(VisualTreeHelper.GetParent(depObj));

            return parent;
        }

        public static Visual FindDescendantByName(Visual element, string name)
        {
            if (element != null && element is FrameworkElement && (element as FrameworkElement).Name == name)
                return element;

            Visual foundElement = null;
            if (element is FrameworkElement)
                (element as FrameworkElement).ApplyTemplate();

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                var visual = VisualTreeHelper.GetChild(element, i) as Visual;
                foundElement = FindDescendantByName(visual, name);
                if (foundElement != null)
                    break;
            }

            return foundElement;
        }

        public static Visual FindDescendantByType(Visual element, Type type)
        {
            return FindDescendantByType(element, type, true);
        }

        public static Visual FindDescendantByType(Visual element, Type type, bool specificTypeOnly)
        {
            if (element == null)
                return null;

            if (specificTypeOnly
                ? element.GetType() == type
                : element.GetType() == type || element.GetType().IsSubclassOf(type))
                return element;

            Visual foundElement = null;
            if (element is FrameworkElement)
                (element as FrameworkElement).ApplyTemplate();

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                var visual = VisualTreeHelper.GetChild(element, i) as Visual;
                foundElement = FindDescendantByType(visual, type, specificTypeOnly);
                if (foundElement != null)
                    break;
            }

            return foundElement;
        }

        public static T FindDescendantByType<T>(Visual element) where T : Visual
        {
            var temp = FindDescendantByType(element, typeof(T));

            return (T) temp;
        }

        public static Visual FindDescendantWithPropertyValue(Visual element,
            DependencyProperty dp, object value)
        {
            if (element == null)
                return null;

            if (element.GetValue(dp).Equals(value))
                return element;

            Visual foundElement = null;
            if (element is FrameworkElement)
                (element as FrameworkElement).ApplyTemplate();

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                var visual = VisualTreeHelper.GetChild(element, i) as Visual;
                foundElement = FindDescendantWithPropertyValue(visual, dp, value);
                if (foundElement != null)
                    break;
            }

            return foundElement;
        }
    }
}