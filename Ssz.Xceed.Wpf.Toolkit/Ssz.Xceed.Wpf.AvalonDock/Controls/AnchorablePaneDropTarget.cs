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
    internal class AnchorablePaneDropTarget : DropTarget<LayoutAnchorablePaneControl>
    {
        #region Members

        private readonly LayoutAnchorablePaneControl _targetPane;
        private readonly int _tabIndex = -1;

        #endregion

        #region Constructors

        internal AnchorablePaneDropTarget(LayoutAnchorablePaneControl paneControl, Rect detectionRect,
            DropTargetType type)
            : base(paneControl, detectionRect, type)
        {
            _targetPane = paneControl;
        }

        internal AnchorablePaneDropTarget(LayoutAnchorablePaneControl paneControl, Rect detectionRect,
            DropTargetType type, int tabIndex)
            : base(paneControl, detectionRect, type)
        {
            _targetPane = paneControl;
            _tabIndex = tabIndex;
        }

        #endregion

        #region Overrides

        protected override void Drop(LayoutAnchorableFloatingWindow floatingWindow)
        {
            var targetModel = _targetPane.Model as ILayoutAnchorablePane;
            var anchorableActive = floatingWindow.Descendents().OfType<LayoutAnchorable>().FirstOrDefault();

            switch (Type)
            {
                case DropTargetType.AnchorablePaneDockBottom:

                    #region DropTargetType.AnchorablePaneDockBottom

                {
                    var parentModel = targetModel.Parent as ILayoutGroup;
                    var parentModelOrientable = targetModel.Parent as ILayoutOrientableGroup;
                    var insertToIndex = parentModel.IndexOfChild(targetModel);

                    if (parentModelOrientable.Orientation != Orientation.Vertical &&
                        parentModel.ChildrenCount == 1)
                        parentModelOrientable.Orientation = Orientation.Vertical;

                    if (parentModelOrientable.Orientation == Orientation.Vertical)
                    {
                        var layoutAnchorablePaneGroup = floatingWindow.RootPanel;
                        if (layoutAnchorablePaneGroup is not null &&
                            (layoutAnchorablePaneGroup.Children.Count == 1 ||
                             layoutAnchorablePaneGroup.Orientation == Orientation.Vertical))
                        {
                            var anchorablesToMove = layoutAnchorablePaneGroup.Children.ToArray();
                            for (var i = 0; i < anchorablesToMove.Length; i++)
                                parentModel.InsertChildAt(insertToIndex + 1 + i, anchorablesToMove[i]);
                        }
                        else
                        {
                            parentModel.InsertChildAt(insertToIndex + 1, floatingWindow.RootPanel);
                        }
                    }
                    else
                    {
                        var targetModelAsPositionableElement = targetModel as ILayoutPositionableElement;
                        var newOrientedPanel = new LayoutAnchorablePaneGroup
                        {
                            Orientation = Orientation.Vertical,
                            DockWidth = targetModelAsPositionableElement.DockWidth,
                            DockHeight = targetModelAsPositionableElement.DockHeight
                        };

                        parentModel.InsertChildAt(insertToIndex, newOrientedPanel);
                        newOrientedPanel.Children.Add(targetModel);
                        newOrientedPanel.Children.Add(floatingWindow.RootPanel);
                    }
                }
                    break;

                #endregion

                case DropTargetType.AnchorablePaneDockTop:

                    #region DropTargetType.AnchorablePaneDockTop

                {
                    var parentModel = targetModel.Parent as ILayoutGroup;
                    var parentModelOrientable = targetModel.Parent as ILayoutOrientableGroup;
                    var insertToIndex = parentModel.IndexOfChild(targetModel);

                    if (parentModelOrientable.Orientation != Orientation.Vertical &&
                        parentModel.ChildrenCount == 1)
                        parentModelOrientable.Orientation = Orientation.Vertical;

                    if (parentModelOrientable.Orientation == Orientation.Vertical)
                    {
                        var layoutAnchorablePaneGroup = floatingWindow.RootPanel;
                        if (layoutAnchorablePaneGroup is not null &&
                            (layoutAnchorablePaneGroup.Children.Count == 1 ||
                             layoutAnchorablePaneGroup.Orientation == Orientation.Vertical))
                        {
                            var anchorablesToMove = layoutAnchorablePaneGroup.Children.ToArray();
                            for (var i = 0; i < anchorablesToMove.Length; i++)
                                parentModel.InsertChildAt(insertToIndex + i, anchorablesToMove[i]);
                        }
                        else
                        {
                            parentModel.InsertChildAt(insertToIndex, floatingWindow.RootPanel);
                        }
                    }
                    else
                    {
                        var targetModelAsPositionableElement = targetModel as ILayoutPositionableElement;
                        var newOrientedPanel = new LayoutAnchorablePaneGroup
                        {
                            Orientation = Orientation.Vertical,
                            DockWidth = targetModelAsPositionableElement.DockWidth,
                            DockHeight = targetModelAsPositionableElement.DockHeight
                        };

                        parentModel.InsertChildAt(insertToIndex, newOrientedPanel);
                        //the floating window must be added after the target modal as it could be raise a CollectGarbage call
                        newOrientedPanel.Children.Add(targetModel);
                        newOrientedPanel.Children.Insert(0, floatingWindow.RootPanel);
                    }
                }
                    break;

                #endregion

                case DropTargetType.AnchorablePaneDockLeft:

                    #region DropTargetType.AnchorablePaneDockLeft

                {
                    var parentModel = targetModel.Parent as ILayoutGroup;
                    var parentModelOrientable = targetModel.Parent as ILayoutOrientableGroup;
                    var insertToIndex = parentModel.IndexOfChild(targetModel);

                    if (parentModelOrientable.Orientation != Orientation.Horizontal &&
                        parentModel.ChildrenCount == 1)
                        parentModelOrientable.Orientation = Orientation.Horizontal;

                    if (parentModelOrientable.Orientation == Orientation.Horizontal)
                    {
                        var layoutAnchorablePaneGroup = floatingWindow.RootPanel;
                        if (layoutAnchorablePaneGroup is not null &&
                            (layoutAnchorablePaneGroup.Children.Count == 1 ||
                             layoutAnchorablePaneGroup.Orientation == Orientation.Horizontal))
                        {
                            var anchorablesToMove = layoutAnchorablePaneGroup.Children.ToArray();
                            for (var i = 0; i < anchorablesToMove.Length; i++)
                                parentModel.InsertChildAt(insertToIndex + i, anchorablesToMove[i]);
                        }
                        else
                        {
                            parentModel.InsertChildAt(insertToIndex, floatingWindow.RootPanel);
                        }
                    }
                    else
                    {
                        var targetModelAsPositionableElement = targetModel as ILayoutPositionableElement;
                        var newOrientedPanel = new LayoutAnchorablePaneGroup
                        {
                            Orientation = Orientation.Horizontal,
                            DockWidth = targetModelAsPositionableElement.DockWidth,
                            DockHeight = targetModelAsPositionableElement.DockHeight
                        };

                        parentModel.InsertChildAt(insertToIndex, newOrientedPanel);
                        //the floating window must be added after the target modal as it could be raise a CollectGarbage call
                        newOrientedPanel.Children.Add(targetModel);
                        newOrientedPanel.Children.Insert(0, floatingWindow.RootPanel);
                    }
                }
                    break;

                #endregion

                case DropTargetType.AnchorablePaneDockRight:

                    #region DropTargetType.AnchorablePaneDockRight

                {
                    var parentModel = targetModel.Parent as ILayoutGroup;
                    var parentModelOrientable = targetModel.Parent as ILayoutOrientableGroup;
                    var insertToIndex = parentModel.IndexOfChild(targetModel);

                    if (parentModelOrientable.Orientation != Orientation.Horizontal &&
                        parentModel.ChildrenCount == 1)
                        parentModelOrientable.Orientation = Orientation.Horizontal;

                    if (parentModelOrientable.Orientation == Orientation.Horizontal)
                    {
                        var layoutAnchorablePaneGroup = floatingWindow.RootPanel;
                        if (layoutAnchorablePaneGroup is not null &&
                            (layoutAnchorablePaneGroup.Children.Count == 1 ||
                             layoutAnchorablePaneGroup.Orientation == Orientation.Horizontal))
                        {
                            var anchorablesToMove = layoutAnchorablePaneGroup.Children.ToArray();
                            for (var i = 0; i < anchorablesToMove.Length; i++)
                                parentModel.InsertChildAt(insertToIndex + 1 + i, anchorablesToMove[i]);
                        }
                        else
                        {
                            parentModel.InsertChildAt(insertToIndex + 1, floatingWindow.RootPanel);
                        }
                    }
                    else
                    {
                        var targetModelAsPositionableElement = targetModel as ILayoutPositionableElement;
                        var newOrientedPanel = new LayoutAnchorablePaneGroup
                        {
                            Orientation = Orientation.Horizontal,
                            DockWidth = targetModelAsPositionableElement.DockWidth,
                            DockHeight = targetModelAsPositionableElement.DockHeight
                        };

                        parentModel.InsertChildAt(insertToIndex, newOrientedPanel);
                        newOrientedPanel.Children.Add(targetModel);
                        newOrientedPanel.Children.Add(floatingWindow.RootPanel);
                    }
                }
                    break;

                #endregion


                case DropTargetType.AnchorablePaneDockInside:

                    #region DropTargetType.AnchorablePaneDockInside

                {
                    var paneModel = targetModel as LayoutAnchorablePane;
                    var layoutAnchorablePaneGroup = floatingWindow.RootPanel;

                    var i = _tabIndex == -1 ? 0 : _tabIndex;
                    foreach (var anchorableToImport in
                        layoutAnchorablePaneGroup.Descendents().OfType<LayoutAnchorable>().ToArray())
                    {
                        paneModel.Children.Insert(i, anchorableToImport);
                        i++;
                    }
                }
                    break;

                #endregion
            }

            anchorableActive.IsActive = true;

            base.Drop(floatingWindow);
        }

        public override Geometry GetPreviewPath(OverlayWindow overlayWindow, LayoutFloatingWindow floatingWindowModel)
        {
            //var anchorablePaneDropTarget = target as AnchorablePaneDropTarget;
            var anchorableFloatingWindowModel = floatingWindowModel as LayoutAnchorableFloatingWindow;
            var layoutAnchorablePane = anchorableFloatingWindowModel.RootPanel as ILayoutPositionableElement;
            var layoutAnchorablePaneWithActualSize =
                anchorableFloatingWindowModel.RootPanel as ILayoutPositionableElementWithActualSize;

            switch (Type)
            {
                case DropTargetType.AnchorablePaneDockBottom:
                {
                    var targetScreenRect = TargetElement.GetScreenArea();
                    targetScreenRect.Offset(-overlayWindow.Left, -overlayWindow.Top);

                    targetScreenRect.Offset(0.0, targetScreenRect.Height / 2.0);
                    targetScreenRect.Height /= 2.0;

                    return new RectangleGeometry(targetScreenRect);
                }
                case DropTargetType.AnchorablePaneDockTop:
                {
                    var targetScreenRect = TargetElement.GetScreenArea();
                    targetScreenRect.Offset(-overlayWindow.Left, -overlayWindow.Top);

                    targetScreenRect.Height /= 2.0;

                    return new RectangleGeometry(targetScreenRect);
                }
                case DropTargetType.AnchorablePaneDockLeft:
                {
                    var targetScreenRect = TargetElement.GetScreenArea();
                    targetScreenRect.Offset(-overlayWindow.Left, -overlayWindow.Top);

                    targetScreenRect.Width /= 2.0;

                    return new RectangleGeometry(targetScreenRect);
                }
                case DropTargetType.AnchorablePaneDockRight:
                {
                    var targetScreenRect = TargetElement.GetScreenArea();
                    targetScreenRect.Offset(-overlayWindow.Left, -overlayWindow.Top);

                    targetScreenRect.Offset(targetScreenRect.Width / 2.0, 0.0);
                    targetScreenRect.Width /= 2.0;

                    return new RectangleGeometry(targetScreenRect);
                }
                case DropTargetType.AnchorablePaneDockInside:
                {
                    var targetScreenRect = TargetElement.GetScreenArea();
                    targetScreenRect.Offset(-overlayWindow.Left, -overlayWindow.Top);

                    if (_tabIndex == -1) return new RectangleGeometry(targetScreenRect);

                    var translatedDetectionRect = new Rect(DetectionRects[0].TopLeft, DetectionRects[0].BottomRight);
                    translatedDetectionRect.Offset(-overlayWindow.Left, -overlayWindow.Top);
                    var pathFigure = new PathFigure();
                    pathFigure.StartPoint = targetScreenRect.TopLeft;
                    pathFigure.Segments.Add(new LineSegment
                        {Point = new Point(targetScreenRect.Left, translatedDetectionRect.Top)});
                    pathFigure.Segments.Add(new LineSegment {Point = translatedDetectionRect.TopLeft});
                    pathFigure.Segments.Add(new LineSegment {Point = translatedDetectionRect.BottomLeft});
                    pathFigure.Segments.Add(new LineSegment {Point = translatedDetectionRect.BottomRight});
                    pathFigure.Segments.Add(new LineSegment {Point = translatedDetectionRect.TopRight});
                    pathFigure.Segments.Add(new LineSegment
                        {Point = new Point(targetScreenRect.Right, translatedDetectionRect.Top)});
                    pathFigure.Segments.Add(new LineSegment {Point = targetScreenRect.TopRight});
                    pathFigure.IsClosed = true;
                    pathFigure.IsFilled = true;
                    pathFigure.Freeze();
                    return new PathGeometry(new[] {pathFigure});
                }
            }

            return null;
        }

        #endregion
    }
}