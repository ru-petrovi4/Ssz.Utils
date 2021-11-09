/*************************************************************************************
   
   Toolkit for WPF

   Copyright (C) 2007-2018 Ssz.Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at https://xceed.com/xceed-toolkit-plus-for-wpf/

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Ssz.Xceed.Wpf.AvalonDock.Layout;

namespace Ssz.Xceed.Wpf.AvalonDock.Controls
{
    public class LayoutAnchorGroupControl : Control, ILayoutControl
    {
        #region Members

        private readonly LayoutAnchorGroup _model;

        #endregion

        #region Constructors

        static LayoutAnchorGroupControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LayoutAnchorGroupControl),
                new FrameworkPropertyMetadata(typeof(LayoutAnchorGroupControl)));
        }

        internal LayoutAnchorGroupControl(LayoutAnchorGroup model)
        {
            _model = model;
            CreateChildrenViews();

            _model.Children.CollectionChanged += (s, e) => OnModelChildrenCollectionChanged(e);
        }

        #endregion

        #region Properties

        public ObservableCollection<LayoutAnchorControl> Children { get; } = new();

        public ILayoutElement Model => _model;

        #endregion

        #region Private Methods

        private void CreateChildrenViews()
        {
            var manager = _model.Root.Manager;
            foreach (var childModel in _model.Children)
            {
                var lac = new LayoutAnchorControl(childModel);
                lac.SetBinding(TemplateProperty,
                    new Binding(DockingManager.AnchorTemplateProperty.Name) {Source = manager});
                Children.Add(lac);
            }
        }

        private void OnModelChildrenCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove ||
                e.Action == NotifyCollectionChangedAction.Replace)
                if (e.OldItems is not null)
                    foreach (var childModel in e.OldItems)
                        Children.Remove(Children.First(cv => cv.Model == childModel));

            if (e.Action == NotifyCollectionChangedAction.Reset)
                Children.Clear();

            if (e.Action == NotifyCollectionChangedAction.Add ||
                e.Action == NotifyCollectionChangedAction.Replace)
                if (e.NewItems is not null)
                {
                    var manager = _model.Root.Manager;
                    var insertIndex = e.NewStartingIndex;
                    foreach (LayoutAnchorable childModel in e.NewItems)
                    {
                        var lac = new LayoutAnchorControl(childModel);
                        lac.SetBinding(TemplateProperty,
                            new Binding(DockingManager.AnchorTemplateProperty.Name) {Source = manager});
                        Children.Insert(insertIndex++, lac);
                    }
                }
        }

        #endregion
    }
}