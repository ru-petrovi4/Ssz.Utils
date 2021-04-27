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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Ssz.Xceed.Wpf.Toolkit.Core.Utilities;
using Ssz.Xceed.Wpf.Toolkit.Media.Animation;

namespace Ssz.Xceed.Wpf.Toolkit.Panels
{
    public class SwitchPanel : PanelBase, IScrollInfo
    {
        #region Static Fields

        private static readonly Vector ZeroVector = new();

        #endregion

        #region Constructors

        public SwitchPanel()
        {
            SetLayouts(new ObservableCollection<AnimationPanel>());
            Loaded += OnLoaded;
        }

        #endregion

        #region VisualChildrenCount Protected Property

        protected override int VisualChildrenCount
        {
            get
            {
                var result = 0;

                if (HasLoaded && _currentLayoutPanel != null)
                    result = _currentLayoutPanel.VisualChildrenCountInternal;
                else
                    result = base.VisualChildrenCount;

                return result;
            }
        }

        #endregion

        #region ChildrenInternal Internal Property

        internal UIElementCollection ChildrenInternal => InternalChildren;

        #endregion

        #region HasLoaded Internal Property

        internal bool HasLoaded
        {
            get => _cacheBits[(int) CacheBits.HasLoaded];
            set => _cacheBits[(int) CacheBits.HasLoaded] = value;
        }

        #endregion

        #region IsScrollingPhysically Private Property

        private bool IsScrollingPhysically
        {
            get
            {
                var isScrollingPhys = false;

                if (_scrollOwner != null)
                {
                    isScrollingPhys = true;

                    if (ActiveLayout != null && ActiveLayout is IScrollInfo)
                        isScrollingPhys = ((IScrollInfo) ActiveLayout).ScrollOwner == null;
                }

                return isScrollingPhys;
            }
        }

        #endregion

        protected override Size MeasureOverride(Size availableSize)
        {
            var layout = Layouts.Count == 0 ? _defaultLayoutCanvas : ActiveLayout;
            var measureSize = layout.MeasureChildrenCore(InternalChildren, availableSize);

            if (IsScrollingPhysically)
            {
                var viewport = availableSize;
                var extent = measureSize;

                //
                // Make sure our offset works with the new size of the panel.  We don't want to show
                // any whitespace if the user scrolled all the way down and then increased the size of the panel.
                //
                var newOffset = new Vector(
                    Math.Max(0, Math.Min(_offset.X, extent.Width - viewport.Width)),
                    Math.Max(0, Math.Min(_offset.Y, extent.Height - viewport.Height)));

                SetScrollingData(viewport, extent, newOffset);
            }

            return measureSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var layout = Layouts.Count == 0 ? _defaultLayoutCanvas : ActiveLayout;

            if (IsScrollingPhysically)
                layout.PhysicalScrollOffset = _offset;
            else
                layout.PhysicalScrollOffset = ZeroVector;

            return layout.ArrangeChildrenCore(InternalChildren, finalSize);
        }

        protected override Visual GetVisualChild(int index)
        {
            if (HasLoaded && _currentLayoutPanel != null)
                return _currentLayoutPanel.GetVisualChildInternal(index);

            return base.GetVisualChild(index);
        }

        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            // The OnNotifyVisualChildAdded/Removed methods get called for all animation panels within a 
            // SwitchPanel.Layouts collection, regardless of whether they are the active layout for the SwitchPanel.
            if (visualAdded is UIElement)
            {
                // do not issue notification for a child that is exiting
                if (_currentLayoutPanel == null || !_currentLayoutPanel.IsRemovingInternalChild)
                    foreach (var panel in Layouts)
                        panel.OnNotifyVisualChildAddedInternal(visualAdded as UIElement);
            }
            else if (visualRemoved is UIElement)
            {
                foreach (var panel in Layouts) panel.OnNotifyVisualChildRemovedInternal(visualRemoved as UIElement);
            }

            if (_currentLayoutPanel != null)
                _currentLayoutPanel.OnSwitchParentVisualChildrenChanged(visualAdded, visualRemoved);
            else
                base.OnVisualChildrenChanged(visualAdded, visualRemoved);
        }

