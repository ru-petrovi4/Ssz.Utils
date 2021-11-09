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
using System.Xml;

namespace Ssz.Xceed.Wpf.AvalonDock.Layout
{
    [Serializable]
    public class LayoutDocument : LayoutContent
    {
        #region Internal Methods

        internal bool CloseDocument()
        {
            if (TestCanClose())
            {
                CloseInternal();
                return true;
            }

            return false;
        }

        #endregion

        #region Properties

        #region CanMove

        internal bool _canMove = true;

        public bool CanMove
        {
            get => _canMove;
            set
            {
                if (_canMove != value)
                {
                    _canMove = value;
                    RaisePropertyChanged("CanMove");
                }
            }
        }

        #endregion

        #region IsVisible

        public bool IsVisible
        {
            get => _isVisible;
            internal set => _isVisible = value;
        }

        private bool _isVisible = true;

        #endregion

        #region Description

        private string _description;

        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    RaisePropertyChanged("Description");
                }
            }
        }

        #endregion

        #endregion

        #region Overrides

        public override void WriteXml(XmlWriter writer)
        {
            base.WriteXml(writer);

            if (!string.IsNullOrWhiteSpace(Description))
                writer.WriteAttributeString("Description", Description);
            if (!CanMove)
                writer.WriteAttributeString("CanMove", CanMove.ToString());
        }

        public override void ReadXml(XmlReader reader)
        {
            if (reader.MoveToAttribute("Description"))
                Description = reader.Value;
            if (reader.MoveToAttribute("CanMove"))
                CanMove = bool.Parse(reader.Value);

            base.ReadXml(reader);
        }

        public override void Close()
        {
            if (Root is not null && Root.Manager is not null)
            {
                var dockingManager = Root.Manager;
                dockingManager._ExecuteCloseCommand(this);
            }
            else
            {
                CloseDocument();
            }
        }

#if TRACE
        public override void ConsoleDump(int tab)
        {
            Trace.Write(new string(' ', tab * 4));
            Trace.WriteLine("Document()");
        }
#endif

        protected override void InternalDock()
        {
            var root = Root as LayoutRoot;
            LayoutDocumentPane documentPane = null;
            if (root.LastFocusedDocument is not null &&
                root.LastFocusedDocument != this)
                documentPane = root.LastFocusedDocument.Parent as LayoutDocumentPane;

            if (documentPane is null) documentPane = root.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();


            var added = false;
            if (root.Manager.LayoutUpdateStrategy is not null)
                added = root.Manager.LayoutUpdateStrategy.BeforeInsertDocument(root, this, documentPane);

            if (!added)
            {
                if (documentPane is null)
                    throw new InvalidOperationException(
                        "Layout must contains at least one LayoutDocumentPane in order to host documents");

                documentPane.Children.Add(this);
                added = true;
            }

            if (root.Manager.LayoutUpdateStrategy is not null)
                root.Manager.LayoutUpdateStrategy.AfterInsertDocument(root, this);


            base.InternalDock();
        }

        #endregion
    }
}