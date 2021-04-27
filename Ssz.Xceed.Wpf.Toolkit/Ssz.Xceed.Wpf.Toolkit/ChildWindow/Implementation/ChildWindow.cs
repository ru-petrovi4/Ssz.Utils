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
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Ssz.Xceed.Wpf.Toolkit.Primitives;

namespace Ssz.Xceed.Wpf.Toolkit
{
#pragma warning disable 0809
#pragma warning disable 0618

    [TemplatePart(Name = PART_WindowRoot, Type = typeof(Grid))]
    [TemplatePart(Name = PART_Root, Type = typeof(Grid))]
    [TemplatePart(Name = PART_WindowControl, Type = typeof(WindowControl))]
    public class ChildWindow : WindowControl
    {
        private const string PART_WindowRoot = "PART_WindowRoot";
        private const string PART_Root = "PART_Root";
        private const string PART_WindowControl = "PART_WindowControl";
        private const int _horizontalOffset = 3;
        private const int _verticalOffset = 3;

        #region Private Members

        private Grid _root;
        private readonly TranslateTransform _moveTransform = new();
        private bool _startupPositionInitialized;
        private FrameworkElement _parentContainer;
        private readonly Rectangle _modalLayer = new();
        private readonly Canvas _modalLayerPanel = new();
        private Grid _windowRoot;
        private WindowControl _windowControl;
        private bool _ignorePropertyChanged;
        private bool _hasWindowContainer;

        #endregion //Private Members

        #region Public Properties

        #region DialogResult

        private bool? _dialogResult;

        /// <summary>
        ///     Gets or sets a value indicating whether the ChildWindow was accepted or canceled.
        /// </summary>
        /// <value>
        ///     True if the child window was accepted; false if the child window was
        ///     canceled. The default is null.
        /// </value>
        [TypeConverter(typeof(NullableBoolConverter))]
        public bool? DialogResult
        {
            get => _dialogResult;
            set
            {
                if (_dialogResult != value)
                {
                    _dialogResult = value;
                    Close();
                }
            }
        }

        #endregion //DialogResult

        #region DesignerWindowState

        public static readonly DependencyProperty DesignerWindowStateProperty =
            DependencyProperty.Register("DesignerWindowState", typeof(WindowState), typeof(ChildWindow),
                new PropertyMetadata(WindowState.Closed, OnDesignerWindowStatePropertyChanged));

        public WindowState DesignerWindowState
        {
            get => (WindowState) GetValue(DesignerWindowStateProperty);
            set => SetValue(DesignerWindowStateProperty, value);
        }

        private static void OnDesignerWindowStatePropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var childWindow = d as ChildWindow;
            if (childWindow != null)
                childWindow.OnDesignerWindowStatePropertyChanged((WindowState) e.OldValue, (WindowState) e.NewValue);
        }

        protected virtual void OnDesignerWindowStatePropertyChanged(WindowState oldValue, WindowState newValue)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                Visibility = newValue == WindowState.Open ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion //DesignerWindowState

        #region FocusedElement

        public static readonly DependencyProperty FocusedElementProperty = DependencyProperty.Register("FocusedElement",
            typeof(FrameworkElement), typeof(ChildWindow), new UIPropertyMetadata(null));

        public FrameworkElement FocusedElement
        {
            get => (FrameworkElement) GetValue(FocusedElementProperty);
            set => SetValue(FocusedElementProperty, value);
        }

        #endregion

        #region IsModal

        public static readonly DependencyProperty IsModalProperty = DependencyProperty.Register("IsModal", typeof(bool),
            typeof(ChildWindow), new UIPropertyMetadata(false, OnIsModalPropertyChanged));

        public bool IsModal
        {
            get => (bool) GetValue(IsModalProperty);
            set => SetValue(IsModalProperty, value);
        }

