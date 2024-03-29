﻿/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Ssz.Xceed.Wpf.Toolkit.Core;
using Ssz.Xceed.Wpf.Toolkit.Core.Input;
using Ssz.Xceed.Wpf.Toolkit.Core.Utilities;

namespace Ssz.Xceed.Wpf.Toolkit.Zoombox
{
    public sealed class Zoombox : ContentControl
    {
        #region ViewStack Property

        public ZoomboxViewStack ViewStack
        {
            get
            {
                if (_viewStack is null && EffectiveViewStackMode != ZoomboxViewStackMode.Disabled)
                    _viewStack = new ZoomboxViewStack(this);
                return _viewStack;
            }
        }

        #endregion

        #region HasArrangedContentPresenter Internal Property

        internal bool HasArrangedContentPresenter
        {
            get => _cacheBits[(int) CacheBits.HasArrangedContentPresenter];
            set => _cacheBits[(int) CacheBits.HasArrangedContentPresenter] = value;
        }

        #endregion

        #region IsUpdatingView Internal Property

        internal bool IsUpdatingView
        {
            get => _cacheBits[(int) CacheBits.IsUpdatingView];
            set => _cacheBits[(int) CacheBits.IsUpdatingView] = value;
        }

        #endregion

        #region ContentOffset Private Property

        private Vector ContentOffset
        {
            get
            {
                // auto-wrapped content is always left and top aligned
                if (IsContentWrapped || _content is null || !(_content is FrameworkElement))
                    return new Vector(0, 0);

                double x = 0;
                double y = 0;
                var contentSize = ContentRect.Size;

                switch ((_content as FrameworkElement).HorizontalAlignment)
                {
                    case HorizontalAlignment.Center:
                    case HorizontalAlignment.Stretch:
                        x = (RenderSize.Width - contentSize.Width) / 2;
                        break;

                    case HorizontalAlignment.Right:
                        x = RenderSize.Width - contentSize.Width;
                        break;
                }

                switch ((_content as FrameworkElement).VerticalAlignment)
                {
                    case VerticalAlignment.Center:
                    case VerticalAlignment.Stretch:
                        y = (RenderSize.Height - contentSize.Height) / 2;
                        break;

                    case VerticalAlignment.Bottom:
                        y = RenderSize.Height - contentSize.Height;
                        break;
                }

                return new Vector(x, y);
            }
        }

        #endregion

        #region ContentRect Private Property

        private Rect ContentRect =>
            _content is null
                ? Rect.Empty
                : new Rect(new Size(_content.RenderSize.Width / _viewboxFactor,
                    _content.RenderSize.Height / _viewboxFactor));

        #endregion

        #region HasRenderedFirstView Private Property

        private bool HasRenderedFirstView
        {
            get => _cacheBits[(int) CacheBits.HasRenderedFirstView];
            set => _cacheBits[(int) CacheBits.HasRenderedFirstView] = value;
        }

        #endregion

        #region HasUIPermission Private Property

        private bool HasUIPermission => _cacheBits[(int) CacheBits.HasUIPermission];

        #endregion

        #region IsContentWrapped Private Property

        private bool IsContentWrapped
        {
            get => _cacheBits[(int) CacheBits.IsContentWrapped];
            set => _cacheBits[(int) CacheBits.IsContentWrapped] = value;
        }

        #endregion

        #region IsDraggingViewport Private Property

        private bool IsDraggingViewport
        {
            get => _cacheBits[(int) CacheBits.IsDraggingViewport];
            set => _cacheBits[(int) CacheBits.IsDraggingViewport] = value;
        }

        #endregion

        #region IsMonitoringInput Private Property

        private bool IsMonitoringInput
        {
            get => _cacheBits[(int) CacheBits.IsMonitoringInput];
            set => _cacheBits[(int) CacheBits.IsMonitoringInput] = value;
        }

        #endregion

        #region IsResizingViewport Private Property

        private bool IsResizingViewport
        {
            get => _cacheBits[(int) CacheBits.IsResizingViewport];
            set => _cacheBits[(int) CacheBits.IsResizingViewport] = value;
        }

        #endregion

        #region IsUpdatingViewport Private Property

        private bool IsUpdatingViewport
        {
            get => _cacheBits[(int) CacheBits.IsUpdatingViewport];
            set => _cacheBits[(int) CacheBits.IsUpdatingViewport] = value;
        }

        #endregion

        #region RefocusViewOnFirstRender Private Property

        private bool RefocusViewOnFirstRender
        {
            get => _cacheBits[(int) CacheBits.RefocusViewOnFirstRender];
            set => _cacheBits[(int) CacheBits.RefocusViewOnFirstRender] = value;
        }

        #endregion

        #region ViewFinderDisplayRect Private Property

        private Rect ViewFinderDisplayRect =>
            _viewFinderDisplay is null
                ? Rect.Empty
                : new Rect(new Point(0, 0),
                    new Point(_viewFinderDisplay.RenderSize.Width, _viewFinderDisplay.RenderSize.Height));

        #endregion

        public void CenterContent()
        {
            if (_content is not null) ZoomTo(ZoomboxView.Center);
        }

        public void FillToBounds()
        {
            if (_content is not null) ZoomTo(ZoomboxView.Fill);
        }

        public void FitToBounds()
        {
            if (_content is not null) ZoomTo(ZoomboxView.Fit);
        }

        public void GoBack()
        {
            if (EffectiveViewStackMode == ZoomboxViewStackMode.Disabled)
                return;

            if (ViewStackIndex > 0) ViewStackIndex--;
        }

        public void GoForward()
        {
            if (EffectiveViewStackMode == ZoomboxViewStackMode.Disabled)
                return;

            if (ViewStackIndex < ViewStack.Count - 1) ViewStackIndex++;
        }

        public void GoHome()
        {
            if (EffectiveViewStackMode == ZoomboxViewStackMode.Disabled)
                return;

            if (ViewStackIndex > 0) ViewStackIndex = 0;
        }

        public override void OnApplyTemplate()
        {
            AttachToVisualTree();
            base.OnApplyTemplate();
        }

        public void RefocusView()
        {
            if (EffectiveViewStackMode == ZoomboxViewStackMode.Disabled)
                return;

            if (ViewStackIndex >= 0 && ViewStackIndex < ViewStack.Count
                                    && CurrentView != ViewStack[ViewStackIndex])
                UpdateView(ViewStack[ViewStackIndex], true, false, ViewStackIndex);
        }

        public void Zoom(double percentage)
        {
            // if there is nothing to scale, just return
            if (_content is null)
                return;

            Zoom(percentage, GetZoomRelativePoint());
        }

        public void Zoom(double percentage, Point relativeTo)
        {
            // if there is nothing to scale, just return
            if (_content is null)
                return;

            // adjust the current scale relative to the given point
            var scale = Scale * (1 + percentage);
            ZoomTo(scale, relativeTo);
        }

        public void ZoomTo(Point position)
        {
            // if there is nothing to pan, just return
            if (_content is null)
                return;
            if (double.IsNaN(position.X) || double.IsNaN(position.Y)) return;
            // zoom to the new region
            ZoomTo(new ZoomboxView(new Point(-position.X, -position.Y)));
        }

        public void ZoomTo(Rect region)
        {
            if (_content is null)
                return;

            // adjust the current scale and position
            UpdateView(new ZoomboxView(region), true, true);
        }

        public void ZoomTo(double scale)
        {
            // if there is nothing to scale, just return
            if (_content is null)
                return;

            // adjust the current scale relative to the center of the content within the control
            ZoomTo(scale, true);
        }

        public void ZoomTo(double scale, Point relativeTo)
        {
            ZoomTo(scale, relativeTo, true, true);
        }

        public void ZoomTo(ZoomboxView view)
        {
            UpdateView(view, true, true);
        }

        internal void UpdateStackProperties()
        {
            SetValue(HasBackStackPropertyKey, ViewStackIndex > 0);
            SetValue(HasForwardStackPropertyKey, ViewStack.Count > ViewStackIndex + 1);
            CommandManager.InvalidateRequerySuggested();
        }

        protected override Size MeasureOverride(Size constraint)
        {
            if (_content is not null)
            {
                // measure visuals according to supplied constraint
                base.MeasureOverride(constraint);

                // now re-measure content to let the child be whatever size it desires
                _content.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            }

            // avoid returning infinity
            if (double.IsInfinity(constraint.Height)) constraint.Height = 0;

            if (double.IsInfinity(constraint.Width)) constraint.Width = 0;
            return constraint;
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            // disconnect SizeChanged handler from old content
            if (oldContent is FrameworkElement)
                (oldContent as FrameworkElement).RemoveHandler(SizeChangedEvent,
                    new SizeChangedEventHandler(OnContentSizeChanged));
            else
                RemoveHandler(SizeChangedEvent, new SizeChangedEventHandler(OnContentSizeChanged));

            // connect SizeChanged handler to new content
            if (_content is FrameworkElement)
                (_content as FrameworkElement).AddHandler(SizeChangedEvent,
                    new SizeChangedEventHandler(OnContentSizeChanged), true);
            else
                AddHandler(SizeChangedEvent, new SizeChangedEventHandler(OnContentSizeChanged), true);

            // update the Visual property of the view finder display panel's VisualBrush
            if (_viewFinderDisplay is not null && _viewFinderDisplay.VisualBrush is not null)
                _viewFinderDisplay.VisualBrush.Visual = _content;
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            MonitorInput();
            base.OnGotKeyboardFocus(e);
        }

        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            MonitorInput();
            base.OnLostKeyboardFocus(e);
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            CoerceValue(IsAnimatedProperty);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (HasArrangedContentPresenter && !HasRenderedFirstView)
            {
                HasRenderedFirstView = true;
                if (RefocusViewOnFirstRender)
                {
                    RefocusViewOnFirstRender = false;
                    var oldAnimated = IsAnimated;
                    IsAnimated = false;
                    try
                    {
                        RefocusView();
                    }
                    finally
                    {
                        IsAnimated = oldAnimated;
                    }
                }
            }

            base.OnRender(drawingContext);
        }

        private static void RefocusView(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var zoombox = o as Zoombox;
            zoombox.UpdateView(zoombox.CurrentView, true, false, zoombox.ViewStackIndex);
        }

        private void AttachToVisualTree()
        {
            // detach from the old tree
            DetachFromVisualTree();

            // create the drag adorner for selection operations
            _dragAdorner = new DragAdorner(this);

            // check the template for a SelectionBrush resource
            if (Template.Resources.Contains("SelectionBrush"))
                _dragAdorner.Brush = Template.Resources["SelectionBrush"] as Brush;

            // check the template for a SelectionPen resource
            if (Template.Resources.Contains("SelectionPen"))
                _dragAdorner.Pen = Template.Resources["SelectionPen"] as Pen;

            // check the template for key bindings
            if (Template.Resources.Contains("InputBindings"))
            {
                var inputBindings = Template.Resources["InputBindings"] as InputBindingCollection;
                if (inputBindings is not null) InputBindings.AddRange(inputBindings);
            }

            // locate the content presenter
            _contentPresenter =
                VisualTreeHelperEx.FindDescendantByType(this, typeof(ContentPresenter)) as ContentPresenter;
            if (_contentPresenter is null)
                throw new InvalidTemplateException(ErrorMessages.GetMessage("ZoomboxTemplateNeedsContent"));

            // check the template for an AdornerDecorator
            AdornerLayer al = null;
            var ad = VisualTreeHelperEx.FindDescendantByType(this, typeof(AdornerDecorator)) as AdornerDecorator;
            if (ad is not null)
                al = ad.AdornerLayer;
            else
                // look for an inherited adorner layer
                try
                {
                    al = AdornerLayer.GetAdornerLayer(this);
                }
                catch (Exception)
                {
                }

            // add the drag adorner to the adorner layer
            if (al is not null) al.Add(_dragAdorner);

            // TODO: Why is it necessary to walk the visual tree the first time through?
            // If we don't do the following, the content is not laid out correctly (centered) initially.
            VisualTreeHelperEx.FindDescendantWithPropertyValue(this, ButtonBase.IsPressedProperty, true);

            // set a reference to the ViewFinder element, if present
            SetValue(ViewFinderPropertyKey, Template.FindName("ViewFinder", this) as FrameworkElement);

            var resolveViewFinderDisplayEventArgs = new ResolveViewFinderDisplayEventArgs();

            RaiseEvent(resolveViewFinderDisplayEventArgs);

            if (resolveViewFinderDisplayEventArgs.ResolvedViewFinderDisplay is not null)
                _viewFinderDisplay = resolveViewFinderDisplayEventArgs.ResolvedViewFinderDisplay;
            else
                // locate the view finder display panel
                _viewFinderDisplay =
                    VisualTreeHelperEx.FindDescendantByType(this, typeof(ZoomboxViewFinderDisplay)) as
                        ZoomboxViewFinderDisplay;

            // if a ViewFinder was specified but no display panel is present, throw an exception
            if (ViewFinder is not null && _viewFinderDisplay is null)
                throw new InvalidTemplateException(ErrorMessages.GetMessage("ZoomboxHasViewFinderButNotDisplay"));

            // set up the VisualBrush and adorner for the display panel
            if (_viewFinderDisplay is not null)
            {
                // create VisualBrush for the view finder display panel
                CreateVisualBrushForViewFinder(_content);

                // hook up event handlers for dragging and resizing the viewport
                _viewFinderDisplay.MouseMove += ViewFinderDisplayMouseMove;
                _viewFinderDisplay.MouseLeftButtonDown += ViewFinderDisplayBeginCapture;
                _viewFinderDisplay.MouseLeftButtonUp += ViewFinderDisplayEndCapture;

                // bind the ViewportRect property of the display to the Viewport of the Zoombox
                var binding = new Binding("Viewport");
                binding.Mode = BindingMode.OneWay;
                binding.Converter = new ViewFinderSelectionConverter(this);
                binding.Source = this;
                _viewFinderDisplay.SetBinding(ZoomboxViewFinderDisplay.ViewportRectProperty, binding);
            }

            // set up event handler to run once the content presenter has been arranged
            _contentPresenter.LayoutUpdated += ContentPresenterFirstArranged;
        }

