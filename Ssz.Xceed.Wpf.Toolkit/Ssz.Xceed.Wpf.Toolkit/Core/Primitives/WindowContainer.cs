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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ssz.Xceed.Wpf.Toolkit.Primitives
{
    public class WindowContainer : Canvas
    {
        #region Constructors

        static WindowContainer()
        {
            // The default background must be transparent in order to be able to trap
            // all mouse events when a modal window is displayed.
            var defaultModalBackgroundBrush = new SolidColorBrush(Colors.Transparent);
            defaultModalBackgroundBrush.Freeze();
            ModalBackgroundBrushProperty = DependencyProperty.Register("ModalBackgroundBrush", typeof(Brush),
                typeof(WindowContainer),
                new UIPropertyMetadata(defaultModalBackgroundBrush, OnModalBackgroundBrushChanged));
        }


        public WindowContainer()
        {
            SizeChanged += WindowContainer_SizeChanged;
            LayoutUpdated += WindowContainer_LayoutUpdated;
            Loaded += WindowContainer_Loaded;
            ClipToBounds = true;
        }

        private void WindowContainer_Loaded(object sender, RoutedEventArgs e)
        {
            SetNextActiveWindow(null);
        }

        #endregion //Constructors

        #region Members

        private Brush _defaultBackgroundBrush;
        private bool _isModalBackgroundApplied;

        #endregion

        #region Properties

        #region ModalBackgroundBrush

        /// <summary>
        ///     Identifies the ModalBackgroundBrush dependency property.
        /// </summary>
        // Initialized in the static constructor.
        public static readonly DependencyProperty ModalBackgroundBrushProperty;

        /// <summary>
        ///     When using a modal window in the WindowContainer, a ModalBackgroundBrush can be set
        ///     for the WindowContainer.
        /// </summary>
        public Brush ModalBackgroundBrush
        {
            get => (Brush) GetValue(ModalBackgroundBrushProperty);
            set => SetValue(ModalBackgroundBrushProperty, value);
        }

        private static void OnModalBackgroundBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var windowContainer = (WindowContainer) d;
            if (windowContainer is not null)
                windowContainer.OnModalBackgroundBrushChanged((Brush) e.OldValue, (Brush) e.NewValue);
        }

        protected virtual void OnModalBackgroundBrushChanged(Brush oldValue, Brush newValue)
        {
            SetModalBackground();
        }

        #endregion //ModalBackgroundBrush

        #endregion

        #region Base Class Override

        /// <summary>
        ///     Measure the size of the WindowContainer based on its children.
        /// </summary>
        protected override Size MeasureOverride(Size constraint)
        {
            var size = base.MeasureOverride(constraint);

            if (Children.Count > 0)
            {
                var width = double.IsNaN(Width)
                    ? Children.OfType<WindowControl>().Max(w => w.Left + w.DesiredSize.Width)
                    : Width;
                var height = double.IsNaN(Height)
                    ? Children.OfType<WindowControl>().Max(w => w.Top + w.DesiredSize.Height)
                    : Height;
                return new Size(Math.Min(width, constraint.Width), Math.Min(height, constraint.Height));
            }

            return size;
        }

        /// <summary>
        ///     Register and unregister to children events.
        /// </summary>
        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);

            if (visualAdded is not null && !(visualAdded is WindowControl))
                throw new InvalidOperationException("WindowContainer can only contain WindowControl types.");

            if (visualRemoved is not null)
            {
                var removedChild = (WindowControl) visualRemoved;
                removedChild.LeftChanged -= Child_LeftChanged;
                removedChild.TopChanged -= Child_TopChanged;
                removedChild.PreviewMouseLeftButtonDown -= Child_PreviewMouseLeftButtonDown;
                removedChild.IsVisibleChanged -= Child_IsVisibleChanged;
                removedChild.IsKeyboardFocusWithinChanged -= Child_IsKeyboardFocusWithinChanged;
                if (removedChild is ChildWindow) ((ChildWindow) removedChild).IsModalChanged -= Child_IsModalChanged;
            }

            if (visualAdded is not null)
            {
                var addedChild = (WindowControl) visualAdded;
                addedChild.LeftChanged += Child_LeftChanged;
                addedChild.TopChanged += Child_TopChanged;
                addedChild.PreviewMouseLeftButtonDown += Child_PreviewMouseLeftButtonDown;
                addedChild.IsVisibleChanged += Child_IsVisibleChanged;
                addedChild.IsKeyboardFocusWithinChanged += Child_IsKeyboardFocusWithinChanged;
                if (addedChild is ChildWindow) ((ChildWindow) addedChild).IsModalChanged += Child_IsModalChanged;
            }
        }

        #endregion

        #region Event Handler

        private void Child_LeftChanged(object sender, EventArgs e)
        {
            var windowControl = (WindowControl) sender;
            if (windowControl is not null) windowControl.Left = GetRestrictedLeft(windowControl);

            SetLeft(windowControl, windowControl.Left);
        }

        private void Child_TopChanged(object sender, EventArgs e)
        {
            var windowControl = (WindowControl) sender;
            if (windowControl is not null) windowControl.Top = GetRestrictedTop(windowControl);

            SetTop(windowControl, windowControl.Top);
        }

        private void Child_PreviewMouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            var windowControl = (WindowControl) sender;

            var modalWindow = GetModalWindow();
            if (modalWindow is null) SetNextActiveWindow(windowControl);
        }

        private void Child_IsModalChanged(object sender, EventArgs e)
        {
            SetModalBackground();
        }

        private void Child_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var windowControl = (WindowControl) sender;

            //Do not give access to data behind the WindowContainer as long as any child of WindowContainer is visible.
            var firstVisibleChild =
                Children.OfType<WindowControl>().FirstOrDefault(x => x.Visibility == Visibility.Visible);
            IsHitTestVisible = firstVisibleChild is not null;

            if ((bool) e.NewValue)
            {
                SetChildPos(windowControl);
                SetNextActiveWindow(windowControl);
            }
            else
            {
                SetNextActiveWindow(null);
            }

            var modalWindow = GetModalWindow();
            foreach (WindowControl window in Children)
                window.IsBlockMouseInputsPanelActive = modalWindow is not null && !Equals(modalWindow, window);

            SetModalBackground();
        }

        private void Child_IsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var windowControl = (WindowControl) sender;
            if ((bool) e.NewValue) SetNextActiveWindow(windowControl);
        }

        private void WindowContainer_LayoutUpdated(object sender, EventArgs e)
        {
            foreach (WindowControl windowControl in Children)
                //we only want to set the start position if this is the first time the control has bee initialized
                if (!windowControl.IsStartupPositionInitialized && windowControl.ActualWidth != 0 &&
                    windowControl.ActualHeight != 0)
                {
                    SetChildPos(windowControl);
                    windowControl.IsStartupPositionInitialized = true;
                }
        }

        private void WindowContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            foreach (WindowControl windowControl in Children)
            {
                //reposition our windows
                windowControl.Left = GetRestrictedLeft(windowControl);
                windowControl.Top = GetRestrictedTop(windowControl);
            }
        }

        private void ExpandWindowControl(WindowControl windowControl)
        {
            if (windowControl is not null)
            {
                windowControl.Left = 0;
                windowControl.Top = 0;
                windowControl.Width = Math.Min(ActualWidth, windowControl.MaxWidth);
                windowControl.Height = Math.Min(ActualHeight, windowControl.MaxHeight);
            }
        }

        #endregion

        #region Private Methods

        private void SetChildPos(WindowControl windowControl)
        {
            // A MessageBox with no X and Y will be centered.
            // A ChildWindow with WindowStartupLocation == Center will be centered.
            if (windowControl is MessageBox && windowControl.Left == 0 && windowControl.Top == 0
                || windowControl is ChildWindow &&
                ((ChildWindow) windowControl).WindowStartupLocation == WindowStartupLocation.Center)
            {
                CenterChild(windowControl);
            }
            else
            {
                SetLeft(windowControl, windowControl.Left);
                SetTop(windowControl, windowControl.Top);
            }
        }

        private void CenterChild(WindowControl windowControl)
        {
            if (windowControl.ActualWidth != 0 && windowControl.ActualHeight != 0)
            {
                windowControl.Left = (ActualWidth - windowControl.ActualWidth) / 2.0;
                windowControl.Top = (ActualHeight - windowControl.ActualHeight) / 2.0;
            }
        }

        private void SetNextActiveWindow(WindowControl windowControl)
        {
            if (!IsLoaded)
                return;

            if (IsModalWindow(windowControl))
            {
                BringToFront(windowControl);
            }
            else
            {
                var modalWindow = GetModalWindow();
                // Modal window is always in front
                if (modalWindow is not null)
                    BringToFront(modalWindow);
                else if (windowControl is not null)
                    BringToFront(windowControl);
                else
                    BringToFront(Children.OfType<WindowControl>()
                        .OrderByDescending(x => GetZIndex(x))
                        .FirstOrDefault(x => x.Visibility == Visibility.Visible));
            }
        }

        private void BringToFront(WindowControl windowControl)
        {
            if (windowControl is not null)
            {
                var maxZIndez = Children.OfType<WindowControl>().Max(x => GetZIndex(x));
                SetZIndex(windowControl, maxZIndez + 1);

                SetActiveWindow(windowControl);
            }
        }

        private void SetActiveWindow(WindowControl windowControl)
        {
            foreach (WindowControl window in Children) window.SetIsActiveInternal(false);
            windowControl.SetIsActiveInternal(true);
        }

        private bool IsModalWindow(WindowControl windowControl)
        {
            return windowControl is MessageBox && windowControl.Visibility == Visibility.Visible
                   || windowControl is ChildWindow && ((ChildWindow) windowControl).IsModal &&
                   ((ChildWindow) windowControl).WindowState == WindowState.Open;
        }

        private WindowControl GetModalWindow()
        {
            return Children.OfType<WindowControl>()
                .OrderByDescending(x => GetZIndex(x))
                .FirstOrDefault(x => IsModalWindow(x) && x.Visibility == Visibility.Visible);
        }

        private double GetRestrictedLeft(WindowControl windowControl)
        {
            if (windowControl.Left < 0)
                return 0;

            if (windowControl.Left + windowControl.ActualWidth > ActualWidth && ActualWidth != 0)
            {
                var x = ActualWidth - windowControl.ActualWidth;
                return x < 0 ? 0 : x;
            }

            return windowControl.Left;
        }

        private double GetRestrictedTop(WindowControl windowControl)
        {
            if (windowControl.Top < 0)
                return 0;

            if (windowControl.Top + windowControl.ActualHeight > ActualHeight && ActualHeight != 0)
            {
                var y = ActualHeight - windowControl.ActualHeight;
                return y < 0 ? 0 : y;
            }

            return windowControl.Top;
        }

        private void SetModalBackground()
        {
            // We have a modal window and a ModalBackgroundBrush set.
            if (GetModalWindow() is not null && ModalBackgroundBrush is not null)
            {
                if (!_isModalBackgroundApplied)
                {
                    _defaultBackgroundBrush = Background;
                    _isModalBackgroundApplied = true;
                }

                Background = ModalBackgroundBrush;
            }
            else
            {
                if (_isModalBackgroundApplied)
                {
                    Background = _defaultBackgroundBrush;
                    _defaultBackgroundBrush = null;
                    _isModalBackgroundApplied = false;
                }
            }
        }

        #endregion
    }
}