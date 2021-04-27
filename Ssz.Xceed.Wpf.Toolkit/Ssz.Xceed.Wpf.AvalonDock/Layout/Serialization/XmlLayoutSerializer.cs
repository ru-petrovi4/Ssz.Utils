/*************************************************************************************
   
   Toolkit for WPF

   Copyright (C) 2007-2018 Ssz.Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at https://xceed.com/xceed-toolkit-plus-for-wpf/

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Ssz.Xceed.Wpf.AvalonDock.Layout.Serialization
{
    public class XmlLayoutSerializer : LayoutSerializer
    {
        #region Constructors

        public XmlLayoutSerializer(DockingManager manager)
            : base(manager)
        {
        }

        #endregion

        #region Public Methods

        public void Serialize(XmlWriter writer)
        {
            var serializer = new XmlSerializer(typeof(LayoutRoot));
            serializer.Serialize(writer, Manager.Layout);
        }

        public void Serialize(TextWriter writer)
        {
            var serializer = new XmlSerializer(typeof(LayoutRoot));
            serializer.Serialize(writer, Manager.Layout);
        }

        public void Serialize(Stream stream)
        {
            var serializer = new XmlSerializer(typeof(LayoutRoot));
            serializer.Serialize(stream, Manager.Layout);
        }

        public void Serialize(string filepath)
        {
            using (var stream = new StreamWriter(filepath))
            {
                Serialize(stream);
            }
        }

        public void Deserialize(Stream stream)
        {
            try
            {
                StartDeserialization();
                var serializer = new XmlSerializer(typeof(LayoutRoot));
                var layout = serializer.Deserialize(stream) as LayoutRoot;
                FixupLayout(layout);
                Manager.Layout = layout;
            }
            finally
            {
                EndDeserialization();
            }
        }

        public void Deserialize(TextReader reader)
        {
            try
            {
                StartDeserialization();
                var serializer = new XmlSerializer(typeof(LayoutRoot));
                var layout = serializer.Deserialize(reader) as LayoutRoot;
                FixupLayout(layout);
                Manager.Layout = layout;
            }
            finally
            {
                EndDeserialization();
            }
        }

        public void Deserialize(XmlReader reader)
        {
            try
            {
                StartDeserialization();
                var serializer = new XmlSerializer(typeof(LayoutRoot));
                var layout = serializer.Deserialize(reader) as LayoutRoot;
                FixupLayout(layout);
                Manager.Layout = layout;
            }
            finally
            {
                EndDeserialization();
            }
        }

        public void Deserialize(string filepath)
        {
            using (var stream = new StreamReader(filepath))
            {
                Deserialize(stream);
            }
        }

        #endregion
    }
}