        private void CreateVisualBrushForViewFinder(Visual visual)
        {
            _viewFinderDisplay.VisualBrush = new VisualBrush(visual);
            _viewFinderDisplay.VisualBrush.Stretch = Stretch.Uniform;
            _viewFinderDisplay.VisualBrush.AlignmentX = AlignmentX.Left;
            _viewFinderDisplay.VisualBrush.AlignmentY = AlignmentY.Top;
        }

        private void ContentPresenterFirstArranged(object sender, EventArgs e)
        {
            // remove the handler
            _contentPresenter.LayoutUpdated -= ContentPresenterFirstArranged;

            // it's now safe to update the view
            HasArrangedContentPresenter = true;
            InvalidateVisual();

            // temporarily disable animations
            var oldAnimated = IsAnimated;
            IsAnimated = false;
            try
            {
                // set the initial view
                var scale = Scale;
                var position = Position;

                // if there are items in the ViewStack and a ViewStackIndex is set, use it
                if (EffectiveViewStackMode != ZoomboxViewStackMode.Disabled)
                {
                    var isInitialViewSet = false;
                    if (ViewStack.Count > 0)
                    {
                        if (ViewStackIndex >= 0)
                        {
                            if (ViewStackIndex > ViewStack.Count - 1)
                                ViewStackIndex = ViewStack.Count - 1;
                            else
                                UpdateView(ViewStack[ViewStackIndex], false, false, ViewStackIndex);
                        }
                        else if (EffectiveViewStackMode != ZoomboxViewStackMode.Auto)
                        {
                            // if this is not an auto-stack, then ensure the index is set to a valid value
                            if (ViewStackIndex < 0) ViewStackIndex = 0;
                        }

                        // if a ViewStackIndex has been set, apply the scale and position values, iff different than the default values
                        if (ViewStackIndex >= 0)
                        {
                            isInitialViewSet = true;
                            if (!DoubleHelper.IsNaN(scale) || !PointHelper.IsEmpty(position))
                                UpdateView(new ZoomboxView(scale, position), false, false);
                        }
                    }

                    if (!isInitialViewSet)
                    {
                        // set initial view according to the scale and position values and push it on the stack, as necessary
                        var initialView = new ZoomboxView(DoubleHelper.IsNaN(Scale) ? 1.0 : Scale,
                            PointHelper.IsEmpty(position) ? new Point() : position);

                        if (EffectiveViewStackMode == ZoomboxViewStackMode.Auto)
                        {
                            ViewStack.PushView(initialView);
                            ViewStackIndex = 0;
                        }
                        else
                        {
                            UpdateView(initialView, false, false);
                        }
                    }
                }
                else
                {
                    // just set initial view according to the scale and position values
                    var initialView = new ZoomboxView(DoubleHelper.IsNaN(Scale) ? 1.0 : Scale, position);
                    UpdateView(initialView, false, false);
                }
            }
            finally
            {
                IsAnimated = oldAnimated;
            }
        }

        private void DetachFromVisualTree()
        {
            // remove the drag adorner
            if (_dragAdorner is not null)
                AdornerLayer.GetAdornerLayer(this).Remove(_dragAdorner);

            // remove the layout updated handler, if present
            if (_contentPresenter is not null) _contentPresenter.LayoutUpdated -= ContentPresenterFirstArranged;

            // remove the view finder display panel's visual brush and adorner
            if (_viewFinderDisplay is not null) _viewFinderDisplay = null;

            // set object references to null
            _contentPresenter = null;
        }

        private void DragDisplayViewport(DragDeltaEventArgs e, bool end)
        {
            // get the scale of the view finder display panel, the selection rect, and the VisualBrush rect
            var scale = _viewFinderDisplay.Scale;
            var viewportRect = _viewFinderDisplay.ViewportRect;
            var vbRect = _viewFinderDisplay.ContentBounds;

            // if the entire content is visible, do nothing
            if (viewportRect.Contains(vbRect))
                return;

            // ensure that we stay within the bounds of the VisualBrush
            var dx = e.HorizontalChange;
            var dy = e.VerticalChange;

            // check left boundary
            if (viewportRect.Left < vbRect.Left)
                dx = Math.Max(0, dx);
            else if (viewportRect.Left + dx < vbRect.Left) dx = vbRect.Left - viewportRect.Left;

            // check right boundary
            if (viewportRect.Right > vbRect.Right)
                dx = Math.Min(0, dx);
            else if (viewportRect.Right + dx > vbRect.Left + vbRect.Width)
                dx = vbRect.Left + vbRect.Width - viewportRect.Right;

            // check top boundary
            if (viewportRect.Top < vbRect.Top)
                dy = Math.Max(0, dy);
            else if (viewportRect.Top + dy < vbRect.Top) dy = vbRect.Top - viewportRect.Top;

            // check bottom boundary
            if (viewportRect.Bottom > vbRect.Bottom)
                dy = Math.Min(0, dy);
            else if (viewportRect.Bottom + dy > vbRect.Top + vbRect.Height)
                dy = vbRect.Top + vbRect.Height - viewportRect.Bottom;

            // call the main OnDrag handler that is used when dragging the content directly
            OnDrag(new DragDeltaEventArgs(-dx / scale / _viewboxFactor, -dy / scale / _viewboxFactor), end);

            // for a drag operation, update the origin with each delta
            _originPoint = _originPoint + new Vector(dx, dy);
        }

        private void InitCommands()
        {
            var binding = new CommandBinding(Back, GoBack, CanGoBack);
            CommandBindings.Add(binding);

            binding = new CommandBinding(Center, CenterContent);
            CommandBindings.Add(binding);

            binding = new CommandBinding(Fill, FillToBounds);
            CommandBindings.Add(binding);

            binding = new CommandBinding(Fit, FitToBounds);
            CommandBindings.Add(binding);

            binding = new CommandBinding(Forward, GoForward, CanGoForward);
            CommandBindings.Add(binding);

            binding = new CommandBinding(Home, GoHome, CanGoHome);
            CommandBindings.Add(binding);

            binding = new CommandBinding(PanDown, PanDownExecuted);
            CommandBindings.Add(binding);

            binding = new CommandBinding(PanLeft, PanLeftExecuted);
            CommandBindings.Add(binding);

            binding = new CommandBinding(PanRight, PanRightExecuted);
            CommandBindings.Add(binding);

            binding = new CommandBinding(PanUp, PanUpExecuted);
            CommandBindings.Add(binding);

            binding = new CommandBinding(Refocus, RefocusView, CanRefocusView);
            CommandBindings.Add(binding);

            binding = new CommandBinding(ZoomIn, ZoomInExecuted);
            CommandBindings.Add(binding);

            binding = new CommandBinding(ZoomOut, ZoomOutExecuted);
            CommandBindings.Add(binding);
        }

        private void MonitorInput()
        {
            // cannot pre-process input in partial trust
            if (HasUIPermission) PreProcessInput();
        }

        private void OnContentSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateViewFinderDisplayContentBounds();

