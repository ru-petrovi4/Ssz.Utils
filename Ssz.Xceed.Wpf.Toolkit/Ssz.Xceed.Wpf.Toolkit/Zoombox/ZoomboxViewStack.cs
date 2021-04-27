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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using Ssz.Xceed.Wpf.Toolkit.Core;

namespace Ssz.Xceed.Wpf.Toolkit.Zoombox
{
    public sealed class ZoomboxViewStack : Collection<ZoomboxView>, IWeakEventListener
    {
        #region Private Fields

        // to save memory, store bool variables in a bit vector
        private BitVector32 _cacheBits = new(0);

        #endregion

        #region Constructors

        public ZoomboxViewStack(Zoombox zoombox)
        {
            _zoomboxRef = new WeakReference(zoombox);
        }

        #endregion

        #region SelectedView Property

        public ZoomboxView SelectedView
        {
            get
            {
                var currentIndex = Zoombox.ViewStackIndex;
                return currentIndex < 0 || currentIndex > Count - 1 ? ZoomboxView.Empty : this[currentIndex];
            }
        }

        #endregion

        #region AreViewsFromSource Internal Property

        internal bool AreViewsFromSource
        {
            get => _cacheBits[(int) CacheBits.AreViewsFromSource];
            set => _cacheBits[(int) CacheBits.AreViewsFromSource] = value;
        }

        #endregion

        #region IsChangeFromSource Private Property

        private bool IsChangeFromSource
        {
            get => _cacheBits[(int) CacheBits.IsChangeFromSource];
            set => _cacheBits[(int) CacheBits.IsChangeFromSource] = value;
        }

        #endregion

        #region IsMovingViews Private Property

        private bool IsMovingViews
        {
            get => _cacheBits[(int) CacheBits.IsMovingViews];
            set => _cacheBits[(int) CacheBits.IsMovingViews] = value;
        }

        #endregion

        #region IsResettingViews Private Property

        private bool IsResettingViews
        {
            get => _cacheBits[(int) CacheBits.IsResettingViews];
            set => _cacheBits[(int) CacheBits.IsResettingViews] = value;
        }

        #endregion

        #region IsSettingInitialViewAfterClear Private Property

        private bool IsSettingInitialViewAfterClear
        {
            get => _cacheBits[(int) CacheBits.IsSettingInitialViewAfterClear];
            set => _cacheBits[(int) CacheBits.IsSettingInitialViewAfterClear] = value;
        }

        #endregion

        #region IWeakEventListener Members

        public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            if (managerType == typeof(CollectionChangedEventManager))
                OnSourceCollectionChanged(sender, (NotifyCollectionChangedEventArgs) e);
            else
                return false;

            return true;
        }

        #endregion

        internal void ClearViewStackSource()
        {
            if (AreViewsFromSource)
            {
                AreViewsFromSource = false;
                MonitorSource(false);
                Source = null;
                using (new SourceAccess(this))
                {
                    Clear();
                }

                Zoombox.CoerceValue(Zoombox.ViewStackModeProperty);
            }
        }

        internal void PushView(ZoomboxView view)
        {
            // clear the forward stack
            var currentIndex = Zoombox.ViewStackIndex;
            while (Count - 1 > currentIndex) RemoveAt(Count - 1);
            Add(view);
        }

        internal void SetViewStackSource(IEnumerable source)
        {
            if (Source != source)
            {
                MonitorSource(false);
                Source = source;
                MonitorSource(true);
                AreViewsFromSource = true;
                Zoombox.CoerceValue(Zoombox.ViewStackModeProperty);
                ResetViews();
            }
        }

        protected override void ClearItems()
        {
            VerifyStackModification();

            var currentDeleted = Zoombox.CurrentViewIndex >= 0;
            base.ClearItems();
            Zoombox.SetViewStackCount(Count);

            // if resetting the views due to a change in the view source collection, just return
            if (IsResettingViews)
                return;

            if (Zoombox.EffectiveViewStackMode == ZoomboxViewStackMode.Auto && Zoombox.CurrentView != ZoomboxView.Empty)
            {
                IsSettingInitialViewAfterClear = true;
                try
                {
                    Add(Zoombox.CurrentView);
                }
                finally
                {
                    IsSettingInitialViewAfterClear = false;
                }

                Zoombox.ViewStackIndex = 0;
                if (currentDeleted) Zoombox.SetCurrentViewIndex(0);
            }
            else
            {
                Zoombox.ViewStackIndex = -1;
                Zoombox.SetCurrentViewIndex(-1);
            }
        }

