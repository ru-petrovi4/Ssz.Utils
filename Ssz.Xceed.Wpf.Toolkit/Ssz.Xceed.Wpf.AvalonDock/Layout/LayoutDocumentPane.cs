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
using System.Diagnostics;
using System.Linq;
using System.Windows.Markup;
using System.Xml;

namespace Ssz.Xceed.Wpf.AvalonDock.Layout
{
    [ContentProperty("Children")]
    [Serializable]
    public class LayoutDocumentPane : LayoutPositionableGroup<LayoutContent>, ILayoutDocumentPane,
        ILayoutPositionableElement, ILayoutContentSelector, ILayoutPaneSerializable
    {
        #region Public Methods

        public int IndexOf(LayoutContent content)
        {
            return Children.IndexOf(content);
        }

        #endregion

        #region Internal Methods

        internal void SetNextSelectedIndex()
        {
            SelectedContentIndex = -1;
            for (var i = 0; i < Children.Count; ++i)
                if (Children[i].IsEnabled)
                {
                    SelectedContentIndex = i;
                    return;
                }
        }

        #endregion

        #region Private Methods

        private void UpdateParentVisibility()
        {
            var parentPane = Parent as ILayoutElementWithVisibility;
            if (parentPane is not null)
                parentPane.ComputeVisibility();
        }

        #endregion

        #region Constructors

        public LayoutDocumentPane()
        {
        }

        public LayoutDocumentPane(LayoutContent firstChild)
        {
            Children.Add(firstChild);
        }

        #endregion

        #region Properties

        #region ShowHeader

        private bool _showHeader = true;

        public bool ShowHeader
        {
            get => _showHeader;
            set
            {
                if (value != _showHeader)
                {
                    _showHeader = value;
                    RaisePropertyChanged("ShowHeader");
                }
            }
        }

        #endregion

        #region SelectedContentIndex

        private int _selectedIndex = -1;

        public int SelectedContentIndex
        {
            get => _selectedIndex;
            set
            {
                if (value < 0 ||
                    value >= Children.Count)
                    value = -1;

                if (_selectedIndex != value)
                {
                    RaisePropertyChanging("SelectedContentIndex");
                    RaisePropertyChanging("SelectedContent");
                    if (_selectedIndex >= 0 &&
                        _selectedIndex < Children.Count)
                        Children[_selectedIndex].IsSelected = false;

                    _selectedIndex = value;

                    if (_selectedIndex >= 0 &&
                        _selectedIndex < Children.Count)
                        Children[_selectedIndex].IsSelected = true;

                    RaisePropertyChanged("SelectedContentIndex");
                    RaisePropertyChanged("SelectedContent");
                }
            }
        }

        #endregion

        #region SelectedContent

        public LayoutContent SelectedContent => _selectedIndex == -1 ? null : Children[_selectedIndex];

        #endregion

        #region ChildrenSorted

        public IEnumerable<LayoutContent> ChildrenSorted
        {
            get
            {
                var listSorted = Children.ToList();
                listSorted.Sort();
                return listSorted;
            }
        }

        #endregion

        #endregion

        #region Overrides

        protected override bool GetVisibility()
        {
            if (Parent is LayoutDocumentPaneGroup)
                return ChildrenCount > 0 && Children.Any(c =>
                    c is LayoutDocument && ((LayoutDocument) c).IsVisible || c is LayoutAnchorable);

            return true;
        }

        protected override void ChildMoved(int oldIndex, int newIndex)
        {
            if (_selectedIndex == oldIndex)
            {
                RaisePropertyChanging("SelectedContentIndex");
                _selectedIndex = newIndex;
                RaisePropertyChanged("SelectedContentIndex");
            }


            base.ChildMoved(oldIndex, newIndex);
        }

        protected override void OnChildrenCollectionChanged()
        {
            if (SelectedContentIndex >= ChildrenCount)
                SelectedContentIndex = Children.Count - 1;
            if (SelectedContentIndex == -1)
            {
                if (ChildrenCount > 0)
                {
                    if (Root is null)
                    {
                        SetNextSelectedIndex();
                    }
                    else
                    {
                        var childrenToSelect = Children
                            .OrderByDescending(c => c.LastActivationTimeStamp.GetValueOrDefault()).First();
                        SelectedContentIndex = Children.IndexOf(childrenToSelect);
                        childrenToSelect.IsActive = true;
                    }
                }
                else
                {
                    if (Root is not null) Root.ActiveContent = null;
                }
            }

            base.OnChildrenCollectionChanged();

            RaisePropertyChanged("ChildrenSorted");
        }

        protected override void OnIsVisibleChanged()
        {
            UpdateParentVisibility();
            base.OnIsVisibleChanged();
        }

        public override void WriteXml(XmlWriter writer)
        {
            if (_id is not null)
                writer.WriteAttributeString("Id", _id);
            if (!_showHeader)
                writer.WriteAttributeString("ShowHeader", _showHeader.ToString());

            base.WriteXml(writer);
        }

        public override void ReadXml(XmlReader reader)
        {
            if (reader.MoveToAttribute("Id"))
                _id = reader.Value;
            if (reader.MoveToAttribute("ShowHeader"))
                _showHeader = bool.Parse(reader.Value);


            base.ReadXml(reader);
        }


#if TRACE
        public override void ConsoleDump(int tab)
        {
            Trace.Write(new string(' ', tab * 4));
            Trace.WriteLine("DocumentPane()");

            foreach (LayoutElement child in Children)
                child.ConsoleDump(tab + 1);
        }
#endif

        #endregion

        #region ILayoutPaneSerializable Interface

        private string _id;

        string ILayoutPaneSerializable.Id
        {
            get => _id;
            set => _id = value;
        }

        #endregion
    }
}