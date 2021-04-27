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
using System.Windows.Controls;
using System.Windows.Markup;
using System.Xml;

namespace Ssz.Xceed.Wpf.AvalonDock.Layout
{
    [ContentProperty("Children")]
    [Serializable]
    public class LayoutDocumentPaneGroup : LayoutPositionableGroup<ILayoutDocumentPane>, ILayoutDocumentPane,
        ILayoutOrientableGroup
    {
        #region Constructors

        public LayoutDocumentPaneGroup()
        {
        }

        public LayoutDocumentPaneGroup(LayoutDocumentPane documentPane)
        {
            Children.Add(documentPane);
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
            return true;
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
            Trace.WriteLine(string.Format("DocumentPaneGroup({0})", Orientation));

            foreach (LayoutElement child in Children)
                child.ConsoleDump(tab + 1);
        }
#endif

        #endregion
    }
}