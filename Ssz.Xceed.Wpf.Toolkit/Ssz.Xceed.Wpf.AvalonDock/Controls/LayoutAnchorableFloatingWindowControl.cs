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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Windows.Shell;
using Ssz.Xceed.Wpf.AvalonDock.Commands;
using Ssz.Xceed.Wpf.AvalonDock.Converters;
using Ssz.Xceed.Wpf.AvalonDock.Layout;
using Ssz.Xceed.Wpf.AvalonDock.Themes;

namespace Ssz.Xceed.Wpf.AvalonDock.Controls
{
    public class LayoutAnchorableFloatingWindowControl : LayoutFloatingWindowControl, IOverlayWindowHost
    {
        #region Event Handlers

        private void LayoutAnchorableFloatingWindowControl_IsVisibleChanged(object sender,
            DependencyPropertyChangedEventArgs e)
        {
            var visibilityBinding = GetBindingExpression(VisibilityProperty);
            if (IsVisible && visibilityBinding == null)
                SetBinding(VisibilityProperty,
                    new Binding("IsVisible")
                    {
                        Source = _model, Converter = new BoolToVisibilityConverter(), Mode = BindingMode.OneWay,
                        ConverterParameter = Visibility.Hidden
                    });
        }

        #endregion

        #region Members

        private readonly LayoutAnchorableFloatingWindow _model;
        private OverlayWindow _overlayWindow;
        private List<IDropArea> _dropAreas;

        #endregion

        #region Constructors

        static LayoutAnchorableFloatingWindowControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LayoutAnchorableFloatingWindowControl),
                new FrameworkPropertyMetadata(typeof(LayoutAnchorableFloatingWindowControl)));
        }

        internal LayoutAnchorableFloatingWindowControl(LayoutAnchorableFloatingWindow model, bool isContentImmutable)
            : base(model, isContentImmutable)
        {
            _model = model;
            HideWindowCommand =
                new RelayCommand(p => OnExecuteHideWindowCommand(p), p => CanExecuteHideWindowCommand(p));
            CloseWindowCommand =
                new RelayCommand(p => OnExecuteCloseWindowCommand(p), p => CanExecuteCloseWindowCommand(p));
            UpdateThemeResources();
        }

        internal LayoutAnchorableFloatingWindowControl(LayoutAnchorableFloatingWindow model)
            : this(model, false)
        {
        }

        #endregion

        #region Properties

        #region SingleContentLayoutItem

        /// <summary>
        ///     SingleContentLayoutItem Dependency Property
        /// </summary>
        public static readonly DependencyProperty SingleContentLayoutItemProperty = DependencyProperty.Register(
            "SingleContentLayoutItem", typeof(LayoutItem), typeof(LayoutAnchorableFloatingWindowControl),
            new FrameworkPropertyMetadata(null, OnSingleContentLayoutItemChanged));

        /// <summary>
        ///     Gets or sets the SingleContentLayoutItem property.  This dependency property
        ///     indicates the layout item of the selected content when is shown a single anchorable pane.
        /// </summary>
        public LayoutItem SingleContentLayoutItem
        {
            get => (LayoutItem) GetValue(SingleContentLayoutItemProperty);
            set => SetValue(SingleContentLayoutItemProperty, value);
        }

        /// <summary>
        ///     Handles changes to the SingleContentLayoutItem property.
        /// </summary>
        private static void OnSingleContentLayoutItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LayoutAnchorableFloatingWindowControl) d).OnSingleContentLayoutItemChanged(e);
        }

        /// <summary>
        ///     Provides derived classes an opportunity to handle changes to the SingleContentLayoutItem property.
        /// </summary>
        protected virtual void OnSingleContentLayoutItemChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        #endregion

        #endregion

        #region Overrides

        public override ILayoutElement Model => _model;

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            var manager = _model.Root.Manager;

            Content = manager.CreateUIElementForModel(_model.RootPanel);

            //SetBinding(VisibilityProperty, new Binding("IsVisible") { Source = _model, Converter = new BoolToVisibilityConverter(), Mode = BindingMode.OneWay, ConverterParameter = Visibility.Hidden });

            //Issue: http://avalondock.codeplex.com/workitem/15036
            IsVisibleChanged += LayoutAnchorableFloatingWindowControl_IsVisibleChanged;

            SetBinding(SingleContentLayoutItemProperty,
                new Binding("Model.SinglePane.SelectedContent")
                    {Source = this, Converter = new LayoutItemFromLayoutModelConverter()});

            _model.PropertyChanged += _model_PropertyChanged;
        }

        protected override void OnClosed(EventArgs e)
        {
            var root = Model.Root;
            if (root != null)
            {
                root.Manager.RemoveFloatingWindow(this);
                root.CollectGarbage();
            }

            if (_overlayWindow != null)
            {
                _overlayWindow.Close();
                _overlayWindow = null;
            }

            base.OnClosed(e);

            if (!CloseInitiatedByUser && root != null) root.FloatingWindows.Remove(_model);

            _model.PropertyChanged -= _model_PropertyChanged;
            IsVisibleChanged -= LayoutAnchorableFloatingWindowControl_IsVisibleChanged;
            BindingOperations.ClearBinding(this, VisibilityProperty);
            BindingOperations.ClearBinding(this, SingleContentLayoutItemProperty);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (CloseInitiatedByUser && !KeepContentVisibleOnClose)
            {
                e.Cancel = true;
                _model.Descendents().OfType<LayoutAnchorable>().ToArray().ForEach(a => a.Hide());
            }

            base.OnClosing(e);
        }

        protected override IntPtr FilterMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case Win32Helper.WM_NCLBUTTONDOWN: //Left button down on title -> start dragging over docking manager
                    if (wParam.ToInt32() == Win32Helper.HT_CAPTION)
                    {
                        _model.Descendents().OfType<LayoutAnchorablePane>()
                                .First(p => p.ChildrenCount > 0 && p.SelectedContent != null).SelectedContent.IsActive =
                            true;
                        handled = true;
                    }

                    break;
                case Win32Helper.WM_NCRBUTTONUP:
                    if (wParam.ToInt32() == Win32Helper.HT_CAPTION)
                    {
                        if (OpenContextMenu())
                            handled = true;

                        if (_model.Root.Manager.ShowSystemMenu)
                            WindowChrome.GetWindowChrome(this).ShowSystemMenu = !handled;
                        else
                            WindowChrome.GetWindowChrome(this).ShowSystemMenu = false;
                    }

                    break;
            }

            return base.FilterMessage(hwnd, msg, wParam, lParam, ref handled);
        }

        internal override void UpdateThemeResources(Theme oldTheme = null)
        {
            base.UpdateThemeResources(oldTheme);

            if (_overlayWindow != null) _overlayWindow.UpdateThemeResources(oldTheme);
        }

        #endregion

        #region Private Methods

        private void _model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "RootPanel" &&
                _model.RootPanel == null)
                InternalClose();
        }

        private void CreateOverlayWindow()
        {
            if (_overlayWindow == null)
                _overlayWindow = new OverlayWindow(this);
            var rectWindow = new Rect(this.PointToScreenDPIWithoutFlowDirection(new Point()),
                this.TransformActualSizeToAncestor());
            _overlayWindow.Left = rectWindow.Left;
            _overlayWindow.Top = rectWindow.Top;
            _overlayWindow.Width = rectWindow.Width;
            _overlayWindow.Height = rectWindow.Height;
        }

        private bool OpenContextMenu()
        {
            var ctxMenu = _model.Root.Manager.AnchorableContextMenu;
            if (ctxMenu != null && SingleContentLayoutItem != null)
            {
                ctxMenu.PlacementTarget = null;
                ctxMenu.Placement = PlacementMode.MousePoint;
                ctxMenu.DataContext = SingleContentLayoutItem;
                ctxMenu.IsOpen = true;
                return true;
            }

            return false;
        }

        private bool IsContextMenuOpen()
        {
            var ctxMenu = _model.Root.Manager.AnchorableContextMenu;
            if (ctxMenu != null && SingleContentLayoutItem != null) return ctxMenu.IsOpen;

            return false;
        }

        #endregion

        #region Commands

        #region HideWindowCommand

        public ICommand HideWindowCommand { get; }

        private bool CanExecuteHideWindowCommand(object parameter)
        {
            if (Model == null)
                return false;

            var root = Model.Root;
            if (root == null)
                return false;

            var manager = root.Manager;
            if (manager == null)
                return false;

            var canExecute = false;
            foreach (var anchorable in Model.Descendents().OfType<LayoutAnchorable>().ToArray())
            {
                if (!anchorable.CanHide)
                {
                    canExecute = false;
                    break;
                }

                var anchorableLayoutItem = manager.GetLayoutItemFromModel(anchorable) as LayoutAnchorableItem;
                if (anchorableLayoutItem == null ||
                    anchorableLayoutItem.HideCommand == null ||
                    !anchorableLayoutItem.HideCommand.CanExecute(parameter))
                {
                    canExecute = false;
                    break;
                }

                canExecute = true;
            }

            return canExecute;
        }

        private void OnExecuteHideWindowCommand(object parameter)
        {
            var manager = Model.Root.Manager;
            foreach (var anchorable in Model.Descendents().OfType<LayoutAnchorable>().ToArray())
            {
                var anchorableLayoutItem = manager.GetLayoutItemFromModel(anchorable) as LayoutAnchorableItem;
                anchorableLayoutItem.HideCommand.Execute(parameter);
            }
        }

        #endregion

        #region CloseWindowCommand

        public ICommand CloseWindowCommand { get; }

        private bool CanExecuteCloseWindowCommand(object parameter)
        {
            if (Model == null)
                return false;

            var root = Model.Root;
            if (root == null)
                return false;

            var manager = root.Manager;
            if (manager == null)
                return false;

            var canExecute = false;
            foreach (var anchorable in Model.Descendents().OfType<LayoutAnchorable>().ToArray())
            {
                if (!anchorable.CanClose)
                {
                    canExecute = false;
                    break;
                }

                var anchorableLayoutItem = manager.GetLayoutItemFromModel(anchorable) as LayoutAnchorableItem;
                if (anchorableLayoutItem == null ||
                    anchorableLayoutItem.CloseCommand == null ||
                    !anchorableLayoutItem.CloseCommand.CanExecute(parameter))
                {
                    canExecute = false;
                    break;
                }

                canExecute = true;
            }

            return canExecute;
        }

        private void OnExecuteCloseWindowCommand(object parameter)
        {
            var manager = Model.Root.Manager;
            foreach (var anchorable in Model.Descendents().OfType<LayoutAnchorable>().ToArray())
            {
                var anchorableLayoutItem = manager.GetLayoutItemFromModel(anchorable) as LayoutAnchorableItem;
                anchorableLayoutItem.CloseCommand.Execute(parameter);
            }
        }

        #endregion

        #endregion

        #region IOverlayWindowHost

        bool IOverlayWindowHost.HitTest(Point dragPoint)
        {
            var detectionRect = new Rect(this.PointToScreenDPIWithoutFlowDirection(new Point()),
                this.TransformActualSizeToAncestor());
            return detectionRect.Contains(dragPoint);
        }

        DockingManager IOverlayWindowHost.Manager => _model.Root.Manager;

        IOverlayWindow IOverlayWindowHost.ShowOverlayWindow(LayoutFloatingWindowControl draggingWindow)
        {
            CreateOverlayWindow();
            _overlayWindow.Owner = draggingWindow;
            _overlayWindow.EnableDropTargets();
            _overlayWindow.Show();

            return _overlayWindow;
        }

        void IOverlayWindowHost.HideOverlayWindow()
        {
            _dropAreas = null;
            _overlayWindow.Owner = null;
            _overlayWindow.HideDropTargets();
        }

        IEnumerable<IDropArea> IOverlayWindowHost.GetDropAreas(LayoutFloatingWindowControl draggingWindow)
        {
            if (_dropAreas != null)
                return _dropAreas;

            _dropAreas = new List<IDropArea>();

            if (draggingWindow.Model is LayoutDocumentFloatingWindow)
                return _dropAreas;

            var rootVisual = (Content as FloatingWindowContentHost).RootVisual;

            foreach (var areaHost in rootVisual.FindVisualChildren<LayoutAnchorablePaneControl>())
                _dropAreas.Add(new DropArea<LayoutAnchorablePaneControl>(
                    areaHost,
                    DropAreaType.AnchorablePane));
            foreach (var areaHost in rootVisual.FindVisualChildren<LayoutDocumentPaneControl>())
                _dropAreas.Add(new DropArea<LayoutDocumentPaneControl>(
                    areaHost,
                    DropAreaType.DocumentPane));

            return _dropAreas;
        }

        #endregion
    }
}