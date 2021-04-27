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
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Ssz.Xceed.Wpf.Toolkit.Core;
using Ssz.Xceed.Wpf.Toolkit.Core.Utilities;
using Ssz.Xceed.Wpf.Toolkit.Media.Animation;

namespace Ssz.Xceed.Wpf.Toolkit.Panels
{
    public abstract class AnimationPanel : PanelBase
    {
        #region Constructors

        public AnimationPanel()
        {
#if DEBUG
            var derivedType = GetType();

            var fields = derivedType.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var field in fields)
                if (field.FieldType == typeof(DependencyProperty))
                {
                    var prop = (DependencyProperty) field.GetValue(null);
                    var metaData = prop.GetMetadata(this);

                    if (metaData is FrameworkPropertyMetadata)
                    {
                        var frameworkData = (FrameworkPropertyMetadata) metaData;

                        if (frameworkData.AffectsArrange || frameworkData.AffectsMeasure ||
                            frameworkData.AffectsParentArrange || frameworkData.AffectsParentMeasure)
                            Console.WriteLine("AnimationPanel: " + derivedType.Name + "." + field.Name +
                                              " - You should not set dependency property metadata flags that " +
                                              "affect measure or arrange, instead call AnimationPanel's InvalidateMeasure or " +
                                              "InvalidateArrange directly.");
                    }
                }
#endif

            Loaded += OnLoaded;
        }

        #endregion

        #region DesiredSize Property

        public new Size DesiredSize => _switchParent != null ? _switchParent.DesiredSize : base.DesiredSize;

        #endregion

        #region RenderSize Property

        public new Size RenderSize
        {
            get => _switchParent != null ? _switchParent.RenderSize : base.RenderSize;
            set => base.RenderSize = value;
        }

        #endregion

        #region IsActiveLayout Property

        public bool IsActiveLayout
        {
            get => _cacheBits[(int) CacheBits.IsActiveLayout];
            private set => _cacheBits[(int) CacheBits.IsActiveLayout] = value;
        }

        #endregion

        #region InternalChildren Protected Property

        protected internal new UIElementCollection InternalChildren =>
            _switchParent == null ? base.InternalChildren : _switchParent.ChildrenInternal;

        #endregion

        #region VisualChildrenCount Protected Property

        protected override int VisualChildrenCount =>
            HasLoaded ? InternalChildren.Count + ExitingChildren.Count : base.VisualChildrenCount;

        #endregion

        #region ChildrensParent Protected Property

        protected PanelBase ChildrensParent => _switchParent != null ? (PanelBase) _switchParent : this;

        #endregion

        #region VisualChildrenCountInternal Internal Property

        internal int VisualChildrenCountInternal => VisualChildrenCount;

        #endregion

        #region HasLoaded Internal Property

        internal bool IsRemovingInternalChild
        {
            get => _cacheBits[(int) CacheBits.IsRemovingInternalChild];
            private set => _cacheBits[(int) CacheBits.IsRemovingInternalChild] = value;
        }

        #endregion

        #region EndSwitchOnAnimationCompleted Private Property

        private bool EndSwitchOnAnimationCompleted
        {
            get => _cacheBits[(int) CacheBits.EndSwitchOnAnimationCompleted];
            set => _cacheBits[(int) CacheBits.EndSwitchOnAnimationCompleted] = value;
        }

        #endregion

        #region HasArranged Private Property

        private bool HasArranged
        {
            get => _cacheBits[(int) CacheBits.HasArranged];
            set => _cacheBits[(int) CacheBits.HasArranged] = value;
        }

        #endregion

        #region HasLoaded Private Property

        protected bool HasLoaded
        {
            get => _switchParent == null ? _cacheBits[(int) CacheBits.HasLoaded] : _switchParent.HasLoaded;
            private set => _cacheBits[(int) CacheBits.HasLoaded] = value;
        }

        #endregion

        #region IsSwitchInProgress Private Property

        private bool IsSwitchInProgress
        {
            get => _cacheBits[(int) CacheBits.IsSwitchInProgress];
            set => _cacheBits[(int) CacheBits.IsSwitchInProgress] = value;
        }

        #endregion

        #region ItemsOwner Private Property

        private ItemsControl ItemsOwner =>
            ItemsControl.GetItemsOwner(_switchParent == null ? this : (Panel) _switchParent);

        #endregion

        #region ExitingChildren Private Property

        private List<UIElement> ExitingChildren
        {
            get
            {
                if (_switchParent != null)
                    return _switchParent.ExitingChildren;

                if (_exitingChildren == null) _exitingChildren = new List<UIElement>();

                return _exitingChildren;
            }
        }

        #endregion

        public new void InvalidateArrange()
        {
            if (_switchParent == null)
                base.InvalidateArrange();
            else
                _switchParent.InvalidateArrange();
        }

        public new void InvalidateMeasure()
        {
            if (_switchParent == null)
                base.InvalidateMeasure();
            else
                _switchParent.InvalidateMeasure();
        }

        public new void InvalidateVisual()
        {
            if (_switchParent == null)
                base.InvalidateVisual();
            else
                _switchParent.InvalidateVisual();
        }

        internal void ActivateLayout()
        {
            HasArranged = false;
            IsActiveLayout = true;
            OnSwitchLayoutActivated();
            RaiseSwitchLayoutActivatedEvent();
        }

        internal void BeginChildExit(UIElement child)
        {
            var state = GetChildState(child);
            if (state != null)
            {
                state.Type = AnimationType.Exit;
                state.HasExitBegun = true;

                ExitingChildren.Add(child);

                if (_switchParent != null)
                    _switchParent.AddVisualChildInternal(child);
                else
                    AddVisualChild(child);

                // raise the ChildExiting event only after the child has been re-added to the visual tree
                var ceea = RaiseChildExitingEvent(child, child, GetExitTo(child), state.CurrentPlacement);

                // begin the exit animation, if necessary
                state.Animator = GetEffectiveAnimator(AnimationType.Exit);
                if (state.Animator != null)
                {
                    state.TargetPlacement = ceea.ExitTo.HasValue ? ceea.ExitTo.Value : Rect.Empty;
                    state.BeginTimestamp = DateTime.Now;

                    // decrement the animating count if this child is already animating because the 
                    // ArrangeChild call will increment it again
                    if (state.IsAnimating) AnimatingChildCount--;
                    ArrangeChild(child, state.TargetPlacement);
                }
                else
                {
                    // no animation, so immediately end the exit routine
                    EndChildExit(child, state);
                }
            }
        }