        private static void OnIsModalPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var childWindow = d as ChildWindow;
            if (childWindow != null)
                childWindow.OnIsModalChanged((bool) e.OldValue, (bool) e.NewValue);
        }

        internal event EventHandler<EventArgs> IsModalChanged;

        private void OnIsModalChanged(bool oldValue, bool newValue)
        {
            var handler = IsModalChanged;
            if (handler != null) handler(this, EventArgs.Empty);

            if (!_hasWindowContainer)
            {
                if (newValue)
                {
                    KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Cycle);
                    ShowModalLayer();
                }
                else
                {
                    KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Continue);
                    HideModalLayer();
                }
            }
        }

        #endregion //IsModal

        #region OverlayBrush (Obsolete)

        [Obsolete(
            "This property is obsolete and should no longer be used. Use WindowContainer.ModalBackgroundBrushProperty instead.")]
        public static readonly DependencyProperty OverlayBrushProperty = DependencyProperty.Register("OverlayBrush",
            typeof(Brush), typeof(ChildWindow), new PropertyMetadata(Brushes.Gray, OnOverlayBrushChanged));

        [Obsolete(
            "This property is obsolete and should no longer be used. Use WindowContainer.ModalBackgroundBrushProperty instead.")]
        public Brush OverlayBrush
        {
            get => (Brush) GetValue(OverlayBrushProperty);
            set => SetValue(OverlayBrushProperty, value);
        }

        private static void OnOverlayBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var childWindow = d as ChildWindow;
            if (childWindow != null)
                childWindow.OnOverlayBrushChanged((Brush) e.OldValue, (Brush) e.NewValue);
        }

        [Obsolete(
            "This method is obsolete and should no longer be used. Use WindowContainer.ModalBackgroundBrushProperty instead.")]
        protected virtual void OnOverlayBrushChanged(Brush oldValue, Brush newValue)
        {
            _modalLayer.Fill = newValue;
        }

        #endregion //OverlayBrush

        #region OverlayOpacity (Obsolete)

        [Obsolete(
            "This property is obsolete and should no longer be used. Use WindowContainer.ModalBackgroundBrushProperty instead.")]
        public static readonly DependencyProperty OverlayOpacityProperty = DependencyProperty.Register("OverlayOpacity",
            typeof(double), typeof(ChildWindow), new PropertyMetadata(0.5, OnOverlayOpacityChanged));

        [Obsolete(
            "This property is obsolete and should no longer be used. Use WindowContainer.ModalBackgroundBrushProperty instead.")]
        public double OverlayOpacity
        {
            get => (double) GetValue(OverlayOpacityProperty);
            set => SetValue(OverlayOpacityProperty, value);
        }

        private static void OnOverlayOpacityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var childWindow = d as ChildWindow;
            if (childWindow != null)
                childWindow.OnOverlayOpacityChanged((double) e.OldValue, (double) e.NewValue);
        }

        [Obsolete(
            "This method is obsolete and should no longer be used. Use WindowContainer.ModalBackgroundBrushProperty instead.")]
        protected virtual void OnOverlayOpacityChanged(double oldValue, double newValue)
        {
            _modalLayer.Opacity = newValue;
        }

        #endregion //OverlayOpacity  

        #region WindowStartupLocation

        public static readonly DependencyProperty WindowStartupLocationProperty =
            DependencyProperty.Register("WindowStartupLocation", typeof(WindowStartupLocation), typeof(ChildWindow),
                new UIPropertyMetadata(WindowStartupLocation.Manual, OnWindowStartupLocationChanged));

        public WindowStartupLocation WindowStartupLocation
        {
            get => (WindowStartupLocation) GetValue(WindowStartupLocationProperty);
            set => SetValue(WindowStartupLocationProperty, value);
        }

        private static void OnWindowStartupLocationChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var childWindow = o as ChildWindow;
            if (childWindow != null)
                childWindow.OnWindowStartupLocationChanged((WindowStartupLocation) e.OldValue,
                    (WindowStartupLocation) e.NewValue);
        }

        protected virtual void OnWindowStartupLocationChanged(WindowStartupLocation oldValue,
            WindowStartupLocation newValue)
        {
            // TODO: Add your property changed side-effects. Descendants can override as well.
        }

        #endregion //WindowStartupLocation

        #region WindowState

        public static readonly DependencyProperty WindowStateProperty = DependencyProperty.Register("WindowState",
            typeof(WindowState), typeof(ChildWindow),
            new FrameworkPropertyMetadata(WindowState.Closed, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnWindowStatePropertyChanged));

        public WindowState WindowState
        {
            get => (WindowState) GetValue(WindowStateProperty);
            set => SetValue(WindowStateProperty, value);
        }

        private static void OnWindowStatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var childWindow = d as ChildWindow;
            if (childWindow != null)
                childWindow.OnWindowStatePropertyChanged((WindowState) e.OldValue, (WindowState) e.NewValue);
        }

        protected virtual void OnWindowStatePropertyChanged(WindowState oldValue, WindowState newValue)
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                if (!_ignorePropertyChanged)
                    SetWindowState(newValue);
            }
            else
            {
                Visibility = DesignerWindowState == WindowState.Open ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        #endregion //WindowState

        #endregion //Public Properties

        #region Constructors

        static ChildWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ChildWindow),
                new FrameworkPropertyMetadata(typeof(ChildWindow)));
        }

        public ChildWindow()
        {
            DesignerWindowState = WindowState.Open;

            _modalLayer.Fill = OverlayBrush;
            _modalLayer.Opacity = OverlayOpacity;
        }

        #endregion //Constructors

        #region Base Class Overrides

        internal override bool AllowPublicIsActiveChange => false;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_windowControl != null)
            {
                _windowControl.HeaderDragDelta -= (o, e) => OnHeaderDragDelta(e);
                _windowControl.HeaderIconDoubleClicked -= (o, e) => OnHeaderIconDoubleClick(e);
                _windowControl.CloseButtonClicked -= (o, e) => OnCloseButtonClicked(e);
            }

            _windowControl = GetTemplateChild(PART_WindowControl) as WindowControl;
            if (_windowControl != null)
            {
                _windowControl.HeaderDragDelta += (o, e) => OnHeaderDragDelta(e);
                _windowControl.HeaderIconDoubleClicked += (o, e) => OnHeaderIconDoubleClick(e);
                _windowControl.CloseButtonClicked += (o, e) => OnCloseButtonClicked(e);
            }

            UpdateBlockMouseInputsPanel();

            _windowRoot = GetTemplateChild(PART_WindowRoot) as Grid;
            if (_windowRoot != null) _windowRoot.RenderTransform = _moveTransform;
            _hasWindowContainer = VisualTreeHelper.GetParent(this) as WindowContainer != null;

            if (!_hasWindowContainer)
            {
                _parentContainer = VisualTreeHelper.GetParent(this) as FrameworkElement;
                if (_parentContainer != null)
                {
                    _parentContainer.LayoutUpdated += ParentContainer_LayoutUpdated;
                    _parentContainer.SizeChanged += ParentContainer_SizeChanged;

                    //this is for XBAP applications only. When inside an XBAP the parent container has no height or width until it has loaded. Therefore
                    //we need to handle the loaded event and reposition the window.
                    if (BrowserInteropHelper.IsBrowserHosted)
                        _parentContainer.Loaded += (o, e) => { ExecuteOpen(); };
                }

                Unloaded += ChildWindow_Unloaded;

                //initialize our modal background width/height
                _modalLayer.Height = _parentContainer.ActualHeight;
                _modalLayer.Width = _parentContainer.ActualWidth;

                _root = GetTemplateChild(PART_Root) as Grid;

#if VS2008
      FocusVisualStyle = null;
#else
                var focusStyle = _root != null ? _root.Resources["FocusVisualStyle"] as Style : null;
                if (focusStyle != null)
                {
                    var focusStyleDataContext = new Setter(DataContextProperty, this);
                    focusStyle.Setters.Add(focusStyleDataContext);
                    FocusVisualStyle = focusStyle;
                }
#endif
                if (_root != null) _root.Children.Add(_modalLayerPanel);
            }
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);

            Action action = () =>
            {
                if (FocusedElement != null)
                    FocusedElement.Focus();
            };

            Dispatcher.BeginInvoke(DispatcherPriority.Background, action);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (WindowState == WindowState.Open)
                switch (e.Key)
                {
                    case Key.Left:
                        Left -= _horizontalOffset;
                        e.Handled = true;
                        break;

                    case Key.Right:
                        Left += _horizontalOffset;
                        e.Handled = true;
                        break;

                    case Key.Down:
                        Top += _verticalOffset;
                        e.Handled = true;
                        break;

                    case Key.Up:
                        Top -= _verticalOffset;
                        e.Handled = true;
                        break;
                }
        }

        protected override void OnLeftPropertyChanged(double oldValue, double newValue)
        {
            base.OnLeftPropertyChanged(oldValue, newValue);

            _hasWindowContainer = VisualTreeHelper.GetParent(this) as WindowContainer != null;
            if (!_hasWindowContainer)
            {
                Left = GetRestrictedLeft();
                ProcessMove(newValue - oldValue, 0);
            }
        }

        protected override void OnTopPropertyChanged(double oldValue, double newValue)
        {
            base.OnTopPropertyChanged(oldValue, newValue);

            _hasWindowContainer = VisualTreeHelper.GetParent(this) as WindowContainer != null;
            if (!_hasWindowContainer)
            {
                Top = GetRestrictedTop();
                ProcessMove(0, newValue - oldValue);
            }
        }

        internal override void UpdateBlockMouseInputsPanel()
        {
            if (_windowControl != null) _windowControl.IsBlockMouseInputsPanelActive = IsBlockMouseInputsPanelActive;
        }

        #endregion //Base Class Overrides

        #region Event Handlers

        protected virtual void OnHeaderDragDelta(DragDeltaEventArgs e)
        {
            if (!IsCurrentWindow(e.OriginalSource))
                return;

            e.Handled = true;

            var args = new DragDeltaEventArgs(e.HorizontalChange, e.VerticalChange);
            args.RoutedEvent = HeaderDragDeltaEvent;
            args.Source = this;
            RaiseEvent(args);

            if (!args.Handled)
                if (Equals(e.OriginalSource, _windowControl))
                {
                    var left = 0.0;

                    if (FlowDirection == FlowDirection.RightToLeft)
                        left = Left - e.HorizontalChange;
                    else
                        left = Left + e.HorizontalChange;

                    Left = left;
                    Top += e.VerticalChange;
                }
        }

        protected virtual void OnHeaderIconDoubleClick(MouseButtonEventArgs e)
        {
            if (!IsCurrentWindow(e.OriginalSource))
                return;

            e.Handled = true;

            var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left);
            args.RoutedEvent = HeaderIconDoubleClickedEvent;
            args.Source = this;
            RaiseEvent(args);

            if (!args.Handled) Close();
        }

        protected virtual void OnCloseButtonClicked(RoutedEventArgs e)
        {
            if (!IsCurrentWindow(e.OriginalSource))
                return;

            e.Handled = true;

            var args = new RoutedEventArgs(CloseButtonClickedEvent, this);
            RaiseEvent(args);

            if (!args.Handled) Close();
        }


        [Obsolete("This method is obsolete and should no longer be used.")]
        private void ParentContainer_LayoutUpdated(object sender, EventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            //we only want to set the start position if this is the first time the control has bee initialized
            if (!_startupPositionInitialized)
            {
                ExecuteOpen();
                _startupPositionInitialized = true;
            }
        }

        [Obsolete("This method is obsolete and should no longer be used.")]
        private void ChildWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_parentContainer != null)
            {
                _parentContainer.LayoutUpdated -= ParentContainer_LayoutUpdated;
                _parentContainer.SizeChanged -= ParentContainer_SizeChanged;

                //this is for XBAP applications only. When inside an XBAP the parent container has no height or width until it has loaded. Therefore
                //we need to handle the loaded event and reposition the window.
                if (BrowserInteropHelper.IsBrowserHosted)
                    _parentContainer.Loaded -= (o, ev) => { ExecuteOpen(); };
            }
        }

        [Obsolete("This method is obsolete and should no longer be used.")]
        private void ParentContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //resize our modal layer
            _modalLayer.Height = e.NewSize.Height;
            _modalLayer.Width = e.NewSize.Width;

            //reposition our window
            Left = GetRestrictedLeft();
            Top = GetRestrictedTop();
        }

        #endregion //Event Handlers

        #region Methods

        #region Private

        [Obsolete(
            "This method is obsolete and should no longer be used. Use WindowContainer.GetRestrictedLeft() instead.")]
        private double GetRestrictedLeft()
        {
            if (Left < 0)
                return 0;

            if (_parentContainer != null && _windowRoot != null)
                if (Left + _windowRoot.ActualWidth > _parentContainer.ActualWidth && _parentContainer.ActualWidth != 0)
                {
                    var left = _parentContainer.ActualWidth - _windowRoot.ActualWidth;
                    return left < 0 ? 0 : left;
                }

            return Left;
        }

        [Obsolete(
            "This method is obsolete and should no longer be used. Use WindowContainer.GetRestrictedTop() instead.")]
        private double GetRestrictedTop()
        {
            if (Top < 0)
                return 0;

            if (_parentContainer != null && _windowRoot != null)
                if (Top + _windowRoot.ActualHeight > _parentContainer.ActualHeight &&
                    _parentContainer.ActualHeight != 0)
                {
                    var top = _parentContainer.ActualHeight - _windowRoot.ActualHeight;
                    return top < 0 ? 0 : top;
                }

            return Top;
        }

        private void SetWindowState(WindowState state)
        {
            switch (state)
            {
                case WindowState.Closed:
                {
                    ExecuteClose();
                    break;
                }
                case WindowState.Open:
                {
                    ExecuteOpen();
                    break;
                }
            }
        }

        private void ExecuteClose()
        {
            var e = new CancelEventArgs();
            OnClosing(e);

            if (!e.Cancel)
            {
                if (!_dialogResult.HasValue)
                    _dialogResult = false;

                OnClosed(EventArgs.Empty);
            }
            else
            {
                CancelClose();
            }
        }

        private void CancelClose()
        {
            _dialogResult = null; //when the close is cancelled, DialogResult should be null

            _ignorePropertyChanged = true;
            WindowState = WindowState.Open; //now reset the window state to open because the close was cancelled
            _ignorePropertyChanged = false;
        }

        private void ExecuteOpen()
        {
            _dialogResult = null; //reset the dialogResult to null each time the window is opened

            if (!_hasWindowContainer)
                if (WindowStartupLocation == WindowStartupLocation.Center)
                    CenterChildWindow();

            if (!_hasWindowContainer)
                BringToFront();
        }

        private bool IsCurrentWindow(object windowtoTest)
        {
            return Equals(_windowControl, windowtoTest);
        }

        [Obsolete("This method is obsolete and should no longer be used. Use WindowContainer.BringToFront() instead.")]
        private void BringToFront()
        {
            var index = 0;

            if (_parentContainer != null)
                index = (int) _parentContainer.GetValue(Panel.ZIndexProperty);

            SetValue(Panel.ZIndexProperty, ++index);

            if (IsModal)
                Panel.SetZIndex(_modalLayerPanel, index - 2);
        }

        [Obsolete("This method is obsolete and should no longer be used. Use WindowContainer.CenterChild() instead.")]
        private void CenterChildWindow()
        {
            if (_parentContainer != null && _windowRoot != null)
            {
                Left = (_parentContainer.ActualWidth - _windowRoot.ActualWidth) / 2.0;
                Top = (_parentContainer.ActualHeight - _windowRoot.ActualHeight) / 2.0;
            }
        }

        [Obsolete("This method is obsolete and should no longer be used.")]
        private void ShowModalLayer()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                if (!_modalLayerPanel.Children.Contains(_modalLayer))
                    _modalLayerPanel.Children.Add(_modalLayer);

                _modalLayer.Visibility = Visibility.Visible;
            }
        }

        [Obsolete("This method is obsolete and should no longer be used.")]
        private void HideModalLayer()
        {
            _modalLayer.Visibility = Visibility.Collapsed;
        }

        [Obsolete(
            "This method is obsolete and should no longer be used. Use the ChildWindow in a WindowContainer instead.")]
        private void ProcessMove(double x, double y)
        {
            _moveTransform.X += x;
            _moveTransform.Y += y;

            InvalidateArrange();
        }

        #endregion //Private

        #region Public

        public void Show()
        {
            WindowState = WindowState.Open;
        }

        public void Close()
        {
            WindowState = WindowState.Closed;
        }

        #endregion //Public

        #endregion //Methods

        #region Events

        /// <summary>
        ///     Occurs when the ChildWindow is closed.
        /// </summary>
        public event EventHandler Closed;

        protected virtual void OnClosed(EventArgs e)
        {
            if (Closed != null)
                Closed(this, e);
        }

        /// <summary>
        ///     Occurs when the ChildWindow is closing.
        /// </summary>
        public event EventHandler<CancelEventArgs> Closing;

        protected virtual void OnClosing(CancelEventArgs e)
        {
            if (Closing != null)
                Closing(this, e);
        }

        #endregion //Events
    }

#pragma warning restore 0809
#pragma warning restore 0618
}