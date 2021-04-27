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
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Ssz.Xceed.Wpf.AvalonDock.Layout
{
    [ContentProperty("RootPanel")]
    [Serializable]
    public class LayoutRoot : LayoutElement, ILayoutContainer, ILayoutRoot, IXmlSerializable
    {
        #region Constructors

        public LayoutRoot()
        {
            RightSide = new LayoutAnchorSide();
            LeftSide = new LayoutAnchorSide();
            TopSide = new LayoutAnchorSide();
            BottomSide = new LayoutAnchorSide();
            RootPanel = new LayoutPanel(new LayoutDocumentPane());
        }

        #endregion

        #region Overrides

#if TRACE
        public override void ConsoleDump(int tab)
        {
            Trace.Write(new string(' ', tab * 4));
            Trace.WriteLine("RootPanel()");

            RootPanel.ConsoleDump(tab + 1);

            Trace.Write(new string(' ', tab * 4));
            Trace.WriteLine("FloatingWindows()");

            foreach (var fw in FloatingWindows)
                fw.ConsoleDump(tab + 1);

            Trace.Write(new string(' ', tab * 4));
            Trace.WriteLine("Hidden()");

            foreach (var hidden in Hidden)
                hidden.ConsoleDump(tab + 1);
        }
#endif

        #endregion

        #region Properties

        #region RootPanel

        private LayoutPanel _rootPanel;

        public LayoutPanel RootPanel
        {
            get => _rootPanel;
            set
            {
                if (_rootPanel != value)
                {
                    RaisePropertyChanging("RootPanel");
                    if (_rootPanel != null &&
                        _rootPanel.Parent == this)
                        _rootPanel.Parent = null;
                    _rootPanel = value;

                    if (_rootPanel == null)
                        _rootPanel = new LayoutPanel(new LayoutDocumentPane());

                    if (_rootPanel != null)
                        _rootPanel.Parent = this;
                    RaisePropertyChanged("RootPanel");
                }
            }
        }

        #endregion

        #region TopSide

        private LayoutAnchorSide _topSide;

        public LayoutAnchorSide TopSide
        {
            get => _topSide;
            set
            {
                if (_topSide != value)
                {
                    RaisePropertyChanging("TopSide");
                    _topSide = value;
                    if (_topSide != null)
                        _topSide.Parent = this;
                    RaisePropertyChanged("TopSide");
                }
            }
        }

        #endregion

        #region RightSide

        private LayoutAnchorSide _rightSide;

        public LayoutAnchorSide RightSide
        {
            get => _rightSide;
            set
            {
                if (_rightSide != value)
                {
                    RaisePropertyChanging("RightSide");
                    _rightSide = value;
                    if (_rightSide != null)
                        _rightSide.Parent = this;
                    RaisePropertyChanged("RightSide");
                }
            }
        }

        #endregion

        #region LeftSide

        private LayoutAnchorSide _leftSide;

        public LayoutAnchorSide LeftSide
        {
            get => _leftSide;
            set
            {
                if (_leftSide != value)
                {
                    RaisePropertyChanging("LeftSide");
                    _leftSide = value;
                    if (_leftSide != null)
                        _leftSide.Parent = this;
                    RaisePropertyChanged("LeftSide");
                }
            }
        }

        #endregion

        #region BottomSide

        private LayoutAnchorSide _bottomSide;

        public LayoutAnchorSide BottomSide
        {
            get => _bottomSide;
            set
            {
                if (_bottomSide != value)
                {
                    RaisePropertyChanging("BottomSide");
                    _bottomSide = value;
                    if (_bottomSide != null)
                        _bottomSide.Parent = this;
                    RaisePropertyChanged("BottomSide");
                }
            }
        }

        #endregion

        #region FloatingWindows

        private ObservableCollection<LayoutFloatingWindow> _floatingWindows;

        public ObservableCollection<LayoutFloatingWindow> FloatingWindows
        {
            get
            {
                if (_floatingWindows == null)
                {
                    _floatingWindows = new ObservableCollection<LayoutFloatingWindow>();
                    _floatingWindows.CollectionChanged += _floatingWindows_CollectionChanged;
                }

                return _floatingWindows;
            }
        }

        #endregion

        #region HiddenAnchorables

        private ObservableCollection<LayoutAnchorable> _hiddenAnchorables;

        public ObservableCollection<LayoutAnchorable> Hidden
        {
            get
            {
                if (_hiddenAnchorables == null)
                {
                    _hiddenAnchorables = new ObservableCollection<LayoutAnchorable>();
                    _hiddenAnchorables.CollectionChanged += _hiddenAnchorables_CollectionChanged;
                }

                return _hiddenAnchorables;
            }
        }

        #endregion

        #region Children

        public IEnumerable<ILayoutElement> Children
        {
            get
            {
                if (RootPanel != null)
                    yield return RootPanel;
                if (_floatingWindows != null)
                    foreach (var floatingWindow in _floatingWindows)
                        yield return floatingWindow;
                if (TopSide != null)
                    yield return TopSide;
                if (RightSide != null)
                    yield return RightSide;
                if (BottomSide != null)
                    yield return BottomSide;
                if (LeftSide != null)
                    yield return LeftSide;
                if (_hiddenAnchorables != null)
                    foreach (var hiddenAnchorable in _hiddenAnchorables)
                        yield return hiddenAnchorable;
            }
        }

        #endregion

        #region ChildrenCount

        public int ChildrenCount =>
            5 +
            (_floatingWindows != null ? _floatingWindows.Count : 0) +
            (_hiddenAnchorables != null ? _hiddenAnchorables.Count : 0);

        #endregion

        #region ActiveContent

        [field: NonSerialized] private WeakReference _activeContent;

        private bool _activeContentSet;

        [XmlIgnore]
        public LayoutContent ActiveContent
        {
            get => _activeContent.GetValueOrDefault<LayoutContent>();
            set
            {
                var currentValue = ActiveContent;
                if (currentValue != value) InternalSetActiveContent(currentValue, value);
            }
        }

        #endregion

        #region LastFocusedDocument

        [field: NonSerialized] private WeakReference _lastFocusedDocument;

        [field: NonSerialized] private bool _lastFocusedDocumentSet;

        [XmlIgnore]
        public LayoutContent LastFocusedDocument
        {
            get => _lastFocusedDocument.GetValueOrDefault<LayoutContent>();
            private set
            {
                var currentValue = LastFocusedDocument;
                if (currentValue != value)
                {
                    RaisePropertyChanging("LastFocusedDocument");
                    if (currentValue != null)
                        currentValue.IsLastFocusedDocument = false;
                    _lastFocusedDocument = new WeakReference(value);
                    currentValue = LastFocusedDocument;
                    if (currentValue != null)
                        currentValue.IsLastFocusedDocument = true;
                    _lastFocusedDocumentSet = currentValue != null;
                    RaisePropertyChanged("LastFocusedDocument");
                }
            }
        }

        #endregion

        #region Manager

        [NonSerialized] private DockingManager _manager;

        [XmlIgnore]
        public DockingManager Manager
        {
            get => _manager;
            internal set
            {
                if (_manager != value)
                {
                    RaisePropertyChanging("Manager");
                    _manager = value;
                    RaisePropertyChanged("Manager");
                }
            }
        }

        #endregion

        #endregion

        #region Public Methods

        public void RemoveChild(ILayoutElement element)
        {
            if (element == RootPanel)
                RootPanel = null;
            else if (_floatingWindows != null && _floatingWindows.Contains(element))
                _floatingWindows.Remove(element as LayoutFloatingWindow);
            else if (_hiddenAnchorables != null && _hiddenAnchorables.Contains(element))
                _hiddenAnchorables.Remove(element as LayoutAnchorable);
            else if (element == TopSide)
                TopSide = null;
            else if (element == RightSide)
                RightSide = null;
            else if (element == BottomSide)
                BottomSide = null;
            else if (element == LeftSide)
                LeftSide = null;
        }

        public void ReplaceChild(ILayoutElement oldElement, ILayoutElement newElement)
        {
            if (oldElement == RootPanel)
            {
                RootPanel = (LayoutPanel) newElement;
            }
            else if (_floatingWindows != null && _floatingWindows.Contains(oldElement))
            {
                var index = _floatingWindows.IndexOf(oldElement as LayoutFloatingWindow);
                _floatingWindows.Remove(oldElement as LayoutFloatingWindow);
                _floatingWindows.Insert(index, newElement as LayoutFloatingWindow);
            }
            else if (_hiddenAnchorables != null && _hiddenAnchorables.Contains(oldElement))
            {
                var index = _hiddenAnchorables.IndexOf(oldElement as LayoutAnchorable);
                _hiddenAnchorables.Remove(oldElement as LayoutAnchorable);
                _hiddenAnchorables.Insert(index, newElement as LayoutAnchorable);
            }
            else if (oldElement == TopSide)
            {
                TopSide = (LayoutAnchorSide) newElement;
            }
            else if (oldElement == RightSide)
            {
                RightSide = (LayoutAnchorSide) newElement;
            }
            else if (oldElement == BottomSide)
            {
                BottomSide = (LayoutAnchorSide) newElement;
            }
            else if (oldElement == LeftSide)
            {
                LeftSide = (LayoutAnchorSide) newElement;
            }
        }

        /// <summary>
        ///     Removes any empty container not directly referenced by other layout items
        /// </summary>
        public void CollectGarbage()
        {
            var exitFlag = true;

            #region collect empty panes

            do
            {
                exitFlag = true;

                //for each content that references via PreviousContainer a disconnected Pane set the property to null
                foreach (var content in this.Descendents().OfType<ILayoutPreviousContainer>().Where(c =>
                    c.PreviousContainer != null &&
                    (c.PreviousContainer.Parent == null || c.PreviousContainer.Parent.Root != this)))
                    content.PreviousContainer = null;

                //for each pane that is empty
                foreach (var emptyPane in this.Descendents().OfType<ILayoutPane>().Where(p => p.ChildrenCount == 0))
                {
                    //...set null any reference coming from contents not yet hosted in a floating window
                    foreach (var contentReferencingEmptyPane in this.Descendents().OfType<LayoutContent>()
                        .Where(c => ((ILayoutPreviousContainer) c).PreviousContainer == emptyPane && !c.IsFloating))
                    {
                        if (contentReferencingEmptyPane is LayoutAnchorable &&
                            !((LayoutAnchorable) contentReferencingEmptyPane).IsVisible)
                            continue;

                        ((ILayoutPreviousContainer) contentReferencingEmptyPane).PreviousContainer = null;
                        contentReferencingEmptyPane.PreviousContainerIndex = -1;
                    }

                    //...if this pane is the only documentpane present in the layout than skip it
                    if (emptyPane is LayoutDocumentPane &&
                        this.Descendents().OfType<LayoutDocumentPane>().Count(c => c != emptyPane) == 0)
                        continue;

                    //...if this empty panes is not referenced by anyone, than removes it from its parent container
                    if (!this.Descendents().OfType<ILayoutPreviousContainer>()
                        .Any(c => c.PreviousContainer == emptyPane))
                    {
                        var parentGroup = emptyPane.Parent;
                        parentGroup.RemoveChild(emptyPane);
                        exitFlag = false;
                        break;
                    }
                }

                if (!exitFlag)
                    //removes any empty anchorable pane group
                    foreach (var emptyPaneGroup in this.Descendents().OfType<LayoutAnchorablePaneGroup>()
                        .Where(p => p.ChildrenCount == 0))
                    {
                        var parentGroup = emptyPaneGroup.Parent;
                        parentGroup.RemoveChild(emptyPaneGroup);
                        exitFlag = false;
                        break;
                    }

                if (!exitFlag)
                    //removes any empty layout panel
                    foreach (var emptyPaneGroup in this.Descendents().OfType<LayoutPanel>()
                        .Where(p => p.ChildrenCount == 0))
                    {
                        var parentGroup = emptyPaneGroup.Parent;
                        parentGroup.RemoveChild(emptyPaneGroup);
                        exitFlag = false;
                        break;
                    }

                if (!exitFlag)
                    //removes any empty floating window
                    foreach (var emptyPaneGroup in this.Descendents().OfType<LayoutFloatingWindow>()
                        .Where(p => p.ChildrenCount == 0))
                    {
                        var parentGroup = emptyPaneGroup.Parent;
                        parentGroup.RemoveChild(emptyPaneGroup);
                        exitFlag = false;
                        break;
                    }

                if (!exitFlag)
                    //removes any empty anchor group
                    foreach (var emptyPaneGroup in this.Descendents().OfType<LayoutAnchorGroup>()
                        .Where(p => p.ChildrenCount == 0))
                    {
                        var parentGroup = emptyPaneGroup.Parent;
                        parentGroup.RemoveChild(emptyPaneGroup);
                        exitFlag = false;
                        break;
                    }
            } while (!exitFlag);

            #endregion

            #region collapse single child anchorable pane groups

            do
            {
                exitFlag = true;
                //for each pane that is empty
                foreach (var paneGroupToCollapse in this.Descendents().OfType<LayoutAnchorablePaneGroup>()
                    .Where(p => p.ChildrenCount == 1 && p.Children[0] is LayoutAnchorablePaneGroup).ToArray())
                {
                    var singleChild = paneGroupToCollapse.Children[0] as LayoutAnchorablePaneGroup;
                    paneGroupToCollapse.Orientation = singleChild.Orientation;
                    paneGroupToCollapse.RemoveChild(singleChild);
                    while (singleChild.ChildrenCount > 0)
                        paneGroupToCollapse.InsertChildAt(
                            paneGroupToCollapse.ChildrenCount, singleChild.Children[0]);

                    exitFlag = false;
                    break;
                }
            } while (!exitFlag);

            #endregion

            #region collapse single child document pane groups

            do
            {
                exitFlag = true;
                //for each pane that is empty
                foreach (var paneGroupToCollapse in this.Descendents().OfType<LayoutDocumentPaneGroup>()
                    .Where(p => p.ChildrenCount == 1 && p.Children[0] is LayoutDocumentPaneGroup).ToArray())
                {
                    var singleChild = paneGroupToCollapse.Children[0] as LayoutDocumentPaneGroup;
                    paneGroupToCollapse.Orientation = singleChild.Orientation;
                    paneGroupToCollapse.RemoveChild(singleChild);
                    while (singleChild.ChildrenCount > 0)
                        paneGroupToCollapse.InsertChildAt(
                            paneGroupToCollapse.ChildrenCount, singleChild.Children[0]);

                    exitFlag = false;
                    break;
                }
            } while (!exitFlag);

            #endregion

            //do
            //{
            //  exitFlag = true;
            //  //for each panel that has only one child
            //  foreach( var panelToCollapse in this.Descendents().OfType<LayoutPanel>().Where( p => p.ChildrenCount == 1 && p.Children[ 0 ] is LayoutPanel ).ToArray() )
            //  {
            //    var singleChild = panelToCollapse.Children[ 0 ] as LayoutPanel;
            //    panelToCollapse.Orientation = singleChild.Orientation;
            //    panelToCollapse.RemoveChild( singleChild );
            //    ILayoutPanelElement[] singleChildChildren = new ILayoutPanelElement[ singleChild.ChildrenCount ];
            //    singleChild.Children.CopyTo( singleChildChildren, 0 );
            //    while( singleChild.ChildrenCount > 0 )
            //    {
            //      panelToCollapse.InsertChildAt(
            //          panelToCollapse.ChildrenCount, singleChildChildren[ panelToCollapse.ChildrenCount ] );
            //    }

            //    exitFlag = false;
            //    break;
            //  }

            //}
            //while( !exitFlag );

            #region Update ActiveContent and LastFocusedDocument properties

            UpdateActiveContentProperty();

            #endregion

#if DEBUG
            Debug.Assert(
                !this.Descendents().OfType<LayoutAnchorablePane>().Any(a => a.ChildrenCount == 0 && a.IsVisible));
#if TRACE
            RootPanel.ConsoleDump(4);
#endif
#endif
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            reader.MoveToContent();
            if (reader.IsEmptyElement)
            {
                reader.Read();
                return;
            }

            Orientation orientation;
            var layoutPanelElements = ReadRootPanel(reader, out orientation);
            if (layoutPanelElements != null)
            {
                RootPanel = new LayoutPanel {Orientation = orientation};
                //Add all children to RootPanel
                for (var i = 0; i < layoutPanelElements.Count; ++i) RootPanel.Children.Add(layoutPanelElements[i]);
            }

            TopSide = new LayoutAnchorSide();
            if (ReadElement(reader) != null) FillLayoutAnchorSide(reader, TopSide);
            RightSide = new LayoutAnchorSide();
            if (ReadElement(reader) != null) FillLayoutAnchorSide(reader, RightSide);
            LeftSide = new LayoutAnchorSide();
            if (ReadElement(reader) != null) FillLayoutAnchorSide(reader, LeftSide);
            BottomSide = new LayoutAnchorSide();
            if (ReadElement(reader) != null) FillLayoutAnchorSide(reader, BottomSide);

            FloatingWindows.Clear();
            var floatingWindows = ReadElementList(reader, true);
            foreach (var floatingWindow in floatingWindows) FloatingWindows.Add((LayoutFloatingWindow) floatingWindow);

            Hidden.Clear();
            var hidden = ReadElementList(reader, false);
            foreach (var hiddenObject in hidden) Hidden.Add((LayoutAnchorable) hiddenObject);
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("RootPanel");
            if (RootPanel != null) RootPanel.WriteXml(writer);
            writer.WriteEndElement();

            writer.WriteStartElement("TopSide");
            if (TopSide != null) TopSide.WriteXml(writer);
            writer.WriteEndElement();

            writer.WriteStartElement("RightSide");
            if (RightSide != null) RightSide.WriteXml(writer);
            writer.WriteEndElement();

            writer.WriteStartElement("LeftSide");
            if (LeftSide != null) LeftSide.WriteXml(writer);
            writer.WriteEndElement();

            writer.WriteStartElement("BottomSide");
            if (BottomSide != null) BottomSide.WriteXml(writer);
            writer.WriteEndElement();

            // Write all floating windows (can be LayoutDocumentFloatingWindow or LayoutAnchorableFloatingWindow).
            // To prevent "can not create instance of abstract type", the type is retrieved with GetType().Name
            writer.WriteStartElement("FloatingWindows");
            foreach (var layoutFloatingWindow in FloatingWindows)
            {
                writer.WriteStartElement(layoutFloatingWindow.GetType().Name);
                layoutFloatingWindow.WriteXml(writer);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();

            writer.WriteStartElement("Hidden");
            foreach (var layoutAnchorable in Hidden)
            {
                writer.WriteStartElement(layoutAnchorable.GetType().Name);
                layoutAnchorable.WriteXml(writer);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        #endregion

        #region Internal Methods

        internal static Type FindType(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            foreach (var type in assembly.GetTypes())
                if (type.Name.Equals(name))
                    return type;
            return null;
        }

        internal void FireLayoutUpdated()
        {
            if (Updated != null)
                Updated(this, EventArgs.Empty);
        }

        internal void OnLayoutElementAdded(LayoutElement element)
        {
            if (ElementAdded != null)
                ElementAdded(this, new LayoutElementEventArgs(element));
        }

        internal void OnLayoutElementRemoved(LayoutElement element)
        {
            if (element.Descendents().OfType<LayoutContent>().Any(c => c == LastFocusedDocument))
                LastFocusedDocument = null;
            if (element.Descendents().OfType<LayoutContent>().Any(c => c == ActiveContent))
                ActiveContent = null;
            if (ElementRemoved != null)
                ElementRemoved(this, new LayoutElementEventArgs(element));
        }

        #endregion

        #region Private Methods

        private void _floatingWindows_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null && (e.Action == NotifyCollectionChangedAction.Remove ||
                                       e.Action == NotifyCollectionChangedAction.Replace))
                foreach (LayoutFloatingWindow element in e.OldItems)
                    if (element.Parent == this)
                        element.Parent = null;

            if (e.NewItems != null && (e.Action == NotifyCollectionChangedAction.Add ||
                                       e.Action == NotifyCollectionChangedAction.Replace))
                foreach (LayoutFloatingWindow element in e.NewItems)
                    element.Parent = this;
        }

        private void _hiddenAnchorables_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove ||
                e.Action == NotifyCollectionChangedAction.Replace)
                if (e.OldItems != null)
                    foreach (LayoutAnchorable element in e.OldItems)
                        if (element.Parent == this)
                            element.Parent = null;

            if (e.Action == NotifyCollectionChangedAction.Add ||
                e.Action == NotifyCollectionChangedAction.Replace)
                if (e.NewItems != null)
                    foreach (LayoutAnchorable element in e.NewItems)
                        if (element.Parent != this)
                        {
                            if (element.Parent != null)
                                element.Parent.RemoveChild(element);
                            element.Parent = this;
                        }
        }

        private void InternalSetActiveContent(LayoutContent currentValue, LayoutContent newActiveContent)
        {
            RaisePropertyChanging("ActiveContent");
            if (currentValue != null)
                currentValue.IsActive = false;
            _activeContent = new WeakReference(newActiveContent);
            currentValue = ActiveContent;
            if (currentValue != null)
                currentValue.IsActive = true;
            RaisePropertyChanged("ActiveContent");
            _activeContentSet = currentValue != null;
            if (currentValue != null)
            {
                if (currentValue.Parent is LayoutDocumentPane || currentValue is LayoutDocument)
                    LastFocusedDocument = currentValue;
            }
            else
            {
                LastFocusedDocument = null;
            }
        }

        private void UpdateActiveContentProperty()
        {
            var activeContent = ActiveContent;
            if (_activeContentSet && (activeContent == null || activeContent.Root != this))
            {
                _activeContentSet = false;
                InternalSetActiveContent(activeContent, null);
            }
        }

        private void FillLayoutAnchorSide(XmlReader reader, LayoutAnchorSide layoutAnchorSide)
        {
            var result = new List<LayoutAnchorGroup>();

            while (true)
            {
                //Read all layoutAnchorSide children
                var element = ReadElement(reader) as LayoutAnchorGroup;
                if (element != null)
                    result.Add(element);
                else if (reader.NodeType == XmlNodeType.EndElement) break;
            }

            reader.ReadEndElement();
            foreach (var las in result) layoutAnchorSide.Children.Add(las);
        }

        private List<ILayoutPanelElement> ReadRootPanel(XmlReader reader, out Orientation orientation)
        {
            orientation = Orientation.Horizontal;
            var result = new List<ILayoutPanelElement>();

            var startElementName = reader.LocalName;
            reader.Read();
            if (reader.LocalName.Equals(startElementName) && reader.NodeType == XmlNodeType.EndElement) return null;

            while (reader.NodeType == XmlNodeType.Whitespace) reader.Read();

            if (reader.LocalName.Equals("RootPanel"))
            {
                orientation = reader.GetAttribute("Orientation") == "Vertical"
                    ? Orientation.Vertical
                    : Orientation.Horizontal;
                reader.Read();

                while (true)
                {
                    //Read all RootPanel children
                    var element = ReadElement(reader) as ILayoutPanelElement;
                    if (element != null)
                        result.Add(element);
                    else if (reader.NodeType == XmlNodeType.EndElement) break;
                }
            }

            reader.ReadEndElement();

            return result;
        }

        private List<object> ReadElementList(XmlReader reader, bool isFloatingWindow)
        {
            var resultList = new List<object>();

            while (reader.NodeType == XmlNodeType.Whitespace) reader.Read();

            if (reader.IsEmptyElement)
            {
                reader.Read();
                return resultList;
            }

            var startElementName = reader.LocalName;
            reader.Read();
            if (reader.LocalName.Equals(startElementName) && reader.NodeType == XmlNodeType.EndElement) return null;

            while (reader.NodeType == XmlNodeType.Whitespace) reader.Read();

            while (true)
                if (isFloatingWindow)
                {
                    var result = ReadElement(reader) as LayoutFloatingWindow;
                    if (result == null) break;
                    resultList.Add(result);
                }
                else
                {
                    var result = ReadElement(reader) as LayoutAnchorable;
                    if (result == null) break;
                    resultList.Add(result);
                }

            reader.ReadEndElement();

            return resultList;
        }

        private object ReadElement(XmlReader reader)
        {
            while (reader.NodeType == XmlNodeType.Whitespace) reader.Read();

            if (reader.NodeType == XmlNodeType.EndElement) return null;

            XmlSerializer serializer;
            switch (reader.LocalName)
            {
                case "LayoutAnchorablePaneGroup":
                    serializer = new XmlSerializer(typeof(LayoutAnchorablePaneGroup));
                    break;
                case "LayoutAnchorablePane":
                    serializer = new XmlSerializer(typeof(LayoutAnchorablePane));
                    break;
                case "LayoutAnchorable":
                    serializer = new XmlSerializer(typeof(LayoutAnchorable));
                    break;
                case "LayoutDocumentPaneGroup":
                    serializer = new XmlSerializer(typeof(LayoutDocumentPaneGroup));
                    break;
                case "LayoutDocumentPane":
                    serializer = new XmlSerializer(typeof(LayoutDocumentPane));
                    break;
                case "LayoutDocument":
                    serializer = new XmlSerializer(typeof(LayoutDocument));
                    break;
                case "LayoutAnchorGroup":
                    serializer = new XmlSerializer(typeof(LayoutAnchorGroup));
                    break;
                case "LayoutPanel":
                    serializer = new XmlSerializer(typeof(LayoutPanel));
                    break;
                case "LayoutDocumentFloatingWindow":
                    serializer = new XmlSerializer(typeof(LayoutDocumentFloatingWindow));
                    break;
                case "LayoutAnchorableFloatingWindow":
                    serializer = new XmlSerializer(typeof(LayoutAnchorableFloatingWindow));
                    break;
                case "LeftSide":
                case "RightSide":
                case "TopSide":
                case "BottomSide":
                    if (reader.IsEmptyElement)
                    {
                        reader.Read();
                        return null;
                    }

                    return reader.Read();
                default:
                    var type = FindType(reader.LocalName);
                    if (type == null)
                        throw new ArgumentException("AvalonDock.LayoutRoot doesn't know how to deserialize " +
                                                    reader.LocalName);
                    serializer = new XmlSerializer(type);
                    break;
            }

            return serializer.Deserialize(reader);
        }

        #endregion

        #region Events

        public event EventHandler Updated;
        public event EventHandler<LayoutElementEventArgs> ElementAdded;
        public event EventHandler<LayoutElementEventArgs> ElementRemoved;

        #endregion
    }
}