        internal void BeginGrandchildAnimation(FrameworkElement grandchild, Rect currentRect, Rect placementRect)
        {
            var isDone = true;
            object placementArgs;
            var state = new ChildState(currentRect);
            SetChildState(grandchild, state);
            state.Type = AnimationType.Switch;
            state.BeginTimestamp = DateTime.Now;
            state.TargetPlacement = placementRect;
            state.Animator = GetEffectiveAnimator(AnimationType.Template);
            if (state.Animator != null && !state.TargetPlacement.IsEmpty)
            {
                var rate = GetEffectiveAnimationRate(AnimationType.Template);
                state.CurrentPlacement = state.Animator.GetInitialChildPlacement(grandchild, state.CurrentPlacement,
                    state.TargetPlacement, this, ref rate, out placementArgs, out isDone);
                state.AnimationRate = rate;
                state.PlacementArgs = placementArgs;
            }

            state.IsAnimating = !isDone;
            grandchild.Arrange(state.IsAnimating ? state.CurrentPlacement : state.TargetPlacement);
            if (state.IsAnimating)
            {
                _animatingGrandchildren.Add(grandchild);
                AnimatingChildCount++;
            }
            else
            {
                state.CurrentPlacement = state.TargetPlacement;
            }
        }

        internal void DeactivateLayout()
        {
            IsActiveLayout = false;
            AnimatingChildCount = 0;
            OnSwitchLayoutDeactivated();
            RaiseSwitchLayoutDeactivatedEvent();
        }

        internal static UIElement FindAncestorChildOfAnimationPanel(DependencyObject element, out AnimationPanel panel)
        {
            panel = null;
            if (element == null)
                return null;

            var parent = VisualTreeHelper.GetParent(element);
            if (parent == null)
                return null;

            if (parent is AnimationPanel || parent is SwitchPanel)
            {
                panel = parent is SwitchPanel
                    ? (parent as SwitchPanel)._currentLayoutPanel
                    : parent as AnimationPanel;
                return element as UIElement;
            }

            return FindAncestorChildOfAnimationPanel(parent, out panel);
        }

        internal Dictionary<string, Rect> GetNewLocationsBasedOnTargetPlacement(SwitchPresenter presenter,
            UIElement parent)
        {
            var state = GetChildState(parent);

            // if necessary, temporarily arrange the element at its final placement
            var rearrange = state.CurrentPlacement != state.TargetPlacement && state.IsAnimating;
            if (rearrange) parent.Arrange(state.TargetPlacement);

            // now create a dictionary of locations for ID'd elements
            var result = new Dictionary<string, Rect>();
            foreach (var entry in presenter._knownIDs)
            {
                var size = entry.Value.RenderSize;
                Point[] points = {new(), new(size.Width, size.Height)};
                (entry.Value.TransformToAncestor(VisualTreeHelper.GetParent(entry.Value) as Visual) as MatrixTransform)
                    .Matrix.Transform(points);
                result[entry.Key] = new Rect(points[0], points[1]);
            }

            // restore the current placement
            if (rearrange) parent.Arrange(state.CurrentPlacement);
            return result;
        }

        internal Visual GetVisualChildInternal(int index)
        {
            return GetVisualChild(index);
        }

        internal void OnNotifyVisualChildAddedInternal(UIElement child)
        {
            OnNotifyVisualChildAdded(child);
        }

        internal void OnNotifyVisualChildRemovedInternal(UIElement child)
        {
            OnNotifyVisualChildRemoved(child);
        }

        internal Size MeasureChildrenCore(UIElementCollection children, Size constraint)
        {
            _currentChildren = children;
            return MeasureChildrenOverride(_currentChildren, constraint);
        }

        internal Size ArrangeChildrenCore(UIElementCollection children, Size finalSize)
        {
            if (_currentChildren != children) _currentChildren = children;

            // always reset the animating children count at the beginning of an arrange
            AnimatingChildCount = 0;
            _animatingGrandchildren.Clear();

            Size result;
            try
            {
                // determine if this arrange represents a layout switch for a SwitchPanel
                if (!HasArranged && _switchParent != null)
                {
                    IsSwitchInProgress = true;
                    _switchParent.BeginLayoutSwitch();
                }

                // arrange active children
                result = ArrangeChildrenOverride(_currentChildren, finalSize);

                // also arrange exiting children, if necessary
                if (ExitingChildren.Count > 0)
                {
                    AnimatingChildCount += ExitingChildren.Count;
                    UpdateExitingChildren();
                }

                // if this is a layout switch, make sure the switch is ended
                if (IsSwitchInProgress)
                {
                    if (AnimatingChildCount == 0)
                        _switchParent.EndLayoutSwitch();
                    else
                        EndSwitchOnAnimationCompleted = true;
                }
            }
            finally
            {
                HasArranged = true;
                IsSwitchInProgress = false;
            }

            return result;
        }

