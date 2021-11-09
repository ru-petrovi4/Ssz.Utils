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
using System.Windows.Markup;
using System.Xml;
using System.Xml.Serialization;

namespace Ssz.Xceed.Wpf.AvalonDock.Layout
{
    [ContentProperty("Children")]
    [Serializable]
    public class LayoutAnchorGroup : LayoutGroup<LayoutAnchorable>, ILayoutPreviousContainer, ILayoutPaneSerializable
    {
        #region Constructors

        #endregion

        #region Overrides

        protected override bool GetVisibility()
        {
            return Children.Count > 0;
        }

        public override void WriteXml(XmlWriter writer)
        {
            if (_id is not null)
                writer.WriteAttributeString("Id", _id);
            if (_previousContainer is not null)
            {
                var paneSerializable = _previousContainer as ILayoutPaneSerializable;
                if (paneSerializable is not null) writer.WriteAttributeString("PreviousContainerId", paneSerializable.Id);
            }

            base.WriteXml(writer);
        }

        public override void ReadXml(XmlReader reader)
        {
            if (reader.MoveToAttribute("Id"))
                _id = reader.Value;
            if (reader.MoveToAttribute("PreviousContainerId"))
                ((ILayoutPreviousContainer) this).PreviousContainerId = reader.Value;


            base.ReadXml(reader);
        }

        #endregion

        #region ILayoutPreviousContainer Interface

        #region PreviousContainer

        [field: NonSerialized] private ILayoutContainer _previousContainer;

        [XmlIgnore]
        ILayoutContainer ILayoutPreviousContainer.PreviousContainer
        {
            get => _previousContainer;
            set
            {
                if (_previousContainer != value)
                {
                    _previousContainer = value;
                    RaisePropertyChanged("PreviousContainer");
                    var paneSerializable = _previousContainer as ILayoutPaneSerializable;
                    if (paneSerializable is not null &&
                        paneSerializable.Id is null)
                        paneSerializable.Id = Guid.NewGuid().ToString();
                }
            }
        }

        #endregion

        string ILayoutPreviousContainer.PreviousContainerId { get; set; }

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