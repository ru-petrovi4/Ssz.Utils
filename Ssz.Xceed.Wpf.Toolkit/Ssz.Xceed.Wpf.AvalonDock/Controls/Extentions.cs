/*************************************************************************************
   
   Toolkit for WPF

   Copyright (C) 2007-2018 Ssz.Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at https://xceed.com/xceed-toolkit-plus-for-wpf/

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Ssz.Xceed.Wpf.AvalonDock.Controls
{
    public static class Extentions
    {
        public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject depObj) where T : DependencyObject
        {
            if (depObj is not null)
                for (var i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    var child = VisualTreeHelper.GetChild(depObj, i);
                    if (child is not null && child is T) yield return (T) child;

                    foreach (var childOfChild in FindVisualChildren<T>(child)) yield return childOfChild;
                }
        }

        public static IEnumerable<T> FindLogicalChildren<T>(this DependencyObject depObj) where T : DependencyObject
        {
            if (depObj is not null)
                foreach (var child in LogicalTreeHelper.GetChildren(depObj).OfType<DependencyObject>())
                {
                    if (child is not null && child is T) yield return (T) child;

                    foreach (var childOfChild in FindLogicalChildren<T>(child)) yield return childOfChild;
                }
        }

        public static DependencyObject FindVisualTreeRoot(this DependencyObject initial)
        {
            var current = initial;
            var result = initial;

            while (current is not null)
            {
                result = current;
                if (current is Visual || current is Visual3D)
                    current = VisualTreeHelper.GetParent(current);
                else
                    // If we're in Logical Land then we must walk 
                    // up the logical tree until we find a 
                    // Visual/Visual3D to get us back to Visual Land.
                    current = LogicalTreeHelper.GetParent(current);
            }

            return result;
        }

        public static T FindVisualAncestor<T>(this DependencyObject dependencyObject) where T : class
        {
            var target = dependencyObject;
            do
            {
                target = VisualTreeHelper.GetParent(target);
            } while (target is not null && !(target is T));

            return target as T;
        }

        public static T FindLogicalAncestor<T>(this DependencyObject dependencyObject) where T : class
        {
            var target = dependencyObject;
            do
            {
                var current = target;
                target = LogicalTreeHelper.GetParent(target);
                if (target is null)
                    target = VisualTreeHelper.GetParent(current);
            } while (target is not null && !(target is T));

            return target as T;
        }
    }
}