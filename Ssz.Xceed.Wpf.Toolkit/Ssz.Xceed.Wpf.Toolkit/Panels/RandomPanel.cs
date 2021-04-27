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
using System.Windows.Controls;

namespace Ssz.Xceed.Wpf.Toolkit.Panels
{
    public class RandomPanel : AnimationPanel
    {
        #region Private Fields

        private Random _random = new();

        #endregion

        protected override Size MeasureChildrenOverride(UIElementCollection children, Size constraint)
        {
            var availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

            foreach (UIElement child in children)
            {
                if (child == null)
                    continue;

                var childSize = new Size(
                    1d * _random.Next(Convert.ToInt32(MinimumWidth), Convert.ToInt32(MaximumWidth)),
                    1d * _random.Next(Convert.ToInt32(MinimumHeight), Convert.ToInt32(MaximumHeight)));

                child.Measure(childSize);
                SetActualSize(child, childSize);
            }

            return new Size();
        }

        protected override Size ArrangeChildrenOverride(UIElementCollection children, Size finalSize)
        {
            foreach (UIElement child in children)
            {
                if (child == null)
                    continue;

                var childSize = GetActualSize(child);

                double x = _random.Next(0, (int) Math.Max(finalSize.Width - childSize.Width, 0));
                double y = _random.Next(0, (int) Math.Max(finalSize.Height - childSize.Height, 0));

                var width = Math.Min(finalSize.Width, childSize.Width);
                var height = Math.Min(finalSize.Height, childSize.Height);

                ArrangeChild(child, new Rect(new Point(x, y), new Size(width, height)));
            }

            return finalSize;
        }

        #region MinimumWidth Property

        public static readonly DependencyProperty MinimumWidthProperty =
            DependencyProperty.Register("MinimumWidth", typeof(double), typeof(RandomPanel),
                new FrameworkPropertyMetadata(
                    10d,
                    OnMinimumWidthChanged,
                    CoerceMinimumWidth));

        public double MinimumWidth
        {
            get => (double) GetValue(MinimumWidthProperty);
            set => SetValue(MinimumWidthProperty, value);
        }

        private static void OnMinimumWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = (RandomPanel) d;

            panel.CoerceValue(MaximumWidthProperty);
            panel.InvalidateMeasure();
        }

        private static object CoerceMinimumWidth(DependencyObject d, object baseValue)
        {
            var panel = (RandomPanel) d;
            var value = (double) baseValue;

            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                return DependencyProperty.UnsetValue;

            var maximum = panel.MaximumWidth;
            if (value > maximum)
                return maximum;

            return baseValue;
        }

        #endregion

        #region MinimumHeight Property

        public static readonly DependencyProperty MinimumHeightProperty =
            DependencyProperty.Register("MinimumHeight", typeof(double), typeof(RandomPanel),
                new FrameworkPropertyMetadata(
                    10d,
                    OnMinimumHeightChanged,
                    CoerceMinimumHeight));

        public double MinimumHeight
        {
            get => (double) GetValue(MinimumHeightProperty);
            set => SetValue(MinimumHeightProperty, value);
        }

        private static void OnMinimumHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = (RandomPanel) d;

            panel.CoerceValue(MaximumHeightProperty);
            panel.InvalidateMeasure();
        }

        private static object CoerceMinimumHeight(DependencyObject d, object baseValue)
        {
            var panel = (RandomPanel) d;
            var value = (double) baseValue;

            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                return DependencyProperty.UnsetValue;

            var maximum = panel.MaximumHeight;
            if (value > maximum)
                return maximum;

            return baseValue;
        }

        #endregion

        #region MaximumWidth Property

        public static readonly DependencyProperty MaximumWidthProperty =
            DependencyProperty.Register("MaximumWidth", typeof(double), typeof(RandomPanel),
                new FrameworkPropertyMetadata(
                    100d,
                    OnMaximumWidthChanged,
                    CoerceMaximumWidth));

        public double MaximumWidth
        {
            get => (double) GetValue(MaximumWidthProperty);
            set => SetValue(MaximumWidthProperty, value);
        }

        private static void OnMaximumWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = (RandomPanel) d;

            panel.CoerceValue(MinimumWidthProperty);
            panel.InvalidateMeasure();
        }

        private static object CoerceMaximumWidth(DependencyObject d, object baseValue)
        {
            var panel = (RandomPanel) d;
            var value = (double) baseValue;

            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                return DependencyProperty.UnsetValue;

            var minimum = panel.MinimumWidth;
            if (value < minimum)
                return minimum;

            return baseValue;
        }

        #endregion

        #region MaximumHeight Property

        public static readonly DependencyProperty MaximumHeightProperty =
            DependencyProperty.Register("MaximumHeight", typeof(double), typeof(RandomPanel),
                new FrameworkPropertyMetadata(
                    100d,
                    OnMaximumHeightChanged,
                    CoerceMaximumHeight));

        public double MaximumHeight
        {
            get => (double) GetValue(MaximumHeightProperty);
            set => SetValue(MaximumHeightProperty, value);
        }

        private static void OnMaximumHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = (RandomPanel) d;

            panel.CoerceValue(MinimumHeightProperty);
            panel.InvalidateMeasure();
        }

        private static object CoerceMaximumHeight(DependencyObject d, object baseValue)
        {
            var panel = (RandomPanel) d;
            var value = (double) baseValue;

            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                return DependencyProperty.UnsetValue;

            var minimum = panel.MinimumHeight;
            if (value < minimum)
                return minimum;

            return baseValue;
        }

        #endregion

        #region Seed Property

        public static readonly DependencyProperty SeedProperty =
            DependencyProperty.Register("Seed", typeof(int), typeof(RandomPanel),
                new FrameworkPropertyMetadata(0,
                    SeedChanged));

        public int Seed
        {
            get => (int) GetValue(SeedProperty);
            set => SetValue(SeedProperty, value);
        }

        private static void SeedChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (obj is RandomPanel)
            {
                var owner = (RandomPanel) obj;
                owner._random = new Random((int) args.NewValue);
                owner.InvalidateArrange();
            }
        }

        #endregion

        #region ActualSize Private Property

        // Using a DependencyProperty as the backing store for ActualSize.  This enables animation, styling, binding, etc...
        private static readonly DependencyProperty ActualSizeProperty =
            DependencyProperty.RegisterAttached("ActualSize", typeof(Size), typeof(RandomPanel),
                new UIPropertyMetadata(new Size()));

        private static Size GetActualSize(DependencyObject obj)
        {
            return (Size) obj.GetValue(ActualSizeProperty);
        }

        private static void SetActualSize(DependencyObject obj, Size value)
        {
            obj.SetValue(ActualSizeProperty, value);
        }

        #endregion
    }
}