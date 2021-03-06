/*************************************************************************************
   
   Toolkit for WPF

   Copyright (C) 2007-2018 Ssz.Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at https://xceed.com/xceed-toolkit-plus-for-wpf/

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System.Linq;
using System.Windows;
using System.Windows.Media;
using Ssz.Xceed.Wpf.AvalonDock.Layout;

namespace Ssz.Xceed.Wpf.AvalonDock.Controls
{
    internal class DocumentPaneGroupDropTarget : DropTarget<LayoutDocumentPaneGroupControl>
    {
        #region Members

        private readonly LayoutDocumentPaneGroupControl _targetPane;

        #endregion

        #region Constructors

        internal DocumentPaneGroupDropTarget(LayoutDocumentPaneGroupControl paneControl, Rect detectionRect,
            DropTargetType type)
            : base(paneControl, detectionRect, type)
        {
            _targetPane = paneControl;
        }

        #endregion

        #region Overrides

        protected override void Drop(LayoutDocumentFloatingWindow floatingWindow)
        {
            var targetModel = _targetPane.Model as ILayoutPane;

            switch (Type)
            {
                case DropTargetType.DocumentPaneGroupDockInside:

                    #region DropTargetType.DocumentPaneGroupDockInside

                {
                    var paneGroupModel = targetModel as LayoutDocumentPaneGroup;
                    var paneModel = paneGroupModel.Children[0] as LayoutDocumentPane;
                    var sourceModel = floatingWindow.RootDocument;

                    paneModel.Children.Insert(0, sourceModel);
                }
                    break;

                #endregion
            }

            base.Drop(floatingWindow);
        }

        protected override void Drop(LayoutAnchorableFloatingWindow floatingWindow)
        {
            var targetModel = _targetPane.Model as ILayoutPane;

            switch (Type)
            {
                case DropTargetType.DocumentPaneGroupDockInside:

                    #region DropTargetType.DocumentPaneGroupDockInside

                {
                    var paneGroupModel = targetModel as LayoutDocumentPaneGroup;
                    var paneModel = paneGroupModel.Children[0] as LayoutDocumentPane;
                    var layoutAnchorablePaneGroup = floatingWindow.RootPanel;

                    var i = 0;
                    foreach (var anchorableToImport in layoutAnchorablePaneGroup.Descendents()
                        .OfType<LayoutAnchorable>().ToArray())
                    {
                        anchorableToImport.SetCanCloseInternal(true);

                        paneModel.Children.Insert(i, anchorableToImport);
                        i++;
                    }
                }
                    break;

                #endregion
            }

            base.Drop(floatingWindow);
        }

        public override Geometry GetPreviewPath(OverlayWindow overlayWindow, LayoutFloatingWindow floatingWindowModel)
        {
            switch (Type)
            {
                case DropTargetType.DocumentPaneGroupDockInside:

                    #region DropTargetType.DocumentPaneGroupDockInside

                {
                    var targetScreenRect = TargetElement.GetScreenArea();
                    targetScreenRect.Offset(-overlayWindow.Left, -overlayWindow.Top);

                    return new RectangleGeometry(targetScreenRect);
                }

                #endregion
            }

            return null;
        }

        #endregion
    }
}