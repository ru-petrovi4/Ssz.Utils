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
using System.Windows.Controls;
using System.Windows.Media;
using Ssz.Xceed.Wpf.AvalonDock.Layout;

namespace Ssz.Xceed.Wpf.AvalonDock.Controls
{
    internal class DocumentPaneDropTarget : DropTarget<LayoutDocumentPaneControl>
    {
        #region Members

        private readonly LayoutDocumentPaneControl _targetPane;
        private readonly int _tabIndex = -1;

        #endregion

        #region Constructors

        internal DocumentPaneDropTarget(LayoutDocumentPaneControl paneControl, Rect detectionRect, DropTargetType type)
            : base(paneControl, detectionRect, type)
        {
            _targetPane = paneControl;
        }

        internal DocumentPaneDropTarget(LayoutDocumentPaneControl paneControl, Rect detectionRect, DropTargetType type,
            int tabIndex)
            : base(paneControl, detectionRect, type)
        {
            _targetPane = paneControl;
            _tabIndex = tabIndex;
        }

        #endregion

        #region Overrides

        protected override void Drop(LayoutDocumentFloatingWindow floatingWindow)
        {
            var targetModel = _targetPane.Model as ILayoutDocumentPane;

            switch (Type)
            {
                case DropTargetType.DocumentPaneDockBottom:

                    #region DropTargetType.DocumentPaneDockBottom

                {
                    var newLayoutDocumentPane = new LayoutDocumentPane(floatingWindow.RootDocument);
                    var parentModel = targetModel.Parent as LayoutDocumentPaneGroup;

                    if (parentModel == null)
                    {
                        var parentContainer = targetModel.Parent;
                        var newParentModel = new LayoutDocumentPaneGroup {Orientation = Orientation.Vertical};
                        parentContainer.ReplaceChild(targetModel, newParentModel);
                        newParentModel.Children.Add(targetModel as LayoutDocumentPane);
                        newParentModel.Children.Add(newLayoutDocumentPane);
                    }
                    else
                    {
                        var manager = parentModel.Root.Manager;
                        if (!manager.AllowMixedOrientation || parentModel.Orientation == Orientation.Vertical)
                        {
                            parentModel.Orientation = Orientation.Vertical;
                            var targetPaneIndex = parentModel.IndexOfChild(targetModel);
                            parentModel.Children.Insert(targetPaneIndex + 1, newLayoutDocumentPane);
                        }
                        else
                        {
                            var newChildGroup = new LayoutDocumentPaneGroup();
                            newChildGroup.Orientation = Orientation.Vertical;
                            parentModel.ReplaceChild(targetModel, newChildGroup);
                            newChildGroup.Children.Add(targetModel);
                            newChildGroup.Children.Add(newLayoutDocumentPane);
                        }
                    }
                }
                    break;

                #endregion

                case DropTargetType.DocumentPaneDockTop:

                    #region DropTargetType.DocumentPaneDockTop

                {
                    var newLayoutDocumentPane = new LayoutDocumentPane(floatingWindow.RootDocument);
                    var parentModel = targetModel.Parent as LayoutDocumentPaneGroup;

                    if (parentModel == null)
                    {
                        var parentContainer = targetModel.Parent;
                        var newParentModel = new LayoutDocumentPaneGroup {Orientation = Orientation.Vertical};
                        parentContainer.ReplaceChild(targetModel, newParentModel);
                        newParentModel.Children.Add(targetModel as LayoutDocumentPane);
                        newParentModel.Children.Insert(0, newLayoutDocumentPane);
                    }
                    else
                    {
                        var manager = parentModel.Root.Manager;
                        if (!manager.AllowMixedOrientation || parentModel.Orientation == Orientation.Vertical)
                        {
                            parentModel.Orientation = Orientation.Vertical;
                            var targetPaneIndex = parentModel.IndexOfChild(targetModel);
                            parentModel.Children.Insert(targetPaneIndex, newLayoutDocumentPane);
                        }
                        else
                        {
                            var newChildGroup = new LayoutDocumentPaneGroup();
                            newChildGroup.Orientation = Orientation.Vertical;
                            parentModel.ReplaceChild(targetModel, newChildGroup);
                            newChildGroup.Children.Add(newLayoutDocumentPane);
                            newChildGroup.Children.Add(targetModel);
                        }
                    }
                }
                    break;

                #endregion

                case DropTargetType.DocumentPaneDockLeft:

                    #region DropTargetType.DocumentPaneDockLeft

                {
                    var newLayoutDocumentPane = new LayoutDocumentPane(floatingWindow.RootDocument);
                    var parentModel = targetModel.Parent as LayoutDocumentPaneGroup;

                    if (parentModel == null)
                    {
                        var parentContainer = targetModel.Parent;
                        var newParentModel = new LayoutDocumentPaneGroup {Orientation = Orientation.Horizontal};
                        parentContainer.ReplaceChild(targetModel, newParentModel);
                        newParentModel.Children.Add(targetModel);
                        newParentModel.Children.Insert(0, newLayoutDocumentPane);
                    }
                    else
                    {
                        var manager = parentModel.Root.Manager;
                        if (!manager.AllowMixedOrientation || parentModel.Orientation == Orientation.Horizontal)
                        {
                            parentModel.Orientation = Orientation.Horizontal;
                            var targetPaneIndex = parentModel.IndexOfChild(targetModel);
                            parentModel.Children.Insert(targetPaneIndex, newLayoutDocumentPane);
                        }
                        else
                        {
                            var newChildGroup = new LayoutDocumentPaneGroup();
                            newChildGroup.Orientation = Orientation.Horizontal;
                            parentModel.ReplaceChild(targetModel, newChildGroup);
                            newChildGroup.Children.Add(newLayoutDocumentPane);
                            newChildGroup.Children.Add(targetModel);
                        }
                    }
                }
                    break;

                #endregion

                case DropTargetType.DocumentPaneDockRight:

                    #region DropTargetType.DocumentPaneDockRight

                {
                    var newLayoutDocumentPane = new LayoutDocumentPane(floatingWindow.RootDocument);
                    var parentModel = targetModel.Parent as LayoutDocumentPaneGroup;

                    if (parentModel == null)
                    {
                        var parentContainer = targetModel.Parent;
                        var newParentModel = new LayoutDocumentPaneGroup {Orientation = Orientation.Horizontal};
                        parentContainer.ReplaceChild(targetModel, newParentModel);
                        newParentModel.Children.Add(targetModel as LayoutDocumentPane);
                        newParentModel.Children.Add(newLayoutDocumentPane);
                    }
                    else
                    {
                        var manager = parentModel.Root.Manager;
                        if (!manager.AllowMixedOrientation || parentModel.Orientation == Orientation.Horizontal)
                        {
                            parentModel.Orientation = Orientation.Horizontal;
                            var targetPaneIndex = parentModel.IndexOfChild(targetModel);
                            parentModel.Children.Insert(targetPaneIndex + 1, newLayoutDocumentPane);
                        }
                        else
                        {
                            var newChildGroup = new LayoutDocumentPaneGroup();
                            newChildGroup.Orientation = Orientation.Horizontal;
                            parentModel.ReplaceChild(targetModel, newChildGroup);
                            newChildGroup.Children.Add(targetModel);
                            newChildGroup.Children.Add(newLayoutDocumentPane);
                        }
                    }
                }
                    break;

                #endregion

                case DropTargetType.DocumentPaneDockInside:

                    #region DropTargetType.DocumentPaneDockInside

                {
                    var paneModel = targetModel as LayoutDocumentPane;
                    var sourceModel = floatingWindow.RootDocument;

                    var i = 0;
                    if (_tabIndex != -1)
                    {
                        i = _tabIndex;
                    }
                    else
                    {
                        var previousIndex = 0;
                        var previousContainer = ((ILayoutPreviousContainer) sourceModel).PreviousContainer;
                        if (ReferenceEquals(previousContainer, targetModel) && sourceModel.PreviousContainerIndex != -1)
                            previousIndex = sourceModel.PreviousContainerIndex;

                        i = previousIndex;
                    }

                    sourceModel.IsActive = false;
                    paneModel.Children.Insert(i, sourceModel);
                    sourceModel.IsActive = true;
                }
                    break;

                #endregion
            }

            base.Drop(floatingWindow);
        }