        internal void AddVisualChildInternal(Visual child)
        {
            AddVisualChild(child);
        }

        internal void BeginLayoutSwitch()
        {
            RaiseSwitchAnimationBegunEvent();
        }

        internal void EndLayoutSwitch()
        {
            RaiseSwitchAnimationCompletedEvent();
        }

        internal Visual GetVisualChildInternal(int index)
        {
            // called from AnimationPanel to access base class method
            return base.GetVisualChild(index);
        }

        internal void OnVisualChildrenChangedInternal(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);
        }

        internal UIElement RegisterPresenter(SwitchPresenter presenter)
        {
            UIElement result = null;
            AnimationPanel ignore;
            result = AnimationPanel.FindAncestorChildOfAnimationPanel(presenter, out ignore);
            if (result != null)
            {
                _presenters.Add(presenter);
                presenter.SwapTheTemplate(ActiveSwitchTemplate, false);
            }

            return result;
        }

        internal void RemoveVisualChildInternal(Visual child)
        {
            RemoveVisualChild(child);
        }

        internal void UnregisterPresenter(SwitchPresenter presenter, DependencyObject container)
        {
            if (container != null)
            {
                _presenters.Remove(presenter);
                presenter.SwapTheTemplate(null, false);
            }
        }

        internal void UpdateSwitchTemplate()
        {
            SetActiveSwitchTemplate(ActiveLayout == null || ActiveLayout.SwitchTemplate == null
                ? SwitchTemplate
                : ActiveLayout.SwitchTemplate);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            HasLoaded = true;

            // invalidate arrange to give enter animations a chance to run
            InvalidateArrange();
        }

        private void LayoutsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Move)
            {
                if (e.NewItems != null)
                    foreach (AnimationPanel panel in e.NewItems)
                    {
                        AddLogicalChild(panel);
                        panel.SetSwitchParent(this);

                        if (panel is IScrollInfo) ((IScrollInfo) panel).ScrollOwner = ScrollOwner;

                        if (IsLoaded)
                            foreach (UIElement child in InternalChildren)
                            {
                                if (child == null)
                                    continue;

                                panel.OnNotifyVisualChildAddedInternal(child);
                            }
                    }

                if (e.OldItems != null)
                    foreach (AnimationPanel panel in e.OldItems)
                    {
                        if (IsLoaded)
                            foreach (UIElement child in InternalChildren)
                            {
                                if (child == null)
                                    continue;

                                panel.OnNotifyVisualChildRemovedInternal(child);
                            }

                        RemoveLogicalChild(panel);
                        panel.SetSwitchParent(null);

                        if (panel is IScrollInfo) ((IScrollInfo) panel).ScrollOwner = null;
                    }
            }

            // ensure valid ActiveLayoutIndex value
            CoerceValue(ActiveLayoutIndexProperty);
            SetActiveLayout(Layouts.Count == 0 ? null : Layouts[ActiveLayoutIndex]);
        }

        private void ResetScrollInfo()
        {
            _offset = new Vector();
            _viewport = _extent = new Size(0, 0);
        }

        private void OnScrollChange()
        {
            if (ScrollOwner != null) ScrollOwner.InvalidateScrollInfo();
        }

        private void SetScrollingData(Size viewport, Size extent, Vector offset)
        {
            _offset = offset;

            if (DoubleHelper.AreVirtuallyEqual(viewport, _viewport) == false ||
                DoubleHelper.AreVirtuallyEqual(extent, _extent) == false ||
                DoubleHelper.AreVirtuallyEqual(offset, _computedOffset) == false)
            {
                _viewport = viewport;
                _extent = extent;
                _computedOffset = offset;
                OnScrollChange();
            }
        }

        private double ValidateInputOffset(double offset, string parameterName)
        {
            if (double.IsNaN(offset))
                throw new ArgumentOutOfRangeException(parameterName);

            return Math.Max(0d, offset);
        }

        private int FindChildFromVisual(Visual vis)
        {
            var index = -1;

            DependencyObject parent = vis;
            DependencyObject child = null;

            do
            {
                child = parent;
                parent = VisualTreeHelper.GetParent(child);
            } while (parent != null && parent != this);

            if (parent == this) index = Children.IndexOf((UIElement) child);

            return index;
        }

        #region CacheBits Nested Type

        private enum CacheBits
        {
            HasLoaded = 0x00000001
        }

        #endregion

        #region AreLayoutSwitchesAnimated Property

        public static readonly DependencyProperty AreLayoutSwitchesAnimatedProperty =
            DependencyProperty.Register("AreLayoutSwitchesAnimated", typeof(bool), typeof(SwitchPanel),
                new FrameworkPropertyMetadata(true));

        public bool AreLayoutSwitchesAnimated
        {
            get => (bool) GetValue(AreLayoutSwitchesAnimatedProperty);
            set => SetValue(AreLayoutSwitchesAnimatedProperty, value);
        }

        #endregion

        #region ActiveLayout Property

        private static readonly DependencyPropertyKey ActiveLayoutPropertyKey =
            DependencyProperty.RegisterReadOnly("ActiveLayout", typeof(AnimationPanel), typeof(SwitchPanel),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure,
                    OnActiveLayoutChanged));

        public static readonly DependencyProperty ActiveLayoutProperty = ActiveLayoutPropertyKey.DependencyProperty;

        public AnimationPanel ActiveLayout => (AnimationPanel) GetValue(ActiveLayoutProperty);

        protected void SetActiveLayout(AnimationPanel value)
        {
            SetValue(ActiveLayoutPropertyKey, value);
        }

        private static void OnActiveLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SwitchPanel) d).OnActiveLayoutChanged(e);
        }

        protected virtual void OnActiveLayoutChanged(DependencyPropertyChangedEventArgs e)
        {
            if (_currentLayoutPanel != null) _currentLayoutPanel.DeactivateLayout();
            _currentLayoutPanel = e.NewValue as AnimationPanel;
            if (_currentLayoutPanel != null)
            {
                var info = _currentLayoutPanel as IScrollInfo;
                if (info != null && info.ScrollOwner != null) info.ScrollOwner.InvalidateScrollInfo();

                _currentLayoutPanel.ActivateLayout();
            }

            RaiseActiveLayoutChangedEvent();
            Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                (ThreadStart) delegate { UpdateSwitchTemplate(); });
        }

        #endregion

        #region ActiveLayoutIndex Property

        public static readonly DependencyProperty ActiveLayoutIndexProperty =
            DependencyProperty.Register("ActiveLayoutIndex", typeof(int), typeof(SwitchPanel),
                new FrameworkPropertyMetadata(-1,
                    OnActiveLayoutIndexChanged, CoerceActiveLayoutIndexValue));

        public int ActiveLayoutIndex
        {
            get => (int) GetValue(ActiveLayoutIndexProperty);
            set => SetValue(ActiveLayoutIndexProperty, value);
        }

        private static void OnActiveLayoutIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SwitchPanel) d).OnActiveLayoutIndexChanged(e);
        }

        protected virtual void OnActiveLayoutIndexChanged(DependencyPropertyChangedEventArgs e)
        {
            SetActiveLayout(Layouts.Count == 0 ? null : Layouts[ActiveLayoutIndex]);
        }

        private static object CoerceActiveLayoutIndexValue(DependencyObject d, object value)
        {
            var panelCount = (d as SwitchPanel).Layouts.Count;
            var result = (int) value;

            if (result < 0 && panelCount > 0)
                result = 0;
            else if (result >= panelCount) result = panelCount - 1;

            return result;
        }

        #endregion

        #region ActiveSwitchTemplate Property

        private static readonly DependencyPropertyKey ActiveSwitchTemplatePropertyKey =
            DependencyProperty.RegisterReadOnly("ActiveSwitchTemplate", typeof(DataTemplate), typeof(SwitchPanel),
                new FrameworkPropertyMetadata(null, OnActiveSwitchTemplateChanged));

        public static readonly DependencyProperty ActiveSwitchTemplateProperty =
            ActiveSwitchTemplatePropertyKey.DependencyProperty;

        public DataTemplate ActiveSwitchTemplate => (DataTemplate) GetValue(ActiveSwitchTemplateProperty);

        protected void SetActiveSwitchTemplate(DataTemplate value)
        {
            SetValue(ActiveSwitchTemplatePropertyKey, value);
        }

        private static void OnActiveSwitchTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SwitchPanel) d).OnActiveSwitchTemplateChanged(e);
        }

        protected virtual void OnActiveSwitchTemplateChanged(DependencyPropertyChangedEventArgs e)
        {
            if (_presenters.Count > 0)
            {
                var template = e.NewValue as DataTemplate;
                var currentChildren = new List<UIElement>(InternalChildren.Count);
                foreach (UIElement child in InternalChildren)
                {
                    if (child == null)
                        continue;

                    currentChildren.Add(child);
                }

                foreach (var presenter in _presenters)
                    if (presenter._switchRoot != null && currentChildren.Contains(presenter._switchRoot))
                        presenter.SwapTheTemplate(template, AreLayoutSwitchesAnimated);
            }
        }

        #endregion

        #region DefaultAnimationRate Property

        public static readonly DependencyProperty DefaultAnimationRateProperty =
            AnimationPanel.DefaultAnimationRateProperty.AddOwner(typeof(SwitchPanel));

        public AnimationRate DefaultAnimationRate
        {
            get => (AnimationRate) GetValue(DefaultAnimationRateProperty);
            set => SetValue(DefaultAnimationRateProperty, value);
        }

        #endregion

        #region DefaultAnimator Property

        public static readonly DependencyProperty DefaultAnimatorProperty =
            AnimationPanel.DefaultAnimatorProperty.AddOwner(typeof(SwitchPanel));

        public IterativeAnimator DefaultAnimator
        {
            get => (IterativeAnimator) GetValue(DefaultAnimatorProperty);
            set => SetValue(DefaultAnimatorProperty, value);
        }

        #endregion

        #region EnterAnimationRate Property

        public static readonly DependencyProperty EnterAnimationRateProperty =
            AnimationPanel.EnterAnimationRateProperty.AddOwner(typeof(SwitchPanel),
                new FrameworkPropertyMetadata(AnimationRate.Default));

        public AnimationRate EnterAnimationRate
        {
            get => (AnimationRate) GetValue(EnterAnimationRateProperty);
            set => SetValue(EnterAnimationRateProperty, value);
        }

        #endregion

        #region EnterAnimator Property

        public static readonly DependencyProperty EnterAnimatorProperty =
            AnimationPanel.EnterAnimatorProperty.AddOwner(typeof(SwitchPanel),
                new FrameworkPropertyMetadata(IterativeAnimator.Default));

        public IterativeAnimator EnterAnimator
        {
            get => (IterativeAnimator) GetValue(EnterAnimatorProperty);
            set => SetValue(EnterAnimatorProperty, value);
        }

        #endregion

        #region ExitAnimationRate Property

        public static readonly DependencyProperty ExitAnimationRateProperty =
            AnimationPanel.ExitAnimationRateProperty.AddOwner(typeof(SwitchPanel),
                new FrameworkPropertyMetadata(AnimationRate.Default));

        public AnimationRate ExitAnimationRate
        {
            get => (AnimationRate) GetValue(ExitAnimationRateProperty);
            set => SetValue(ExitAnimationRateProperty, value);
        }

        #endregion

        #region ExitAnimator Property

        public static readonly DependencyProperty ExitAnimatorProperty =
            AnimationPanel.ExitAnimatorProperty.AddOwner(typeof(SwitchPanel),
                new FrameworkPropertyMetadata(IterativeAnimator.Default));

        public IterativeAnimator ExitAnimator
        {
            get => (IterativeAnimator) GetValue(ExitAnimatorProperty);
            set => SetValue(ExitAnimatorProperty, value);
        }

        #endregion

        #region LayoutAnimationRate Property

        public static readonly DependencyProperty LayoutAnimationRateProperty =
            AnimationPanel.LayoutAnimationRateProperty.AddOwner(typeof(SwitchPanel),
                new FrameworkPropertyMetadata(AnimationRate.Default));

        public AnimationRate LayoutAnimationRate
        {
            get => (AnimationRate) GetValue(LayoutAnimationRateProperty);
            set => SetValue(LayoutAnimationRateProperty, value);
        }

        #endregion

        #region LayoutAnimator Property

        public static readonly DependencyProperty LayoutAnimatorProperty =
            AnimationPanel.LayoutAnimatorProperty.AddOwner(typeof(SwitchPanel),
                new FrameworkPropertyMetadata(IterativeAnimator.Default));

        public IterativeAnimator LayoutAnimator
        {
            get => (IterativeAnimator) GetValue(LayoutAnimatorProperty);
            set => SetValue(LayoutAnimatorProperty, value);
        }

        #endregion

        #region Layouts Property

        private static readonly DependencyPropertyKey LayoutsPropertyKey =
            DependencyProperty.RegisterReadOnly("Layouts", typeof(ObservableCollection<AnimationPanel>),
                typeof(SwitchPanel),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange,
                    OnLayoutsChanged));

        public static readonly DependencyProperty LayoutsProperty = LayoutsPropertyKey.DependencyProperty;

        public ObservableCollection<AnimationPanel> Layouts =>
            (ObservableCollection<AnimationPanel>) GetValue(LayoutsProperty);

        protected void SetLayouts(ObservableCollection<AnimationPanel> value)
        {
            SetValue(LayoutsPropertyKey, value);
        }

        private static void OnLayoutsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SwitchPanel) d).OnLayoutsChanged(e);
        }

        protected virtual void OnLayoutsChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
                (e.NewValue as ObservableCollection<AnimationPanel>).CollectionChanged
                    += LayoutsCollectionChanged;
            if (e.OldValue != null)
                (e.OldValue as ObservableCollection<AnimationPanel>).CollectionChanged
                    -= LayoutsCollectionChanged;
        }

        #endregion

        #region SwitchAnimationRate Property

        public static readonly DependencyProperty SwitchAnimationRateProperty =
            AnimationPanel.SwitchAnimationRateProperty.AddOwner(typeof(SwitchPanel),
                new FrameworkPropertyMetadata(AnimationRate.Default));

        public AnimationRate SwitchAnimationRate
        {
            get => (AnimationRate) GetValue(SwitchAnimationRateProperty);
            set => SetValue(SwitchAnimationRateProperty, value);
        }

        #endregion

        #region SwitchAnimator Property

        public static readonly DependencyProperty SwitchAnimatorProperty =
            AnimationPanel.SwitchAnimatorProperty.AddOwner(typeof(SwitchPanel),
                new FrameworkPropertyMetadata(IterativeAnimator.Default));

        public IterativeAnimator SwitchAnimator
        {
            get => (IterativeAnimator) GetValue(SwitchAnimatorProperty);
            set => SetValue(SwitchAnimatorProperty, value);
        }

        #endregion

        #region SwitchTemplate Property

        public static readonly DependencyProperty SwitchTemplateProperty =
            DependencyProperty.Register("SwitchTemplate", typeof(DataTemplate), typeof(SwitchPanel),
                new FrameworkPropertyMetadata(null,
                    OnSwitchTemplateChanged));

        public DataTemplate SwitchTemplate
        {
            get => (DataTemplate) GetValue(SwitchTemplateProperty);
            set => SetValue(SwitchTemplateProperty, value);
        }

        private static void OnSwitchTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SwitchPanel) d).OnSwitchTemplateChanged(e);
        }

        protected virtual void OnSwitchTemplateChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateSwitchTemplate();
        }

        #endregion

        #region TemplateAnimationRate Property

        public static readonly DependencyProperty TemplateAnimationRateProperty =
            AnimationPanel.TemplateAnimationRateProperty.AddOwner(typeof(SwitchPanel),
                new FrameworkPropertyMetadata(AnimationRate.Default));

        public AnimationRate TemplateAnimationRate
        {
            get => (AnimationRate) GetValue(TemplateAnimationRateProperty);
            set => SetValue(TemplateAnimationRateProperty, value);
        }

        #endregion

        #region TemplateAnimator Property

        public static readonly DependencyProperty TemplateAnimatorProperty =
            AnimationPanel.TemplateAnimatorProperty.AddOwner(typeof(SwitchPanel),
                new FrameworkPropertyMetadata(IterativeAnimator.Default));

        public IterativeAnimator TemplateAnimator
        {
            get => (IterativeAnimator) GetValue(TemplateAnimatorProperty);
            set => SetValue(TemplateAnimatorProperty, value);
        }

        #endregion

        #region ExitingChildren Internal Property

        internal List<UIElement> ExitingChildren { get; } = new();

        #endregion

        #region ActiveLayoutChanged Event

        public static readonly RoutedEvent ActiveLayoutChangedEvent =
            EventManager.RegisterRoutedEvent("ActiveLayoutChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler),
                typeof(SwitchPanel));

        public event RoutedEventHandler ActiveLayoutChanged
        {
            add => AddHandler(ActiveLayoutChangedEvent, value);
            remove => RemoveHandler(ActiveLayoutChangedEvent, value);
        }

        protected RoutedEventArgs RaiseActiveLayoutChangedEvent()
        {
            return RaiseActiveLayoutChangedEvent(this);
        }

        internal static RoutedEventArgs RaiseActiveLayoutChangedEvent(UIElement target)
        {
            if (target == null)
                return null;

            var args = new RoutedEventArgs();
            args.RoutedEvent = ActiveLayoutChangedEvent;
            RoutedEventHelper.RaiseEvent(target, args);
            return args;
        }

        #endregion

        #region SwitchAnimationBegun Event

        public static readonly RoutedEvent SwitchAnimationBegunEvent =
            EventManager.RegisterRoutedEvent("SwitchAnimationBegun", RoutingStrategy.Bubble, typeof(RoutedEventHandler),
                typeof(SwitchPanel));

        public event RoutedEventHandler SwitchAnimationBegun
        {
            add => AddHandler(SwitchAnimationBegunEvent, value);
            remove => RemoveHandler(SwitchAnimationBegunEvent, value);
        }

        protected RoutedEventArgs RaiseSwitchAnimationBegunEvent()
        {
            return RaiseSwitchAnimationBegunEvent(this);
        }

        private static RoutedEventArgs RaiseSwitchAnimationBegunEvent(UIElement target)
        {
            if (target == null)
                return null;

            var args = new RoutedEventArgs();
            args.RoutedEvent = SwitchAnimationBegunEvent;
            RoutedEventHelper.RaiseEvent(target, args);
            return args;
        }

        #endregion

        #region SwitchAnimationCompleted Event

        public static readonly RoutedEvent SwitchAnimationCompletedEvent =
            EventManager.RegisterRoutedEvent("SwitchAnimationCompleted", RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(SwitchPanel));

        public event RoutedEventHandler SwitchAnimationCompleted
        {
            add => AddHandler(SwitchAnimationCompletedEvent, value);
            remove => RemoveHandler(SwitchAnimationCompletedEvent, value);
        }

        protected RoutedEventArgs RaiseSwitchAnimationCompletedEvent()
        {
            return RaiseSwitchAnimationCompletedEvent(this);
        }

        private static RoutedEventArgs RaiseSwitchAnimationCompletedEvent(UIElement target)
        {
            if (target == null)
                return null;

            var args = new RoutedEventArgs();
            args.RoutedEvent = SwitchAnimationCompletedEvent;
            RoutedEventHelper.RaiseEvent(target, args);
            return args;
        }

        #endregion

        #region IScrollInfo Members

        public bool CanHorizontallyScroll
        {
            get
            {
                if (ActiveLayout != null && ActiveLayout is IScrollInfo)
                    return ((IScrollInfo) ActiveLayout).CanHorizontallyScroll;

                return _allowHorizontal;
            }
            set
            {
                if (ActiveLayout != null && ActiveLayout is IScrollInfo)
                    ((IScrollInfo) ActiveLayout).CanHorizontallyScroll = value;
                else
                    _allowHorizontal = value;
            }
        }

        public bool CanVerticallyScroll
        {
            get
            {
                if (ActiveLayout != null && ActiveLayout is IScrollInfo)
                    return ((IScrollInfo) ActiveLayout).CanVerticallyScroll;

                return _allowVertical;
            }
            set
            {
                if (ActiveLayout != null && ActiveLayout is IScrollInfo)
                    ((IScrollInfo) ActiveLayout).CanVerticallyScroll = value;
                else
                    _allowVertical = value;
            }
        }

        public double ExtentHeight
        {
            get
            {
                if (ActiveLayout != null && ActiveLayout is IScrollInfo)
                    return ((IScrollInfo) ActiveLayout).ExtentHeight;

                return _extent.Height;
            }
        }

        public double ExtentWidth
        {
            get
            {
                if (ActiveLayout != null && ActiveLayout is IScrollInfo)
                    return ((IScrollInfo) ActiveLayout).ExtentWidth;

                return _extent.Width;
            }
        }

        public double HorizontalOffset
        {
            get
            {
                if (ActiveLayout != null && ActiveLayout is IScrollInfo)
                    return ((IScrollInfo) ActiveLayout).HorizontalOffset;

                return _offset.X;
            }
        }

        public void LineDown()
        {
            if (ActiveLayout != null && ActiveLayout is IScrollInfo)
                ((IScrollInfo) ActiveLayout).LineDown();
            else
                SetVerticalOffset(VerticalOffset + 1d);
        }

        public void LineLeft()
        {
            if (ActiveLayout != null && ActiveLayout is IScrollInfo)
                ((IScrollInfo) ActiveLayout).LineLeft();
            else
                SetHorizontalOffset(VerticalOffset - 1d);
        }

        public void LineRight()
        {
            if (ActiveLayout != null && ActiveLayout is IScrollInfo)
                ((IScrollInfo) ActiveLayout).LineRight();
            else
                SetHorizontalOffset(VerticalOffset + 1d);
        }

        public void LineUp()
        {
            if (ActiveLayout != null && ActiveLayout is IScrollInfo)
                ((IScrollInfo) ActiveLayout).LineUp();
            else
                SetVerticalOffset(VerticalOffset + 1d);
        }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            if (ActiveLayout != null && ActiveLayout is IScrollInfo)
                return ((IScrollInfo) ActiveLayout).MakeVisible(visual, rectangle);

            if (rectangle.IsEmpty || visual == null || visual == this || !IsAncestorOf(visual))
                return Rect.Empty;

            rectangle = visual.TransformToAncestor(this).TransformBounds(rectangle);

            if (IsScrollingPhysically == false)
                return rectangle;

            //
            // Make sure we can find the child...
            //
            var index = FindChildFromVisual(visual);

            if (index == -1)
                throw new ArgumentException("visual");

            //
            // Since our _Offset pushes the items down we need to correct it here to 
            // give a true rectangle of the child.
            //
            var itemRect = rectangle;
            itemRect.Offset(_offset);

            var viewRect = new Rect(new Point(_offset.X, _offset.Y), _viewport);

            Vector newPhysOffset;
            if (ScrollHelper.ScrollLeastAmount(viewRect, itemRect, out newPhysOffset))
            {
                SetHorizontalOffset(newPhysOffset.X);
                SetVerticalOffset(newPhysOffset.Y);
            }

            return rectangle;
        }

        public void MouseWheelDown()
        {
            if (ActiveLayout != null && ActiveLayout is IScrollInfo)
                ((IScrollInfo) ActiveLayout).MouseWheelDown();
            else
                SetVerticalOffset(VerticalOffset + SystemParameters.WheelScrollLines);
        }

        public void MouseWheelLeft()
        {
            if (ActiveLayout != null && ActiveLayout is IScrollInfo)
                ((IScrollInfo) ActiveLayout).MouseWheelLeft();
            else
                SetVerticalOffset(VerticalOffset - 3d);
        }

        public void MouseWheelRight()
        {
            if (ActiveLayout != null && ActiveLayout is IScrollInfo)
                ((IScrollInfo) ActiveLayout).MouseWheelRight();
            else
                SetVerticalOffset(VerticalOffset + 3d);
        }

        public void MouseWheelUp()
        {
            if (ActiveLayout != null && ActiveLayout is IScrollInfo)
                ((IScrollInfo) ActiveLayout).MouseWheelUp();
            else
                SetVerticalOffset(VerticalOffset - SystemParameters.WheelScrollLines);
        }

        public void PageDown()
        {
            if (ActiveLayout != null && ActiveLayout is IScrollInfo)
                ((IScrollInfo) ActiveLayout).PageDown();
            else
                SetVerticalOffset(VerticalOffset + ViewportHeight);
        }

        public void PageLeft()
        {
            if (ActiveLayout != null && ActiveLayout is IScrollInfo)
                ((IScrollInfo) ActiveLayout).PageLeft();
            else
                SetHorizontalOffset(HorizontalOffset - ViewportWidth);
        }

        public void PageRight()
        {
            if (ActiveLayout != null && ActiveLayout is IScrollInfo)
                ((IScrollInfo) ActiveLayout).PageRight();
            else
                SetHorizontalOffset(HorizontalOffset + ViewportWidth);
        }

        public void PageUp()
        {
            if (ActiveLayout != null && ActiveLayout is IScrollInfo)
                ((IScrollInfo) ActiveLayout).PageUp();
            else
                SetVerticalOffset(VerticalOffset - ViewportHeight);
        }

        public ScrollViewer ScrollOwner
        {
            get => _scrollOwner;
            set
            {
                foreach (var layout in Layouts)
                    if (layout != null && layout is IScrollInfo)
                        ((IScrollInfo) layout).ScrollOwner = value;

                if (_scrollOwner != value)
                {
                    _scrollOwner = value;

                    ResetScrollInfo();
                }
            }
        }

        public void SetHorizontalOffset(double offset)
        {
            offset = ValidateInputOffset(offset, "HorizontalOffset");

            offset = Math.Min(offset, ExtentWidth - ViewportWidth);

            if (!DoubleHelper.AreVirtuallyEqual(offset, _offset.X))
            {
                _offset.X = offset;
                InvalidateMeasure();
            }
        }

        public void SetVerticalOffset(double offset)
        {
            offset = ValidateInputOffset(offset, "VerticalOffset");

            offset = Math.Min(offset, ExtentHeight - ViewportHeight);

            if (!DoubleHelper.AreVirtuallyEqual(offset, _offset.Y))
            {
                _offset.Y = offset;
                InvalidateMeasure();
            }
        }

        public double VerticalOffset
        {
            get
            {
                if (ActiveLayout != null && ActiveLayout is IScrollInfo)
                    return ((IScrollInfo) ActiveLayout).VerticalOffset;

                return _offset.Y;
            }
        }

        public double ViewportHeight
        {
            get
            {
                if (ActiveLayout != null && ActiveLayout is IScrollInfo)
                    return ((IScrollInfo) ActiveLayout).ViewportHeight;

                return _viewport.Height;
            }
        }

        public double ViewportWidth
        {
            get
            {
                if (ActiveLayout != null && ActiveLayout is IScrollInfo)
                    return ((IScrollInfo) ActiveLayout).ViewportWidth;

                return _viewport.Width;
            }
        }

        #endregion

        #region Private Fields

        internal AnimationPanel _currentLayoutPanel;

        private readonly AnimationPanel _defaultLayoutCanvas = new WrapPanel();
        private readonly Collection<SwitchPresenter> _presenters = new();

        private BitVector32 _cacheBits = new(0);

        private bool _allowHorizontal;
        private bool _allowVertical;
        private Vector _computedOffset = new(0.0, 0.0);
        private Size _extent = new(0, 0);
        private Vector _offset = new(0, 0);
        private ScrollViewer _scrollOwner;
        private Size _viewport;

        #endregion
    }
}