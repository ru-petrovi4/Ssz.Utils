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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Ssz.Xceed.Wpf.AvalonDock.Layout;

namespace Ssz.Xceed.Wpf.AvalonDock.Controls
{
    public class LayoutAnchorSideControl : Control, ILayoutControl
    {
        #region Members

        private readonly LayoutAnchorSide _model;

        #endregion

        #region Constructors

        static LayoutAnchorSideControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LayoutAnchorSideControl),
                new FrameworkPropertyMetadata(typeof(LayoutAnchorSideControl)));
        }

        internal LayoutAnchorSideControl(LayoutAnchorSide model)
        {
            if (model is null)
                throw new ArgumentNullException("model");


            _model = model;

            CreateChildrenViews();

            _model.Children.CollectionChanged += (s, e) => OnModelChildrenCollectionChanged(e);

            UpdateSide();
        }

        #endregion

        #region Properties

        #region Model

        public ILayoutElement Model => _model;

        #endregion

        #region Children

        public ObservableCollection<LayoutAnchorGroupControl> Children { get; } = new();

        #endregion

        #region IsLeftSide

        /// <summary>
        ///     IsLeftSide Read-Only Dependency Property
        /// </summary>
        private static readonly DependencyPropertyKey IsLeftSidePropertyKey = DependencyProperty.RegisterReadOnly(
            "IsLeftSide", typeof(bool), typeof(LayoutAnchorSideControl),
            new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsLeftSideProperty = IsLeftSidePropertyKey.DependencyProperty;

        /// <summary>
        ///     Gets the IsLeftSide property.  This dependency property
        ///     indicates this control is anchored to left side.
        /// </summary>
        public bool IsLeftSide => (bool) GetValue(IsLeftSideProperty);

        /// <summary>
        ///     Provides a secure method for setting the IsLeftSide property.
        ///     This dependency property indicates this control is anchored to left side.
        /// </summary>
        /// <param name="value">The new value for the property.</param>
        protected void SetIsLeftSide(bool value)
        {
            SetValue(IsLeftSidePropertyKey, value);
        }

        #endregion

        #region IsTopSide

        /// <summary>
        ///     IsTopSide Read-Only Dependency Property
        /// </summary>
        private static readonly DependencyPropertyKey IsTopSidePropertyKey = DependencyProperty.RegisterReadOnly(
            "IsTopSide", typeof(bool), typeof(LayoutAnchorSideControl),
            new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsTopSideProperty = IsTopSidePropertyKey.DependencyProperty;

        /// <summary>
        ///     Gets the IsTopSide property.  This dependency property
        ///     indicates this control is anchored to top side.
        /// </summary>
        public bool IsTopSide => (bool) GetValue(IsTopSideProperty);

        /// <summary>
        ///     Provides a secure method for setting the IsTopSide property.
        ///     This dependency property indicates this control is anchored to top side.
        /// </summary>
        /// <param name="value">The new value for the property.</param>
        protected void SetIsTopSide(bool value)
        {
            SetValue(IsTopSidePropertyKey, value);
        }

        #endregion

        #region IsRightSide

        /// <summary>
        ///     IsRightSide Read-Only Dependency Property
        /// </summary>
        private static readonly DependencyPropertyKey IsRightSidePropertyKey = DependencyProperty.RegisterReadOnly(
            "IsRightSide", typeof(bool), typeof(LayoutAnchorSideControl),
            new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsRightSideProperty = IsRightSidePropertyKey.DependencyProperty;

        /// <summary>
        ///     Gets the IsRightSide property.  This dependency property
        ///     indicates this control is anchored to right side.
        /// </summary>
        public bool IsRightSide => (bool) GetValue(IsRightSideProperty);

        /// <summary>
        ///     Provides a secure method for setting the IsRightSide property.
        ///     This dependency property indicates this control is anchored to right side.
        /// </summary>
        /// <param name="value">The new value for the property.</param>
        protected void SetIsRightSide(bool value)
        {
            SetValue(IsRightSidePropertyKey, value);
        }

        #endregion

        #region IsBottomSide

        /// <summary>
        ///     IsBottomSide Read-Only Dependency Property
        /// </summary>
        private static readonly DependencyPropertyKey IsBottomSidePropertyKey = DependencyProperty.RegisterReadOnly(
            "IsBottomSide", typeof(bool), typeof(LayoutAnchorSideControl),
            new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsBottomSideProperty = IsBottomSidePropertyKey.DependencyProperty;

        /// <summary>
        ///     Gets the IsBottomSide property.  This dependency property
        ///     indicates if this panel is anchored to bottom side.
        /// </summary>
        public bool IsBottomSide => (bool) GetValue(IsBottomSideProperty);

        /// <summary>
        ///     Provides a secure method for setting the IsBottomSide property.
        ///     This dependency property indicates if this panel is anchored to bottom side.
        /// </summary>
        /// <param name="value">The new value for the property.</param>
        protected void SetIsBottomSide(bool value)
        {
            SetValue(IsBottomSidePropertyKey, value);
        }

        #endregion

        #endregion

        #region Overrides

        #endregion

        #region Private Methods

        private void CreateChildrenViews()
        {
            var manager = _model.Root.Manager;
            foreach (var childModel in _model.Children)
                Children.Add(manager.CreateUIElementForModel(childModel) as LayoutAnchorGroupControl);
        }

        private void OnModelChildrenCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems is not null &&
                (e.Action == NotifyCollectionChangedAction.Remove ||
                 e.Action == NotifyCollectionChangedAction.Replace))
                foreach (var childModel in e.OldItems)
                    Children.Remove(Children.First(cv => cv.Model == childModel));

            if (e.Action == NotifyCollectionChangedAction.Reset)
                Children.Clear();

            if (e.NewItems is not null &&
                (e.Action == NotifyCollectionChangedAction.Add ||
                 e.Action == NotifyCollectionChangedAction.Replace))
            {
                var manager = _model.Root.Manager;
                var insertIndex = e.NewStartingIndex;
                foreach (LayoutAnchorGroup childModel in e.NewItems)
                    Children.Insert(insertIndex++,
                        manager.CreateUIElementForModel(childModel) as LayoutAnchorGroupControl);
            }
        }

        private void UpdateSide()
        {
            switch (_model.Side)
            {
                case AnchorSide.Left:
                    SetIsLeftSide(true);
                    break;
                case AnchorSide.Top:
                    SetIsTopSide(true);
                    break;
                case AnchorSide.Right:
                    SetIsRightSide(true);
                    break;
                case AnchorSide.Bottom:
                    SetIsBottomSide(true);
                    break;
            }
        }

        #endregion
    }
}