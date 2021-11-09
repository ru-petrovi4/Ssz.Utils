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
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Ssz.Xceed.Wpf.Toolkit.Primitives
{
    [TemplatePart(Name = PART_HeaderThumb, Type = typeof(Thumb))]
    [TemplatePart(Name = PART_Icon, Type = typeof(Image))]
    [TemplatePart(Name = PART_CloseButton, Type = typeof(Button))]
    [TemplatePart(Name = PART_ToolWindowCloseButton, Type = typeof(Button))]
    [TemplatePart(Name = PART_BlockMouseInputsBorder, Type = typeof(Border))]
    [TemplatePart(Name = PART_HeaderGrid, Type = typeof(Grid))]
    public class WindowControl : ContentControl
    {
        private const string PART_HeaderThumb = "PART_HeaderThumb";
        private const string PART_Icon = "PART_Icon";
        private const string PART_CloseButton = "PART_CloseButton";
        private const string PART_ToolWindowCloseButton = "PART_ToolWindowCloseButton";
        private const string PART_BlockMouseInputsBorder = "PART_BlockMouseInputsBorder";
        private const string PART_HeaderGrid = "PART_HeaderGrid";

        public static readonly ComponentResourceKey DefaultCloseButtonStyleKey =
            new(typeof(WindowControl), "DefaultCloseButtonStyle");

        #region Base Class Overrides

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_headerThumb is not null)
            {
                _headerThumb.PreviewMouseLeftButtonDown -= HeaderPreviewMouseLeftButtonDown;
                _headerThumb.PreviewMouseRightButtonDown -= HeaderPreviewMouseRightButtonDown;
                _headerThumb.DragDelta -= HeaderThumbDragDelta;
            }

            _headerThumb = Template.FindName(PART_HeaderThumb, this) as Thumb;
            if (_headerThumb is not null)
            {
                _headerThumb.PreviewMouseLeftButtonDown += HeaderPreviewMouseLeftButtonDown;
                _headerThumb.PreviewMouseRightButtonDown += HeaderPreviewMouseRightButtonDown;
                _headerThumb.DragDelta += HeaderThumbDragDelta;
            }

            if (_icon is not null) _icon.MouseLeftButtonDown -= IconMouseLeftButtonDown;
            _icon = Template.FindName(PART_Icon, this) as Image;
            if (_icon is not null) _icon.MouseLeftButtonDown += IconMouseLeftButtonDown;

            if (_closeButton is not null) _closeButton.Click -= Close;
            _closeButton = Template.FindName(PART_CloseButton, this) as Button;
            if (_closeButton is not null) _closeButton.Click += Close;

            if (_windowToolboxCloseButton is not null) _windowToolboxCloseButton.Click -= Close;
            _windowToolboxCloseButton = Template.FindName(PART_ToolWindowCloseButton, this) as Button;
            if (_windowToolboxCloseButton is not null) _windowToolboxCloseButton.Click += Close;

            _windowBlockMouseInputsPanel = Template.FindName(PART_BlockMouseInputsBorder, this) as Border;
            UpdateBlockMouseInputsPanel();
        }

        #endregion //Base Class Overrides

        #region Members

        private Thumb _headerThumb;
        private Image _icon;
        private Button _closeButton;
        private Button _windowToolboxCloseButton;
        private bool _setIsActiveInternal;
        internal Border _windowBlockMouseInputsPanel;

        #endregion

        #region Constructors

        static WindowControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WindowControl),
                new FrameworkPropertyMetadata(typeof(WindowControl)));
        }

        #endregion //Constructors

        #region Dependency Properties

        #region Public DP

        #region Caption

        public static readonly DependencyProperty CaptionProperty = DependencyProperty.Register("Caption",
            typeof(string), typeof(WindowControl), new UIPropertyMetadata(string.Empty));

        public string Caption
        {
            get => (string) GetValue(CaptionProperty);
            set => SetValue(CaptionProperty, value);
        }

        #endregion //Caption

        #region CaptionForeground

        public static readonly DependencyProperty CaptionForegroundProperty =
            DependencyProperty.Register("CaptionForeground", typeof(Brush), typeof(WindowControl),
                new UIPropertyMetadata(null));

        public Brush CaptionForeground
        {
            get => (Brush) GetValue(CaptionForegroundProperty);
            set => SetValue(CaptionForegroundProperty, value);
        }

        #endregion //CaptionForeground

        #region CaptionShadowBrush

        public static readonly DependencyProperty CaptionShadowBrushProperty =
            DependencyProperty.Register("CaptionShadowBrush", typeof(Brush), typeof(WindowControl),
                new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(179, 255, 255, 255))));

        public Brush CaptionShadowBrush
        {
            get => (Brush) GetValue(CaptionShadowBrushProperty);
            set => SetValue(CaptionShadowBrushProperty, value);
        }

        #endregion //CaptionShadowBrush

        #region CaptionIcon

        public static readonly DependencyProperty CaptionIconProperty = DependencyProperty.Register("CaptionIcon",
            typeof(ImageSource), typeof(WindowControl), new UIPropertyMetadata(null));

        public ImageSource CaptionIcon
        {
            get => (ImageSource) GetValue(CaptionIconProperty);
            set => SetValue(CaptionIconProperty, value);
        }

        #endregion //CaptionIcon

        #region CloseButtonStyle

        public static readonly DependencyProperty CloseButtonStyleProperty =
            DependencyProperty.Register("CloseButtonStyle", typeof(Style), typeof(WindowControl),
                new UIPropertyMetadata(null));

        public Style CloseButtonStyle
        {
            get => (Style) GetValue(CloseButtonStyleProperty);
            set => SetValue(CloseButtonStyleProperty, value);
        }

        #endregion //CloseButtonStyle

        #region CloseButtonVisibility

        public static readonly DependencyProperty CloseButtonVisibilityProperty =
            DependencyProperty.Register("CloseButtonVisibility", typeof(Visibility), typeof(WindowControl),
                new PropertyMetadata(Visibility.Visible, null, OnCoerceCloseButtonVisibility));

        public Visibility CloseButtonVisibility
        {
            get => (Visibility) GetValue(CloseButtonVisibilityProperty);
            set => SetValue(CloseButtonVisibilityProperty, value);
        }

        private static object OnCoerceCloseButtonVisibility(DependencyObject d, object basevalue)
        {
            if (basevalue == DependencyProperty.UnsetValue)
                return basevalue;

            var windowControl = d as WindowControl;
            if (windowControl is null)
                return basevalue;
            return windowControl.OnCoerceCloseButtonVisibility((Visibility) basevalue);
        }

        protected virtual object OnCoerceCloseButtonVisibility(Visibility newValue)
        {
            return newValue;
        }

        #endregion //CloseButtonVisibility

        #region IsActive

        public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register("IsActive",
            typeof(bool), typeof(WindowControl), new UIPropertyMetadata(true, null, OnCoerceIsActive));

        public bool IsActive
        {
            get => (bool) GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        private static object OnCoerceIsActive(DependencyObject d, object basevalue)
        {
            var w = d as WindowControl;
            if (w is not null && !w._setIsActiveInternal && !w.AllowPublicIsActiveChange)
                throw new InvalidOperationException(
                    "Cannot set IsActive directly. This is handled by the underlying system");

            return basevalue;
        }

        internal void SetIsActiveInternal(bool isActive)
        {
            _setIsActiveInternal = true;
            IsActive = isActive;
            _setIsActiveInternal = false;
        }

        #endregion //IsActive


        #region Left

        public static readonly DependencyProperty LeftProperty = DependencyProperty.Register("Left", typeof(double),
            typeof(WindowControl), new PropertyMetadata(0.0, OnLeftPropertyChanged, OnCoerceLeft));

        public double Left
        {
            get => (double) GetValue(LeftProperty);
            set => SetValue(LeftProperty, value);
        }

        private static object OnCoerceLeft(DependencyObject d, object basevalue)
        {
            if (basevalue == DependencyProperty.UnsetValue)
                return basevalue;

            var windowControl = (WindowControl) d;
            if (windowControl is null)
                return basevalue;

            return windowControl.OnCoerceLeft(basevalue);
        }

        private object OnCoerceLeft(object newValue)
        {
            var value = (double) newValue;
            if (Equals(value, double.NaN))
                return 0.0;

            return newValue;
        }

        private static void OnLeftPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var windowControl = obj as WindowControl;
            if (windowControl is not null)
                windowControl.OnLeftPropertyChanged((double) e.OldValue, (double) e.NewValue);
        }

        internal event EventHandler<EventArgs> LeftChanged;

        protected virtual void OnLeftPropertyChanged(double oldValue, double newValue)
        {
            var handler = LeftChanged;
            if (handler is not null) handler(this, EventArgs.Empty);
        }

        #endregion //Left

        #region Top

        public static readonly DependencyProperty TopProperty = DependencyProperty.Register("Top", typeof(double),
            typeof(WindowControl), new PropertyMetadata(0.0, OnTopPropertyChanged, OnCoerceTop));

        public double Top
        {
            get => (double) GetValue(TopProperty);
            set => SetValue(TopProperty, value);
        }

        private static object OnCoerceTop(DependencyObject d, object basevalue)
        {
            if (basevalue == DependencyProperty.UnsetValue)
                return basevalue;

            var windowControl = (WindowControl) d;
            if (windowControl is null)
                return basevalue;

            return windowControl.OnCoerceTop(basevalue);
        }

        private object OnCoerceTop(object newValue)
        {
            var value = (double) newValue;
            if (Equals(value, double.NaN))
                return 0.0;

            return newValue;
        }

        private static void OnTopPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var windowControl = obj as WindowControl;
            if (windowControl is not null)
                windowControl.OnTopPropertyChanged((double) e.OldValue, (double) e.NewValue);
        }

        internal event EventHandler<EventArgs> TopChanged;

        protected virtual void OnTopPropertyChanged(double oldValue, double newValue)
        {
            var handler = TopChanged;
            if (handler is not null) handler(this, EventArgs.Empty);
        }

        #endregion //TopProperty


        #region WindowBackground

        public static readonly DependencyProperty WindowBackgroundProperty =
            DependencyProperty.Register("WindowBackground", typeof(Brush), typeof(WindowControl),
                new PropertyMetadata(null));

        public Brush WindowBackground
        {
            get => (Brush) GetValue(WindowBackgroundProperty);
            set => SetValue(WindowBackgroundProperty, value);
        }

        #endregion // WindowBackground

        #region WindowBorderBrush

        public static readonly DependencyProperty WindowBorderBrushProperty =
            DependencyProperty.Register("WindowBorderBrush", typeof(Brush), typeof(WindowControl),
                new PropertyMetadata(null));

        public Brush WindowBorderBrush
        {
            get => (Brush) GetValue(WindowBorderBrushProperty);
            set => SetValue(WindowBorderBrushProperty, value);
        }

        #endregion //WindowBorderBrush

        #region WindowBorderThickness

        public static readonly DependencyProperty WindowBorderThicknessProperty =
            DependencyProperty.Register("WindowBorderThickness", typeof(Thickness), typeof(WindowControl),
                new PropertyMetadata(new Thickness(0)));

        public Thickness WindowBorderThickness
        {
            get => (Thickness) GetValue(WindowBorderThicknessProperty);
            set => SetValue(WindowBorderThicknessProperty, value);
        }

        #endregion //WindowBorderThickness

        #region WindowInactiveBackground

        public static readonly DependencyProperty WindowInactiveBackgroundProperty =
            DependencyProperty.Register("WindowInactiveBackground", typeof(Brush), typeof(WindowControl),
                new PropertyMetadata(null));

        public Brush WindowInactiveBackground
        {
            get => (Brush) GetValue(WindowInactiveBackgroundProperty);
            set => SetValue(WindowInactiveBackgroundProperty, value);
        }

        #endregion // WindowInactiveBackground

        #region WindowOpacity

        public static readonly DependencyProperty WindowOpacityProperty =
            DependencyProperty.Register("WindowOpacity", typeof(double), typeof(WindowControl),
                new PropertyMetadata(1d));

        public double WindowOpacity
        {
            get => (double) GetValue(WindowOpacityProperty);
            set => SetValue(WindowOpacityProperty, value);
        }

        #endregion //WindowOpacity

        #region WindowStyle

        public static readonly DependencyProperty WindowStyleProperty = DependencyProperty.Register("WindowStyle",
            typeof(WindowStyle), typeof(WindowControl),
            new PropertyMetadata(WindowStyle.SingleBorderWindow, null, OnCoerceWindowStyle));

        public WindowStyle WindowStyle
        {
            get => (WindowStyle) GetValue(WindowStyleProperty);
            set => SetValue(WindowStyleProperty, value);
        }

        private static object OnCoerceWindowStyle(DependencyObject d, object basevalue)
        {
            if (basevalue == DependencyProperty.UnsetValue)
                return basevalue;

            var windowControl = d as WindowControl;
            if (windowControl is null)
                return basevalue;
            return windowControl.OnCoerceWindowStyle((WindowStyle) basevalue);
        }

        protected virtual object OnCoerceWindowStyle(WindowStyle newValue)
        {
            return newValue;
        }

        private static void OnWindowStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = (WindowControl) d;
            if (window is not null)
                window.OnWindowStyleChanged((WindowStyle) e.OldValue, (WindowStyle) e.NewValue);
        }

        protected virtual void OnWindowStyleChanged(WindowStyle oldValue, WindowStyle newValue)
        {
        }

        #endregion //WindowStyle  

        #region WindowThickness

        public static readonly DependencyProperty WindowThicknessProperty = DependencyProperty.Register(
            "WindowThickness", typeof(Thickness), typeof(WindowControl),
            new PropertyMetadata(new Thickness(SystemParameters.ResizeFrameVerticalBorderWidth - 3,
                SystemParameters.ResizeFrameHorizontalBorderHeight - 3,
                SystemParameters.ResizeFrameVerticalBorderWidth - 3,
                SystemParameters.ResizeFrameHorizontalBorderHeight - 3)));

        public Thickness WindowThickness
        {
            get => (Thickness) GetValue(WindowThicknessProperty);
            set => SetValue(WindowThicknessProperty, value);
        }

        #endregion //WindowThickness

        #endregion //Public DP

        #endregion //Dependency Properties

        #region Internal Properties

        internal bool IsStartupPositionInitialized { get; set; }

        #region IsBlockMouseInputsPanelActive (Internal)

        internal bool IsBlockMouseInputsPanelActive
        {
            get => _IsBlockMouseInputsPanelActive;
            set
            {
                if (value != _IsBlockMouseInputsPanelActive)
                {
                    _IsBlockMouseInputsPanelActive = value;
                    UpdateBlockMouseInputsPanel();
                }
            }
        }

        private bool _IsBlockMouseInputsPanelActive;

        #endregion

        internal virtual bool AllowPublicIsActiveChange => true;

        #endregion //Internal Properties

        #region Events

        #region HeaderMouseLeftButtonClickedEvent

        public static readonly RoutedEvent HeaderMouseLeftButtonClickedEvent =
            EventManager.RegisterRoutedEvent("HeaderMouseLeftButtonClicked", RoutingStrategy.Bubble,
                typeof(MouseButtonEventHandler), typeof(WindowControl));

        public event MouseButtonEventHandler HeaderMouseLeftButtonClicked
        {
            add => AddHandler(HeaderMouseLeftButtonClickedEvent, value);
            remove => RemoveHandler(HeaderMouseLeftButtonClickedEvent, value);
        }

        #endregion //HeaderMouseLeftButtonClickedEvent

        #region HeaderMouseRightButtonClickedEvent

        public static readonly RoutedEvent HeaderMouseRightButtonClickedEvent =
            EventManager.RegisterRoutedEvent("HeaderMouseRightButtonClicked", RoutingStrategy.Bubble,
                typeof(MouseButtonEventHandler), typeof(WindowControl));

        public event MouseButtonEventHandler HeaderMouseRightButtonClicked
        {
            add => AddHandler(HeaderMouseRightButtonClickedEvent, value);
            remove => RemoveHandler(HeaderMouseRightButtonClickedEvent, value);
        }

        #endregion //HeaderMouseRightButtonClickedEvent

        #region HeaderMouseLeftButtonDoubleClickedEvent

        public static readonly RoutedEvent HeaderMouseLeftButtonDoubleClickedEvent =
            EventManager.RegisterRoutedEvent("HeaderMouseLeftButtonDoubleClicked", RoutingStrategy.Bubble,
                typeof(MouseButtonEventHandler), typeof(WindowControl));

        public event MouseButtonEventHandler HeaderMouseLeftButtonDoubleClicked
        {
            add => AddHandler(HeaderMouseLeftButtonDoubleClickedEvent, value);
            remove => RemoveHandler(HeaderMouseLeftButtonDoubleClickedEvent, value);
        }

        #endregion //HeaderMouseLeftButtonDoubleClickedEvent

        #region HeaderDragDeltaEvent

        public static readonly RoutedEvent HeaderDragDeltaEvent = EventManager.RegisterRoutedEvent("HeaderDragDelta",
            RoutingStrategy.Bubble, typeof(DragDeltaEventHandler), typeof(WindowControl));

        public event DragDeltaEventHandler HeaderDragDelta
        {
            add => AddHandler(HeaderDragDeltaEvent, value);
            remove => RemoveHandler(HeaderDragDeltaEvent, value);
        }

        #endregion //HeaderDragDeltaEvent

        #region HeaderIconClickedEvent

        public static readonly RoutedEvent HeaderIconClickedEvent =
            EventManager.RegisterRoutedEvent("HeaderIconClicked", RoutingStrategy.Bubble,
                typeof(MouseButtonEventHandler), typeof(WindowControl));

        public event MouseButtonEventHandler HeaderIconClicked
        {
            add => AddHandler(HeaderIconClickedEvent, value);
            remove => RemoveHandler(HeaderIconClickedEvent, value);
        }

        #endregion //HeaderIconClickedEvent

        #region HeaderIconDoubleClickedEvent

        public static readonly RoutedEvent HeaderIconDoubleClickedEvent =
            EventManager.RegisterRoutedEvent("HeaderIconDoubleClicked", RoutingStrategy.Bubble,
                typeof(MouseButtonEventHandler), typeof(WindowControl));

        public event MouseButtonEventHandler HeaderIconDoubleClicked
        {
            add => AddHandler(HeaderIconDoubleClickedEvent, value);
            remove => RemoveHandler(HeaderIconDoubleClickedEvent, value);
        }

        #endregion //HeaderIconDoubleClickedEvent

        #region CloseButtonClickedEvent

        public static readonly RoutedEvent CloseButtonClickedEvent =
            EventManager.RegisterRoutedEvent("CloseButtonClicked", RoutingStrategy.Bubble, typeof(RoutedEventHandler),
                typeof(WindowControl));

        public event RoutedEventHandler CloseButtonClicked
        {
            add => AddHandler(CloseButtonClickedEvent, value);
            remove => RemoveHandler(CloseButtonClickedEvent, value);
        }

        #endregion //CloseButtonClickedEvent

        #endregion //Events

        #region Event Handler

        private void HeaderPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left);
            args.RoutedEvent = e.ClickCount == 2
                ? HeaderMouseLeftButtonDoubleClickedEvent
                : HeaderMouseLeftButtonClickedEvent;
            args.Source = this;
            RaiseEvent(args);
        }

        private void HeaderPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Right);
            args.RoutedEvent = HeaderMouseRightButtonClickedEvent;
            args.Source = this;
            RaiseEvent(args);
        }

        private void HeaderThumbDragDelta(object sender, DragDeltaEventArgs e)
        {
            var args = new DragDeltaEventArgs(e.HorizontalChange, e.VerticalChange);
            args.RoutedEvent = HeaderDragDeltaEvent;
            args.Source = this;
            RaiseEvent(args);
        }

        private void IconMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left);
            args.RoutedEvent = e.ClickCount == 2 ? HeaderIconDoubleClickedEvent : HeaderIconClickedEvent;
            args.Source = this;
            RaiseEvent(args);
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(CloseButtonClickedEvent, this));
        }

        #endregion // Event Handler

        #region Internal Methods

        internal virtual void UpdateBlockMouseInputsPanel()
        {
            if (_windowBlockMouseInputsPanel is not null)
                _windowBlockMouseInputsPanel.Visibility =
                    IsBlockMouseInputsPanelActive ? Visibility.Visible : Visibility.Collapsed;
        }

        internal double GetHeaderHeight()
        {
            var headerGrid = Template.FindName(PART_HeaderGrid, this) as Grid;
            if (headerGrid is not null)
                return headerGrid.ActualHeight;
            return 0;
        }

        #endregion

        #region Private Methods

        #endregion //Private Methods
    }
}