        internal void OnSwitchParentVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            OnVisualChildrenChanged(visualAdded, visualRemoved);
        }

        protected sealed override Size MeasureOverride(Size constraint)
        {
            return MeasureChildrenCore(InternalChildren, constraint);
        }

        protected abstract Size MeasureChildrenOverride(UIElementCollection children, Size constraint);

        protected sealed override Size ArrangeOverride(Size finalSize)
        {
            return ArrangeChildrenCore(_currentChildren, finalSize);
        }

        protected abstract Size ArrangeChildrenOverride(UIElementCollection children, Size finalSize);

        protected void ArrangeChild(UIElement child, Rect placementRect)
        {
            // Offset in case SwitchPanel is handling scroll.
            if (placementRect.IsEmpty == false && PhysicalScrollOffset.Length > 0)
                placementRect.Offset(-PhysicalScrollOffset);

            // cannot start animations unless the panel is loaded
            if (HasLoaded)
            {
                if (BeginChildAnimation(child, placementRect)) AnimatingChildCount++;
            }
            else
            {
                // just arrange the child if the panel has not yet loaded
                child.Arrange(placementRect);
            }
        }

        protected new void AddVisualChild(Visual child)
        {
            if (_switchParent == null)
                base.AddVisualChild(child);
            else
                _switchParent.AddVisualChildInternal(child);
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index < 0) throw new IndexOutOfRangeException();
            if (index >= InternalChildren.Count)
            {
                var exitIndex = index - InternalChildren.Count;
                if (exitIndex < 0 || exitIndex >= ExitingChildren.Count)
                    throw new IndexOutOfRangeException();

                return ExitingChildren[exitIndex];
            }

            return _switchParent == null ? base.GetVisualChild(index) : _switchParent.GetVisualChildInternal(index);
        }

        protected virtual void OnNotifyVisualChildAdded(UIElement child)
        {
        }

        protected virtual void OnNotifyVisualChildRemoved(UIElement child)
        {
        }

        protected virtual void OnSwitchLayoutActivated()
        {
        }

        protected virtual void OnSwitchLayoutDeactivated()
        {
        }

        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            if (!IsRemovingInternalChild)
                if (visualRemoved is UIElement && visualRemoved != null)
                {
                    IsRemovingInternalChild = true;
                    try
                    {
                        BeginChildExit(visualRemoved as UIElement);
                    }
                    finally
                    {
                        IsRemovingInternalChild = false;
                    }
                }

            if (_switchParent == null)
            {
                // The OnNotifyChildAdded/Removed methods get called for all animation panels within a 
                // SwitchPanel.Layouts collection, regardless of whether they are the active layout 
                // for the SwitchPanel.  Here, we also ensure that the methods are called for standalone panels.
                if (visualAdded is UIElement)
                    OnNotifyVisualChildAdded(visualAdded as UIElement);
                else if (visualRemoved is UIElement) OnNotifyVisualChildRemoved(visualRemoved as UIElement);
                base.OnVisualChildrenChanged(visualAdded, visualRemoved);
            }
            else
            {
                _switchParent.OnVisualChildrenChangedInternal(visualAdded, visualRemoved);
            }
        }

        protected new void RemoveVisualChild(Visual child)
        {
            if (_switchParent == null)
                base.RemoveVisualChild(child);
            else
                _switchParent.RemoveVisualChildInternal(child);
        }

        protected int FindChildFromVisual(Visual vis)
        {
            var index = -1;

            DependencyObject parent = vis;
            DependencyObject child = null;

            do
            {
                child = parent;
                parent = VisualTreeHelper.GetParent(child);
            } while (parent != null && parent != ChildrensParent);

            if (parent == ChildrensParent) index = ChildrensParent.Children.IndexOf((UIElement) child);

            return index;
        }

        private bool BeginChildAnimation(UIElement child, Rect placementRect)
        {
            // a private attached property is used to hold the information needed 
            // to calculate the location of items on subsequent frame refreshes
            bool newStateCreated;
            var state = EnsureChildState(child, placementRect, out newStateCreated);
            if (state.HasEnterCompleted)
            {
                if (state.Type != AnimationType.Exit)
                {
                    state.BeginTimestamp = DateTime.Now;
                    state.Type = IsSwitchInProgress ? AnimationType.Switch : AnimationType.Layout;
                    state.TargetPlacement = placementRect;
                }
            }
            else
            {
                // if the child is in the middle of an enter animation, we
                // still need to update the placement rect
                state.BeginTimestamp = DateTime.Now;
                state.TargetPlacement = placementRect;
            }

            if (!state.HasExitCompleted)
            {
                var isDone = true;
                object placementArgs;
                if (state.Type != AnimationType.Enter) state.Animator = GetEffectiveAnimator(state.Type);
                if (state.Animator != null && !state.TargetPlacement.IsEmpty)
                {
                    var rate = GetEffectiveAnimationRate(state.Type);
                    state.CurrentPlacement = state.Animator.GetInitialChildPlacement(
                        child, state.CurrentPlacement, state.TargetPlacement, this,
                        ref rate, out placementArgs, out isDone);
                    state.AnimationRate = rate;
                    state.PlacementArgs = placementArgs;
                }

                state.IsAnimating = !isDone;
                if (!state.IsAnimating) state.CurrentPlacement = state.TargetPlacement;
            }

            // JZ this might not be needed nice the OnRender will arrange
            if (state.IsAnimating == false) UpdateTrueArrange(child, state);

            return state.IsAnimating;
        }

        private void BeginChildEnter(UIElement child, ChildState state)
        {
            state.Type = AnimationType.Enter;

            // raise the ChildEntering event
            var ceea = RaiseChildEnteringEvent(child,
                child, GetEnterFrom(child), state.CurrentPlacement);

            // begin the enter animation, if necessary
            state.Animator = GetEffectiveAnimator(AnimationType.Enter);
            if (state.Animator != null && ceea.EnterFrom.HasValue)
            {
                state.CurrentPlacement = ceea.EnterFrom.Value;
                state.BeginTimestamp = DateTime.Now;
            }
        }

        private void EndChildEnter(UIElement child, ChildState state)
        {
            // raise the ChildExited event
            state.HasEnterCompleted = true;
            RaiseChildEnteredEvent(child, child, state.TargetPlacement);
        }

        private void EndChildExit(UIElement child, ChildState state)
        {
            // raise the ChildExited event
            state.HasExitCompleted = true;
            RaiseChildExitedEvent(child, child);

            // remove the visual child relationship
            if (ExitingChildren.Contains(child))
            {
                IsRemovingInternalChild = true;
                try
                {
                    if (_switchParent != null)
                        _switchParent.RemoveVisualChildInternal(child);
                    else
                        RemoveVisualChild(child);
                }
                finally
                {
                    IsRemovingInternalChild = false;
                }

                ExitingChildren.Remove(child);
            }

            child.ClearValue(ChildStatePropertyKey);
        }

        private ChildState EnsureChildState(UIElement child, Rect placementRect, out bool newStateCreated)
        {
            newStateCreated = false;
            var state = GetChildState(child);
            if (state == null)
            {
                // if this is null, it's because this is the first time that
                // the object has been arranged
                state = new ChildState(placementRect);
                SetChildState(child, state);
                BeginChildEnter(child, state);
                newStateCreated = true;
            }

            return state;
        }

        internal AnimationRate GetEffectiveAnimationRate(AnimationType animationType)
        {
            var result = _switchParent == null ? DefaultAnimationRate : _switchParent.DefaultAnimationRate;
            switch (animationType)
            {
                case AnimationType.Enter:
                    if (EnterAnimationRate != AnimationRate.Default)
                        result = EnterAnimationRate;
                    else if (_switchParent != null && _switchParent.EnterAnimationRate != AnimationRate.Default)
                        result = _switchParent.EnterAnimationRate;
                    break;

                case AnimationType.Exit:
                    if (ExitAnimationRate != AnimationRate.Default)
                        result = ExitAnimationRate;
                    else if (_switchParent != null && _switchParent.ExitAnimationRate != AnimationRate.Default)
                        result = _switchParent.ExitAnimationRate;
                    break;

                case AnimationType.Layout:
                    if (LayoutAnimationRate != AnimationRate.Default)
                        result = LayoutAnimationRate;
                    else if (_switchParent != null && _switchParent.LayoutAnimationRate != AnimationRate.Default)
                        result = _switchParent.LayoutAnimationRate;
                    break;

                case AnimationType.Switch:
                    if (SwitchAnimationRate != AnimationRate.Default)
                        result = SwitchAnimationRate;
                    else if (_switchParent != null && _switchParent.SwitchAnimationRate != AnimationRate.Default)
                        result = _switchParent.SwitchAnimationRate;
                    break;

                case AnimationType.Template:
                    if (TemplateAnimationRate != AnimationRate.Default)
                        result = TemplateAnimationRate;
                    else if (_switchParent != null && _switchParent.TemplateAnimationRate != AnimationRate.Default)
                        result = _switchParent.TemplateAnimationRate;
                    break;
            }

            return result;
        }

        private IterativeAnimator GetEffectiveAnimator(AnimationType animationType)
        {
            var result = _switchParent == null ? DefaultAnimator : _switchParent.DefaultAnimator;
            switch (animationType)
            {
                case AnimationType.Enter:
                    if (EnterAnimator != IterativeAnimator.Default || _switchParent != null &&
                        _switchParent.EnterAnimator != IterativeAnimator.Default)
                        result = EnterAnimator == IterativeAnimator.Default
                            ? _switchParent.EnterAnimator
                            : EnterAnimator;
                    break;

                case AnimationType.Exit:
                    if (ExitAnimator != IterativeAnimator.Default || _switchParent != null &&
                        _switchParent.ExitAnimator != IterativeAnimator.Default)
                        result = ExitAnimator == IterativeAnimator.Default ? _switchParent.ExitAnimator : ExitAnimator;
                    break;

                case AnimationType.Layout:
                    if (LayoutAnimator != IterativeAnimator.Default || _switchParent != null &&
                        _switchParent.LayoutAnimator != IterativeAnimator.Default)
                        result = LayoutAnimator == IterativeAnimator.Default
                            ? _switchParent.LayoutAnimator
                            : LayoutAnimator;
                    break;

                case AnimationType.Switch:
                    if (_switchParent != null && !_switchParent.AreLayoutSwitchesAnimated)
                    {
                        result = null;
                    }
                    else
                    {
                        if (SwitchAnimator != IterativeAnimator.Default ||
                            _switchParent.SwitchAnimator != IterativeAnimator.Default)
                            result = SwitchAnimator == IterativeAnimator.Default
                                ? _switchParent.SwitchAnimator
                                : SwitchAnimator;
                    }

                    break;

                case AnimationType.Template:
                    if (TemplateAnimator != IterativeAnimator.Default || _switchParent != null &&
                        _switchParent.TemplateAnimator != IterativeAnimator.Default)
                        result = TemplateAnimator == IterativeAnimator.Default
                            ? _switchParent.TemplateAnimator
                            : TemplateAnimator;
                    break;
            }

            return result;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            HasLoaded = true;

            // invalidate arrange to give enter animations a chance to run
            InvalidateArrange();
        }

        private void OnRendering(object sender, EventArgs e)
        {
            if (!IsActiveLayout)
                return;

            if (_currentChildren != null)
                foreach (UIElement child in _currentChildren)
                {
                    if (child == null)
                        continue;

                    var state = GetChildState(child);
                    if (state != null)
                    {
                        var t = DateTime.Now.Subtract(state.BeginTimestamp);
                        if (state.IsAnimating)
                        {
                            bool isDone;
                            state.CurrentPlacement = state.Animator.GetNextChildPlacement(child, t,
                                state.CurrentPlacement,
                                state.TargetPlacement, this, state.AnimationRate, ref state.PlacementArgs, out isDone);
                            state.IsAnimating = !isDone;
                            UpdateTrueArrange(child, state);
                            if (!state.IsAnimating) AnimatingChildCount--;
                        }
                    }
                }

            foreach (var grandchild in _animatingGrandchildren)
            {
                var state = GetChildState(grandchild);
                if (state != null && state.IsAnimating)
                {
                    var t = DateTime.Now.Subtract(state.BeginTimestamp);
                    bool isDone;
                    state.CurrentPlacement = state.Animator.GetNextChildPlacement(grandchild, t, state.CurrentPlacement,
                        state.TargetPlacement, this, state.AnimationRate, ref state.PlacementArgs, out isDone);
                    state.IsAnimating = !isDone;
                    var rect = state.IsAnimating ? state.CurrentPlacement : state.TargetPlacement;
                    grandchild.Arrange(rect);
                    if (!state.IsAnimating) AnimatingChildCount--;
                }
            }

            UpdateExitingChildren();

            if (AnimatingChildCount == 0) _animatingGrandchildren.Clear();
        }

        private void UpdateExitingChildren()
        {
            if (ExitingChildren.Count > 0)
            {
                var exitingChildren = new List<UIElement>(ExitingChildren);
                foreach (var child in exitingChildren)
                {
                    if (child == null)
                        continue;

                    var state = GetChildState(child);
                    if (state != null)
                    {
                        var t = DateTime.Now.Subtract(state.BeginTimestamp);
                        if (state.IsAnimating)
                        {
                            bool isDone;
                            state.CurrentPlacement = state.Animator.GetNextChildPlacement(child, t,
                                state.CurrentPlacement,
                                state.TargetPlacement, this, state.AnimationRate, ref state.PlacementArgs, out isDone);
                            state.IsAnimating = !isDone;
                            UpdateTrueArrange(child, state);
                            if (!state.IsAnimating) AnimatingChildCount--;
                        }
                    }
                }
            }
        }

        private void UpdateTrueArrange(UIElement child, ChildState state)
        {
            if (!state.TargetPlacement.IsEmpty)
                child.Arrange(state.IsAnimating && state.Animator != null
                    ? state.CurrentPlacement
                    : state.TargetPlacement);

            // if the child is done entering, complete the enter routine
            if (!state.IsAnimating && !state.HasEnterCompleted) EndChildEnter(child, state);

            // if the child is done exiting, complete the exit routine
            if (!state.IsAnimating && state.HasExitBegun) EndChildExit(child, state);
        }

        #region ChildState Nested Type

        private sealed class ChildState
        {
            private BitVector32 _cacheBits = new(0);
            public AnimationRate AnimationRate;
            public IterativeAnimator Animator;
            public DateTime BeginTimestamp;
            public Rect CurrentPlacement;
            public object PlacementArgs;
            public Rect TargetPlacement;

            public AnimationType Type;

            public ChildState(Rect currentRect)
            {
                CurrentPlacement = currentRect;
                TargetPlacement = currentRect;
                BeginTimestamp = DateTime.Now;
            }

            public bool HasEnterCompleted
            {
                get => _cacheBits[(int) CacheBits.HasEnterCompleted];
                set => _cacheBits[(int) CacheBits.HasEnterCompleted] = value;
            }

            public bool HasExitBegun
            {
                get => _cacheBits[(int) CacheBits.HasExitBegun];
                set => _cacheBits[(int) CacheBits.HasExitBegun] = value;
            }

            public bool HasExitCompleted
            {
                get => _cacheBits[(int) CacheBits.HasExitCompleted];
                set => _cacheBits[(int) CacheBits.HasExitCompleted] = value;
            }

            public bool IsAnimating
            {
                get => _cacheBits[(int) CacheBits.IsAnimating];
                set => _cacheBits[(int) CacheBits.IsAnimating] = value;
            }

            private enum CacheBits
            {
                IsAnimating = 0x00000001,
                HasEnterCompleted = 0x00000002,
                HasExitBegun = 0x00000004,
                HasExitCompleted = 0x00000008
            }
        }

        #endregion

        #region AnimationType Nested Type

        internal enum AnimationType
        {
            Enter,
            Exit,
            Layout,
            Switch,
            Template
        }

        #endregion

        #region CacheBits Nested Type

        private enum CacheBits
        {
            IsActiveLayout = 0x00000001,
            IsSwitchInProgress = 0x00000002,
            EndSwitchOnAnimationCompleted = 0x00000010,
            IsRemovingInternalChild = 0x00000020,
            HasLoaded = 0x00000040,
            HasArranged = 0x00000080
        }

        #endregion

        #region ChildState Private Property

        private static readonly DependencyPropertyKey ChildStatePropertyKey =
            DependencyProperty.RegisterAttachedReadOnly("ChildState", typeof(ChildState), typeof(AnimationPanel),
                new FrameworkPropertyMetadata(null));

        private static ChildState GetChildState(DependencyObject d)
        {
            return (ChildState) d.GetValue(ChildStatePropertyKey.DependencyProperty);
        }

        private static void SetChildState(DependencyObject d, ChildState value)
        {
            d.SetValue(ChildStatePropertyKey, value);
        }

        #endregion

        #region DefaultAnimationRate Property

        public static readonly DependencyProperty DefaultAnimationRateProperty =
            DependencyProperty.Register("DefaultAnimationRate", typeof(AnimationRate), typeof(AnimationPanel),
                new FrameworkPropertyMetadata((AnimationRate) 1d),
                ValidateDefaultAnimationRate);

        public AnimationRate DefaultAnimationRate
        {
            get => (AnimationRate) GetValue(DefaultAnimationRateProperty);
            set => SetValue(DefaultAnimationRateProperty, value);
        }

        private static bool ValidateDefaultAnimationRate(object value)
        {
            if ((AnimationRate) value == AnimationRate.Default)
                throw new ArgumentException(
                    ErrorMessages.GetMessage(ErrorMessages.DefaultAnimationRateAnimationRateDefault));

            return true;
        }

        #endregion

        #region DefaultAnimator Property

        public static readonly DependencyProperty DefaultAnimatorProperty =
            DependencyProperty.Register("DefaultAnimator", typeof(IterativeAnimator), typeof(AnimationPanel),
                new FrameworkPropertyMetadata(Animators.Linear),
                ValidateDefaultAnimator);

        public IterativeAnimator DefaultAnimator
        {
            get => (IterativeAnimator) GetValue(DefaultAnimatorProperty);
            set => SetValue(DefaultAnimatorProperty, value);
        }

        private static bool ValidateDefaultAnimator(object value)
        {
            if (value == IterativeAnimator.Default)
                throw new ArgumentException(
                    ErrorMessages.GetMessage(ErrorMessages.DefaultAnimatorIterativeAnimationDefault));

            return true;
        }

        #endregion

        #region EnterAnimationRate Property

        public static readonly DependencyProperty EnterAnimationRateProperty =
            DependencyProperty.Register("EnterAnimationRate", typeof(AnimationRate), typeof(AnimationPanel),
                new FrameworkPropertyMetadata(AnimationRate.Default));

        public AnimationRate EnterAnimationRate
        {
            get => (AnimationRate) GetValue(EnterAnimationRateProperty);
            set => SetValue(EnterAnimationRateProperty, value);
        }

        #endregion

        #region EnterAnimator Property

        public static readonly DependencyProperty EnterAnimatorProperty =
            DependencyProperty.Register("EnterAnimator", typeof(IterativeAnimator), typeof(AnimationPanel),
                new FrameworkPropertyMetadata(IterativeAnimator.Default));

        public IterativeAnimator EnterAnimator
        {
            get => (IterativeAnimator) GetValue(EnterAnimatorProperty);
            set => SetValue(EnterAnimatorProperty, value);
        }

        #endregion

        #region EnterFrom Attached Property

        public static readonly DependencyProperty EnterFromProperty =
            DependencyProperty.RegisterAttached("EnterFrom", typeof(Rect?), typeof(AnimationPanel),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        public static Rect? GetEnterFrom(DependencyObject d)
        {
            return (Rect?) d.GetValue(EnterFromProperty);
        }

        public static void SetEnterFrom(DependencyObject d, Rect? value)
        {
            d.SetValue(EnterFromProperty, value);
        }

        #endregion

        #region ExitAnimationRate Property

        public static readonly DependencyProperty ExitAnimationRateProperty =
            DependencyProperty.Register("ExitAnimationRate", typeof(AnimationRate), typeof(AnimationPanel),
                new FrameworkPropertyMetadata(AnimationRate.Default));

        public AnimationRate ExitAnimationRate
        {
            get => (AnimationRate) GetValue(ExitAnimationRateProperty);
            set => SetValue(ExitAnimationRateProperty, value);
        }

        #endregion

        #region ExitAnimator Property

        public static readonly DependencyProperty ExitAnimatorProperty =
            DependencyProperty.Register("ExitAnimator", typeof(IterativeAnimator), typeof(AnimationPanel),
                new FrameworkPropertyMetadata(IterativeAnimator.Default));

        public IterativeAnimator ExitAnimator
        {
            get => (IterativeAnimator) GetValue(ExitAnimatorProperty);
            set => SetValue(ExitAnimatorProperty, value);
        }

        #endregion

        #region ExitTo Attached Property

        public static readonly DependencyProperty ExitToProperty =
            DependencyProperty.RegisterAttached("ExitTo", typeof(Rect?), typeof(AnimationPanel),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        public static Rect? GetExitTo(DependencyObject d)
        {
            return (Rect?) d.GetValue(ExitToProperty);
        }

        public static void SetExitTo(DependencyObject d, Rect? value)
        {
            d.SetValue(ExitToProperty, value);
        }

        #endregion

        #region LayoutAnimationRate Property

        public static readonly DependencyProperty LayoutAnimationRateProperty =
            DependencyProperty.Register("LayoutAnimationRate", typeof(AnimationRate), typeof(AnimationPanel),
                new FrameworkPropertyMetadata(AnimationRate.Default));

        public AnimationRate LayoutAnimationRate
        {
            get => (AnimationRate) GetValue(LayoutAnimationRateProperty);
            set => SetValue(LayoutAnimationRateProperty, value);
        }

        #endregion

        #region LayoutAnimator Property

        public static readonly DependencyProperty LayoutAnimatorProperty =
            DependencyProperty.Register("LayoutAnimator", typeof(IterativeAnimator), typeof(AnimationPanel),
                new FrameworkPropertyMetadata(IterativeAnimator.Default));

        public IterativeAnimator LayoutAnimator
        {
            get => (IterativeAnimator) GetValue(LayoutAnimatorProperty);
            set => SetValue(LayoutAnimatorProperty, value);
        }

        #endregion

        #region SwitchAnimationRate Property

        public static readonly DependencyProperty SwitchAnimationRateProperty =
            DependencyProperty.Register("SwitchAnimationRate", typeof(AnimationRate), typeof(AnimationPanel),
                new FrameworkPropertyMetadata(AnimationRate.Default));

        public AnimationRate SwitchAnimationRate
        {
            get => (AnimationRate) GetValue(SwitchAnimationRateProperty);
            set => SetValue(SwitchAnimationRateProperty, value);
        }

        #endregion

        #region SwitchAnimator Property

        public static readonly DependencyProperty SwitchAnimatorProperty =
            DependencyProperty.Register("SwitchAnimator", typeof(IterativeAnimator), typeof(AnimationPanel),
                new FrameworkPropertyMetadata(IterativeAnimator.Default));

        public IterativeAnimator SwitchAnimator
        {
            get => (IterativeAnimator) GetValue(SwitchAnimatorProperty);
            set => SetValue(SwitchAnimatorProperty, value);
        }

        #endregion

        #region SwitchParent Property

        private static readonly DependencyPropertyKey SwitchParentPropertyKey =
            DependencyProperty.RegisterReadOnly("SwitchParent", typeof(SwitchPanel), typeof(AnimationPanel),
                new FrameworkPropertyMetadata(null,
                    OnSwitchParentChanged));

        public static readonly DependencyProperty SwitchParentProperty = SwitchParentPropertyKey.DependencyProperty;

        public SwitchPanel SwitchParent => (SwitchPanel) GetValue(SwitchParentProperty);

        protected internal void SetSwitchParent(SwitchPanel value)
        {
            SetValue(SwitchParentPropertyKey, value);
        }

        private static void OnSwitchParentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AnimationPanel) d).OnSwitchParentChanged(e);
        }

        protected virtual void OnSwitchParentChanged(DependencyPropertyChangedEventArgs e)
        {
            _switchParent = e.NewValue as SwitchPanel;
        }

        #endregion

        #region SwitchTemplate Property

        public static readonly DependencyProperty SwitchTemplateProperty =
            DependencyProperty.Register("SwitchTemplate", typeof(DataTemplate), typeof(AnimationPanel),
                new FrameworkPropertyMetadata(null,
                    OnSwitchTemplateChanged));

        public DataTemplate SwitchTemplate
        {
            get => (DataTemplate) GetValue(SwitchTemplateProperty);
            set => SetValue(SwitchTemplateProperty, value);
        }

        private static void OnSwitchTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AnimationPanel) d).OnSwitchTemplateChanged(e);
        }

        protected virtual void OnSwitchTemplateChanged(DependencyPropertyChangedEventArgs e)
        {
            if (_switchParent != null && _switchParent.ActiveLayout == this) _switchParent.UpdateSwitchTemplate();
        }

        #endregion

        #region TemplateAnimationRate Property

        public static readonly DependencyProperty TemplateAnimationRateProperty =
            DependencyProperty.Register("TemplateAnimationRate", typeof(AnimationRate), typeof(AnimationPanel),
                new FrameworkPropertyMetadata(AnimationRate.Default));

        public AnimationRate TemplateAnimationRate
        {
            get => (AnimationRate) GetValue(TemplateAnimationRateProperty);
            set => SetValue(TemplateAnimationRateProperty, value);
        }

        #endregion

        #region TemplateAnimator Property

        public static readonly DependencyProperty TemplateAnimatorProperty =
            DependencyProperty.Register("TemplateAnimator", typeof(IterativeAnimator), typeof(AnimationPanel),
                new FrameworkPropertyMetadata(IterativeAnimator.Default));

        public IterativeAnimator TemplateAnimator
        {
            get => (IterativeAnimator) GetValue(TemplateAnimatorProperty);
            set => SetValue(TemplateAnimatorProperty, value);
        }

        #endregion

        #region PhysicalScrollOffset Internal Property

        internal Vector PhysicalScrollOffset { get; set; }

        #endregion

        #region AnimatingChildCount Private Property

        private int AnimatingChildCount
        {
            get => _animatingChildCount;
            set
            {
                // start the animation pump if the value goes positive
                if (_animatingChildCount == 0 && value > 0)
                {
                    CompositionTarget.Rendering += OnRendering;
                    RaiseAnimationBegunEvent();
                }

                // stop the animation pump if the value goes to 0
                if (_animatingChildCount != 0 && value == 0)
                {
                    if (EndSwitchOnAnimationCompleted && _switchParent != null)
                    {
                        EndSwitchOnAnimationCompleted = false;
                        _switchParent.EndLayoutSwitch();
                    }

                    CompositionTarget.Rendering -= OnRendering;
                    RaiseAnimationCompletedEvent();
                }

                _animatingChildCount = value;
            }
        }

        private int _animatingChildCount;

        #endregion

        #region AnimationBegun Event

        public static readonly RoutedEvent AnimationBegunEvent =
            EventManager.RegisterRoutedEvent("AnimationBegun", RoutingStrategy.Bubble, typeof(RoutedEventHandler),
                typeof(AnimationPanel));

        public event RoutedEventHandler AnimationBegun
        {
            add => AddHandler(AnimationBegunEvent, value);
            remove => RemoveHandler(AnimationBegunEvent, value);
        }

        protected RoutedEventArgs RaiseAnimationBegunEvent()
        {
            return RaiseAnimationBegunEvent(_switchParent != null ? _switchParent : (UIElement) this);
        }

        private static RoutedEventArgs RaiseAnimationBegunEvent(UIElement target)
        {
            if (target == null)
                return null;

            var args = new RoutedEventArgs();
            args.RoutedEvent = AnimationBegunEvent;
            RoutedEventHelper.RaiseEvent(target, args);
            return args;
        }

        #endregion

        #region AnimationCompleted Event

        public static readonly RoutedEvent AnimationCompletedEvent =
            EventManager.RegisterRoutedEvent("AnimationCompleted", RoutingStrategy.Bubble, typeof(RoutedEventHandler),
                typeof(AnimationPanel));

        public event RoutedEventHandler AnimationCompleted
        {
            add => AddHandler(AnimationCompletedEvent, value);
            remove => RemoveHandler(AnimationCompletedEvent, value);
        }

        protected RoutedEventArgs RaiseAnimationCompletedEvent()
        {
            return RaiseAnimationCompletedEvent(_switchParent != null ? _switchParent : (UIElement) this);
        }

        private static RoutedEventArgs RaiseAnimationCompletedEvent(UIElement target)
        {
            if (target == null)
                return null;

            var args = new RoutedEventArgs();
            args.RoutedEvent = AnimationCompletedEvent;
            RoutedEventHelper.RaiseEvent(target, args);
            return args;
        }

        #endregion

        #region ChildEntered Event

        public static readonly RoutedEvent ChildEnteredEvent =
            EventManager.RegisterRoutedEvent("ChildEntered", RoutingStrategy.Bubble, typeof(ChildEnteredEventHandler),
                typeof(AnimationPanel));

        public event ChildEnteredEventHandler ChildEntered
        {
            add => AddHandler(ChildEnteredEvent, value);
            remove => RemoveHandler(ChildEnteredEvent, value);
        }

        protected ChildEnteredEventArgs RaiseChildEnteredEvent(UIElement child, Rect arrangeRect)
        {
            return RaiseChildEnteredEvent(this, child, arrangeRect);
        }

        internal static ChildEnteredEventArgs RaiseChildEnteredEvent(UIElement target, UIElement child,
            Rect arrangeRect)
        {
            if (target == null)
                return null;

            var args = new ChildEnteredEventArgs(child, arrangeRect);
            args.RoutedEvent = ChildEnteredEvent;
            RoutedEventHelper.RaiseEvent(target, args);
            return args;
        }

        #endregion

        #region ChildEntering Event

        public static readonly RoutedEvent ChildEnteringEvent =
            EventManager.RegisterRoutedEvent("ChildEntering", RoutingStrategy.Bubble, typeof(ChildEnteringEventHandler),
                typeof(AnimationPanel));

        public event ChildEnteringEventHandler ChildEntering
        {
            add => AddHandler(ChildEnteringEvent, value);
            remove => RemoveHandler(ChildEnteringEvent, value);
        }

        protected ChildEnteringEventArgs RaiseChildEnteringEvent(UIElement child, Rect? EnterFrom, Rect ArrangeRect)
        {
            return RaiseChildEnteringEvent(this, child, EnterFrom, ArrangeRect);
        }

        private static ChildEnteringEventArgs RaiseChildEnteringEvent(UIElement target, UIElement child,
            Rect? EnterFrom, Rect ArrangeRect)
        {
            if (target == null)
                return null;

            var args = new ChildEnteringEventArgs(child, EnterFrom, ArrangeRect);
            args.RoutedEvent = ChildEnteringEvent;
            RoutedEventHelper.RaiseEvent(target, args);
            return args;
        }

        #endregion

        #region ChildExited Event

        public static readonly RoutedEvent ChildExitedEvent =
            EventManager.RegisterRoutedEvent("ChildExited", RoutingStrategy.Bubble, typeof(ChildExitedEventHandler),
                typeof(AnimationPanel));

        public event ChildExitedEventHandler ChildExited
        {
            add => AddHandler(ChildExitedEvent, value);
            remove => RemoveHandler(ChildExitedEvent, value);
        }

        protected ChildExitedEventArgs RaiseChildExitedEvent(UIElement child)
        {
            return RaiseChildExitedEvent(this, child);
        }

        private static ChildExitedEventArgs RaiseChildExitedEvent(UIElement target, UIElement child)
        {
            if (target == null)
                return null;

            var args = new ChildExitedEventArgs(child);
            args.RoutedEvent = ChildExitedEvent;
            RoutedEventHelper.RaiseEvent(target, args);
            return args;
        }

        #endregion

        #region ChildExiting Event

        public static readonly RoutedEvent ChildExitingEvent =
            EventManager.RegisterRoutedEvent("ChildExiting", RoutingStrategy.Bubble, typeof(ChildExitingEventHandler),
                typeof(AnimationPanel));

        public event ChildExitingEventHandler ChildExiting
        {
            add => AddHandler(ChildExitingEvent, value);
            remove => RemoveHandler(ChildExitingEvent, value);
        }

        protected ChildExitingEventArgs RaiseChildExitingEvent(UIElement child, Rect? exitTo, Rect arrangeRect)
        {
            return RaiseChildExitingEvent(this, child, exitTo, arrangeRect);
        }

        private static ChildExitingEventArgs RaiseChildExitingEvent(UIElement target, UIElement child, Rect? exitTo,
            Rect arrangeRect)
        {
            if (target == null)
                return null;

            var args = new ChildExitingEventArgs(child, exitTo, arrangeRect);
            args.RoutedEvent = ChildExitingEvent;
            RoutedEventHelper.RaiseEvent(target, args);
            return args;
        }

        #endregion

        #region SwitchLayoutActivated Event

        public static readonly RoutedEvent SwitchLayoutActivatedEvent =
            EventManager.RegisterRoutedEvent("SwitchLayoutActivated", RoutingStrategy.Direct,
                typeof(RoutedEventHandler), typeof(AnimationPanel));

        public event RoutedEventHandler SwitchLayoutActivated
        {
            add => AddHandler(SwitchLayoutActivatedEvent, value);
            remove => RemoveHandler(SwitchLayoutActivatedEvent, value);
        }

        protected RoutedEventArgs RaiseSwitchLayoutActivatedEvent()
        {
            return RaiseSwitchLayoutActivatedEvent(this);
        }

        internal static RoutedEventArgs RaiseSwitchLayoutActivatedEvent(UIElement target)
        {
            if (target == null)
                return null;

            var args = new RoutedEventArgs();
            args.RoutedEvent = SwitchLayoutActivatedEvent;
            RoutedEventHelper.RaiseEvent(target, args);
            return args;
        }

        #endregion

        #region SwitchLayoutDeactivated Event

        public static readonly RoutedEvent SwitchLayoutDeactivatedEvent =
            EventManager.RegisterRoutedEvent("SwitchLayoutDeactivated", RoutingStrategy.Direct,
                typeof(RoutedEventHandler), typeof(AnimationPanel));

        public event RoutedEventHandler SwitchLayoutDeactivated
        {
            add => AddHandler(SwitchLayoutDeactivatedEvent, value);
            remove => RemoveHandler(SwitchLayoutDeactivatedEvent, value);
        }

        protected RoutedEventArgs RaiseSwitchLayoutDeactivatedEvent()
        {
            return RaiseSwitchLayoutDeactivatedEvent(this);
        }

        internal static RoutedEventArgs RaiseSwitchLayoutDeactivatedEvent(UIElement target)
        {
            if (target == null)
                return null;

            var args = new RoutedEventArgs();
            args.RoutedEvent = SwitchLayoutDeactivatedEvent;
            RoutedEventHelper.RaiseEvent(target, args);
            return args;
        }

        #endregion

        #region Private Fields

        private UIElementCollection _currentChildren;
        private readonly Collection<FrameworkElement> _animatingGrandchildren = new();
        private SwitchPanel _switchParent;
        private List<UIElement> _exitingChildren;
        private BitVector32 _cacheBits = new(1);

        #endregion
    }
}