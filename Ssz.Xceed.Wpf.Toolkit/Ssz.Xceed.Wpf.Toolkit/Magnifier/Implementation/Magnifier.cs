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
using System.Windows.Media;
using Ssz.Xceed.Wpf.Toolkit.Core.Utilities;

namespace Ssz.Xceed.Wpf.Toolkit
{
    [TemplatePart(Name = PART_VisualBrush, Type = typeof(VisualBrush))]
    public class Magnifier : Control
    {
        private const double DEFAULT_SIZE = 100d;
        private const string PART_VisualBrush = "PART_VisualBrush";

        #region Private Members

        private VisualBrush _visualBrush = new();

        #endregion //Private Members

        #region Base Class Overrides

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var newBrush = GetTemplateChild(PART_VisualBrush) as VisualBrush;

            // Just create a brush as placeholder even if there is no such brush.
            // This avoids having to "if" each access to the _visualBrush member.
            // Do not keep the current _visualBrush whatsoever to avoid memory leaks.
            if (newBrush is null) newBrush = new VisualBrush();

            newBrush.Viewbox = _visualBrush.Viewbox;
            _visualBrush = newBrush;
        }

        #endregion // Base Class Overrides

        #region Methods

        private void UpdateViewBox()
        {
            if (!IsInitialized)
                return;

            ViewBox = new Rect(
                ViewBox.Location,
                new Size(ActualWidth * ZoomFactor, ActualHeight * ZoomFactor));
        }

        #endregion //Methods

        #region Properties

        #region FrameType

        public static readonly DependencyProperty FrameTypeProperty = DependencyProperty.Register("FrameType",
            typeof(FrameType), typeof(Magnifier), new UIPropertyMetadata(FrameType.Circle, OnFrameTypeChanged));

        public FrameType FrameType
        {
            get => (FrameType) GetValue(FrameTypeProperty);
            set => SetValue(FrameTypeProperty, value);
        }

        private static void OnFrameTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var m = (Magnifier) d;
            m.OnFrameTypeChanged((FrameType) e.OldValue, (FrameType) e.NewValue);
        }

        protected virtual void OnFrameTypeChanged(FrameType oldValue, FrameType newValue)
        {
            UpdateSizeFromRadius();
        }

        #endregion //FrameType

        #region Radius

        public static readonly DependencyProperty RadiusProperty = DependencyProperty.Register("Radius", typeof(double),
            typeof(Magnifier), new FrameworkPropertyMetadata(DEFAULT_SIZE / 2, OnRadiusPropertyChanged));

        public double Radius
        {
            get => (double) GetValue(RadiusProperty);
            set => SetValue(RadiusProperty, value);
        }

        private static void OnRadiusPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var m = (Magnifier) d;
            m.OnRadiusChanged(e);
        }

        protected virtual void OnRadiusChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateSizeFromRadius();
        }

        #endregion

        #region Target

        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.Register("Target", typeof(UIElement), typeof(Magnifier));

        public UIElement Target
        {
            get => (UIElement) GetValue(TargetProperty);
            set => SetValue(TargetProperty, value);
        }

        #endregion //Target

        #region ViewBox

        internal Rect ViewBox
        {
            get => _visualBrush.Viewbox;
            set => _visualBrush.Viewbox = value;
        }

        #endregion

        #region ZoomFactor

        public static readonly DependencyProperty ZoomFactorProperty = DependencyProperty.Register("ZoomFactor",
            typeof(double), typeof(Magnifier), new FrameworkPropertyMetadata(0.5, OnZoomFactorPropertyChanged),
            OnValidationCallback);

        public double ZoomFactor
        {
            get => (double) GetValue(ZoomFactorProperty);
            set => SetValue(ZoomFactorProperty, value);
        }

        private static bool OnValidationCallback(object baseValue)
        {
            var zoomFactor = (double) baseValue;
            return zoomFactor >= 0;
        }

        private static void OnZoomFactorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var m = (Magnifier) d;
            m.OnZoomFactorChanged(e);
        }

        protected virtual void OnZoomFactorChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateViewBox();
        }

        #endregion //ZoomFactor

        #endregion //Properties

        #region Constructors

        /// <summary>
        ///     Initializes static members of the <see cref="Magnifier" /> class.
        /// </summary>
        static Magnifier()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Magnifier),
                new FrameworkPropertyMetadata(typeof(Magnifier)));
            HeightProperty.OverrideMetadata(typeof(Magnifier), new FrameworkPropertyMetadata(DEFAULT_SIZE));
            WidthProperty.OverrideMetadata(typeof(Magnifier), new FrameworkPropertyMetadata(DEFAULT_SIZE));
        }

        public Magnifier()
        {
            SizeChanged += OnSizeChangedEvent;
        }

        private void OnSizeChangedEvent(object sender, SizeChangedEventArgs e)
        {
            UpdateViewBox();
        }

        private void UpdateSizeFromRadius()
        {
            if (FrameType == FrameType.Circle)
            {
                var newSize = Radius * 2;
                if (!DoubleHelper.AreVirtuallyEqual(Width, newSize)) Width = newSize;

                if (!DoubleHelper.AreVirtuallyEqual(Height, newSize)) Height = newSize;
            }
        }

        #endregion
    }
}