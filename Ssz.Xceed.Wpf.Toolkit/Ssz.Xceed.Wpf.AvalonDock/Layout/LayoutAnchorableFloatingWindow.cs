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
using System.Xml.Serialization;

namespace Ssz.Xceed.Wpf.AvalonDock.Layout
{
    [Serializable]
    [ContentProperty("RootPanel")]
    public class LayoutAnchorableFloatingWindow : LayoutFloatingWindow, ILayoutElementWithVisibility
    {
        #region Constructors

        #endregion

        #region ILayoutElementWithVisibility Interface

        void ILayoutElementWithVisibility.ComputeVisibility()
        {
            ComputeVisibility();
        }

        #endregion

        #region Events

        public event EventHandler IsVisibleChanged;

        #endregion

        #region Members

        private LayoutAnchorablePaneGroup _rootPanel;

        [NonSerialized] private bool _isVisible = true;

        #endregion

        #region Properties

        #region IsSinglePane

        public bool IsSinglePane
        {
            get
            {
                return RootPanel is not null &&
                       RootPanel.Descendents().OfType<ILayoutAnchorablePane>().Where(p => p.IsVisible).Count() == 1;
            }
        }

        #endregion

        #region IsVisible

        [XmlIgnore]
        public bool IsVisible
        {
            get => _isVisible;
            private set
            {
                if (_isVisible != value)
                {
                    RaisePropertyChanging("IsVisible");
                    _isVisible = value;
                    RaisePropertyChanged("IsVisible");
                    if (IsVisibleChanged is not null)
                        IsVisibleChanged(this, EventArgs.Empty);
                }
            }
        }

        #endregion

        #region RootPanel

        public LayoutAnchorablePaneGroup RootPanel
        {
            get => _rootPanel;
            set
            {
                if (_rootPanel != value)
                {
                    RaisePropertyChanging("RootPanel");

                    if (_rootPanel is not null)
                        _rootPanel.ChildrenTreeChanged -= _rootPanel_ChildrenTreeChanged;

                    _rootPanel = value;
                    if (_rootPanel is not null)
                        _rootPanel.Parent = this;

                    if (_rootPanel is not null)
                        _rootPanel.ChildrenTreeChanged += _rootPanel_ChildrenTreeChanged;

                    RaisePropertyChanged("RootPanel");
                    RaisePropertyChanged("IsSinglePane");
                    RaisePropertyChanged("SinglePane");
                    RaisePropertyChanged("Children");
                    RaisePropertyChanged("ChildrenCount");
                    ((ILayoutElementWithVisibility) this).ComputeVisibility();
                }
            }
        }

        #endregion

        #region SinglePane

        public ILayoutAnchorablePane SinglePane
        {
            get
            {
                if (!IsSinglePane)
                    return null;

                var singlePane = RootPanel.Descendents().OfType<LayoutAnchorablePane>().Single(p => p.IsVisible);
                singlePane.UpdateIsDirectlyHostedInFloatingWindow();
                return singlePane;
            }
        }

        #endregion

        #endregion

        #region Overrides

        public override IEnumerable<ILayoutElement> Children
        {
            get
            {
                if (ChildrenCount == 1)
                    yield return RootPanel;
            }
        }

        public override void RemoveChild(ILayoutElement element)
        {
            Debug.Assert(element == RootPanel && element is not null);
            RootPanel = null;
        }

        public override void ReplaceChild(ILayoutElement oldElement, ILayoutElement newElement)
        {
            Debug.Assert(oldElement == RootPanel && oldElement is not null);
            RootPanel = newElement as LayoutAnchorablePaneGroup;
        }

        public override int ChildrenCount
        {
            get
            {
                if (RootPanel is null)
                    return 0;
                return 1;
            }
        }

        public override bool IsValid => RootPanel is not null;

        public override void ReadXml(XmlReader reader)
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
                if (reader.LocalName.Equals(localName) && reader.NodeType == XmlNodeType.EndElement) break;

                if (reader.NodeType == XmlNodeType.Whitespace)
                {
                    reader.Read();
                    continue;
                }

                XmlSerializer serializer;
                if (reader.LocalName.Equals("LayoutAnchorablePaneGroup"))
                {
                    serializer = new XmlSerializer(typeof(LayoutAnchorablePaneGroup));
                }
                else
                {
                    var type = LayoutRoot.FindType(reader.LocalName);
                    if (type is null)
                        throw new ArgumentException(
                            "AvalonDock.LayoutAnchorableFloatingWindow doesn't know how to deserialize " +
                            reader.LocalName);
                    serializer = new XmlSerializer(type);
                }

                RootPanel = (LayoutAnchorablePaneGroup) serializer.Deserialize(reader);
            }

            reader.ReadEndElement();
        }

#if TRACE
        public override void ConsoleDump(int tab)
        {
            Trace.Write(new string(' ', tab * 4));
            Trace.WriteLine("FloatingAnchorableWindow()");

            RootPanel.ConsoleDump(tab + 1);
        }
#endif

        #endregion

        #region Private Methods

        private void _rootPanel_ChildrenTreeChanged(object sender, ChildrenTreeChangedEventArgs e)
        {
            RaisePropertyChanged("IsSinglePane");
            RaisePropertyChanged("SinglePane");
        }

        private void ComputeVisibility()
        {
            if (RootPanel is not null)
                IsVisible = RootPanel.IsVisible;
            else
                IsVisible = false;
        }

        #endregion
    }
}