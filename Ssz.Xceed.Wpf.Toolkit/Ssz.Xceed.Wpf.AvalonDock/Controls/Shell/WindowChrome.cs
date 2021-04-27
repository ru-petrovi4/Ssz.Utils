/*************************************************************************************
   
   Toolkit for WPF

   Copyright (C) 2007-2018 Ssz.Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at https://xceed.com/xceed-toolkit-plus-for-wpf/

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Data;
using Standard;

namespace Microsoft.Windows.Shell
{
    public class WindowChrome : Freezable
    {
        public static readonly DependencyProperty WindowChromeProperty = DependencyProperty.RegisterAttached(
            "WindowChrome",
            typeof(WindowChrome),
            typeof(WindowChrome),
            new PropertyMetadata(null, _OnChromeChanged));

        public static readonly DependencyProperty CaptionHeightProperty = DependencyProperty.Register(
            "CaptionHeight",
            typeof(double),
            typeof(WindowChrome),
            new PropertyMetadata(
                0d,
                (d, e) => ((WindowChrome) d)._OnPropertyChangedThatRequiresRepaint()),
            value => (double) value >= 0d);

        public static readonly DependencyProperty ResizeBorderThicknessProperty = DependencyProperty.Register(
            "ResizeBorderThickness",
            typeof(Thickness),
            typeof(WindowChrome),
            new PropertyMetadata(default(Thickness)), value => Utility.IsThicknessNonNegative((Thickness) value));

        public static readonly DependencyProperty IsHitTestVisibleInChromeProperty =
            DependencyProperty.RegisterAttached(
                "IsHitTestVisibleInChrome",
                typeof(bool),
                typeof(WindowChrome),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty GlassFrameThicknessProperty = DependencyProperty.Register(
            "GlassFrameThickness",
            typeof(Thickness),
            typeof(WindowChrome),
            new PropertyMetadata(
                default(Thickness),
                (d, e) => ((WindowChrome) d)._OnPropertyChangedThatRequiresRepaint(),
                (d, o) => _CoerceGlassFrameThickness((Thickness) o)));

        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
            "CornerRadius",
            typeof(CornerRadius),
            typeof(WindowChrome),
            new PropertyMetadata(
                default(CornerRadius),
                (d, e) => ((WindowChrome) d)._OnPropertyChangedThatRequiresRepaint()),
            value => Utility.IsCornerRadiusValid((CornerRadius) value));

        private static readonly List<_SystemParameterBoundProperty> _BoundProperties = new()
        {
            new() {DependencyProperty = CornerRadiusProperty, SystemParameterPropertyName = "WindowCornerRadius"},
            new() {DependencyProperty = CaptionHeightProperty, SystemParameterPropertyName = "WindowCaptionHeight"},
            new()
            {
                DependencyProperty = ResizeBorderThicknessProperty,
                SystemParameterPropertyName = "WindowResizeBorderThickness"
            },
            new()
            {
                DependencyProperty = GlassFrameThicknessProperty,
                SystemParameterPropertyName = "WindowNonClientFrameThickness"
            }
        };

        public WindowChrome()
        {
            // Effective default values for some of these properties are set to be bindings
            // that set them to system defaults.
            // A more correct way to do this would be to Coerce the value iff the source of the DP was the default value.
            // Unfortunately with the current property system we can't detect whether the value being applied at the time
            // of the coersion is the default.
            foreach (var bp in _BoundProperties)
            {
                // This list must be declared after the DP's are assigned.
                Assert.IsNotNull(bp.DependencyProperty);
                BindingOperations.SetBinding(
                    this,
                    bp.DependencyProperty,
                    new Binding
                    {
                        Source = SystemParameters2.Current,
                        Path = new PropertyPath(bp.SystemParameterPropertyName),
                        Mode = BindingMode.OneWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    });
            }
        }

        // Named property available for fully extending the glass frame.
        public static Thickness GlassFrameCompleteThickness => new(-1);

        protected override Freezable CreateInstanceCore()
        {
            return new WindowChrome();
        }

        private void _OnPropertyChangedThatRequiresRepaint()
        {
            var handler = PropertyChangedThatRequiresRepaint;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        internal event EventHandler PropertyChangedThatRequiresRepaint;

        private struct _SystemParameterBoundProperty
        {
            public string SystemParameterPropertyName { get; set; }
            public DependencyProperty DependencyProperty { get; set; }
        }

        #region Attached Properties

        private static void _OnChromeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // The different design tools handle drawing outside their custom window objects differently.
            // Rather than try to support this concept in the design surface let the designer draw its own
            // chrome anyways.
            // There's certainly room for improvement here.
            if (DesignerProperties.GetIsInDesignMode(d)) return;

            var window = (Window) d;
            var newChrome = (WindowChrome) e.NewValue;

            Assert.IsNotNull(window);

            // Update the ChromeWorker with this new object.

            // If there isn't currently a worker associated with the Window then assign a new one.
            // There can be a many:1 relationship of to Window to WindowChrome objects, but a 1:1 for a Window and a WindowChromeWorker.
            var chromeWorker = WindowChromeWorker.GetWindowChromeWorker(window);
            if (chromeWorker == null)
            {
                chromeWorker = new WindowChromeWorker();
                WindowChromeWorker.SetWindowChromeWorker(window, chromeWorker);
            }

            chromeWorker.SetWindowChrome(newChrome);
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static WindowChrome GetWindowChrome(Window window)
        {
            Verify.IsNotNull(window, "window");
            return (WindowChrome) window.GetValue(WindowChromeProperty);
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static void SetWindowChrome(Window window, WindowChrome chrome)
        {
            Verify.IsNotNull(window, "window");
            window.SetValue(WindowChromeProperty, chrome);
        }


        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static bool GetIsHitTestVisibleInChrome(IInputElement inputElement)
        {
            Verify.IsNotNull(inputElement, "inputElement");
            var dobj = inputElement as DependencyObject;
            if (dobj == null) throw new ArgumentException("The element must be a DependencyObject", "inputElement");
            return (bool) dobj.GetValue(IsHitTestVisibleInChromeProperty);
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static void SetIsHitTestVisibleInChrome(IInputElement inputElement, bool hitTestVisible)
        {
            Verify.IsNotNull(inputElement, "inputElement");
            var dobj = inputElement as DependencyObject;
            if (dobj == null) throw new ArgumentException("The element must be a DependencyObject", "inputElement");
            dobj.SetValue(IsHitTestVisibleInChromeProperty, hitTestVisible);
        }

        #endregion

        #region Dependency Properties

        /// <summary>The extent of the top of the window to treat as the caption.</summary>
        public double CaptionHeight
        {
            get => (double) GetValue(CaptionHeightProperty);
            set => SetValue(CaptionHeightProperty, value);
        }


        public Thickness ResizeBorderThickness
        {
            get => (Thickness) GetValue(ResizeBorderThicknessProperty);
            set => SetValue(ResizeBorderThicknessProperty, value);
        }


        private static object _CoerceGlassFrameThickness(Thickness thickness)
        {
            // If it's explicitly set, but set to a thickness with at least one negative side then 
            // coerce the value to the stock GlassFrameCompleteThickness.
            if (!Utility.IsThicknessNonNegative(thickness)) return GlassFrameCompleteThickness;

            return thickness;
        }

        public Thickness GlassFrameThickness
        {
            get => (Thickness) GetValue(GlassFrameThicknessProperty);
            set => SetValue(GlassFrameThicknessProperty, value);
        }


        public CornerRadius CornerRadius
        {
            get => (CornerRadius) GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        #region ShowSystemMenu

        /// <summary>
        ///     Gets or sets the ShowSystemMenu property.  This dependency property
        ///     indicates if the system menu should be shown at right click on the caption.
        /// </summary>
        public bool ShowSystemMenu { get; set; }

        #endregion

        #endregion
    }
}