            if (HasArrangedContentPresenter)
            {
                if (HasRenderedFirstView)
                {
                    UpdateView(CurrentView, true, false, CurrentViewIndex);
                }
                else
                {
                    // if the content size changes after the content presenter has been arranged,
                    // but before the first view is rendered, invalidate the render so we can refocus 
                    // the view on the first render
                    RefocusViewOnFirstRender = true;
                    InvalidateVisual();
                }
            }
        }

        private void OnDrag(DragDeltaEventArgs e, bool end)
        {
            var relativePosition = _relativePosition;
            var scale = Scale;
            var newPosition = relativePosition + ContentOffset * scale +
                              new Vector(e.HorizontalChange * scale, e.VerticalChange * scale);

            // update the transform
            UpdateView(new ZoomboxView(scale, newPosition), false, end);
        }

        private void OnLayoutUpdated(object sender, EventArgs e)
        {
            UpdateViewport();
        }

        private void OnSelectRegion(DragDeltaEventArgs e, bool end)
        {
            // draw adorner rect
            if (end)
            {
                _dragAdorner.Rect = Rect.Empty;

                if (_trueContent is not null)
                {
                    // get the selected region (in the content's coordinate space) based on the adorner's last position and size
                    var selection =
                        new Rect(
                            TranslatePoint(_dragAdorner.LastPosition, _trueContent),
                            TranslatePoint(
                                _dragAdorner.LastPosition + new Vector(_dragAdorner.LastSize.Width,
                                    _dragAdorner.LastSize.Height), _trueContent));

                    // zoom to the selection
                    ZoomTo(selection);
                }
            }
            else
            {
                _dragAdorner.Rect =
                    Rect.Intersect(
                        new Rect(
                            _originPoint,
                            new Vector(e.HorizontalChange, e.VerticalChange)),
                        new Rect(
                            new Point(0, 0),
                            new Point(RenderSize.Width, RenderSize.Height)));
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!HasArrangedContentPresenter)
                return;

            // when the size is changing, the viewbox factor must be updated before updating the view
            UpdateViewboxFactor();

            var oldIsAnimated = IsAnimated;
            IsAnimated = false;
            try
            {
                UpdateView(CurrentView, false, false, ViewStackIndex);
            }
            finally
            {
                IsAnimated = oldIsAnimated;
            }
        }

        private void PreProcessInput()
        {
            // if mouse is over the Zoombox element or if it has keyboard focus, pre-process input 
            // to update the KeyModifier trigger properties (e.g., DragModifiersAreActive)
            if (IsMouseOver || IsKeyboardFocusWithin)
            {
                if (!IsMonitoringInput)
                {
                    IsMonitoringInput = true;
                    InputManager.Current.PreNotifyInput += PreProcessInput;
                    UpdateKeyModifierTriggerProperties();
                }
            }
            else
            {
                if (IsMonitoringInput)
                {
                    IsMonitoringInput = false;
                    InputManager.Current.PreNotifyInput -= PreProcessInput;

                    SetAreDragModifiersActive(false);
                    SetAreRelativeZoomModifiersActive(false);
                    SetAreZoomModifiersActive(false);
                    SetAreZoomToSelectionModifiersActive(false);
                }
            }
        }

        private void PreProcessInput(object sender, NotifyInputEventArgs e)
        {
            if (e.StagingItem.Input is KeyEventArgs) UpdateKeyModifierTriggerProperties();
        }

        private void ProcessMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (ZoomToSelectionModifiers.AreActive)
            {
                SetIsDraggingContent(false);
                SetIsSelectingRegion(true);
            }
            else if (DragModifiers.AreActive)
            {
                SetIsSelectingRegion(false);
                SetIsDraggingContent(true);
            }
            else
            {
                SetIsSelectingRegion(false);
                SetIsDraggingContent(false);
            }

            // if nothing to do, just return
            if (!IsSelectingRegion && !IsDraggingContent)
                return;

            // set the origin point and capture the mouse
            _originPoint = e.GetPosition(this);
            _contentPresenter.CaptureMouse();
            e.Handled = true;
            if (IsDraggingContent)
                // execute the Drag operation
                OnDrag(new DragDeltaEventArgs(0, 0), false);
            else if (IsSelectingRegion) OnSelectRegion(new DragDeltaEventArgs(0, 0), false);
        }

        private void ProcessMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (!IsDraggingContent && !IsSelectingRegion)
                return;

            var endDrag = IsDraggingContent;

            SetIsDraggingContent(false);
            SetIsSelectingRegion(false);

            _originPoint = new Point();
            _contentPresenter.ReleaseMouseCapture();
            e.Handled = true;

            if (endDrag)
                OnDrag(new DragDeltaEventArgs(0, 0), true);
            else
                OnSelectRegion(new DragDeltaEventArgs(0, 0), true);
        }

        private void ProcessMouseMove(MouseEventArgs e)
        {
            if (e.MouseDevice.LeftButton != MouseButtonState.Pressed)
                return;

            if (!IsDraggingContent && !IsSelectingRegion)
                return;

            var pos = e.GetPosition(this);
            e.Handled = true;

            if (IsDraggingContent)
            {
                var delta = (pos - _originPoint) / Scale;
                OnDrag(new DragDeltaEventArgs(delta.X, delta.Y), false);
                _originPoint = pos;
            }
            else if (IsSelectingRegion)
            {
                var delta = pos - _originPoint;
                OnSelectRegion(new DragDeltaEventArgs(delta.X, delta.Y), false);
            }
        }

        private void ProcessMouseWheelZoom(MouseWheelEventArgs e)
        {
            if (_content is null)
                return;

            // check modifiers to see if there's work to do
            var doZoom = ZoomModifiers.AreActive;
            var doRelativeZoom = RelativeZoomModifiers.AreActive;

            // can't do both, so assume relative zoom
            if (doZoom && doRelativeZoom) doZoom = false;

            if (!(doZoom || doRelativeZoom))
                return;

            e.Handled = true;
            var percentage = e.Delta / MOUSE_WHEEL_DELTA * ZoomPercentage / 100;

            // Are we doing a zoom relative to the current mouse position?
            if (doRelativeZoom)
                Zoom(percentage, Mouse.GetPosition(_content));
            else
                Zoom(percentage);
        }

        private void ProcessNavigationButton(RoutedEventArgs e)
        {
            if (e is MouseButtonEventArgs)
            {
                var mbea = e as MouseButtonEventArgs;
                if (mbea.ChangedButton == MouseButton.XButton1
                    || mbea.ChangedButton == MouseButton.XButton2)
                {
                    if (mbea.ChangedButton == MouseButton.XButton2)
                        GoForward();
                    else
                        GoBack();
                    mbea.Handled = true;
                }
            }
            else if (e is KeyEventArgs)
            {
                var kea = e as KeyEventArgs;
                if (kea.Key == Key.Back || kea.Key == Key.BrowserBack || kea.Key == Key.BrowserForward)
                {
                    if (kea.Key == Key.BrowserForward)
                        GoForward();
                    else
                        GoBack();
                    kea.Handled = true;
                }
            }
        }

        private void ResizeDisplayViewport(DragDeltaEventArgs e, ResizeEdge relativeTo)
        {
            // get the existing viewport rect and scale
            var viewportRect = _viewFinderDisplay.ViewportRect;
            var scale = _viewFinderDisplay.Scale;

            // ensure that we stay within the bounds of the VisualBrush
            var x = Math.Max(_resizeViewportBounds.Left,
                Math.Min(_resizeDraggingPoint.X + e.HorizontalChange, _resizeViewportBounds.Right));
            var y = Math.Max(_resizeViewportBounds.Top,
                Math.Min(_resizeDraggingPoint.Y + e.VerticalChange, _resizeViewportBounds.Bottom));

            // get the selected region in the coordinate space of the content
            var anchorPoint = new Point(_resizeAnchorPoint.X / scale, _resizeAnchorPoint.Y / scale);
            var newRegionVector = new Vector((x - _resizeAnchorPoint.X) / scale / _viewboxFactor,
                (y - _resizeAnchorPoint.Y) / scale / _viewboxFactor);
            var region = new Rect(anchorPoint, newRegionVector);

            // now translate the region from the coordinate space of the content 
            // to the coordinate space of the content presenter
            region =
                new Rect(
                    _content.TranslatePoint(region.TopLeft, _contentPresenter),
                    _content.TranslatePoint(region.BottomRight, _contentPresenter));

            // calculate actual scale value
            var aspectX = RenderSize.Width / region.Width;
            var aspectY = RenderSize.Height / region.Height;
            scale = aspectX < aspectY ? aspectX : aspectY;

            // scale relative to the anchor point
            ZoomTo(scale, anchorPoint, false, false);
        }

        private void UpdateKeyModifierTriggerProperties()
        {
            SetAreDragModifiersActive(DragModifiers.AreActive);
            SetAreRelativeZoomModifiersActive(RelativeZoomModifiers.AreActive);
            SetAreZoomModifiersActive(ZoomModifiers.AreActive);
            SetAreZoomToSelectionModifiersActive(ZoomToSelectionModifiers.AreActive);
        }

        private void UpdateView(ZoomboxView view, bool allowAnimation, bool allowStackAddition)
        {
            UpdateView(view, allowAnimation, allowStackAddition, -1);
        }

        private void UpdateView(ZoomboxView view, bool allowAnimation, bool allowStackAddition, int stackIndex)
        {
            if (_contentPresenter is null || _content is null || !HasArrangedContentPresenter)
                return;

            // if an absolute view is being used and only a Scale value has been specified,
            // use the ZoomTo() function to perform a relative zoom
            if (view.ViewKind == ZoomboxViewKind.Absolute && PointHelper.IsEmpty(view.Position))
            {
                ZoomTo(view.Scale, allowStackAddition);
                return;
            }

            // disallow reentrancy
            if (!IsUpdatingView)
            {
                IsUpdatingView = true;
                try
                {
                    // determine the new scale and position
                    var newRelativeScale = _viewboxFactor;
                    var newRelativePosition = new Point();
                    var region = Rect.Empty;
                    switch (view.ViewKind)
                    {
                        case ZoomboxViewKind.Empty:
                            break;

                        case ZoomboxViewKind.Absolute:
                            newRelativeScale = DoubleHelper.IsNaN(view.Scale) ? _relativeScale : view.Scale;
                            newRelativePosition = PointHelper.IsEmpty(view.Position)
                                ? _relativePosition
                                : new Point(view.Position.X, view.Position.Y) - ContentOffset * newRelativeScale;
                            break;

                        case ZoomboxViewKind.Region:
                            region = view.Region;
                            break;

                        case ZoomboxViewKind.Center:
                        {
                            // get the current ContentRect in the coordinate space of the Zoombox control
                            var currentContentRect =
                                new Rect(
                                    _content.TranslatePoint(ContentRect.TopLeft, this),
                                    _content.TranslatePoint(ContentRect.BottomRight, this));

                            // inflate (or deflate) the rect by the appropriate amounts in the x & y directions
                            region = Rect.Inflate(currentContentRect,
                                (RenderSize.Width / _viewboxFactor - currentContentRect.Width) / 2,
                                (RenderSize.Height / _viewboxFactor - currentContentRect.Height) / 2);

                            // now translate the centered rect back to the coordinate space of the content
                            region = new Rect(TranslatePoint(region.TopLeft, _content),
                                TranslatePoint(region.BottomRight, _content));
                        }
                            break;

                        case ZoomboxViewKind.Fit:
                            region = ContentRect;
                            break;

                        case ZoomboxViewKind.Fill:
                            region = CalculateFillRect();
                            break;
                    }

                    if (view.ViewKind != ZoomboxViewKind.Empty)
                    {
                        if (!region.IsEmpty)
                        {
                            // ZOOMING TO A REGION
                            CalculatePositionAndScale(region, ref newRelativePosition, ref newRelativeScale);
                        }
                        else if (view != ZoomboxView.Empty)
                        {
                            // USING ABSOLUTE POSITION AND SCALE VALUES

                            // ensure that the scale value falls within the valid range
                            if (newRelativeScale > MaxScale)
                                newRelativeScale = MaxScale;
                            else if (newRelativeScale < MinScale) newRelativeScale = MinScale;
                        }

                        var currentScale = _relativeScale;
                        var currentX = _relativePosition.X;
                        var currentY = _relativePosition.Y;

                        ScaleTransform st = null;
                        TranslateTransform tt = null;
                        TransformGroup tg = null;

                        if (_contentPresenter.RenderTransform != Transform.Identity)
                        {
                            tg = _contentPresenter.RenderTransform as TransformGroup;
                            st = tg.Children[0] as ScaleTransform;
                            tt = tg.Children[1] as TranslateTransform;
                            currentScale = st.ScaleX;
                            currentX = tt.X;
                            currentY = tt.Y;
                        }

                        if (KeepContentInBounds)
                        {
                            var boundsRect = new Rect(new Size(ContentRect.Width * newRelativeScale,
                                ContentRect.Height * newRelativeScale));

                            // Calc viewport rect (should be inside bounds content rect)
                            var viewportPosition = new Point(-newRelativePosition.X, -newRelativePosition.Y);
                            var viewportRect = new Rect(viewportPosition, _contentPresenter.RenderSize);

                            if (DoubleHelper.AreVirtuallyEqual(_relativeScale, newRelativeScale)
                            ) // we are positioning the content, not scaling
                            {
                                // Handle the width and height seperately since the content extent 
                                // could be contained only partially in the viewport.  Also if the viewport is only 
                                // partially contained within the content extent.

                                //
                                // Content extent width is greater than the viewport's width (Zoomed in).  Make sure we restrict
                                // the viewport X inside the content.
                                //
                                if (IsGreaterThanOrClose(boundsRect.Width, viewportRect.Width))
                                {
                                    if (boundsRect.Right < viewportRect.Right)
                                        newRelativePosition.X = -(boundsRect.Width - viewportRect.Width);

                                    if (boundsRect.Left > viewportRect.Left) newRelativePosition.X = 0;
                                }
                                //
                                // Viewport width is greater than the content extent's width (Zoomed out).  Make sure we restrict
                                // the content X inside the viewport.
                                //
                                else if (IsGreaterThanOrClose(viewportRect.Width, boundsRect.Width))
                                {
                                    if (viewportRect.Right < boundsRect.Right)
                                        newRelativePosition.X = viewportRect.Width - boundsRect.Width;

                                    if (viewportRect.Left > boundsRect.Left) newRelativePosition.X = 0;
                                }

                                //
                                // Content extent height is greater than the viewport's height (Zoomed in).  Make sure we restrict
                                // the viewport Y inside the content.
                                //
                                if (IsGreaterThanOrClose(boundsRect.Height, viewportRect.Height))
                                {
                                    if (boundsRect.Bottom < viewportRect.Bottom)
                                        newRelativePosition.Y = -(boundsRect.Height - viewportRect.Height);

                                    if (boundsRect.Top > viewportRect.Top) newRelativePosition.Y = 0;
                                }
                                //
                                // Viewport height is greater than the content extent's height (Zoomed out).  Make sure we restrict
                                // the content Y inside the viewport.
                                //
                                else if (IsGreaterThanOrClose(viewportRect.Height, boundsRect.Height))
                                {
                                    if (viewportRect.Bottom < boundsRect.Bottom)
                                        newRelativePosition.Y = viewportRect.Height - boundsRect.Height;

                                    if (viewportRect.Top > boundsRect.Top) newRelativePosition.Y = 0;
                                }
                            }
                        }

                        st = new ScaleTransform(newRelativeScale / _viewboxFactor, newRelativeScale / _viewboxFactor);
                        tt = new TranslateTransform(newRelativePosition.X, newRelativePosition.Y);
                        tg = new TransformGroup();
                        tg.Children.Add(st);
                        tg.Children.Add(tt);

                        _contentPresenter.RenderTransform = tg;

                        if (allowAnimation && IsAnimated)
                        {
                            var daScale = new DoubleAnimation(currentScale, newRelativeScale / _viewboxFactor,
                                AnimationDuration);
                            daScale.AccelerationRatio = AnimationAccelerationRatio;
                            daScale.DecelerationRatio = AnimationDecelerationRatio;

                            var daTranslateX = new DoubleAnimation(currentX, newRelativePosition.X, AnimationDuration);
                            daTranslateX.AccelerationRatio = AnimationAccelerationRatio;
                            daTranslateX.DecelerationRatio = AnimationDecelerationRatio;

                            var daTranslateY = new DoubleAnimation(currentY, newRelativePosition.Y, AnimationDuration);
                            daTranslateY.AccelerationRatio = AnimationAccelerationRatio;
                            daTranslateY.DecelerationRatio = AnimationDecelerationRatio;
                            daTranslateY.CurrentTimeInvalidated += UpdateViewport;
                            daTranslateY.CurrentStateInvalidated += ZoomAnimationCompleted;

                            // raise animation beginning event before beginning the animations
                            RaiseEvent(new RoutedEventArgs(AnimationBeginningEvent, this));

                            st.BeginAnimation(ScaleTransform.ScaleXProperty, daScale);
                            st.BeginAnimation(ScaleTransform.ScaleYProperty, daScale);
                            tt.BeginAnimation(TranslateTransform.XProperty, daTranslateX);
                            tt.BeginAnimation(TranslateTransform.YProperty, daTranslateY);
                        }

                        // maintain the relative scale and position for dragging and animating purposes
                        _relativePosition = newRelativePosition;
                        _relativeScale = newRelativeScale;

                        // update the Scale and Position properties to keep them in sync with the current view
                        Scale = newRelativeScale;
                        _basePosition = newRelativePosition + ContentOffset * newRelativeScale;
                        UpdateViewport();
                    }

                    // add the current view to the view stack
                    if (EffectiveViewStackMode == ZoomboxViewStackMode.Auto && allowStackAddition)
                    {
                        // if the last view was pushed on the stack within the last 300 milliseconds, discard it
                        // since proximally close views are probably the result of a mouse wheel scroll
                        if (ViewStack.Count > 1
                            && Math.Abs(DateTime.Now.Ticks - _lastStackAddition.Ticks) <
                            TimeSpan.FromMilliseconds(300).Ticks)
                        {
                            ViewStack.RemoveAt(ViewStack.Count - 1);
                            _lastStackAddition = DateTime.Now - TimeSpan.FromMilliseconds(300);
                        }

                        // if the current view is the same as the last view, do nothing
                        if (ViewStack.Count > 0 && view == ViewStack.SelectedView)
                        {
                            // do nothing
                        }
                        else
                        {
                            // otherwise, push the current view on stack
                            ViewStack.PushView(view);
                            ViewStackIndex++;
                            stackIndex = ViewStackIndex;

                            // update the timestamp for the last stack addition
                            _lastStackAddition = DateTime.Now;
                        }
                    }

                    // update the stack indices used by CurrentViewChanged event
                    _lastViewIndex = CurrentViewIndex;
                    SetCurrentViewIndex(stackIndex);

                    // set the new view parameters
                    // NOTE: this is the only place where the CurrentView member should be set
                    SetCurrentView(view);
                }
                finally
                {
                    IsUpdatingView = false;
                }
            }
        }

        private bool IsGreaterThanOrClose(double value1, double value2)
        {
            return value1 <= value2 ? DoubleHelper.AreVirtuallyEqual(value1, value2) : true;
        }

        private Rect CalculateFillRect()
        {
            // determine the x-y ratio of the current Viewport
            var xyRatio = RenderSize.Width / RenderSize.Height;

            // now find the maximal rect within the ContentRect that has the same ratio
            double x = 0;
            double y = 0;
            var width = ContentRect.Width;
            var height = ContentRect.Height;
            if (xyRatio > width / height)
            {
                height = width / xyRatio;
                y = (ContentRect.Height - height) / 2;
            }
            else
            {
                width = height * xyRatio;
                x = (ContentRect.Width - width) / 2;
            }

            return new Rect(x, y, width, height);
        }

        private void CalculatePositionAndScale(Rect region, ref Point newRelativePosition, ref double newRelativeScale)
        {
            // if there is nothing to scale, just return
            // if the region has no area, just return
            if (region.Width == 0 || region.Height == 0)
                return;

            // verify that the selected region intersects with the content, which prevents 
            // the scale operation from zooming the content out of the current view
            if (!ContentRect.IntersectsWith(region))
                return;

            // translate the region from the coordinate space of the content 
            // to the coordinate space of the content presenter
            region =
                new Rect(
                    _content.TranslatePoint(region.TopLeft, _contentPresenter),
                    _content.TranslatePoint(region.BottomRight, _contentPresenter));

            // calculate actual zoom, which must fit the entire selection 
            // while maintaining a 1:1 ratio
            var aspectX = RenderSize.Width / region.Width;
            var aspectY = RenderSize.Height / region.Height;
            newRelativeScale = aspectX < aspectY ? aspectX : aspectY;

            // ensure that the scale value falls within the valid range
            if (newRelativeScale > MaxScale)
                newRelativeScale = MaxScale;
            else if (newRelativeScale < MinScale) newRelativeScale = MinScale;

            // determine the new content position for this zoom operation based 
            // on HorizontalContentAlignment and VerticalContentAlignment
            double horizontalOffset = 0;
            double verticalOffset = 0;
            switch (HorizontalContentAlignment)
            {
                case HorizontalAlignment.Center:
                case HorizontalAlignment.Stretch:
                    horizontalOffset = (RenderSize.Width - region.Width * newRelativeScale) / 2;
                    break;

                case HorizontalAlignment.Right:
                    horizontalOffset = RenderSize.Width - region.Width * newRelativeScale;
                    break;
            }

            switch (VerticalContentAlignment)
            {
                case VerticalAlignment.Center:
                case VerticalAlignment.Stretch:
                    verticalOffset = (RenderSize.Height - region.Height * newRelativeScale) / 2;
                    break;

                case VerticalAlignment.Bottom:
                    verticalOffset = RenderSize.Height - region.Height * newRelativeScale;
                    break;
            }

            newRelativePosition =
                new Point(-region.TopLeft.X * newRelativeScale, -region.TopLeft.Y * newRelativeScale)
                + new Vector(horizontalOffset, verticalOffset);
        }

        private void UpdateViewFinderDisplayContentBounds()
        {
            if (_content is null || _trueContent is null || _viewFinderDisplay is null)
                return;

            UpdateViewboxFactor();

            // ensure the display panel has a size
            var contentSize = _content.RenderSize;
            var viewFinderSize = _viewFinderDisplay.AvailableSize;
            if (viewFinderSize.Width > 0d && DoubleHelper.AreVirtuallyEqual(viewFinderSize.Height, 0d))
                // update height to accomodate width, while keeping a ratio equal to the actual content
                viewFinderSize = new Size(viewFinderSize.Width,
                    contentSize.Height * viewFinderSize.Width / contentSize.Width);
            else if (viewFinderSize.Height > 0d && DoubleHelper.AreVirtuallyEqual(viewFinderSize.Width, 0d))
                // update width to accomodate height, while keeping a ratio equal to the actual content
                viewFinderSize = new Size(contentSize.Width * viewFinderSize.Height / contentSize.Height,
                    viewFinderSize.Width);

            // determine the scale of the view finder display panel
            var aspectX = viewFinderSize.Width / contentSize.Width;
            var aspectY = viewFinderSize.Height / contentSize.Height;
            var scale = aspectX < aspectY ? aspectX : aspectY;

            // determine the rect of the VisualBrush
            var vbWidth = contentSize.Width * scale;
            var vbHeight = contentSize.Height * scale;

            // set the ContentBounds and Scale properties on the view finder display panel
            _viewFinderDisplay.Scale = scale;
            _viewFinderDisplay.ContentBounds = new Rect(new Size(vbWidth, vbHeight));
        }

        private void UpdateViewboxFactor()
        {
            if (_content is null || _trueContent is null)
                return;

            var contentWidth = _content.RenderSize.Width;
            var trueContentWidth = _trueContent.RenderSize.Width;

            if (DoubleHelper.AreVirtuallyEqual(contentWidth, 0d) ||
                DoubleHelper.AreVirtuallyEqual(trueContentWidth, 0d))
                _viewboxFactor = 1d;
            else
                _viewboxFactor = contentWidth / trueContentWidth;
        }

        private void UpdateViewport()
        {
            // if we haven't attached to the visual tree yet or we don't have content, just return
            if (_contentPresenter is null || _trueContent is null)
                return;

            IsUpdatingViewport = true;
            try
            {
                // calculate the current viewport
                var viewport =
                    new Rect(
                        TranslatePoint(new Point(0d, 0d), _trueContent),
                        TranslatePoint(new Point(RenderSize.Width, RenderSize.Height), _trueContent));

                // if the viewport has changed, set the Viewport dependency property
                if (!DoubleHelper.AreVirtuallyEqual(viewport, Viewport)) SetValue(ViewportPropertyKey, viewport);
            }
            finally
            {
                IsUpdatingViewport = false;
            }
        }

        private void UpdateViewport(object sender, EventArgs e)
        {
            UpdateViewport();
        }

        private void ViewFinderDisplayBeginCapture(object sender, MouseButtonEventArgs e)
        {
            const double ARBITRARY_LARGE_VALUE = 10000000000;

            // if we need to acquire capture, the Tag property of the view finder display panel
            // will be a ResizeEdge value.
            if (_viewFinderDisplay.Tag is ResizeEdge)
            {
                // if the Tag is ResizeEdge.None, then its a drag operation; otherwise, its a resize
                if ((ResizeEdge) _viewFinderDisplay.Tag == ResizeEdge.None)
                {
                    IsDraggingViewport = true;
                }
                else
                {
                    IsResizingViewport = true;
                    var direction = new Vector();
                    switch ((ResizeEdge) _viewFinderDisplay.Tag)
                    {
                        case ResizeEdge.TopLeft:
                            _resizeDraggingPoint = _viewFinderDisplay.ViewportRect.TopLeft;
                            _resizeAnchorPoint = _viewFinderDisplay.ViewportRect.BottomRight;
                            direction = new Vector(-1, -1);
                            break;

                        case ResizeEdge.TopRight:
                            _resizeDraggingPoint = _viewFinderDisplay.ViewportRect.TopRight;
                            _resizeAnchorPoint = _viewFinderDisplay.ViewportRect.BottomLeft;
                            direction = new Vector(1, -1);
                            break;

                        case ResizeEdge.BottomLeft:
                            _resizeDraggingPoint = _viewFinderDisplay.ViewportRect.BottomLeft;
                            _resizeAnchorPoint = _viewFinderDisplay.ViewportRect.TopRight;
                            direction = new Vector(-1, 1);
                            break;

                        case ResizeEdge.BottomRight:
                            _resizeDraggingPoint = _viewFinderDisplay.ViewportRect.BottomRight;
                            _resizeAnchorPoint = _viewFinderDisplay.ViewportRect.TopLeft;
                            direction = new Vector(1, 1);
                            break;
                        case ResizeEdge.Left:
                            _resizeDraggingPoint = new Point(_viewFinderDisplay.ViewportRect.Left,
                                _viewFinderDisplay.ViewportRect.Top + _viewFinderDisplay.ViewportRect.Height / 2);
                            _resizeAnchorPoint = new Point(_viewFinderDisplay.ViewportRect.Right,
                                _viewFinderDisplay.ViewportRect.Top + _viewFinderDisplay.ViewportRect.Height / 2);
                            direction = new Vector(-1, 0);
                            break;
                        case ResizeEdge.Top:
                            _resizeDraggingPoint = new Point(
                                _viewFinderDisplay.ViewportRect.Left + _viewFinderDisplay.ViewportRect.Width / 2,
                                _viewFinderDisplay.ViewportRect.Top);
                            _resizeAnchorPoint = new Point(
                                _viewFinderDisplay.ViewportRect.Left + _viewFinderDisplay.ViewportRect.Width / 2,
                                _viewFinderDisplay.ViewportRect.Bottom);
                            direction = new Vector(0, -1);
                            break;
                        case ResizeEdge.Right:
                            _resizeDraggingPoint = new Point(_viewFinderDisplay.ViewportRect.Right,
                                _viewFinderDisplay.ViewportRect.Top + _viewFinderDisplay.ViewportRect.Height / 2);
                            _resizeAnchorPoint = new Point(_viewFinderDisplay.ViewportRect.Left,
                                _viewFinderDisplay.ViewportRect.Top + _viewFinderDisplay.ViewportRect.Height / 2);
                            direction = new Vector(1, 0);
                            break;
                        case ResizeEdge.Bottom:
                            _resizeDraggingPoint = new Point(
                                _viewFinderDisplay.ViewportRect.Left + _viewFinderDisplay.ViewportRect.Width / 2,
                                _viewFinderDisplay.ViewportRect.Bottom);
                            _resizeAnchorPoint = new Point(
                                _viewFinderDisplay.ViewportRect.Left + _viewFinderDisplay.ViewportRect.Width / 2,
                                _viewFinderDisplay.ViewportRect.Top);
                            direction = new Vector(0, 1);
                            break;
                    }

                    var scale = _viewFinderDisplay.Scale;
                    var contentRect = _viewFinderDisplay.ContentBounds;
                    var minVector = new Vector(direction.X * ARBITRARY_LARGE_VALUE,
                        direction.Y * ARBITRARY_LARGE_VALUE);
                    var maxVector = new Vector(direction.X * contentRect.Width / MaxScale,
                        direction.Y * contentRect.Height / MaxScale);
                    _resizeViewportBounds = new Rect(_resizeAnchorPoint + minVector, _resizeAnchorPoint + maxVector);
                }

                // store the origin of the operation and acquire capture
                _originPoint = e.GetPosition(_viewFinderDisplay);
                _viewFinderDisplay.CaptureMouse();
                e.Handled = true;
            }
        }

        private void ViewFinderDisplayEndCapture(object sender, MouseButtonEventArgs e)
        {
            // if a drag or resize is in progress, end it and release capture
            if (IsDraggingViewport || IsResizingViewport)
            {
                // call the DragDisplayViewport method to end the operation
                // and store the current position on the stack
                DragDisplayViewport(new DragDeltaEventArgs(0, 0), true);

                // reset the dragging state variables and release capture
                IsDraggingViewport = false;
                IsResizingViewport = false;
                _originPoint = new Point();
                _viewFinderDisplay.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void ViewFinderDisplayMouseMove(object sender, MouseEventArgs e)
        {
            // if a drag operation is in progress, update the operation
            if (e.MouseDevice.LeftButton == MouseButtonState.Pressed
                && (IsDraggingViewport || IsResizingViewport))
            {
                var pos = e.GetPosition(_viewFinderDisplay);
                var delta = pos - _originPoint;
                if (IsDraggingViewport)
                    DragDisplayViewport(new DragDeltaEventArgs(delta.X, delta.Y), false);
                else
                    ResizeDisplayViewport(new DragDeltaEventArgs(delta.X, delta.Y),
                        (ResizeEdge) _viewFinderDisplay.Tag);
                e.Handled = true;
            }
            else
            {
                // update the cursor based on the nearest corner
                var mousePos = e.GetPosition(_viewFinderDisplay);
                var viewportRect = _viewFinderDisplay.ViewportRect;
                /*
                double cornerDelta = viewportRect.Width * viewportRect.Height > 100 ? 5.0
                    : Math.Sqrt( viewportRect.Width * viewportRect.Height ) / 2;
                */
                // if the mouse is within the Rect and the Rect does not encompass the entire content, set the appropriate cursor
                if (viewportRect.Contains(mousePos)
                    && !DoubleHelper.AreVirtuallyEqual(Rect.Intersect(viewportRect, _viewFinderDisplay.ContentBounds),
                        _viewFinderDisplay.ContentBounds))
                {
                    /*
                  if( PointHelper.DistanceBetween( mousePos, viewportRect.TopLeft ) < cornerDelta )
                  {
                    _viewFinderDisplay.Tag = ResizeEdge.TopLeft;
                    _viewFinderDisplay.Cursor = Cursors.SizeNWSE;
                  }
                  else if( PointHelper.DistanceBetween( mousePos, viewportRect.BottomRight ) < cornerDelta )
                  {
                    _viewFinderDisplay.Tag = ResizeEdge.BottomRight;
                    _viewFinderDisplay.Cursor = Cursors.SizeNWSE;
                  }
                  else if( PointHelper.DistanceBetween( mousePos, viewportRect.TopRight ) < cornerDelta )
                  {
                    _viewFinderDisplay.Tag = ResizeEdge.TopRight;
                    _viewFinderDisplay.Cursor = Cursors.SizeNESW;
                  }
                  else if( PointHelper.DistanceBetween( mousePos, viewportRect.BottomLeft ) < cornerDelta )
                  {
                    _viewFinderDisplay.Tag = ResizeEdge.BottomLeft;
                    _viewFinderDisplay.Cursor = Cursors.SizeNESW;
                  }
                  else if( mousePos.X <= viewportRect.Left + cornerDelta )
                  {
                    _viewFinderDisplay.Tag = ResizeEdge.Left;
                    _viewFinderDisplay.Cursor = Cursors.SizeWE;
                  }
                  else if( mousePos.Y <= viewportRect.Top + cornerDelta )
                  {
                    _viewFinderDisplay.Tag = ResizeEdge.Top;
                    _viewFinderDisplay.Cursor = Cursors.SizeNS;
                  }
                  else if( mousePos.X >= viewportRect.Right - cornerDelta )
                  {
                    _viewFinderDisplay.Tag = ResizeEdge.Right;
                    _viewFinderDisplay.Cursor = Cursors.SizeWE;
                  }
                  else if( mousePos.Y >= viewportRect.Bottom - cornerDelta )
                  {
                    _viewFinderDisplay.Tag = ResizeEdge.Bottom;
                    _viewFinderDisplay.Cursor = Cursors.SizeNS;
                  }
                  else */
                    {
                        _viewFinderDisplay.Tag = ResizeEdge.None;
                        _viewFinderDisplay.Cursor = Cursors.SizeAll;
                    }
                }
                else
                {
                    _viewFinderDisplay.Tag = null;
                    _viewFinderDisplay.Cursor = Cursors.Arrow;
                }
            }
        }

        private void ZoomAnimationCompleted(object sender, EventArgs e)
        {
            if ((sender as AnimationClock).CurrentState != ClockState.Active)
            {
                // remove the event handlers
                (sender as AnimationClock).CurrentStateInvalidated -= ZoomAnimationCompleted;
                (sender as AnimationClock).CurrentTimeInvalidated -= UpdateViewport;

                // raise animation completed event
                RaiseEvent(new RoutedEventArgs(AnimationCompletedEvent, this));
            }
        }

        private void ZoomTo(double scale, bool allowStackAddition)
        {
            // if there is nothing to scale, just return
            if (_content is null)
                return;

            // adjust the current scale relative to the zoom origin
            ZoomTo(scale, GetZoomRelativePoint(), true, allowStackAddition);
        }

        private void ZoomTo(double scale, Point relativeTo, bool restrictRelativePointToContent,
            bool allowStackAddition)
        {
            // if there is nothing to scale, just return
            if (_content is null)
                return;

            // if necessary, verify that the relativeTo point falls within the content
            if (restrictRelativePointToContent && !new Rect(_content.RenderSize).Contains(relativeTo))
                return;

            // ensure that the scale value falls within the valid range
            if (scale > MaxScale)
                scale = MaxScale;
            else if (scale < MinScale) scale = MinScale;

            // internally, updates are always relative to the Zoombox control
            var translateFrom = relativeTo;
            if (HasRenderedFirstView)
            {
                // Note that this TranslatePoint approach will not work until the first render occurs
                relativeTo = _content.TranslatePoint(relativeTo, this);

                // adjust translateFrom based on relativeTo
                translateFrom = TranslatePoint(relativeTo, _contentPresenter);
            }
            else
            {
                // prior to the first render, just use the ContentPresenter's transform and do not adjust translateFrom
                if (_contentPresenter.RenderTransform == Transform.Identity)
                    // in order for this approach to work, we must at least make one pass to update a generic view
                    // with Scale = 1.0 and Position = 0,0
                    UpdateView(new ZoomboxView(1, new Point(0, 0)), false, false);

                // now there should be a valid RenderTransform
                relativeTo = _contentPresenter.RenderTransform.Transform(relativeTo);
            }

            // determine the new content position for this zoom operation
            var translateTo = new Point(relativeTo.X - translateFrom.X * scale / _viewboxFactor,
                                  relativeTo.Y - translateFrom.Y * scale / _viewboxFactor)
                              + ContentOffset * scale / _viewboxFactor;
            UpdateView(new ZoomboxView(scale, translateTo), !IsResizingViewport, allowStackAddition);
        }

        private Point GetZoomRelativePoint()
        {
            Point zoomPoint;

            if (ZoomOn == ZoomboxZoomOn.View)
            {
                // Transform the viewport point to the content
                var viewportZoomOrigin = new Point();

                viewportZoomOrigin.X = Viewport.X + Viewport.Width * ZoomOrigin.X;
                viewportZoomOrigin.Y = Viewport.Y + Viewport.Height * ZoomOrigin.Y;

                var contentZoomOrigin = _trueContent.TranslatePoint(viewportZoomOrigin, _content);

                if (contentZoomOrigin.X < 0)
                    contentZoomOrigin.X = 0;
                else if (contentZoomOrigin.X > _content.RenderSize.Width)
                    contentZoomOrigin.X = _content.RenderSize.Width;

                if (contentZoomOrigin.Y < 0)
                    contentZoomOrigin.Y = 0;
                else if (contentZoomOrigin.Y > _content.RenderSize.Height)
                    contentZoomOrigin.Y = _content.RenderSize.Height;

                zoomPoint = contentZoomOrigin;
            }
            else
            {
                zoomPoint = new Point(_content.RenderSize.Width * ZoomOrigin.X,
                    _content.RenderSize.Height * ZoomOrigin.Y);
            }

            return zoomPoint;
        }

        #region OnMouseEnter Methods

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            MonitorInput();

            base.OnMouseEnter(e);
        }

        #endregion

        #region OnMouseLeave Methods

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            MonitorInput();

            base.OnMouseLeave(e);
        }

        #endregion

        #region ViewFinderSelectionConverter Nested Type

        private sealed class ViewFinderSelectionConverter : IValueConverter
        {
            private readonly Zoombox _zoombox;

            public ViewFinderSelectionConverter(Zoombox zoombox)
            {
                _zoombox = zoombox;
            }

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                var viewport = (Rect) value;
                if (viewport.IsEmpty)
                    return viewport;

                // adjust the viewport from the coordinate space of the Content element
                // to the coordinate space of the view finder display panel
                var scale = _zoombox._viewFinderDisplay.Scale * _zoombox._viewboxFactor;
                var result = new Rect(viewport.Left * scale, viewport.Top * scale,
                    viewport.Width * scale, viewport.Height * scale);
                result.Offset(_zoombox._viewFinderDisplay.ContentBounds.Left,
                    _zoombox._viewFinderDisplay.ContentBounds.Top);
                return result;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return null;
            }
        }

        #endregion

        #region DragAdorner Nested Type

        internal sealed class DragAdorner : Adorner
        {
            public static readonly DependencyProperty BrushProperty =
                DependencyProperty.Register("Brush", typeof(Brush), typeof(DragAdorner),
                    new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

            public static readonly DependencyProperty PenProperty =
                DependencyProperty.Register("Pen", typeof(Pen), typeof(DragAdorner),
                    new FrameworkPropertyMetadata(
                        new Pen(new SolidColorBrush(Color.FromArgb(0x7F, 0x3F, 0x3F, 0x3F)), 2d),
                        FrameworkPropertyMetadataOptions.AffectsRender));

            public static readonly DependencyProperty RectProperty =
                DependencyProperty.Register("Rect", typeof(Rect), typeof(DragAdorner),
                    new FrameworkPropertyMetadata(Rect.Empty, FrameworkPropertyMetadataOptions.AffectsRender,
                        OnRectChanged));

            public DragAdorner(UIElement adornedElement)
                : base(adornedElement)
            {
                ClipToBounds = true;
            }

            public Brush Brush
            {
                get => (Brush) GetValue(BrushProperty);
                set => SetValue(BrushProperty, value);
            }

            public Pen Pen
            {
                get => (Pen) GetValue(PenProperty);
                set => SetValue(PenProperty, value);
            }

            public Rect Rect
            {
                get => (Rect) GetValue(RectProperty);
                set => SetValue(RectProperty, value);
            }

            public Point LastPosition { get; private set; }

            public Size LastSize { get; private set; }

            private static void OnRectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                var dragAdorner = (DragAdorner) d;
                var rect = (Rect) e.NewValue;

                // ignore empty values
                if (rect.IsEmpty)
                    return;

                // if the value is not empty, cache the position and size
                dragAdorner.LastPosition = ((Rect) e.NewValue).TopLeft;
                dragAdorner.LastSize = ((Rect) e.NewValue).Size;
            }

            protected override void OnRender(DrawingContext drawingContext)
            {
                drawingContext.DrawRectangle(Brush, Pen, Rect);
            }
        }

        #endregion

        #region CacheBits Nested Type

        private enum CacheBits
        {
            IsUpdatingView = 0x00000001,
            IsUpdatingViewport = 0x00000002,
            IsDraggingViewport = 0x00000004,
            IsResizingViewport = 0x00000008,
            IsMonitoringInput = 0x00000010,
            IsContentWrapped = 0x00000020,
            HasArrangedContentPresenter = 0x00000040,
            HasRenderedFirstView = 0x00000080,
            RefocusViewOnFirstRender = 0x00000100,
            HasUIPermission = 0x00000200
        }

        #endregion

        #region ResizeEdge Nested Type

        private enum ResizeEdge
        {
            None,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight,
            Left,
            Top,
            Right,
            Bottom
        }

        #endregion

        #region Constructors

        static Zoombox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Zoombox), new FrameworkPropertyMetadata(typeof(Zoombox)));
            ClipToBoundsProperty.OverrideMetadata(typeof(Zoombox), new FrameworkPropertyMetadata(true));
            FocusableProperty.OverrideMetadata(typeof(Zoombox), new FrameworkPropertyMetadata(true));
            HorizontalContentAlignmentProperty.OverrideMetadata(typeof(Zoombox),
                new FrameworkPropertyMetadata(HorizontalAlignment.Center, RefocusView));
            VerticalContentAlignmentProperty.OverrideMetadata(typeof(Zoombox),
                new FrameworkPropertyMetadata(VerticalAlignment.Center, RefocusView));
            ContentProperty.OverrideMetadata(typeof(Zoombox), new FrameworkPropertyMetadata(null, CoerceContentValue));
        }

        public Zoombox()
        {
            try
            {
                // WARNING
                //new UIPermission( PermissionState.Unrestricted ).Demand();
                _cacheBits[(int) CacheBits.HasUIPermission] = true;
            }
            catch (SecurityException)
            {
            }

            InitCommands();

            // use the LayoutUpdated event to keep the Viewport in sync
            LayoutUpdated += OnLayoutUpdated;
            AddHandler(SizeChangedEvent, new SizeChangedEventHandler(OnSizeChanged), true);

            CoerceValue(ViewStackModeProperty);
        }

        #endregion

        #region AnimationAccelerationRatio Property

        public static readonly DependencyProperty AnimationAccelerationRatioProperty =
            DependencyProperty.Register("AnimationAccelerationRatio", typeof(double), typeof(Zoombox),
                new FrameworkPropertyMetadata(0d),
                ValidateAccelerationRatio);

        public double AnimationAccelerationRatio
        {
            get => (double) GetValue(AnimationAccelerationRatioProperty);
            set => SetValue(AnimationAccelerationRatioProperty, value);
        }

        private static bool ValidateAccelerationRatio(object value)
        {
            var newValue = (double) value;
            if (newValue < 0 || newValue > 1 || DoubleHelper.IsNaN(newValue))
                throw new ArgumentException(ErrorMessages.GetMessage("AnimationAccelerationRatioOOR"));

            return true;
        }

        #endregion

        #region AnimationDecelerationRatio Property

        public static readonly DependencyProperty AnimationDecelerationRatioProperty =
            DependencyProperty.Register("AnimationDecelerationRatio", typeof(double), typeof(Zoombox),
                new FrameworkPropertyMetadata(0d),
                ValidateDecelerationRatio);

        public double AnimationDecelerationRatio
        {
            get => (double) GetValue(AnimationDecelerationRatioProperty);
            set => SetValue(AnimationDecelerationRatioProperty, value);
        }

        private static bool ValidateDecelerationRatio(object value)
        {
            var newValue = (double) value;
            if (newValue < 0 || newValue > 1 || DoubleHelper.IsNaN(newValue))
                throw new ArgumentException(ErrorMessages.GetMessage("AnimationDecelerationRatioOOR"));

            return true;
        }

        #endregion

        #region AnimationDuration Property

        public static readonly DependencyProperty AnimationDurationProperty =
            DependencyProperty.Register("AnimationDuration", typeof(Duration), typeof(Zoombox),
                new FrameworkPropertyMetadata(new Duration(TimeSpan.FromMilliseconds(300))));

        public Duration AnimationDuration
        {
            get => (Duration) GetValue(AnimationDurationProperty);
            set => SetValue(AnimationDurationProperty, value);
        }

        #endregion

        #region AreDragModifiersActive Property

        private static readonly DependencyPropertyKey AreDragModifiersActivePropertyKey =
            DependencyProperty.RegisterReadOnly("AreDragModifiersActive", typeof(bool), typeof(Zoombox),
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty AreDragModifiersActiveProperty =
            AreDragModifiersActivePropertyKey.DependencyProperty;

        public bool AreDragModifiersActive => (bool) GetValue(AreDragModifiersActiveProperty);

        private void SetAreDragModifiersActive(bool value)
        {
            SetValue(AreDragModifiersActivePropertyKey, value);
        }

        #endregion

        #region AreRelativeZoomModifiersActive Property

        private static readonly DependencyPropertyKey AreRelativeZoomModifiersActivePropertyKey =
            DependencyProperty.RegisterReadOnly("AreRelativeZoomModifiersActive", typeof(bool), typeof(Zoombox),
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty AreRelativeZoomModifiersActiveProperty =
            AreRelativeZoomModifiersActivePropertyKey.DependencyProperty;

        public bool AreRelativeZoomModifiersActive => (bool) GetValue(AreRelativeZoomModifiersActiveProperty);

        private void SetAreRelativeZoomModifiersActive(bool value)
        {
            SetValue(AreRelativeZoomModifiersActivePropertyKey, value);
        }

        #endregion

        #region AreZoomModifiersActive Property

        private static readonly DependencyPropertyKey AreZoomModifiersActivePropertyKey =
            DependencyProperty.RegisterReadOnly("AreZoomModifiersActive", typeof(bool), typeof(Zoombox),
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty AreZoomModifiersActiveProperty =
            AreZoomModifiersActivePropertyKey.DependencyProperty;

        public bool AreZoomModifiersActive => (bool) GetValue(AreZoomModifiersActiveProperty);

        private void SetAreZoomModifiersActive(bool value)
        {
            SetValue(AreZoomModifiersActivePropertyKey, value);
        }

        #endregion

        #region AreZoomToSelectionModifiersActive Property

        private static readonly DependencyPropertyKey AreZoomToSelectionModifiersActivePropertyKey =
            DependencyProperty.RegisterReadOnly("AreZoomToSelectionModifiersActive", typeof(bool), typeof(Zoombox),
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty AreZoomToSelectionModifiersActiveProperty =
            AreZoomToSelectionModifiersActivePropertyKey.DependencyProperty;

        public bool AreZoomToSelectionModifiersActive => (bool) GetValue(AreZoomToSelectionModifiersActiveProperty);

        private void SetAreZoomToSelectionModifiersActive(bool value)
        {
            SetValue(AreZoomToSelectionModifiersActivePropertyKey, value);
        }

        #endregion

        #region AutoWrapContentWithViewbox Property

        public static readonly DependencyProperty AutoWrapContentWithViewboxProperty =
            DependencyProperty.Register("AutoWrapContentWithViewbox", typeof(bool), typeof(Zoombox),
                new FrameworkPropertyMetadata(true,
                    OnAutoWrapContentWithViewboxChanged));

        public bool AutoWrapContentWithViewbox
        {
            get => (bool) GetValue(AutoWrapContentWithViewboxProperty);
            set => SetValue(AutoWrapContentWithViewboxProperty, value);
        }

        private static void OnAutoWrapContentWithViewboxChanged(DependencyObject o,
            DependencyPropertyChangedEventArgs e)
        {
            o.CoerceValue(ContentProperty);
        }

        private static object CoerceContentValue(DependencyObject d, object value)
        {
            return ((Zoombox) d).CoerceContentValue(value);
        }

        private object CoerceContentValue(object value)
        {
            if (value is not null && !(value is UIElement) && !(bool) GetValue(DesignerProperties.IsInDesignModeProperty))
                throw new InvalidContentException(ErrorMessages.GetMessage("ZoomboxContentMustBeUIElement"));

            object oldContent = _content;
            if (value != _trueContent || IsContentWrapped != AutoWrapContentWithViewbox)
            {
                // check whether the content is currently wrapped and needs to be unwrapped
                if (IsContentWrapped && _content is Viewbox && _content != _trueContent)
                {
                    var viewbox = (Viewbox) _content;

                    BindingOperations.ClearAllBindings(viewbox);
                    viewbox.Child = null;

                    RemoveLogicalChild(viewbox);
                }

                // make sure the view finder's visual brush is null
                if (_viewFinderDisplay is not null && _viewFinderDisplay.VisualBrush is not null)
                {
                    _viewFinderDisplay.VisualBrush.Visual = null;
                    _viewFinderDisplay.VisualBrush = null;
                }

                // update the cached content and true content values
                _content = value as UIElement;
                _trueContent = value as UIElement;

                // if necessary, unparent the existing content
                if (_contentPresenter is not null && _contentPresenter.Content is not null) _contentPresenter.Content = null;

                // if necessary, wrap the content
                IsContentWrapped = false;
                if (AutoWrapContentWithViewbox)
                {
                    // create a viewbox and make it the logical child of the Zoombox
                    var viewbox = new Viewbox();
                    AddLogicalChild(viewbox);

                    // now set the new parent to be the viewbox
                    viewbox.Child = value as UIElement;
                    _content = viewbox;
                    viewbox.HorizontalAlignment = HorizontalAlignment.Left;
                    viewbox.VerticalAlignment = VerticalAlignment.Top;
                    IsContentWrapped = true;
                }

                if (_contentPresenter is not null) _contentPresenter.Content = _content;

                if (_viewFinderDisplay is not null) CreateVisualBrushForViewFinder(_content);
                UpdateViewFinderDisplayContentBounds();
            }

            // if the content changes, we need to reset the flags used to first render and arrange the content
            if (oldContent != _content
                && HasArrangedContentPresenter
                && HasRenderedFirstView)
            {
                HasArrangedContentPresenter = false;
                HasRenderedFirstView = false;
                RefocusViewOnFirstRender = true;
                _contentPresenter.LayoutUpdated += ContentPresenterFirstArranged;
            }

            return _content;
        }

        private UIElement _trueContent; //null

        #endregion

        #region CurrentView Property

        private static readonly DependencyPropertyKey CurrentViewPropertyKey =
            DependencyProperty.RegisterReadOnly("CurrentView", typeof(ZoomboxView), typeof(Zoombox),
                new FrameworkPropertyMetadata(ZoomboxView.Empty,
                    OnCurrentViewChanged));

        public static readonly DependencyProperty CurrentViewProperty = CurrentViewPropertyKey.DependencyProperty;

        public ZoomboxView CurrentView => (ZoomboxView) GetValue(CurrentViewProperty);

        private void SetCurrentView(ZoomboxView value)
        {
            SetValue(CurrentViewPropertyKey, value);
        }

        private static void OnCurrentViewChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var zoombox = (Zoombox) o;
            if (!zoombox.IsUpdatingView) zoombox.ZoomTo(zoombox.CurrentView);
            zoombox.RaiseEvent(new ZoomboxViewChangedEventArgs(e.OldValue as ZoomboxView, e.NewValue as ZoomboxView,
                zoombox._lastViewIndex, zoombox.CurrentViewIndex));
        }

        #endregion

        #region CurrentViewIndex Property

        private static readonly DependencyPropertyKey CurrentViewIndexPropertyKey =
            DependencyProperty.RegisterReadOnly("CurrentViewIndex", typeof(int), typeof(Zoombox),
                new FrameworkPropertyMetadata(-1));

        public static readonly DependencyProperty CurrentViewIndexProperty =
            CurrentViewIndexPropertyKey.DependencyProperty;

        public int CurrentViewIndex => (int) GetValue(CurrentViewIndexProperty);

        internal void SetCurrentViewIndex(int value)
        {
            SetValue(CurrentViewIndexPropertyKey, value);
        }

        #endregion

        #region DragModifiers Property

        public static readonly DependencyProperty DragModifiersProperty =
            DependencyProperty.Register("DragModifiers", typeof(KeyModifierCollection), typeof(Zoombox),
                new FrameworkPropertyMetadata(GetDefaultDragModifiers()));

        [TypeConverter(typeof(KeyModifierCollectionConverter))]
        public KeyModifierCollection DragModifiers
        {
            get => (KeyModifierCollection) GetValue(DragModifiersProperty);
            set => SetValue(DragModifiersProperty, value);
        }

        private static KeyModifierCollection GetDefaultDragModifiers()
        {
            var result = new KeyModifierCollection();
            result.Add(KeyModifier.Ctrl);
            result.Add(KeyModifier.Exact);
            return result;
        }

        #endregion

        #region DragOnPreview Property

        public static readonly DependencyProperty DragOnPreviewProperty =
            DependencyProperty.Register("DragOnPreview", typeof(bool), typeof(Zoombox),
                new FrameworkPropertyMetadata(false));

        public bool DragOnPreview
        {
            get => (bool) GetValue(DragOnPreviewProperty);
            set => SetValue(DragOnPreviewProperty, value);
        }

        #endregion

        #region EffectiveViewStackMode Property

        private static readonly DependencyPropertyKey EffectiveViewStackModePropertyKey =
            DependencyProperty.RegisterReadOnly("EffectiveViewStackMode", typeof(ZoomboxViewStackMode), typeof(Zoombox),
                new FrameworkPropertyMetadata(ZoomboxViewStackMode.Auto));

        public static readonly DependencyProperty EffectiveViewStackModeProperty =
            EffectiveViewStackModePropertyKey.DependencyProperty;

        public ZoomboxViewStackMode EffectiveViewStackMode =>
            (ZoomboxViewStackMode) GetValue(EffectiveViewStackModeProperty);

        private void SetEffectiveViewStackMode(ZoomboxViewStackMode value)
        {
            SetValue(EffectiveViewStackModePropertyKey, value);
        }

        #endregion

        #region HasBackStack Property

        private static readonly DependencyPropertyKey HasBackStackPropertyKey =
            DependencyProperty.RegisterReadOnly("HasBackStack", typeof(bool), typeof(Zoombox),
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty HasBackStackProperty = HasBackStackPropertyKey.DependencyProperty;

        public bool HasBackStack => (bool) GetValue(HasBackStackProperty);

        #endregion

        #region HasForwardStack Property

        private static readonly DependencyPropertyKey HasForwardStackPropertyKey =
            DependencyProperty.RegisterReadOnly("HasForwardStack", typeof(bool), typeof(Zoombox),
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty HasForwardStackProperty =
            HasForwardStackPropertyKey.DependencyProperty;

        public bool HasForwardStack => (bool) GetValue(HasForwardStackProperty);

        #endregion

        #region IsAnimated Property

        public static readonly DependencyProperty IsAnimatedProperty =
            DependencyProperty.Register("IsAnimated", typeof(bool), typeof(Zoombox),
                new FrameworkPropertyMetadata(true,
                    null, CoerceIsAnimatedValue));

        public bool IsAnimated
        {
            get => (bool) GetValue(IsAnimatedProperty);
            set => SetValue(IsAnimatedProperty, value);
        }

        private static object CoerceIsAnimatedValue(DependencyObject d, object value)
        {
            var zoombox = (Zoombox) d;
            var result = (bool) value;
            if (!zoombox.IsInitialized) result = false;
            return result;
        }

        #endregion

        #region IsDraggingContent Property

        private static readonly DependencyPropertyKey IsDraggingContentPropertyKey =
            DependencyProperty.RegisterReadOnly("IsDraggingContent", typeof(bool), typeof(Zoombox),
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsDraggingContentProperty =
            IsDraggingContentPropertyKey.DependencyProperty;

        public bool IsDraggingContent => (bool) GetValue(IsDraggingContentProperty);

        private void SetIsDraggingContent(bool value)
        {
            SetValue(IsDraggingContentPropertyKey, value);
        }

        #endregion

        #region IsSelectingRegion Property

        private static readonly DependencyPropertyKey IsSelectingRegionPropertyKey =
            DependencyProperty.RegisterReadOnly("IsSelectingRegion", typeof(bool), typeof(Zoombox),
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsSelectingRegionProperty =
            IsSelectingRegionPropertyKey.DependencyProperty;

        public bool IsSelectingRegion => (bool) GetValue(IsSelectingRegionProperty);

        private void SetIsSelectingRegion(bool value)
        {
            SetValue(IsSelectingRegionPropertyKey, value);
        }

        #endregion

        #region MaxScale Property

        public static readonly DependencyProperty MaxScaleProperty =
            DependencyProperty.Register("MaxScale", typeof(double), typeof(Zoombox),
                new FrameworkPropertyMetadata(100d, FrameworkPropertyMetadataOptions.AffectsMeasure,
                    OnMaxScaleChanged, CoerceMaxScaleValue));

        public double MaxScale
        {
            get => (double) GetValue(MaxScaleProperty);
            set => SetValue(MaxScaleProperty, value);
        }

        private static void OnMaxScaleChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var zoombox = (Zoombox) o;
            zoombox.CoerceValue(MinScaleProperty);
            zoombox.CoerceValue(ScaleProperty);
        }

        private static object CoerceMaxScaleValue(DependencyObject d, object value)
        {
            var zoombox = (Zoombox) d;
            var result = (double) value;
            if (result < zoombox.MinScale) result = zoombox.MinScale;
            return result;
        }

        #endregion

        #region MinScale Property

        public static readonly DependencyProperty MinScaleProperty =
            DependencyProperty.Register("MinScale", typeof(double), typeof(Zoombox),
                new FrameworkPropertyMetadata(0.01d, FrameworkPropertyMetadataOptions.AffectsMeasure,
                    OnMinScaleChanged, CoerceMinScaleValue));

        public double MinScale
        {
            get => (double) GetValue(MinScaleProperty);
            set => SetValue(MinScaleProperty, value);
        }

        private static void OnMinScaleChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var zoombox = (Zoombox) o;
            zoombox.CoerceValue(MinScaleProperty);
            zoombox.CoerceValue(ScaleProperty);
        }

        private static object CoerceMinScaleValue(DependencyObject d, object value)
        {
            var zoombox = (Zoombox) d;
            var result = (double) value;
            if (result > zoombox.MaxScale) result = zoombox.MaxScale;
            return result;
        }

        #endregion

        #region NavigateOnPreview Property

        public static readonly DependencyProperty NavigateOnPreviewProperty =
            DependencyProperty.Register("NavigateOnPreview", typeof(bool), typeof(Zoombox),
                new FrameworkPropertyMetadata(false));

        public bool NavigateOnPreview
        {
            get => (bool) GetValue(NavigateOnPreviewProperty);
            set => SetValue(NavigateOnPreviewProperty, value);
        }

        #endregion

        #region PanDistance Property

        public static readonly DependencyProperty PanDistanceProperty =
            DependencyProperty.Register("PanDistance", typeof(double), typeof(Zoombox),
                new FrameworkPropertyMetadata(5d));

        public double PanDistance
        {
            get => (double) GetValue(PanDistanceProperty);
            set => SetValue(PanDistanceProperty, value);
        }

        #endregion

        #region Position Property

        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.Register("Position", typeof(Point), typeof(Zoombox),
                new FrameworkPropertyMetadata(PointHelper.Empty, OnPositionChanged));

        public Point Position
        {
            get => (Point) GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }

        private static void OnPositionChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var zoombox = (Zoombox) o;
            if (!zoombox.IsUpdatingViewport)
            {
                var newPosition = (Point) e.NewValue;
                var scale = zoombox.Scale;
                if (scale > 0) zoombox.ZoomTo(new Point(-newPosition.X, -newPosition.Y));
            }
        }

        #endregion

        #region RelativeZoomModifiers Property

        public static readonly DependencyProperty RelativeZoomModifiersProperty =
            DependencyProperty.Register("RelativeZoomModifiers", typeof(KeyModifierCollection), typeof(Zoombox),
                new FrameworkPropertyMetadata(GetDefaultRelativeZoomModifiers()));

        [TypeConverter(typeof(KeyModifierCollectionConverter))]
        public KeyModifierCollection RelativeZoomModifiers
        {
            get => (KeyModifierCollection) GetValue(RelativeZoomModifiersProperty);
            set => SetValue(RelativeZoomModifiersProperty, value);
        }

        private static KeyModifierCollection GetDefaultRelativeZoomModifiers()
        {
            var result = new KeyModifierCollection();
            result.Add(KeyModifier.Ctrl);
            result.Add(KeyModifier.Alt);
            result.Add(KeyModifier.Exact);
            return result;
        }

        #endregion

        #region Scale Property

        public static readonly DependencyProperty ScaleProperty =
            DependencyProperty.Register("Scale", typeof(double), typeof(Zoombox),
                new FrameworkPropertyMetadata(double.NaN,
                    OnScaleChanged, CoerceScaleValue));

        public double Scale
        {
            get => (double) GetValue(ScaleProperty);
            set => SetValue(ScaleProperty, value);
        }

        private static void OnScaleChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var zoombox = (Zoombox) o;
            if (!zoombox.IsUpdatingView)
            {
                var newScale = (double) e.NewValue;
                zoombox.ZoomTo(newScale);
            }
        }

        private static object CoerceScaleValue(DependencyObject d, object value)
        {
            var zoombox = (Zoombox) d;
            var result = (double) value;

            if (result < zoombox.MinScale) result = zoombox.MinScale;

            if (result > zoombox.MaxScale) result = zoombox.MaxScale;

            if (double.IsNaN(result)) result = 1;

            return result;
        }

        #endregion

        #region ViewFinder Property

        private static readonly DependencyPropertyKey ViewFinderPropertyKey =
            DependencyProperty.RegisterReadOnly("ViewFinder", typeof(FrameworkElement), typeof(Zoombox),
                new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty ViewFinderProperty = ViewFinderPropertyKey.DependencyProperty;

        public FrameworkElement ViewFinder => (FrameworkElement) GetValue(ViewFinderProperty);

        #endregion

        #region ViewFinderVisibility Attached Property

        public static readonly DependencyProperty ViewFinderVisibilityProperty =
            DependencyProperty.RegisterAttached("ViewFinderVisibility", typeof(Visibility), typeof(Zoombox),
                new FrameworkPropertyMetadata(Visibility.Visible));

        public static Visibility GetViewFinderVisibility(DependencyObject d)
        {
            return (Visibility) d.GetValue(ViewFinderVisibilityProperty);
        }

        public static void SetViewFinderVisibility(DependencyObject d, Visibility value)
        {
            d.SetValue(ViewFinderVisibilityProperty, value);
        }

        #endregion

        #region Viewport Property

        private static readonly DependencyPropertyKey ViewportPropertyKey =
            DependencyProperty.RegisterReadOnly("Viewport", typeof(Rect), typeof(Zoombox),
                new FrameworkPropertyMetadata(Rect.Empty,
                    OnViewportChanged));

        public static readonly DependencyProperty ViewportProperty = ViewportPropertyKey.DependencyProperty;

        public Rect Viewport => (Rect) GetValue(ViewportProperty);

        private static void OnViewportChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            // keep the Position property in sync with the Viewport
            var zoombox = (Zoombox) o;
            zoombox.Position = new Point(-zoombox.Viewport.Left * zoombox.Scale / zoombox._viewboxFactor,
                -zoombox.Viewport.Top * zoombox.Scale / zoombox._viewboxFactor);
        }

        #endregion

        #region ViewStackCount Property

        private static readonly DependencyPropertyKey ViewStackCountPropertyKey =
            DependencyProperty.RegisterReadOnly("ViewStackCount", typeof(int), typeof(Zoombox),
                new FrameworkPropertyMetadata(-1,
                    OnViewStackCountChanged));

        public static readonly DependencyProperty ViewStackCountProperty = ViewStackCountPropertyKey.DependencyProperty;

        public int ViewStackCount => (int) GetValue(ViewStackCountProperty);

        internal void SetViewStackCount(int value)
        {
            SetValue(ViewStackCountPropertyKey, value);
        }

        private static void OnViewStackCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Zoombox) d).OnViewStackCountChanged(e);
        }

        private void OnViewStackCountChanged(DependencyPropertyChangedEventArgs e)
        {
            if (EffectiveViewStackMode == ZoomboxViewStackMode.Disabled)
                return;

            UpdateStackProperties();
        }

        #endregion

        #region ViewStackIndex Property

        public static readonly DependencyProperty ViewStackIndexProperty =
            DependencyProperty.Register("ViewStackIndex", typeof(int), typeof(Zoombox),
                new FrameworkPropertyMetadata(-1,
                    OnViewStackIndexChanged, CoerceViewStackIndexValue));

        public int ViewStackIndex
        {
            get => (int) GetValue(ViewStackIndexProperty);
            set => SetValue(ViewStackIndexProperty, value);
        }

        private static void OnViewStackIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Zoombox) d).OnViewStackIndexChanged(e);
        }

        private void OnViewStackIndexChanged(DependencyPropertyChangedEventArgs e)
        {
            if (EffectiveViewStackMode == ZoomboxViewStackMode.Disabled)
                return;

            if (!IsUpdatingView)
            {
                var viewIndex = ViewStackIndex;
                if (viewIndex >= 0 && viewIndex < ViewStack.Count)
                    // update the current view, but don't allow the new view 
                    // to be added to the view stack
                    UpdateView(ViewStack[viewIndex], true, false, viewIndex);
            }

            UpdateStackProperties();
            RaiseEvent(new IndexChangedEventArgs(ViewStackIndexChangedEvent, (int) e.OldValue, (int) e.NewValue));
        }

        private static object CoerceViewStackIndexValue(DependencyObject d, object value)
        {
            var zoombox = d as Zoombox;
            return zoombox.EffectiveViewStackMode == ZoomboxViewStackMode.Disabled ? -1 : value;
        }

        #endregion

        #region ViewStackMode Property

        public static readonly DependencyProperty ViewStackModeProperty =
            DependencyProperty.Register("ViewStackMode", typeof(ZoomboxViewStackMode), typeof(Zoombox),
                new FrameworkPropertyMetadata(ZoomboxViewStackMode.Default,
                    OnViewStackModeChanged, CoerceViewStackModeValue));

        public ZoomboxViewStackMode ViewStackMode
        {
            get => (ZoomboxViewStackMode) GetValue(ViewStackModeProperty);
            set => SetValue(ViewStackModeProperty, value);
        }

        private static void OnViewStackModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Zoombox) d).OnViewStackModeChanged(e);
        }

        private void OnViewStackModeChanged(DependencyPropertyChangedEventArgs e)
        {
            if ((ZoomboxViewStackMode) e.NewValue == ZoomboxViewStackMode.Disabled && _viewStack is not null)
            {
                _viewStack.ClearViewStackSource();
                _viewStack = null;
            }
        }

        private static object CoerceViewStackModeValue(DependencyObject d, object value)
        {
            var zoombox = d as Zoombox;
            var effectiveMode = (ZoomboxViewStackMode) value;

            // if the effective mode is currently disabled, it must be updated first
            if (zoombox.EffectiveViewStackMode == ZoomboxViewStackMode.Disabled)
                zoombox.SetEffectiveViewStackMode(effectiveMode);

            // now determine the correct effective mode
            if (effectiveMode != ZoomboxViewStackMode.Disabled)
            {
                if (effectiveMode == ZoomboxViewStackMode.Default)
                    effectiveMode = zoombox.ViewStack.AreViewsFromSource
                        ? ZoomboxViewStackMode.Manual
                        : ZoomboxViewStackMode.Auto;
                if (zoombox.ViewStack.AreViewsFromSource && effectiveMode != ZoomboxViewStackMode.Manual)
                    throw new InvalidOperationException(ErrorMessages.GetMessage("ViewModeInvalidForSource"));
            }

            // update the effective mode
            zoombox.SetEffectiveViewStackMode(effectiveMode);
            return value;
        }

        #endregion

        #region ViewStackSource Property

        public static readonly DependencyProperty ViewStackSourceProperty =
            DependencyProperty.Register("ViewStackSource", typeof(IEnumerable), typeof(Zoombox),
                new FrameworkPropertyMetadata(null,
                    OnViewStackSourceChanged));

        [Bindable(true)]
        public IEnumerable ViewStackSource
        {
            get => _viewStack is null ? null : ViewStack.Source;
            set
            {
                if (value is null)
                    ClearValue(ViewStackSourceProperty);
                else
                    SetValue(ViewStackSourceProperty, value);
            }
        }

        private static void OnViewStackSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var zoombox = (Zoombox) d;
            var oldValue = (IEnumerable) e.OldValue;
            var newValue = (IEnumerable) e.NewValue;

            // We need to know whether the new value represents an explicit null value 
            // or whether it came from a binding. The latter indicates that we stay in ViewStackSource mode,
            // but with a null collection.
            if (e.NewValue is null && !BindingOperations.IsDataBound(d, ViewStackSourceProperty))
            {
                if (zoombox.ViewStack is not null) zoombox.ViewStack.ClearViewStackSource();
            }
            else
            {
                zoombox.ViewStack.SetViewStackSource(newValue);
            }

            zoombox.CoerceValue(ViewStackModeProperty);
        }

        #endregion

        #region ZoomModifiers Property

        public static readonly DependencyProperty ZoomModifiersProperty =
            DependencyProperty.Register("ZoomModifiers", typeof(KeyModifierCollection), typeof(Zoombox),
                new FrameworkPropertyMetadata(GetDefaultZoomModifiers()));

        [TypeConverter(typeof(KeyModifierCollectionConverter))]
        public KeyModifierCollection ZoomModifiers
        {
            get => (KeyModifierCollection) GetValue(ZoomModifiersProperty);
            set => SetValue(ZoomModifiersProperty, value);
        }

        private static KeyModifierCollection GetDefaultZoomModifiers()
        {
            var result = new KeyModifierCollection();
            result.Add(KeyModifier.Shift);
            result.Add(KeyModifier.Exact);
            return result;
        }

        #endregion

        #region ZoomOnPreview Property

        public static readonly DependencyProperty ZoomOnPreviewProperty =
            DependencyProperty.Register("ZoomOnPreview", typeof(bool), typeof(Zoombox),
                new FrameworkPropertyMetadata(true));

        public bool ZoomOnPreview
        {
            get => (bool) GetValue(ZoomOnPreviewProperty);
            set => SetValue(ZoomOnPreviewProperty, value);
        }

        #endregion

        #region ZoomOrigin Property

        public static readonly DependencyProperty ZoomOriginProperty =
            DependencyProperty.Register("ZoomOrigin", typeof(Point), typeof(Zoombox),
                new FrameworkPropertyMetadata(new Point(0.5d, 0.5d)));

        public Point ZoomOrigin
        {
            get => (Point) GetValue(ZoomOriginProperty);
            set => SetValue(ZoomOriginProperty, value);
        }

        #endregion

        #region ZoomPercentage Property

        public static readonly DependencyProperty ZoomPercentageProperty =
            DependencyProperty.Register("ZoomPercentage", typeof(double), typeof(Zoombox),
                new FrameworkPropertyMetadata(5d));

        public double ZoomPercentage
        {
            get => (double) GetValue(ZoomPercentageProperty);
            set => SetValue(ZoomPercentageProperty, value);
        }

        #endregion

        #region ZoomOn Property

        public static readonly DependencyProperty ZoomOnProperty =
            DependencyProperty.Register("ZoomOn", typeof(ZoomboxZoomOn), typeof(Zoombox),
                new FrameworkPropertyMetadata(ZoomboxZoomOn.Content));

        public ZoomboxZoomOn ZoomOn
        {
            get => (ZoomboxZoomOn) GetValue(ZoomOnProperty);
            set => SetValue(ZoomOnProperty, value);
        }

        #endregion

        #region ZoomToSelectionModifiers Property

        public static readonly DependencyProperty ZoomToSelectionModifiersProperty =
            DependencyProperty.Register("ZoomToSelectionModifiers", typeof(KeyModifierCollection), typeof(Zoombox),
                new FrameworkPropertyMetadata(GetDefaultZoomToSelectionModifiers()));

        [TypeConverter(typeof(KeyModifierCollectionConverter))]
        public KeyModifierCollection ZoomToSelectionModifiers
        {
            get => (KeyModifierCollection) GetValue(ZoomToSelectionModifiersProperty);
            set => SetValue(ZoomToSelectionModifiersProperty, value);
        }

        private static KeyModifierCollection GetDefaultZoomToSelectionModifiers()
        {
            var result = new KeyModifierCollection();
            result.Add(KeyModifier.Alt);
            result.Add(KeyModifier.Exact);
            return result;
        }

        #endregion

        #region KeepContentInBounds Property

        public static readonly DependencyProperty KeepContentInBoundsProperty =
            DependencyProperty.Register("KeepContentInBounds", typeof(bool), typeof(Zoombox),
                new FrameworkPropertyMetadata(false,
                    OnKeepContentInBoundsChanged));

        public bool KeepContentInBounds
        {
            get => (bool) GetValue(KeepContentInBoundsProperty);
            set => SetValue(KeepContentInBoundsProperty, value);
        }

        private static void OnKeepContentInBoundsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Zoombox) d).OnKeepContentInBoundsChanged(e);
        }

        private void OnKeepContentInBoundsChanged(DependencyPropertyChangedEventArgs e)
        {
            //
            // Update view and see if we need to reposition the content
            //
            var oldIsAnimated = IsAnimated;
            IsAnimated = false;
            try
            {
                UpdateView(CurrentView, false, false, ViewStackIndex);
            }
            finally
            {
                IsAnimated = oldIsAnimated;
            }
        }

        #endregion

        #region AnimationBeginning Event

        public static readonly RoutedEvent AnimationBeginningEvent =
            EventManager.RegisterRoutedEvent("AnimationBeginning", RoutingStrategy.Bubble, typeof(RoutedEventHandler),
                typeof(Zoombox));

        public event RoutedEventHandler AnimationBeginning
        {
            add => AddHandler(AnimationBeginningEvent, value);
            remove => RemoveHandler(AnimationBeginningEvent, value);
        }

        #endregion

        #region AnimationCompleted Event

        public static readonly RoutedEvent AnimationCompletedEvent =
            EventManager.RegisterRoutedEvent("AnimationCompleted", RoutingStrategy.Bubble, typeof(RoutedEventHandler),
                typeof(Zoombox));

        public event RoutedEventHandler AnimationCompleted
        {
            add => AddHandler(AnimationCompletedEvent, value);
            remove => RemoveHandler(AnimationCompletedEvent, value);
        }

        #endregion

        #region CurrentViewChanged Event

        public static readonly RoutedEvent CurrentViewChangedEvent =
            EventManager.RegisterRoutedEvent("CurrentViewChanged", RoutingStrategy.Bubble,
                typeof(ZoomboxViewChangedEventHandler), typeof(Zoombox));

        public event ZoomboxViewChangedEventHandler CurrentViewChanged
        {
            add => AddHandler(CurrentViewChangedEvent, value);
            remove => RemoveHandler(CurrentViewChangedEvent, value);
        }

        #endregion

        #region ResolveViewFinderDisplay Event

        public static readonly RoutedEvent ResolveViewFinderDisplayEvent =
            EventManager.RegisterRoutedEvent("ResolveViewFinderDisplay", RoutingStrategy.Bubble,
                typeof(ResolveViewFinderDisplayEventHandler), typeof(Zoombox));

        public event ResolveViewFinderDisplayEventHandler ResolveViewFinderDisplay
        {
            add => AddHandler(ResolveViewFinderDisplayEvent, value);
            remove => RemoveHandler(ResolveViewFinderDisplayEvent, value);
        }

        #endregion

        #region ViewStackIndexChanged Event

        public static readonly RoutedEvent ViewStackIndexChangedEvent =
            EventManager.RegisterRoutedEvent("ViewStackIndexChanged", RoutingStrategy.Bubble,
                typeof(IndexChangedEventHandler), typeof(Zoombox));

        public event IndexChangedEventHandler ViewStackIndexChanged
        {
            add => AddHandler(ViewStackIndexChangedEvent, value);
            remove => RemoveHandler(ViewStackIndexChangedEvent, value);
        }

        #endregion

        #region Back Command

        public static RoutedUICommand Back = new("Go Back", "GoBack", typeof(Zoombox));

        private void CanGoBack(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = EffectiveViewStackMode != ZoomboxViewStackMode.Disabled
                           && ViewStackIndex > 0;
        }

        private void GoBack(object sender, ExecutedRoutedEventArgs e)
        {
            GoBack();
        }

        #endregion

        #region Center Command

        public static RoutedUICommand Center = new("Center Content", "Center", typeof(Zoombox));

        private void CenterContent(object sender, ExecutedRoutedEventArgs e)
        {
            CenterContent();
        }

        #endregion

        #region Fill Command

        public static RoutedUICommand Fill = new("Fill Bounds with Content", "FillToBounds", typeof(Zoombox));

        private void FillToBounds(object sender, ExecutedRoutedEventArgs e)
        {
            FillToBounds();
        }

        #endregion

        #region Fit Command

        public static RoutedUICommand Fit = new("Fit Content within Bounds", "FitToBounds", typeof(Zoombox));

        private void FitToBounds(object sender, ExecutedRoutedEventArgs e)
        {
            FitToBounds();
        }

        #endregion

        #region Forward Command

        public static RoutedUICommand Forward = new("Go Forward", "GoForward", typeof(Zoombox));

        private void CanGoForward(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = EffectiveViewStackMode != ZoomboxViewStackMode.Disabled
                           && ViewStackIndex < ViewStack.Count - 1;
        }

        private void GoForward(object sender, ExecutedRoutedEventArgs e)
        {
            GoForward();
        }

        #endregion

        #region Home Command

        public static RoutedUICommand Home = new("Go Home", "GoHome", typeof(Zoombox));

        private void CanGoHome(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = EffectiveViewStackMode != ZoomboxViewStackMode.Disabled
                           && ViewStack.Count > 0
                           && ViewStackIndex != 0;
        }

        private void GoHome(object sender, ExecutedRoutedEventArgs e)
        {
            GoHome();
        }

        #endregion

        #region PanDown Command

        public static RoutedUICommand PanDown = new("Pan Down", "PanDown", typeof(Zoombox));

        private void PanDownExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Position = new Point(_basePosition.X, _basePosition.Y + PanDistance);
        }

        #endregion

        #region PanLeft Command

        public static RoutedUICommand PanLeft = new("Pan Left", "PanLeft", typeof(Zoombox));

        private void PanLeftExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Position = new Point(_basePosition.X - PanDistance, _basePosition.Y);
        }

        #endregion

        #region PanRight Command

        public static RoutedUICommand PanRight = new("Pan Right", "PanRight", typeof(Zoombox));

        private void PanRightExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Position = new Point(_basePosition.X + PanDistance, _basePosition.Y);
        }

        #endregion

        #region PanUp Command

        public static RoutedUICommand PanUp = new("Pan Up", "PanUp", typeof(Zoombox));

        private void PanUpExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Position = new Point(_basePosition.X, _basePosition.Y - PanDistance);
        }

        #endregion

        #region Refocus Command

        public static RoutedUICommand Refocus = new("Refocus View", "Refocus", typeof(Zoombox));

        private void CanRefocusView(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = EffectiveViewStackMode == ZoomboxViewStackMode.Manual && ViewStackIndex >= 0 &&
                           ViewStackIndex < ViewStack.Count && CurrentView != ViewStack[ViewStackIndex];
        }

        private void RefocusView(object sender, ExecutedRoutedEventArgs e)
        {
            RefocusView();
        }

        #endregion

        #region ZoomIn Command

        public static RoutedUICommand ZoomIn = new("Zoom In", "ZoomIn", typeof(Zoombox));

        private void ZoomInExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Zoom(ZoomPercentage / 100);
        }

        #endregion

        #region ZoomOut Command

        public static RoutedUICommand ZoomOut = new("Zoom Out", "ZoomOut", typeof(Zoombox));

        private void ZoomOutExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Zoom(-ZoomPercentage / 100);
        }

        #endregion

        #region OnKeyDown Methods

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (NavigateOnPreview && !e.Handled) ProcessNavigationButton(e);

            base.OnPreviewKeyDown(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!NavigateOnPreview && !e.Handled) ProcessNavigationButton(e);

            base.OnKeyDown(e);
        }

        #endregion

        #region OnMouseDown Methods

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            if (NavigateOnPreview && !e.Handled) ProcessNavigationButton(e);

            base.OnPreviewMouseDown(e);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (!NavigateOnPreview && !e.Handled) ProcessNavigationButton(e);

            base.OnMouseDown(e);
        }

        #endregion

        #region OnMouseLeftButton Methods

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (DragOnPreview && !e.Handled && _contentPresenter is not null) ProcessMouseLeftButtonDown(e);

            base.OnPreviewMouseLeftButtonDown(e);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (!DragOnPreview && !e.Handled && _contentPresenter is not null) ProcessMouseLeftButtonDown(e);

            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (DragOnPreview && !e.Handled && _contentPresenter is not null) ProcessMouseLeftButtonUp(e);

            base.OnPreviewMouseLeftButtonUp(e);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (!DragOnPreview && !e.Handled && _contentPresenter is not null) ProcessMouseLeftButtonUp(e);

            base.OnMouseLeftButtonUp(e);
        }

        #endregion

        #region OnMouseMove Methods

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            if (DragOnPreview && !e.Handled && _contentPresenter is not null) ProcessMouseMove(e);

            base.OnPreviewMouseMove(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!DragOnPreview && !e.Handled && _contentPresenter is not null) ProcessMouseMove(e);

            base.OnMouseMove(e);
        }

        #endregion

        #region OnMouseWheel Methods

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            if (ZoomOnPreview && !e.Handled && _contentPresenter is not null) ProcessMouseWheelZoom(e);

            base.OnPreviewMouseWheel(e);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (!ZoomOnPreview && !e.Handled && _contentPresenter is not null) ProcessMouseWheelZoom(e);

            base.OnMouseWheel(e);
        }

        #endregion

        #region Private Fields

        // the default value for a single mouse wheel delta appears to be 28
        private static readonly int MOUSE_WHEEL_DELTA = 28;

        // the content control's one and only content presenter
        private ContentPresenter _contentPresenter;

        // the content of the Zoombox (cast as a UIElement)
        private UIElement _content;

        // the drag adorner used for selecting a region in a zoom-to-selection operation
        private DragAdorner _dragAdorner;

        // the view stack
        private ZoomboxViewStack _viewStack;

        // the view finder display panel
        // this is used to show the current viewport
        private ZoomboxViewFinderDisplay _viewFinderDisplay;

        // state variables used during drag and select operations
        private Rect _resizeViewportBounds = Rect.Empty;
        private Point _resizeAnchorPoint = new(0, 0);
        private Point _resizeDraggingPoint = new(0, 0);
        private Point _originPoint = new(0, 0);

        private double _viewboxFactor = 1.0;
        private double _relativeScale = 1.0;
        private Point _relativePosition;
        private Point _basePosition;

        // used to track the time delta between stack operations
        private DateTime _lastStackAddition;

        // used to provide stack index when view changes
        private int _lastViewIndex = -1;

        private BitVector32 _cacheBits = new(0);

        #endregion
    }
}