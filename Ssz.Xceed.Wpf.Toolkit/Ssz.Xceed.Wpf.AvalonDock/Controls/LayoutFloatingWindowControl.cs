/*************************************************************************************
   
   Toolkit for WPF

   Copyright (C) 2007-2018 Ssz.Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at https://xceed.com/xceed-toolkit-plus-for-wpf/

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Ssz.Xceed.Wpf.AvalonDock.Layout;
using Ssz.Xceed.Wpf.AvalonDock.Themes;
using SystemCommands = Microsoft.Windows.Shell.SystemCommands;

namespace Ssz.Xceed.Wpf.AvalonDock.Controls
{
    public abstract class LayoutFloatingWindowControl : Window, ILayoutControl
    {
        #region Internal Classes

        protected internal class FloatingWindowContentHost : HwndHost
        {
            #region Constructors

            public FloatingWindowContentHost(LayoutFloatingWindowControl owner)
            {
                _owner = owner;
                var manager = _owner.Model.Root.Manager;

                var binding = new Binding("SizeToContent") {Source = _owner};
                BindingOperations.SetBinding(this, SizeToContentProperty, binding);
            }

            #endregion

            #region Event Handlers

            private void Content_SizeChanged(object sender, SizeChangedEventArgs e)
            {
                InvalidateMeasure();
                InvalidateArrange();
            }

            #endregion

            #region Members

            private readonly LayoutFloatingWindowControl _owner;
            private HwndSource _wpfContentHost;
            private Border _rootPresenter;
            private DockingManager _manager;

            #endregion

            #region Properties

            #region RootVisual

            public Visual RootVisual => _rootPresenter;

            #endregion

            #region Content

            /// <summary>
            ///     Content Dependency Property
            /// </summary>
            public static readonly DependencyProperty ContentProperty = DependencyProperty.Register("Content",
                typeof(UIElement), typeof(FloatingWindowContentHost),
                new FrameworkPropertyMetadata(null, OnContentChanged));

            /// <summary>
            ///     Gets or sets the Content property.  This dependency property
            ///     indicates ....
            /// </summary>
            public UIElement Content
            {
                get => (UIElement) GetValue(ContentProperty);
                set => SetValue(ContentProperty, value);
            }

            /// <summary>
            ///     Handles changes to the Content property.
            /// </summary>
            private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                ((FloatingWindowContentHost) d).OnContentChanged((UIElement) e.OldValue, (UIElement) e.NewValue);
            }

            /// <summary>
            ///     Provides derived classes an opportunity to handle changes to the Content property.
            /// </summary>
            protected virtual void OnContentChanged(UIElement oldValue, UIElement newValue)
            {
                if (_rootPresenter is not null)
                    _rootPresenter.Child = Content;

                var oldContent = oldValue as FrameworkElement;
                if (oldContent is not null) oldContent.SizeChanged -= Content_SizeChanged;

                var newContent = newValue as FrameworkElement;
                if (newContent is not null) newContent.SizeChanged += Content_SizeChanged;
            }

            #endregion

            #region SizeToContent

            /// <summary>
            ///     SizeToContent Dependency Property
            /// </summary>
            public static readonly DependencyProperty SizeToContentProperty = DependencyProperty.Register(
                "SizeToContent", typeof(SizeToContent), typeof(FloatingWindowContentHost),
                new FrameworkPropertyMetadata(SizeToContent.Manual, OnSizeToContentChanged));

            /// <summary>
            ///     Gets or sets the SizeToContent property.
            /// </summary>
            public SizeToContent SizeToContent
            {
                get => (SizeToContent) GetValue(SizeToContentProperty);
                set => SetValue(SizeToContentProperty, value);
            }

            /// <summary>
            ///     Handles changes to the SizeToContent property.
            /// </summary>
            private static void OnSizeToContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                ((FloatingWindowContentHost) d).OnSizeToContentChanged((SizeToContent) e.OldValue,
                    (SizeToContent) e.NewValue);
            }

            /// <summary>
            ///     Provides derived classes an opportunity to handle changes to the SizeToContent property.
            /// </summary>
            protected virtual void OnSizeToContentChanged(SizeToContent oldValue, SizeToContent newValue)
            {
                if (_wpfContentHost is not null) _wpfContentHost.SizeToContent = newValue;
            }

            #endregion

            #endregion

            #region Overrides

            protected override HandleRef BuildWindowCore(HandleRef hwndParent)
            {
                _wpfContentHost = new HwndSource(new HwndSourceParameters
                {
                    ParentWindow = hwndParent.Handle,
                    WindowStyle = Win32Helper.WS_CHILD | Win32Helper.WS_VISIBLE | Win32Helper.WS_CLIPSIBLINGS |
                                  Win32Helper.WS_CLIPCHILDREN,
                    Width = 1,
                    Height = 1
                });

                _rootPresenter = new Border {Child = new AdornerDecorator {Child = Content}, Focusable = true};
                _rootPresenter.SetBinding(Border.BackgroundProperty, new Binding("Background") {Source = _owner});
                _wpfContentHost.RootVisual = _rootPresenter;

                _manager = _owner.Model.Root.Manager;
                _manager.InternalAddLogicalChild(_rootPresenter);

                return new HandleRef(this, _wpfContentHost.Handle);
            }

            protected override void DestroyWindowCore(HandleRef hwnd)
            {
                _manager.InternalRemoveLogicalChild(_rootPresenter);
                if (_wpfContentHost is not null)
                {
                    _wpfContentHost.Dispose();
                    _wpfContentHost = null;
                }
            }

            protected override Size MeasureOverride(Size constraint)
            {
                if (Content is null)
                    return base.MeasureOverride(constraint);

                Content.Measure(constraint);
                return Content.DesiredSize;
            }

            #endregion
        }

        #endregion

        #region Members

        private ResourceDictionary currentThemeResourceDictionary; // = null
        private bool _isInternalChange; //false
        private readonly ILayoutElement _model;
        private bool _attachDrag;
        private HwndSource _hwndSrc;
        private HwndSourceHook _hwndSrcHook;
        private DragService _dragService;
        private bool _internalCloseFlag;
        private bool _isClosing;

        #endregion

        #region Constructors

        static LayoutFloatingWindowControl()
        {
            ContentProperty.OverrideMetadata(typeof(LayoutFloatingWindowControl),
                new FrameworkPropertyMetadata(null, null, CoerceContentValue));
            AllowsTransparencyProperty.OverrideMetadata(typeof(LayoutFloatingWindowControl),
                new FrameworkPropertyMetadata(false));
            ShowInTaskbarProperty.OverrideMetadata(typeof(LayoutFloatingWindowControl),
                new FrameworkPropertyMetadata(false));
        }

        protected LayoutFloatingWindowControl(ILayoutElement model)
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            _model = model;
        }

        protected LayoutFloatingWindowControl(ILayoutElement model, bool isContentImmutable)
            : this(model)
        {
            IsContentImmutable = isContentImmutable;
        }

        #endregion

        #region Properties

        #region Model

        public abstract ILayoutElement Model { get; }

        #endregion

        #region IsContentImmutable

        /// <summary>
        ///     IsContentImmutable Dependency Property
        /// </summary>
        public static readonly DependencyProperty IsContentImmutableProperty = DependencyProperty.Register(
            "IsContentImmutable", typeof(bool), typeof(LayoutFloatingWindowControl),
            new FrameworkPropertyMetadata(false));

        /// <summary>
        ///     Gets/sets the IsContentImmutable property.  This dependency property
        ///     indicates if the content can be modified.
        /// </summary>
        public bool IsContentImmutable
        {
            get => (bool) GetValue(IsContentImmutableProperty);
            private set => SetValue(IsContentImmutableProperty, value);
        }

        #endregion

        #region IsDragging

        /// <summary>
        ///     IsDragging Read-Only Dependency Property
        /// </summary>
        private static readonly DependencyPropertyKey IsDraggingPropertyKey = DependencyProperty.RegisterReadOnly(
            "IsDragging", typeof(bool), typeof(LayoutFloatingWindowControl),
            new FrameworkPropertyMetadata(false, OnIsDraggingChanged));

        public static readonly DependencyProperty IsDraggingProperty = IsDraggingPropertyKey.DependencyProperty;

        /// <summary>
        ///     Gets the IsDragging property.  This dependency property
        ///     indicates that this floating window is being dragged.
        /// </summary>
        public bool IsDragging => (bool) GetValue(IsDraggingProperty);

        /// <summary>
        ///     Provides a secure method for setting the IsDragging property.
        ///     This dependency property indicates that this floating window is being dragged.
        /// </summary>
        /// <param name="value">The new value for the property.</param>
        protected void SetIsDragging(bool value)
        {
            SetValue(IsDraggingPropertyKey, value);
        }

        /// <summary>
        ///     Handles changes to the IsDragging property.
        /// </summary>
        private static void OnIsDraggingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LayoutFloatingWindowControl) d).OnIsDraggingChanged(e);
        }

        /// <summary>
        ///     Provides derived classes an opportunity to handle changes to the IsDragging property.
        /// </summary>
        protected virtual void OnIsDraggingChanged(DependencyPropertyChangedEventArgs e)
        {
            if ((bool) e.NewValue)
                CaptureMouse();
            else
                ReleaseMouseCapture();
        }

        #endregion

        #region CloseInitiatedByUser

        protected bool CloseInitiatedByUser => !_internalCloseFlag;

        #endregion

        #region KeepContentVisibleOnClose

        internal bool KeepContentVisibleOnClose { get; set; }

        #endregion

        #region IsMaximized

        /// <summary>
        ///     IsMaximized Dependency Property
        /// </summary>
        public static readonly DependencyProperty IsMaximizedProperty = DependencyProperty.Register("IsMaximized",
            typeof(bool), typeof(LayoutFloatingWindowControl),
            new FrameworkPropertyMetadata(false));

        /// <summary>
        ///     Gets/sets the IsMaximized property.  This dependency property
        ///     indicates if the window is maximized.
        /// </summary>
        public bool IsMaximized
        {
            get => (bool) GetValue(IsMaximizedProperty);
            private set
            {
                SetValue(IsMaximizedProperty, value);
                UpdatePositionAndSizeOfPanes();
            }
        }

        /// <summary>
        ///     Provides a secure method for setting the IsMaximized property.
        ///     This dependency property indicates if the window is maximized.
        /// </summary>
        /// <param name="value">The new value for the property.</param>
        protected override void OnStateChanged(EventArgs e)
        {
            if (!_isInternalChange)
            {
                if (WindowState == WindowState.Maximized)
                    UpdateMaximizedState(true);
                else
                    WindowState = IsMaximized ? WindowState.Maximized : WindowState.Normal;
            }

            base.OnStateChanged(e);
        }

        #endregion

        #endregion

        #region Overrides

        protected override void OnClosed(EventArgs e)
        {
            if (Content is not null)
            {
                var host = Content as FloatingWindowContentHost;
                host.Dispose();

                if (_hwndSrc is not null)
                {
                    _hwndSrc.RemoveHook(_hwndSrcHook);
                    _hwndSrc.Dispose();
                    _hwndSrc = null;
                }
            }

            base.OnClosed(e);
        }

        protected override void OnInitialized(EventArgs e)
        {
            CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand,
                (s, args) => SystemCommands.CloseWindow((Window) args.Parameter)));
            CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand,
                (s, args) => SystemCommands.MaximizeWindow((Window) args.Parameter)));
            CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand,
                (s, args) => SystemCommands.MinimizeWindow((Window) args.Parameter)));
            CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand,
                (s, args) => SystemCommands.RestoreWindow((Window) args.Parameter)));
            //Debug.Assert(this.Owner is not null);
            base.OnInitialized(e);
        }

        #endregion

        #region Internal Methods

        internal virtual void UpdateThemeResources(Theme oldTheme = null)
        {
            if (oldTheme is not null)
            {
                if (oldTheme is DictionaryTheme)
                {
                    if (currentThemeResourceDictionary is not null)
                    {
                        Resources.MergedDictionaries.Remove(currentThemeResourceDictionary);
                        currentThemeResourceDictionary = null;
                    }
                }
                else
                {
                    var resourceDictionaryToRemove =
                        Resources.MergedDictionaries.FirstOrDefault(r => r.Source == oldTheme.GetResourceUri());
                    if (resourceDictionaryToRemove is not null)
                        Resources.MergedDictionaries.Remove(
                            resourceDictionaryToRemove);
                }
            }

            var manager = _model.Root.Manager;
            if (manager.Theme is not null)
            {
                if (manager.Theme is DictionaryTheme)
                {
                    currentThemeResourceDictionary = ((DictionaryTheme) manager.Theme).ThemeResourceDictionary;
                    Resources.MergedDictionaries.Add(currentThemeResourceDictionary);
                }
                else
                {
                    Resources.MergedDictionaries.Add(new ResourceDictionary {Source = manager.Theme.GetResourceUri()});
                }
            }
        }

        internal void AttachDrag(bool onActivated = true)
        {
            if (onActivated)
            {
                _attachDrag = true;
                Activated += OnActivated;
            }
            else
            {
                var windowHandle = new WindowInteropHelper(this).Handle;
                var lParam = new IntPtr(((int) Left & 0xFFFF) | ((int) Top << 16));
                Win32Helper.SendMessage(windowHandle, Win32Helper.WM_NCLBUTTONDOWN, new IntPtr(Win32Helper.HT_CAPTION),
                    lParam);
            }
        }

        protected virtual IntPtr FilterMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            handled = false;

            switch (msg)
            {
                case Win32Helper.WM_ACTIVATE:
                    if (((int) wParam & 0xFFFF) == Win32Helper.WA_INACTIVE)
                        if (lParam == this.GetParentWindowHandle())
                        {
                            Win32Helper.SetActiveWindow(_hwndSrc.Handle);
                            handled = true;
                        }

                    break;
                case Win32Helper.WM_EXITSIZEMOVE:
                    UpdatePositionAndSizeOfPanes();

                    if (_dragService is not null)
                    {
                        bool dropFlag;
                        var mousePosition = this.TransformToDeviceDPI(Win32Helper.GetMousePosition());
                        _dragService.Drop(mousePosition, out dropFlag);
                        _dragService = null;
                        SetIsDragging(false);

                        if (dropFlag)
                            InternalClose();
                    }

                    break;
                case Win32Helper.WM_MOVING:
                {
                    UpdateDragPosition();
                    if (IsMaximized) UpdateMaximizedState(false);
                }
                    break;
                case Win32Helper.WM_LBUTTONUP
                    : //set as handled right button click on title area (after showing context menu)
                    if (_dragService is not null && Mouse.LeftButton == MouseButtonState.Released)
                    {
                        _dragService.Abort();
                        _dragService = null;
                        SetIsDragging(false);
                    }

                    break;
                case Win32Helper.WM_SYSCOMMAND:
                    var command = (int) wParam & 0xFFF0;
                    if (command == Win32Helper.SC_MAXIMIZE || command == Win32Helper.SC_RESTORE)
                        UpdateMaximizedState(command == Win32Helper.SC_MAXIMIZE);
                    break;
            }


            return IntPtr.Zero;
        }

        internal void InternalClose()
        {
            _internalCloseFlag = true;
            if (!_isClosing)
            {
                _isClosing = true;
                Close();
            }
        }

        #endregion

        #region Private Methods

        private static object CoerceContentValue(DependencyObject sender, object content)
        {
            var lfwc = sender as LayoutFloatingWindowControl;
            if (lfwc is not null)
            {
                if (lfwc.IsLoaded && lfwc.IsContentImmutable)
                    return lfwc.Content;
                return new FloatingWindowContentHost(sender as LayoutFloatingWindowControl)
                    {Content = content as UIElement};
            }

            return null;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;

            this.SetParentToMainWindowOf(Model.Root.Manager);

            _hwndSrc = PresentationSource.FromDependencyObject(this) as HwndSource;
            _hwndSrcHook = FilterMessage;
            _hwndSrc.AddHook(_hwndSrcHook);

            // Restore maximize state
            var maximized = Model.Descendents().OfType<ILayoutElementForFloatingWindow>().Any(l => l.IsMaximized);
            UpdateMaximizedState(maximized);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= OnUnloaded;

            if (_hwndSrc is not null)
            {
                _hwndSrc.RemoveHook(_hwndSrcHook);
                InternalClose();
            }
        }

        private void OnActivated(object sender, EventArgs e)
        {
            Activated -= OnActivated;

            if (_attachDrag && Mouse.LeftButton == MouseButtonState.Pressed)
            {
                var windowHandle = new WindowInteropHelper(this).Handle;
                var mousePosition = this.PointToScreenDPI(Mouse.GetPosition(this));
                var clientArea = Win32Helper.GetClientRect(windowHandle);
                var windowArea = Win32Helper.GetWindowRect(windowHandle);

                Left = mousePosition.X - windowArea.Width / 2.0;
                Top = mousePosition.Y - (windowArea.Height - clientArea.Height) / 2.0;
                _attachDrag = false;

                var lParam = new IntPtr(((int) mousePosition.X & 0xFFFF) | ((int) mousePosition.Y << 16));
                Win32Helper.SendMessage(windowHandle, Win32Helper.WM_NCLBUTTONDOWN, new IntPtr(Win32Helper.HT_CAPTION),
                    lParam);
            }
        }

        private void UpdatePositionAndSizeOfPanes()
        {
            foreach (var posElement in Model.Descendents().OfType<ILayoutElementForFloatingWindow>())
            {
                posElement.FloatingLeft = Left;
                posElement.FloatingTop = Top;
                posElement.FloatingWidth = Width;
                posElement.FloatingHeight = Height;
            }
        }

        private void UpdateMaximizedState(bool isMaximized)
        {
            foreach (var posElement in Model.Descendents().OfType<ILayoutElementForFloatingWindow>())
                posElement.IsMaximized = isMaximized;
            IsMaximized = isMaximized;
            _isInternalChange = true;
            WindowState = isMaximized ? WindowState.Maximized : WindowState.Normal;
            _isInternalChange = false;
        }

        private void UpdateDragPosition()
        {
            if (_dragService is null)
            {
                _dragService = new DragService(this);
                SetIsDragging(true);
            }

            var mousePosition = this.TransformToDeviceDPI(Win32Helper.GetMousePosition());
            _dragService.UpdateMouseLocation(mousePosition);
        }

        #endregion
    }
}