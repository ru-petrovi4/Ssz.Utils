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
using System.Linq;

namespace Ssz.Xceed.Wpf.AvalonDock.Layout.Serialization
{
    public abstract class LayoutSerializer
    {
        #region Constructors

        public LayoutSerializer(DockingManager manager)
        {
            if (manager is null)
                throw new ArgumentNullException("manager");

            Manager = manager;
            _previousAnchorables = Manager.Layout.Descendents().OfType<LayoutAnchorable>().ToArray();
            _previousDocuments = Manager.Layout.Descendents().OfType<LayoutDocument>().ToArray();
        }

        #endregion

        #region Properties

        public DockingManager Manager { get; }

        #endregion

        #region Events

        public event EventHandler<LayoutSerializationCallbackEventArgs> LayoutSerializationCallback;

        #endregion

        #region Members

        private readonly LayoutAnchorable[] _previousAnchorables;
        private readonly LayoutDocument[] _previousDocuments;

        #endregion

        #region Methods

        protected virtual void FixupLayout(LayoutRoot layout)
        {
            //fix container panes
            foreach (var lcToAttach in layout.Descendents().OfType<ILayoutPreviousContainer>()
                .Where(lc => lc.PreviousContainerId is not null))
            {
                var paneContainerToAttach = layout.Descendents().OfType<ILayoutPaneSerializable>()
                    .FirstOrDefault(lps => lps.Id == lcToAttach.PreviousContainerId);
                if (paneContainerToAttach is null)
                    throw new ArgumentException(string.Format("Unable to find a pane with id ='{0}'",
                        lcToAttach.PreviousContainerId));

                lcToAttach.PreviousContainer = paneContainerToAttach as ILayoutContainer;
            }


            //now fix the content of the layoutcontents
            foreach (var lcToFix in layout.Descendents().OfType<LayoutAnchorable>().Where(lc => lc.Content is null)
                .ToArray())
            {
                LayoutAnchorable previousAchorable = null;
                if (lcToFix.ContentId is not null)
                    //try find the content in replaced layout
                    previousAchorable = _previousAnchorables.FirstOrDefault(a => a.ContentId == lcToFix.ContentId);

                if (LayoutSerializationCallback is not null)
                {
                    var args = new LayoutSerializationCallbackEventArgs(lcToFix,
                        previousAchorable is not null ? previousAchorable.Content : null);
                    LayoutSerializationCallback(this, args);
                    if (args.Cancel)
                        lcToFix.Close();
                    else if (args.Content is not null)
                        lcToFix.Content = args.Content;
                    else if (args.Model.Content is not null)
                        lcToFix.Hide(false);
                }
                else if (previousAchorable is null)
                {
                    lcToFix.Hide(false);
                }
                else
                {
                    lcToFix.Content = previousAchorable.Content;
                    lcToFix.IconSource = previousAchorable.IconSource;
                }
            }


            foreach (var lcToFix in layout.Descendents().OfType<LayoutDocument>().Where(lc => lc.Content is null)
                .ToArray())
            {
                LayoutDocument previousDocument = null;
                if (lcToFix.ContentId is not null)
                    //try find the content in replaced layout
                    previousDocument = _previousDocuments.FirstOrDefault(a => a.ContentId == lcToFix.ContentId);

                if (LayoutSerializationCallback is not null)
                {
                    var args = new LayoutSerializationCallbackEventArgs(lcToFix,
                        previousDocument is not null ? previousDocument.Content : null);
                    LayoutSerializationCallback(this, args);

                    if (args.Cancel)
                        lcToFix.Close();
                    else if (args.Content is not null)
                        lcToFix.Content = args.Content;
                    else if (args.Model.Content is not null)
                        lcToFix.Close();
                }
                else if (previousDocument is null)
                {
                    lcToFix.Close();
                }
                else
                {
                    lcToFix.Content = previousDocument.Content;
                    lcToFix.IconSource = previousDocument.IconSource;
                }
            }

            layout.CollectGarbage();
        }

        protected void StartDeserialization()
        {
            Manager.SuspendDocumentsSourceBinding = true;
            Manager.SuspendAnchorablesSourceBinding = true;
        }

        protected void EndDeserialization()
        {
            Manager.SuspendDocumentsSourceBinding = false;
            Manager.SuspendAnchorablesSourceBinding = false;
        }

        #endregion
    }
}