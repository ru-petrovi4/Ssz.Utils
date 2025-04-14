using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Ssz.Operator.Core.Drawings;
using System;
using System.Collections.Generic;

namespace Ssz.Operator.Core.Utils
{
    public static class TreeHelper
    {
        #region public functions        

        /// <summary>
        ///     Searches in all sub-tree        
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parent"></param>
        /// <param name="additionalCheck"></param>
        /// <returns></returns>
        public static IEnumerable<T> FindChilds<T>(Visual? parent, Func<T, bool>? additionalCheck = null)
            where T : class
        {
            var result = new List<T>();

            if (parent is null) 
                return result;

            if (additionalCheck is null)
            {
                foreach (var visualDescendant in parent.GetVisualDescendants())
                {
                    var child = visualDescendant as T;
                    if (child is not null)
                        result.Add(child);
                }
            }
            else
            {
                foreach (var visualDescendant in parent.GetVisualDescendants())
                {
                    var child = visualDescendant as T;
                    if (child is not null && additionalCheck(child))
                        result.Add(child);
                }
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
        public static T? FindChild<T>(Visual parent, Func<T, bool>? additionalCheck = null)
            where T : class
        {
            if (parent is null) 
                return null;            

            if (additionalCheck is null)
            {
                foreach (var visualDescendant in parent.GetVisualDescendants())
                {
                    var child = visualDescendant as T;
                    if (child is not null)
                        return child;
                }
            }
            else
            {
                foreach (var visualDescendant in parent.GetVisualDescendants())
                {
                    var child = visualDescendant as T;
                    if (child is not null && additionalCheck(child))
                        return child;
                }
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
        public static IEnumerable<T> FindChildsOrSelf<T>(Visual parent, Func<T, bool>? additionalCheck = null)
            where T : class
        {
            var result = new List<T>();

            if (parent is null) 
                return result;

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

        public static T? FindParent<T>(StyledElement? obj, Func<T, bool>? additionalCheck = null)
            where T : class
        {
            if (obj is null) 
                return null;

            for (; ; )
            {
                StyledElement? parent;
                try
                {
                    parent = obj.Parent;
                }
                catch (Exception)
                {
                    return null;
                }

                if (parent is null) 
                    return null;

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

        public static T? FindParentOrSelf<T>(StyledElement obj, Func<T, bool>? additionalCheck = null)
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

        public static bool IsUserVisible(Visual? element, Visual container)
        {
            if (element is null || !element.IsVisible)
                return false;

            var transformedBounds = element.GetTransformedBounds();
            if (transformedBounds is null)
                return false;

            var clip = transformedBounds.Value.Clip;
            if (clip == default)
                return false;

            //var matrix = element.TransformToVisual(container);
            //if (matrix is null)
            //    return false;

            //Rect bounds = new Rect(new Point(0.0, 0.0),
            //    matrix.Value.Transform(new Point(element.Bounds.Width, element.Bounds.Height)));           

            //var rect = new Rect(0.0, 0.0, container.Bounds.Width, container.Bounds.Height);
            //return rect.Contains(bounds.TopLeft) || rect.Contains(bounds.BottomRight);

            return true;
        }

        public static HorizontalAlignment GetHorizontalAlignment(DsPageHorizontalAlignment dsPageHorizontalAlignment)
        {
            switch (dsPageHorizontalAlignment)
            {
                case DsPageHorizontalAlignment.Left:
                    return HorizontalAlignment.Left;
                case DsPageHorizontalAlignment.Center:
                    return HorizontalAlignment.Center;
                case DsPageHorizontalAlignment.Right:
                    return HorizontalAlignment.Right;
                default:
                    return HorizontalAlignment.Center;
            }
        }

        public static VerticalAlignment GetVerticalAlignment(DsPageVerticalAlignment dsPageVerticalAlignment)
        {
            switch (dsPageVerticalAlignment)
            {
                case DsPageVerticalAlignment.Top:
                    return VerticalAlignment.Top;
                case DsPageVerticalAlignment.Center:
                    return VerticalAlignment.Center;
                case DsPageVerticalAlignment.Bottom:
                    return VerticalAlignment.Bottom;
                default:
                    return VerticalAlignment.Center;
            }
        }

        public static HorizontalAlignment GetHorizontalAlignment_FromWpf(int horizontalAlignment)
        {
            switch (horizontalAlignment)
            {
                case 0: // Left
                    return HorizontalAlignment.Left;
                case 1: // Center
                    return HorizontalAlignment.Center;
                case 2: // Right
                    return HorizontalAlignment.Right;
                default: // Stretch
                    return HorizontalAlignment.Stretch;
            }
        }

        public static VerticalAlignment GetVerticalAlignment_FromWpf(int verticalAlignment)
        {
            switch (verticalAlignment)
            {
                case 0: // Top
                    return VerticalAlignment.Top;
                case 1: // Center
                    return VerticalAlignment.Center;
                case 2: // Bottom
                    return VerticalAlignment.Bottom;
                default: // Stretch
                    return VerticalAlignment.Stretch;
            }
        }

        public static TextAlignment GetTextAlignment_FromWpf(int textAlignment)
        {
            switch (textAlignment)
            {
                case 0: // Left
                    return TextAlignment.Left;
                case 1: // Right
                    return TextAlignment.Right;
                case 2: // Center
                    return TextAlignment.Center;
                default: // Justify
                    return TextAlignment.Justify;
            }
        }

        public static TextWrapping GetTextWrapping_FromWpf(int textWrapping)
        {
            switch (textWrapping)
            {
                case 0: // WrapWithOverflow
                    return TextWrapping.WrapWithOverflow;
                case 1: // NoWrap
                    return TextWrapping.NoWrap;                
                default: // Wrap
                    return TextWrapping.Wrap;
            }
        }

        public static PlacementMode GetPlacementMode_FromWpf(int v)
        {
            // TODO
            return PlacementMode.Pointer;
        }

        internal static PenLineJoin GetPenLineJoin_FromWpf(int penLineJoin)
        {
            switch (penLineJoin)
            {
                case 0: // Miter
                    return PenLineJoin.Miter;
                case 1: // Bevel
                    return PenLineJoin.Bevel;
                default: // Round
                    return PenLineJoin.Round;
            }
        }

        internal static PenLineCap GetPenLineCap_FromWpf(int penLineCap)
        {
            switch (penLineCap)
            {
                case 0: // Flat
                    return PenLineCap.Flat;
                case 1: // Square
                    return PenLineCap.Square;
                case 2: // Round
                    return PenLineCap.Round;
                default: // Triangle
                    return PenLineCap.Round;
            }
        }

        #endregion
    }
}