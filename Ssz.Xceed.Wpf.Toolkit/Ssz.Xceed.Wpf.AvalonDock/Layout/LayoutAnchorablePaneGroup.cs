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
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Xml;

namespace Ssz.Xceed.Wpf.AvalonDock.Layout
{
    [ContentProperty("Children")]
    [Serializable]
    public class LayoutAnchorablePaneGroup : LayoutPositionableGroup<ILayoutAnchorablePane>, ILayoutAnchorablePane,
        ILayoutOrientableGroup
    {
        #region Private Methods

        private void UpdateParentVisibility()
        {
            var parentPane = Parent as ILayoutElementWithVisibility;
            if (parentPane != null)
                parentPane.ComputeVisibility();
        }

        #endregion

        #region Constructors

        public LayoutAnchorablePaneGroup()
        {
        }

        public LayoutAnchorablePaneGroup(LayoutAnchorablePane firstChild)
        {
            Children.Add(firstChild);
        }

        #endregion

        #region Properties

        #region Orientation

        private Orientation _orientation;

        public Orientation Orientation
        {
            get => _orientation;
            set
            {
                if (_orientation != value)
                {
                    RaisePropertyChanging("Orientation");
                    _orientation = value;
                    RaisePropertyChanged("Orientation");
                }
            }
        }

        #endregion

        #endregion

        #region Overrides

        protected override bool GetVisibility()
        {
            return Children.Count > 0 && Children.Any(c => c.IsVisible);
        }

        protected override void OnIsVisibleChanged()
        {
            UpdateParentVisibility();
            base.OnIsVisibleChanged();
        }

        protected override void OnDockWidthChanged()
        {
            if (DockWidth.IsAbsolute && ChildrenCount == 1)
                ((ILayoutPositionableElement) Children[0]).DockWidth = DockWidth;

            base.OnDockWidthChanged();
        }

        protected override void OnDockHeightChanged()
        {
            if (DockHeight.IsAbsolute && ChildrenCount == 1)
                ((ILayoutPositionableElement) Children[0]).DockHeight = DockHeight;
            base.OnDockHeightChanged();
        }

        protected override void OnChildrenCollectionChanged()
        {
            if (DockWidth.IsAbsolute && ChildrenCount == 1)
                ((ILayoutPositionableElement) Children[0]).DockWidth = DockWidth;
            if (DockHeight.IsAbsolute && ChildrenCount == 1)
                ((ILayoutPositionableElement) Children[0]).DockHeight = DockHeight;
            base.OnChildrenCollectionChanged();
        }

        public override void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("Orientation", Orientation.ToString());
            base.WriteXml(writer);
        }

        public override void ReadXml(XmlReader reader)
        {
            if (reader.MoveToAttribute("Orientation"))
                Orientation = (Orientation) Enum.Parse(typeof(Orientation), reader.Value, true);
            base.ReadXml(reader);
        }

#if TRACE
        public override void ConsoleDump(int tab)
        {
            Trace.Write(new string(' ', tab * 4));
            Trace.WriteLine(string.Format("AnchorablePaneGroup({0})", Orientation));

            foreach (LayoutElement child in Children)
                child.ConsoleDump(tab + 1);
        }
#endif

        #endregion
    }
}