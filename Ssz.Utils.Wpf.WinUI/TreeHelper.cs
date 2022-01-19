using System;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Ssz.Utils.Wpf
{
    public static class TreeHelper
    {
        #region public functions

        /// <summary>
        ///     Searches in all sub-tree
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="that"></param>
        /// <param name="elementName"></param>
        /// <returns></returns>
        public static T? FindChild<T>(DependencyObject that, string elementName)
            where T : FrameworkElement
        {
            try
            {
                var childrenCount = VisualTreeHelper.GetChildrenCount(that);

                //Logger.Verbose("FindChild: find child element with name {0} from element type {1}, childs count {2}", elementName, that.GetType().Name, childrenCount);

                for (var i = 0; i < childrenCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(that, i);

                    var frameworkElement = child as FrameworkElement;
                    if (frameworkElement is not null)
                    {
                        //Logger.Verbose("FindChild: child element with name {0},  type {1}", frameworkElement.Name ?? "<null>", frameworkElement.GetType().Name);

                        if (elementName == frameworkElement.Name)
                            return (T)frameworkElement;

                        frameworkElement = FindChild<T>(frameworkElement, elementName);
                        if (frameworkElement is not null)
                            return (T)frameworkElement;
                    }
                }
            }
            catch (Exception)
            {
                //Logger.Verbose(e, "FindChild throw exception ");
            }

            return null;
        }

        /// <summary>
        ///     Searches in all sub-tree        
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parent"></param>
        /// <param name="additionalCheck"></param>
        /// <returns></returns>
        public static IEnumerable<T> FindChilds<T>(DependencyObject? parent, Func<T, bool>? additionalCheck = null)
            where T : class
        {
            var result = new List<T>();

            if (parent is null) return result;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int index = 0; index < childrenCount; index++)
            {
                var child = VisualTreeHelper.GetChild(parent, index) as T;

                if (child is not null)
                {
                    if (additionalCheck is null)
                    {
                        result.Add(child);
                    }
                    else
                    {
                        if (additionalCheck(child))
                            result.Add(child);
                    }
                }
            }

            for (int index = 0; index < childrenCount; index++)
            {
                result.AddRange(FindChilds<T>(VisualTreeHelper.GetChild(parent, index), additionalCheck));
            }

            return result;
        }

        /// <summary>
        ///     Searches in all sub-tree        
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parent"></param>
        /// <param name="additionalCheck"></param>
        /// <returns></returns>
        public static IEnumerable<T> FindChildsOrSelf<T>(DependencyObject parent, Func<T, bool>? additionalCheck = null)
            where T : class
        {
            var result = new List<T>();

            if (parent is null) return result;

            var typedParent = parent as T;
            if (typedParent is not null)
            {
                if (additionalCheck is null)
                {
                    result.Add(typedParent);
                }
                else
                {
                    if (additionalCheck(typedParent))
                        result.Add(typedParent);
                }
            }

            result.AddRange(FindChilds<T>(parent, additionalCheck));

            return result;
        }

        /// <summary>
        ///     Searches in all sub-tree
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parent"></param>
        /// <param name="additionalCheck"></param>
        /// <returns></returns>
        public static T? FindChild<T>(DependencyObject parent, Func<T, bool>? additionalCheck = null)
            where T : class
        {
            if (parent is null) return null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int index = 0; index < childrenCount; index++)
            {
                T? child = null;
                try
                {
                    child = VisualTreeHelper.GetChild(parent, index) as T;
                }
                catch (Exception)
                {
                }

                if (child is not null)
                {
                    if (additionalCheck is null)
                    {
                        return child;
                    }
                    else
                    {
                        if (additionalCheck(child))
                            return child;
                    }
                }
            }

            for (int index = 0; index < childrenCount; index++)
            {
                T? child = null;
                try
                {
                    child = FindChild<T>(VisualTreeHelper.GetChild(parent, index), additionalCheck);
                }
                catch (Exception)
                {
                }
                
                if (child is not null) return child;
            }

            return null;
        }

        /*
        public static IEnumerable<T> FindParents<T>(DependencyObject parent, Func<T, bool> additionalCheck = null)
            where T : class
        {
            var result = new List<T>();

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int index = 0; index < childrenCount; index++)
            {
                T child = VisualTreeHelper.GetChild(parent, index) as T;

                if (child is not null)
                {
                    if (additionalCheck is null)
                    {
                        result.Add(child);
                    }
                    else
                    {
                        if (additionalCheck(child))
                            result.Add(child);
                    }
                }
            }

            for (int index = 0; index < childrenCount; index++)
            {
                result.AddRange(FindChilds<T>(VisualTreeHelper.GetChild(parent, index), additionalCheck));

            }

            return result;
        }        
         
        public static void TryFreeze(DependencyObject obj)
        {
            var freezableArray = TreeHelper.FindChildsOrSelf<Freezable>(obj).ToArray();
            foreach (var freezable in freezableArray)
            {
                if (freezable.CanFreeze) freezable.Freeze();
            }
        }    
         */

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="additionalCheck"></param>
        /// <returns></returns>
        public static T? FindParent<T>(DependencyObject? obj, Func<T, bool>? additionalCheck = null)
            where T : class
        {
            if (obj is null) return null;

            for (;;)
            {
                DependencyObject parent;
                try
                {
                    parent = VisualTreeHelper.GetParent(obj);
                }
                catch (Exception)
                {
                    return null;
                }

                if (parent is null) return null;

                var parentT = parent as T;

                if (parentT is not null)
                {
                    if (additionalCheck is null)
                    {
                        return parentT;
                    }
                    else
                    {
                        if (additionalCheck(parentT))
                            return parentT;
                    }
                }

                obj = parent;
            }
        }

        public static T? FindParentOrSelf<T>(DependencyObject obj, Func<T, bool>? additionalCheck = null)
            where T : class
        {
            var t = obj as T;
            if (t is not null)
            {
                if (additionalCheck is null)
                {
                    return t;
                }
                else
                {
                    if (additionalCheck(t))
                        return t;
                }
            }

            return FindParent<T>(obj, additionalCheck);
        }
        ///// <summary>
        ///// Determines if the specified WPF element in the specified WPF container is currently visible
        ///// </summary>
        ///// <remarks>
        ///// WPF has a complex tree system where controls are embeded into other controls and walking that 
        ///// stack is not easy.  This method will walk the stack for us to determine if both the element
        ///// and the container it is in are visible.
        ///// 
        ///// An example of this use is with alarms.  Alarms will typically be embedded into a ListView.
        ///// When we acknowledge the page, we want to only acknowledge the alarms that are currently 
        ///// visible to the user.  So we rip through the entire alarm list, and for each element in
        ///// the list, we check to see if that element is visible within its ListView container.
        ///// </remarks>
        ///// <example>
        ///// This sample shows how to do something with the visible elements in a WPF ListView control named 'MyListView'
        ///// <code>
        ///// for (int i=0; i_MyListView.Items.Count; i++)
        ///// {
        /////     FrameworkElement container = MyListView;
        /////     FrameworkElement element = container.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
        /////     if (TreeHelper.IsUserVisible(element, container))
        /////     {
        /////         //Do something with the visible item
        /////     }
        ///// }
        ///// </code>
        ///// </example>
        ///// <param name="element">The element that we are checking if it is visible</param>
        ///// <param name="container">The container that is holding this element</param>
        ///// <returns>
        ///// true if the element is visible
        ///// false if the element is not visible
        ///// </returns>
        //public static bool IsUserVisible(FrameworkElement? element, FrameworkElement container)
        //{
        //    if (element is null || !element.IsVisible)
        //        return false;

        //    Rect bounds = element
        //        .TransformToAncestor(container)
        //        .TransformBounds(new Rect(0.0, 0.0, element.ActualWidth, element.ActualHeight));

        //    var rect = new Rect(0.0, 0.0, container.ActualWidth, container.ActualHeight);
        //    return rect.Contains(bounds.TopLeft) || rect.Contains(bounds.BottomRight);

        //}

        ///// <summary>       
        ///// </summary>
        ///// <param name="element"></param>
        ///// <returns></returns>
        //public static List<DependencyProperty> GetDependencyProperties(Object element)
        //{
        //    List<DependencyProperty> properties = new List<DependencyProperty>();
        //    MarkupObject markupObject = MarkupWriter.GetMarkupObjectFor(element);
        //    if (markupObject is not null)
        //    {
        //        foreach (MarkupProperty mp in markupObject.Properties)
        //        {
        //            if (mp.DependencyProperty is not null)
        //            {
        //                properties.Add(mp.DependencyProperty);
        //            }
        //        }
        //    }
        //    return properties;
        //}

        #endregion
    }
}