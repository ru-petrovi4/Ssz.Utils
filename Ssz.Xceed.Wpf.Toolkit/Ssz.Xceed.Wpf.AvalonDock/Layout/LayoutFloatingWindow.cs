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
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Ssz.Xceed.Wpf.AvalonDock.Layout
{
    [Serializable]
    public abstract class LayoutFloatingWindow : LayoutElement, ILayoutContainer, IXmlSerializable
    {
        #region Constructors

        #endregion

        #region Properties

        #region Children

        public abstract IEnumerable<ILayoutElement> Children { get; }

        #endregion

        #region ChildrenCount

        public abstract int ChildrenCount { get; }

        #endregion

        #region IsValid

        public abstract bool IsValid { get; }

        #endregion

        #endregion

        #region Public Methods

        public abstract void RemoveChild(ILayoutElement element);

        public abstract void ReplaceChild(ILayoutElement oldElement, ILayoutElement newElement);

        public XmlSchema GetSchema()
        {
            return null;
        }

        public abstract void ReadXml(XmlReader reader);

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
    }
}