        protected override void InsertItem(int index, ZoomboxView view)
        {
            VerifyStackModification();

            if (Zoombox.HasArrangedContentPresenter
                && Zoombox.ViewStackIndex >= index
                && !IsSettingInitialViewAfterClear
                && !IsResettingViews
                && !IsMovingViews)
            {
                var oldUpdatingView = Zoombox.IsUpdatingView;
                Zoombox.IsUpdatingView = true;
                try
                {
                    Zoombox.ViewStackIndex++;
                    if (Zoombox.CurrentViewIndex != -1) Zoombox.SetCurrentViewIndex(Zoombox.CurrentViewIndex + 1);
                }
                finally
                {
                    Zoombox.IsUpdatingView = oldUpdatingView;
                }
            }

            base.InsertItem(index, view);
            Zoombox.SetViewStackCount(Count);
        }

        protected override void RemoveItem(int index)
        {
            VerifyStackModification();

            var currentDeleted = Zoombox.ViewStackIndex == index;
            if (!IsMovingViews)
                // if an item below the current index was deleted 
                // (or if the last item is currently selected and it was deleted),
                // adjust the ViewStackIndex and CurrentViewIndex values
                if (Zoombox.HasArrangedContentPresenter
                    && (Zoombox.ViewStackIndex > index
                        || currentDeleted && Zoombox.ViewStackIndex == Zoombox.ViewStack.Count - 1))
                {
                    // if removing the last item, just clear the stack, which ensures the proper
                    // behavior based on the ViewStackMode
                    if (currentDeleted && Zoombox.ViewStack.Count == 1)
                    {
                        Clear();
                        return;
                    }

                    var oldUpdatingView = Zoombox.IsUpdatingView;
                    Zoombox.IsUpdatingView = true;
                    try
                    {
                        Zoombox.ViewStackIndex--;
                        if (Zoombox.CurrentViewIndex != -1) Zoombox.SetCurrentViewIndex(Zoombox.CurrentViewIndex - 1);
                    }
                    finally
                    {
                        Zoombox.IsUpdatingView = oldUpdatingView;
                    }
                }

            base.RemoveItem(index);

            // if the current view was deleted, we may need to update the view index 
            // (unless a non-stack view is in effect)
            if (!IsMovingViews && currentDeleted && Zoombox.CurrentViewIndex != -1) Zoombox.RefocusView();

            Zoombox.SetViewStackCount(Count);
        }

        protected override void SetItem(int index, ZoomboxView view)
        {
            VerifyStackModification();

            base.SetItem(index, view);

            // if the set item is the current item, update the zoombox
            if (index == Zoombox.CurrentViewIndex) Zoombox.RefocusView();
        }

        private static ZoomboxView GetViewFromSourceItem(object item)
        {
            var view = item is ZoomboxView
                ? item as ZoomboxView
                : ZoomboxViewConverter.Converter.ConvertFrom(item) as ZoomboxView;
            if (view == null)
                throw new InvalidCastException(string.Format(ErrorMessages.GetMessage("UnableToConvertToZoomboxView"),
                    item));

            return view;
        }

        private void InsertViews(int index, IList newItems)
        {
            using (new SourceAccess(this))
            {
                foreach (var item in newItems)
                {
                    var view = GetViewFromSourceItem(item);
                    if (index >= Count)
                        Add(view);
                    else
                        Insert(index, view);
                    index++;
                }
            }
        }

        private void MonitorSource(bool monitor)
        {
            if (Source != null && Source is INotifyCollectionChanged)
            {
                if (monitor)
                    CollectionChangedEventManager.AddListener(Source as INotifyCollectionChanged, this);
                else
                    CollectionChangedEventManager.RemoveListener(Source as INotifyCollectionChanged, this);
            }
        }

