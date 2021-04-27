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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Ssz.Xceed.Wpf.AvalonDock.Layout
{
    [Serializable]
    public abstract class LayoutGroup<T> : LayoutGroupBase, ILayoutContainer, ILayoutGroup, IXmlSerializable
        where T : class, ILayoutElement
    {
        #region Members

        #endregion

        #region Constructors

        internal LayoutGroup()
        {
            Children.CollectionChanged += _children_CollectionChanged;
        }

        #endregion

        #region ILayoutContainer Interface

        IEnumerable<ILayoutElement> ILayoutContainer.Children => Children;

        #endregion

        #region Overrides

        protected override void OnParentChanged(ILayoutContainer oldValue, ILayoutContainer newValue)
        {
            base.OnParentChanged(oldValue, newValue);

            ComputeVisibility();
        }

        #endregion

        #region Properties

        #region Children

        public ObservableCollection<T> Children { get; } = new();

        #endregion

        #region IsVisible

        private bool _isVisible = true;

        public bool IsVisible
        {
            get => _isVisible;
            protected set
            {
                if (_isVisible != value)
                {
                    RaisePropertyChanging("IsVisible");
                    _isVisible = value;
                    OnIsVisibleChanged();
                    RaisePropertyChanged("IsVisible");
                }
            }
        }

        #endregion

        #region ChildrenCount

        public int ChildrenCount => Children.Count;

        #endregion

        #endregion

        #region Public Methods

        public void ComputeVisibility()
        {
            IsVisible = GetVisibility();
        }

        public void MoveChild(int oldIndex, int newIndex)
        {
            if (oldIndex == newIndex)
                return;
            Children.Move(oldIndex, newIndex);
            ChildMoved(oldIndex, newIndex);
        }

        public void RemoveChildAt(int childIndex)
        {
            Children.RemoveAt(childIndex);
        }

        public int IndexOfChild(ILayoutElement element)
        {
            return Children.Cast<ILayoutElement>().ToList().IndexOf(element);
        }

        public void InsertChildAt(int index, ILayoutElement element)
        {
            Children.Insert(index, (T) element);
        }

        public void RemoveChild(ILayoutElement element)
        {
            Children.Remove((T) element);
        }

        public void ReplaceChild(ILayoutElement oldElement, ILayoutElement newElement)
        {
            var index = Children.IndexOf((T) oldElement);
            Children.Insert(index, (T) newElement);
            Children.RemoveAt(index + 1);
        }

        public void ReplaceChildAt(int index, ILayoutElement element)
        {
            Children[index] = (T) element;
        }


        public XmlSchema GetSchema()
        {
            return null;
        }

        public virtual void ReadXml(XmlReader reader)
        {
            reader.MoveToContent();
            if (reader.IsEmptyElement)
            {
                reader.Read();
                ComputeVisibility();
                return;
            }

            var localName = reader.LocalName;
            reader.Read();
            while (true)
            {
                if (reader.LocalName == localName &&
                    reader.NodeType == XmlNodeType.EndElement)
                    break;
                if (reader.NodeType == XmlNodeType.Whitespace)
                {
                    reader.Read();
                    continue;
                }

                XmlSerializer serializer = null;
                if (reader.LocalName == "LayoutAnchorablePaneGroup")
                {
                    serializer = new XmlSerializer(typeof(LayoutAnchorablePaneGroup));
                }
                else if (reader.LocalName == "LayoutAnchorablePane")
                {
                    serializer = new XmlSerializer(typeof(LayoutAnchorablePane));
                }
                else if (reader.LocalName == "LayoutAnchorable")
                {
                    serializer = new XmlSerializer(typeof(LayoutAnchorable));
                }
                else if (reader.LocalName == "LayoutDocumentPaneGroup")
                {
                    serializer = new XmlSerializer(typeof(LayoutDocumentPaneGroup));
                }
                else if (reader.LocalName == "LayoutDocumentPane")
                {
                    serializer = new XmlSerializer(typeof(LayoutDocumentPane));
                }
                else if (reader.LocalName == "LayoutDocument")
                {
                    serializer = new XmlSerializer(typeof(LayoutDocument));
                }
                else if (reader.LocalName == "LayoutAnchorGroup")
                {
                    serializer = new XmlSerializer(typeof(LayoutAnchorGroup));
                }
                else if (reader.LocalName == "LayoutPanel")
                {
                    serializer = new XmlSerializer(typeof(LayoutPanel));
                }
                else
                {
                    var type = FindType(reader.LocalName);
                    if (type == null)
                        throw new ArgumentException("AvalonDock.LayoutGroup doesn't know how to deserialize " +
                                                    reader.LocalName);
                    serializer = new XmlSerializer(type);
                }

                Children.Add((T) serializer.Deserialize(reader));
            }

            reader.ReadEndElement();
        }

        public virtual void WriteXml(XmlWriter writer)
        {
            foreach (var child in Children)
            {
                var type = child.GetType();
                var serializer = new XmlSerializer(type);
                serializer.Serialize(writer, child);
            }
        }

        #endregion

        #region Internal Methods

        protected virtual void OnIsVisibleChanged()
        {
            UpdateParentVisibility();
        }

        protected abstract bool GetVisibility();

        protected virtual void ChildMoved(int oldIndex, int newIndex)
        {
        }

        #endregion

        #region Private Methods

        private void _children_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove ||
                e.Action == NotifyCollectionChangedAction.Replace)
                if (e.OldItems != null)
                    foreach (LayoutElement element in e.OldItems)
                        if (element.Parent == this)
                            element.Parent = null;

            if (e.Action == NotifyCollectionChangedAction.Add ||
                e.Action == NotifyCollectionChangedAction.Replace)
                if (e.NewItems != null)
                    foreach (LayoutElement element in e.NewItems)
                        if (element.Parent != this)
                        {
                            if (element.Parent != null)
                                element.Parent.RemoveChild(element);
                            element.Parent = this;
                        }

            ComputeVisibility();
            OnChildrenCollectionChanged();
            NotifyChildrenTreeChanged(ChildrenTreeChange.DirectChildrenChanged);
            RaisePropertyChanged("ChildrenCount");
        }

        private void UpdateParentVisibility()
        {
            var parentPane = Parent as ILayoutElementWithVisibility;
            if (parentPane != null)
                parentPane.ComputeVisibility();
        }

        private Type FindType(string name)
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            foreach (var t in a.GetTypes())
                if (t.Name.Equals(name))
                    return t;
            return null;
        }

        #endregion
    }
}