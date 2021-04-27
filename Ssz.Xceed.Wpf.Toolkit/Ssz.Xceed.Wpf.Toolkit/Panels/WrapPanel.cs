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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Ssz.Xceed.Wpf.Toolkit.Core.Utilities;

namespace Ssz.Xceed.Wpf.Toolkit.Panels
{
    public class WrapPanel : AnimationPanel
    {
        protected override Size MeasureChildrenOverride(UIElementCollection children, Size constraint)
        {
            double desiredExtent = 0;
            double desiredStack = 0;

            var isHorizontal = Orientation == Orientation.Horizontal;
            var constraintExtent = isHorizontal ? constraint.Width : constraint.Height;

            var itemWidth = ItemWidth;
            var itemHeight = ItemHeight;
            var itemExtent = isHorizontal ? itemWidth : itemHeight;

            var hasExplicitItemWidth = !double.IsNaN(itemWidth);
            var hasExplicitItemHeight = !double.IsNaN(itemHeight);
            var useItemExtent = isHorizontal ? hasExplicitItemWidth : hasExplicitItemHeight;

            double lineExtent = 0;
            double lineStack = 0;

            var childConstraint = new Size(hasExplicitItemWidth ? itemWidth : constraint.Width,
                hasExplicitItemHeight ? itemHeight : constraint.Height);

            var isReversed = IsChildOrderReversed;
            var from = isReversed ? children.Count - 1 : 0;
            var to = isReversed ? 0 : children.Count - 1;
            var step = isReversed ? -1 : 1;

            for (int i = from, pass = 0; pass < children.Count; i += step, pass++)
            {
                var child = children[i];

                child.Measure(childConstraint);

                var childExtent = isHorizontal
                    ? hasExplicitItemWidth ? itemWidth : child.DesiredSize.Width
                    : hasExplicitItemHeight
                        ? itemHeight
                        : child.DesiredSize.Height;
                var childStack = isHorizontal
                    ? hasExplicitItemHeight ? itemHeight : child.DesiredSize.Height
                    : hasExplicitItemWidth
                        ? itemWidth
                        : child.DesiredSize.Width;

                if (lineExtent + childExtent > constraintExtent)
                {
                    desiredExtent = Math.Max(lineExtent, desiredExtent);
                    desiredStack += lineStack;
                    lineExtent = childExtent;
                    lineStack = childStack;

                    if (childExtent > constraintExtent)
                    {
                        desiredExtent = Math.Max(childExtent, desiredExtent);
                        desiredStack += childStack;
                        lineExtent = 0;
                        lineStack = 0;
                    }
                }
                else
                {
                    lineExtent += childExtent;
                    lineStack = Math.Max(childStack, lineStack);
                }
            }

            desiredExtent = Math.Max(lineExtent, desiredExtent);
            desiredStack += lineStack;

            return isHorizontal
                ? new Size(desiredExtent, desiredStack)
                : new Size(desiredStack, desiredExtent);
        }

        protected override Size ArrangeChildrenOverride(UIElementCollection children, Size finalSize)
        {
            var isHorizontal = Orientation == Orientation.Horizontal;
            var finalExtent = isHorizontal ? finalSize.Width : finalSize.Height;

            var itemWidth = ItemWidth;
            var itemHeight = ItemHeight;
            var itemExtent = isHorizontal ? itemWidth : itemHeight;

            var hasExplicitItemWidth = !double.IsNaN(itemWidth);
            var hasExplicitItemHeight = !double.IsNaN(itemHeight);
            var useItemExtent = isHorizontal ? hasExplicitItemWidth : hasExplicitItemHeight;

            double lineExtent = 0;
            double lineStack = 0;
            double lineStackSum = 0;

            var from = IsChildOrderReversed ? children.Count - 1 : 0;
            var to = IsChildOrderReversed ? 0 : children.Count - 1;
            var step = IsChildOrderReversed ? -1 : 1;

            var childrenInLine = new Collection<UIElement>();

            for (int i = from, pass = 0; pass < children.Count; i += step, pass++)
            {
                var child = children[i];

                var childExtent = isHorizontal
                    ? hasExplicitItemWidth ? itemWidth : child.DesiredSize.Width
                    : hasExplicitItemHeight
                        ? itemHeight
                        : child.DesiredSize.Height;
                var childStack = isHorizontal
                    ? hasExplicitItemHeight ? itemHeight : child.DesiredSize.Height
                    : hasExplicitItemWidth
                        ? itemWidth
                        : child.DesiredSize.Width;

                if (lineExtent + childExtent > finalExtent)
                {
                    ArrangeLineOfChildren(childrenInLine, isHorizontal, lineStack, lineStackSum, itemExtent,
                        useItemExtent);

                    lineStackSum += lineStack;
                    lineExtent = childExtent;

                    if (childExtent > finalExtent)
                    {
                        childrenInLine.Add(child);
                        ArrangeLineOfChildren(childrenInLine, isHorizontal, childStack, lineStackSum, itemExtent,
                            useItemExtent);
                        lineStackSum += childStack;
                        lineExtent = 0;
                    }

                    childrenInLine.Add(child);
                }
                else
                {
                    childrenInLine.Add(child);
                    lineExtent += childExtent;
                    lineStack = Math.Max(childStack, lineStack);
                }
            }

            if (childrenInLine.Count > 0)
                ArrangeLineOfChildren(childrenInLine, isHorizontal, lineStack, lineStackSum, itemExtent, useItemExtent);

            return finalSize;
        }