        private void MoveViews(int oldIndex, int newIndex, IList movedItems)
        {
            using (new SourceAccess(this))
            {
                var currentIndex = Zoombox.ViewStackIndex;
                var indexAfterMove = currentIndex;

                // adjust the current index, if it was affected by the move
                if (!(oldIndex < currentIndex && newIndex < currentIndex
                      || oldIndex > currentIndex && newIndex > currentIndex))
                {
                    if (currentIndex >= oldIndex && currentIndex < oldIndex + movedItems.Count)
                        indexAfterMove += newIndex - oldIndex;
                    else if (currentIndex >= newIndex) indexAfterMove += movedItems.Count;
                }

                IsMovingViews = true;
                try
                {
                    for (var i = 0; i < movedItems.Count; i++) RemoveAt(oldIndex);
                    for (var i = 0; i < movedItems.Count; i++)
                        Insert(newIndex + i, GetViewFromSourceItem(movedItems[i]));
                    if (indexAfterMove != currentIndex)
                    {
                        Zoombox.ViewStackIndex = indexAfterMove;
                        Zoombox.SetCurrentViewIndex(indexAfterMove);
                    }
                }
                finally
                {
                    IsMovingViews = false;
                }
            }
        }

        private void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    InsertViews(e.NewStartingIndex, e.NewItems);
                    break;

                case NotifyCollectionChangedAction.Move:
                    MoveViews(e.OldStartingIndex, e.NewStartingIndex, e.OldItems);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    RemoveViews(e.OldStartingIndex, e.OldItems);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    ResetViews();
                    break;

                case NotifyCollectionChangedAction.Reset:
                    ResetViews();
                    break;
            }
        }

        private void ResetViews()
        {
            using (new SourceAccess(this))
            {
                var currentIndex = Zoombox.ViewStackIndex;
                IsResettingViews = true;
                try
                {
                    Clear();
                    foreach (var item in Source)
                    {
                        var view = GetViewFromSourceItem(item);
                        Add(view);
                    }

                    currentIndex = Math.Min(Math.Max(0, currentIndex), Count - 1);

                    Zoombox.ViewStackIndex = currentIndex;
                    Zoombox.SetCurrentViewIndex(currentIndex);
                    Zoombox.RefocusView();
                }
                finally
                {
                    IsResettingViews = false;
                }
            }
        }

        private void RemoveViews(int index, IList removedItems)
        {
            using (new SourceAccess(this))
            {
                for (var i = 0; i < removedItems.Count; i++) RemoveAt(index);
            }
        }

        private void VerifyStackModification()
        {
            if (AreViewsFromSource && !IsChangeFromSource)
                throw new InvalidOperationException(ErrorMessages.GetMessage("ViewStackCannotBeManipulatedNow"));
        }

        #region SourceAccess Nested Type

        private sealed class SourceAccess : IDisposable
        {
            private ZoomboxViewStack _viewStack;

            public SourceAccess(ZoomboxViewStack viewStack)
            {
                _viewStack = viewStack;
                _viewStack.IsChangeFromSource = true;
            }

            public void Dispose()
            {
                _viewStack.IsChangeFromSource = false;
                _viewStack = null;
                GC.SuppressFinalize(this);
            }

            ~SourceAccess()
            {
                Dispose();
            }
        }

        #endregion

        #region CacheBits Nested Type

        private enum CacheBits
        {
            AreViewsFromSource = 0x00000001,
            IsChangeFromSource = 0x00000002,
            IsResettingViews = 0x00000004,
            IsMovingViews = 0x00000008,
            IsSettingInitialViewAfterClear = 0x00000010
        }

        #endregion

        #region Source Internal Property

        internal IEnumerable Source { get; private set; }

        // if the view stack is generated by items within the ViewStackSource collection
        // of the Zoombox, then we maintain a strong reference to the source

        #endregion

        #region Zoombox Private Property

        private Zoombox Zoombox => _zoomboxRef.Target as Zoombox;

        // maintain a weak reference to the Zoombox that owns the stack
        private readonly WeakReference _zoomboxRef;

        #endregion
    }
}