        protected override void Drop(LayoutAnchorableFloatingWindow floatingWindow)
        {
            var targetModel = _targetPane.Model as ILayoutDocumentPane;

            switch (Type)
            {
                case DropTargetType.DocumentPaneDockBottom:

                    #region DropTargetType.DocumentPaneDockBottom

                {
                    var parentModel = targetModel.Parent as LayoutDocumentPaneGroup;
                    var newLayoutDocumentPane = new LayoutDocumentPane();

                    if (parentModel == null)
                    {
                        var parentContainer = targetModel.Parent;
                        var newParentModel = new LayoutDocumentPaneGroup {Orientation = Orientation.Vertical};
                        parentContainer.ReplaceChild(targetModel, newParentModel);
                        newParentModel.Children.Add(targetModel as LayoutDocumentPane);
                        newParentModel.Children.Add(newLayoutDocumentPane);
                    }
                    else
                    {
                        var manager = parentModel.Root.Manager;
                        if (!manager.AllowMixedOrientation || parentModel.Orientation == Orientation.Vertical)
                        {
                            parentModel.Orientation = Orientation.Vertical;
                            var targetPaneIndex = parentModel.IndexOfChild(targetModel);
                            parentModel.Children.Insert(targetPaneIndex + 1, newLayoutDocumentPane);
                        }
                        else
                        {
                            var newChildGroup = new LayoutDocumentPaneGroup();
                            newChildGroup.Orientation = Orientation.Vertical;
                            parentModel.ReplaceChild(targetModel, newChildGroup);
                            newChildGroup.Children.Add(targetModel);
                            newChildGroup.Children.Add(newLayoutDocumentPane);
                        }
                    }

                    foreach (var cntToTransfer in floatingWindow.RootPanel.Descendents().OfType<LayoutAnchorable>()
                        .ToArray())
                        newLayoutDocumentPane.Children.Add(cntToTransfer);
                }
                    break;

                #endregion

                case DropTargetType.DocumentPaneDockTop:

                    #region DropTargetType.DocumentPaneDockTop

                {
                    var parentModel = targetModel.Parent as LayoutDocumentPaneGroup;
                    var newLayoutDocumentPane = new LayoutDocumentPane();

                    if (parentModel == null)
                    {
                        var parentContainer = targetModel.Parent;
                        var newParentModel = new LayoutDocumentPaneGroup {Orientation = Orientation.Vertical};
                        parentContainer.ReplaceChild(targetModel, newParentModel);
                        newParentModel.Children.Add(newLayoutDocumentPane);
                        newParentModel.Children.Add(targetModel as LayoutDocumentPane);
                    }
                    else
                    {
                        var manager = parentModel.Root.Manager;
                        if (!manager.AllowMixedOrientation || parentModel.Orientation == Orientation.Vertical)
                        {
                            parentModel.Orientation = Orientation.Vertical;
                            var targetPaneIndex = parentModel.IndexOfChild(targetModel);
                            parentModel.Children.Insert(targetPaneIndex, newLayoutDocumentPane);
                        }
                        else
                        {
                            var newChildGroup = new LayoutDocumentPaneGroup();
                            newChildGroup.Orientation = Orientation.Vertical;
                            parentModel.ReplaceChild(targetModel, newChildGroup);
                            newChildGroup.Children.Add(newLayoutDocumentPane);
                            newChildGroup.Children.Add(targetModel);
                        }
                    }

                    foreach (var cntToTransfer in floatingWindow.RootPanel.Descendents().OfType<LayoutAnchorable>()
                        .ToArray())
                        newLayoutDocumentPane.Children.Add(cntToTransfer);
                }
                    break;

                #endregion

                case DropTargetType.DocumentPaneDockLeft:

                    #region DropTargetType.DocumentPaneDockLeft

                {
                    var parentModel = targetModel.Parent as LayoutDocumentPaneGroup;
                    var newLayoutDocumentPane = new LayoutDocumentPane();

                    if (parentModel == null)
                    {
                        var parentContainer = targetModel.Parent;
                        var newParentModel = new LayoutDocumentPaneGroup {Orientation = Orientation.Horizontal};
                        parentContainer.ReplaceChild(targetModel, newParentModel);
                        newParentModel.Children.Add(newLayoutDocumentPane);
                        newParentModel.Children.Add(targetModel as LayoutDocumentPane);
                    }
                    else
                    {
                        var manager = parentModel.Root.Manager;
                        if (!manager.AllowMixedOrientation || parentModel.Orientation == Orientation.Horizontal)
                        {
                            parentModel.Orientation = Orientation.Horizontal;
                            var targetPaneIndex = parentModel.IndexOfChild(targetModel);
                            parentModel.Children.Insert(targetPaneIndex, newLayoutDocumentPane);
                        }
                        else
                        {
                            var newChildGroup = new LayoutDocumentPaneGroup();
                            newChildGroup.Orientation = Orientation.Horizontal;
                            parentModel.ReplaceChild(targetModel, newChildGroup);
                            newChildGroup.Children.Add(newLayoutDocumentPane);
                            newChildGroup.Children.Add(targetModel);
                        }
                    }

                    foreach (var cntToTransfer in floatingWindow.RootPanel.Descendents().OfType<LayoutAnchorable>()
                        .ToArray())
                        newLayoutDocumentPane.Children.Add(cntToTransfer);
                }
                    break;

                #endregion

                case DropTargetType.DocumentPaneDockRight:

                    #region DropTargetType.DocumentPaneDockRight

                {
                    var parentModel = targetModel.Parent as LayoutDocumentPaneGroup;
                    var newLayoutDocumentPane = new LayoutDocumentPane();

                    if (parentModel == null)
                    {
                        var parentContainer = targetModel.Parent;
                        var newParentModel = new LayoutDocumentPaneGroup {Orientation = Orientation.Horizontal};
                        parentContainer.ReplaceChild(targetModel, newParentModel);
                        newParentModel.Children.Add(targetModel as LayoutDocumentPane);
                        newParentModel.Children.Add(newLayoutDocumentPane);
                    }
                    else
                    {
                        var manager = parentModel.Root.Manager;
                        if (!manager.AllowMixedOrientation || parentModel.Orientation == Orientation.Horizontal)
                        {
                            parentModel.Orientation = Orientation.Horizontal;
                            var targetPaneIndex = parentModel.IndexOfChild(targetModel);
                            parentModel.Children.Insert(targetPaneIndex + 1, newLayoutDocumentPane);
                        }
                        else
                        {
                            var newChildGroup = new LayoutDocumentPaneGroup();
                            newChildGroup.Orientation = Orientation.Horizontal;
                            parentModel.ReplaceChild(targetModel, newChildGroup);
                            newChildGroup.Children.Add(targetModel);
                            newChildGroup.Children.Add(newLayoutDocumentPane);
                        }
                    }

                    foreach (var cntToTransfer in floatingWindow.RootPanel.Descendents().OfType<LayoutAnchorable>()
                        .ToArray())
                        newLayoutDocumentPane.Children.Add(cntToTransfer);
                }
                    break;

                #endregion

                case DropTargetType.DocumentPaneDockInside:

                    #region DropTargetType.DocumentPaneDockInside

                {
                    var paneModel = targetModel as LayoutDocumentPane;
                    var layoutAnchorablePaneGroup = floatingWindow.RootPanel;

                    var checkPreviousContainer = true;
                    var i = 0;
                    if (_tabIndex != -1)
                    {
                        i = _tabIndex;
                        checkPreviousContainer = false;
                    }

                    LayoutAnchorable anchorableToActivate = null;

                    foreach (var anchorableToImport in layoutAnchorablePaneGroup.Descendents()
                        .OfType<LayoutAnchorable>().ToArray())
                    {
                        if (checkPreviousContainer)
                        {
                            var previousContainer = ((ILayoutPreviousContainer) anchorableToImport).PreviousContainer;
                            if (ReferenceEquals(previousContainer, targetModel) &&
                                anchorableToImport.PreviousContainerIndex != -1)
                                i = anchorableToImport.PreviousContainerIndex;
                            checkPreviousContainer = false;
                        }

                        anchorableToImport.SetCanCloseInternal(true);

                        paneModel.Children.Insert(i, anchorableToImport);
                        i++;
                        anchorableToActivate = anchorableToImport;
                    }

                    anchorableToActivate.IsActive = true;
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
                case DropTargetType.DocumentPaneDockInside:
                {
                    var targetScreenRect = TargetElement.GetScreenArea();
                    targetScreenRect.Offset(-overlayWindow.Left, -overlayWindow.Top);

                    if (_tabIndex == -1)
                    {
                        return new RectangleGeometry(targetScreenRect);
                    }

                    var translatedDetectionRect = new Rect(DetectionRects[0].TopLeft, DetectionRects[0].BottomRight);
                    translatedDetectionRect.Offset(-overlayWindow.Left, -overlayWindow.Top);

                    var pathFigure = new PathFigure();
                    pathFigure.StartPoint = targetScreenRect.BottomRight;
                    pathFigure.Segments.Add(new LineSegment
                        {Point = new Point(targetScreenRect.Right, translatedDetectionRect.Bottom)});
                    pathFigure.Segments.Add(new LineSegment {Point = translatedDetectionRect.BottomRight});
                    pathFigure.Segments.Add(new LineSegment {Point = translatedDetectionRect.TopRight});
                    pathFigure.Segments.Add(new LineSegment {Point = translatedDetectionRect.TopLeft});
                    pathFigure.Segments.Add(new LineSegment {Point = translatedDetectionRect.BottomLeft});
                    pathFigure.Segments.Add(new LineSegment
                        {Point = new Point(targetScreenRect.Left, translatedDetectionRect.Bottom)});
                    pathFigure.Segments.Add(new LineSegment {Point = targetScreenRect.BottomLeft});
                    pathFigure.IsClosed = true;
                    pathFigure.IsFilled = true;
                    pathFigure.Freeze();
                    return new PathGeometry(new[] {pathFigure});
                }
                case DropTargetType.DocumentPaneDockBottom:
                {
                    var targetScreenRect = TargetElement.GetScreenArea();
                    targetScreenRect.Offset(-overlayWindow.Left, -overlayWindow.Top);
                    targetScreenRect.Offset(0.0, targetScreenRect.Height / 2.0);
                    targetScreenRect.Height /= 2.0;
                    return new RectangleGeometry(targetScreenRect);
                }
                case DropTargetType.DocumentPaneDockTop:
                {
                    var targetScreenRect = TargetElement.GetScreenArea();
                    targetScreenRect.Offset(-overlayWindow.Left, -overlayWindow.Top);
                    targetScreenRect.Height /= 2.0;
                    return new RectangleGeometry(targetScreenRect);
                }
                case DropTargetType.DocumentPaneDockLeft:
                {
                    var targetScreenRect = TargetElement.GetScreenArea();
                    targetScreenRect.Offset(-overlayWindow.Left, -overlayWindow.Top);
                    targetScreenRect.Width /= 2.0;
                    return new RectangleGeometry(targetScreenRect);
                }
                case DropTargetType.DocumentPaneDockRight:
                {
                    var targetScreenRect = TargetElement.GetScreenArea();
                    targetScreenRect.Offset(-overlayWindow.Left, -overlayWindow.Top);
                    targetScreenRect.Offset(targetScreenRect.Width / 2.0, 0.0);
                    targetScreenRect.Width /= 2.0;
                    return new RectangleGeometry(targetScreenRect);
                }
            }

            return null;
        }

        #endregion
    }
}