        private void ArrangeLineOfChildren(Collection<UIElement> children, bool isHorizontal, double lineStack,
            double lineStackSum, double itemExtent, bool useItemExtent)
        {
            double extent = 0;
            foreach (var child in children)
            {
                var childExtent = isHorizontal ? child.DesiredSize.Width : child.DesiredSize.Height;
                var elementExtent = useItemExtent ? itemExtent : childExtent;
                ArrangeChild(child, isHorizontal
                    ? new Rect(extent, lineStackSum, elementExtent, lineStack)
                    : new Rect(lineStackSum, extent, lineStack, elementExtent));
                extent += elementExtent;
            }

            children.Clear();
        }

        private static void OnInvalidateMeasure(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AnimationPanel) d).InvalidateMeasure();
        }

        private static bool IsWidthHeightValid(object value)
        {
            var num = (double) value;
            return DoubleHelper.IsNaN(num) || num >= 0d && !double.IsPositiveInfinity(num);
        }

        #region Orientation Property

        public static readonly DependencyProperty OrientationProperty =
            StackPanel.OrientationProperty.AddOwner(typeof(WrapPanel),
                new FrameworkPropertyMetadata(Orientation.Horizontal,
                    OnOrientationChanged));

        public Orientation Orientation
        {
            get => _orientation;
            set => SetValue(OrientationProperty, value);
        }

        private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = (WrapPanel) d;
            panel._orientation = (Orientation) e.NewValue;
            panel.InvalidateMeasure();
        }

        private Orientation _orientation;

        #endregion

        #region ItemWidth Property

        public static readonly DependencyProperty ItemWidthProperty =
            DependencyProperty.Register("ItemWidth", typeof(double), typeof(WrapPanel),
                new FrameworkPropertyMetadata(double.NaN,
                    OnInvalidateMeasure), IsWidthHeightValid);

        [TypeConverter(typeof(LengthConverter))]
        public double ItemWidth
        {
            get => (double) GetValue(ItemWidthProperty);
            set => SetValue(ItemWidthProperty, value);
        }

        #endregion

        #region ItemHeight Property

        public static readonly DependencyProperty ItemHeightProperty =
            DependencyProperty.Register("ItemHeight", typeof(double), typeof(WrapPanel),
                new FrameworkPropertyMetadata(double.NaN,
                    OnInvalidateMeasure), IsWidthHeightValid);

        [TypeConverter(typeof(LengthConverter))]
        public double ItemHeight
        {
            get => (double) GetValue(ItemHeightProperty);
            set => SetValue(ItemHeightProperty, value);
        }

        #endregion

        #region IsChildOrderReversed Property

        public static readonly DependencyProperty IsStackReversedProperty =
            DependencyProperty.Register("IsChildOrderReversed", typeof(bool), typeof(WrapPanel),
                new FrameworkPropertyMetadata(false,
                    OnInvalidateMeasure));

        public bool IsChildOrderReversed
        {
            get => (bool) GetValue(IsStackReversedProperty);
            set => SetValue(IsStackReversedProperty, value);
        }

        #endregion
    }
}