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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Ssz.Xceed.Wpf.AvalonDock.Layout;

namespace Ssz.Xceed.Wpf.AvalonDock.Controls
{
    internal class DockingManagerDropTarget : DropTarget<DockingManager>
    {
        #region Members

        private readonly DockingManager _manager;

        #endregion

        #region Constructors

        internal DockingManagerDropTarget(DockingManager manager, Rect detectionRect, DropTargetType type)
            : base(manager, detectionRect, type)
        {
            _manager = manager;
        }

        #endregion

        #region Overrides

        protected override void Drop(LayoutAnchorableFloatingWindow floatingWindow)
        {
            switch (Type)
            {
                case DropTargetType.DockingManagerDockLeft:

                    #region DropTargetType.DockingManagerDockLeft

                {
                    if (_manager.Layout.RootPanel.Orientation != Orientation.Horizontal &&
                        _manager.Layout.RootPanel.Children.Count == 1)
                        _manager.Layout.RootPanel.Orientation = Orientation.Horizontal;

                    if (_manager.Layout.RootPanel.Orientation == Orientation.Horizontal)
                    {
                        var layoutAnchorablePaneGroup = floatingWindow.RootPanel;
                        if (layoutAnchorablePaneGroup is not null &&
                            layoutAnchorablePaneGroup.Orientation == Orientation.Horizontal)
                        {
                            var childrenToTransfer = layoutAnchorablePaneGroup.Children.ToArray();
                            for (var i = 0; i < childrenToTransfer.Length; i++)
                                _manager.Layout.RootPanel.Children.Insert(i, childrenToTransfer[i]);
                        }
                        else
                        {
                            _manager.Layout.RootPanel.Children.Insert(0, floatingWindow.RootPanel);
                        }
                    }
                    else
                    {
                        var newOrientedPanel = new LayoutPanel
                        {
                            Orientation = Orientation.Horizontal
                        };

                        newOrientedPanel.Children.Add(floatingWindow.RootPanel);
                        newOrientedPanel.Children.Add(_manager.Layout.RootPanel);

                        _manager.Layout.RootPanel = newOrientedPanel;
                    }
                }
                    break;

                #endregion

                case DropTargetType.DockingManagerDockRight:

                    #region DropTargetType.DockingManagerDockRight

                {
                    if (_manager.Layout.RootPanel.Orientation != Orientation.Horizontal &&
                        _manager.Layout.RootPanel.Children.Count == 1)
                        _manager.Layout.RootPanel.Orientation = Orientation.Horizontal;

                    if (_manager.Layout.RootPanel.Orientation == Orientation.Horizontal)
                    {
                        var layoutAnchorablePaneGroup = floatingWindow.RootPanel;
                        if (layoutAnchorablePaneGroup is not null &&
                            layoutAnchorablePaneGroup.Orientation == Orientation.Horizontal)
                        {
                            var childrenToTransfer = layoutAnchorablePaneGroup.Children.ToArray();
                            for (var i = 0; i < childrenToTransfer.Length; i++)
                                _manager.Layout.RootPanel.Children.Add(childrenToTransfer[i]);
                        }
                        else
                        {
                            _manager.Layout.RootPanel.Children.Add(floatingWindow.RootPanel);
                        }
                    }
                    else
                    {
                        var newOrientedPanel = new LayoutPanel
                        {
                            Orientation = Orientation.Horizontal
                        };

                        newOrientedPanel.Children.Add(floatingWindow.RootPanel);
                        newOrientedPanel.Children.Insert(0, _manager.Layout.RootPanel);


                        _manager.Layout.RootPanel = newOrientedPanel;
                    }
                }
                    break;

                #endregion

                case DropTargetType.DockingManagerDockTop:

                    #region DropTargetType.DockingManagerDockTop

                {
                    if (_manager.Layout.RootPanel.Orientation != Orientation.Vertical &&
                        _manager.Layout.RootPanel.Children.Count == 1)
                        _manager.Layout.RootPanel.Orientation = Orientation.Vertical;

                    if (_manager.Layout.RootPanel.Orientation == Orientation.Vertical)
                    {
                        var layoutAnchorablePaneGroup = floatingWindow.RootPanel;
                        if (layoutAnchorablePaneGroup is not null &&
                            layoutAnchorablePaneGroup.Orientation == Orientation.Vertical)
                        {
                            var childrenToTransfer = layoutAnchorablePaneGroup.Children.ToArray();
                            for (var i = 0; i < childrenToTransfer.Length; i++)
                                _manager.Layout.RootPanel.Children.Insert(i, childrenToTransfer[i]);
                        }
                        else
                        {
                            _manager.Layout.RootPanel.Children.Insert(0, floatingWindow.RootPanel);
                        }
                    }
                    else
                    {
                        var newOrientedPanel = new LayoutPanel
                        {
                            Orientation = Orientation.Vertical
                        };

                        newOrientedPanel.Children.Add(floatingWindow.RootPanel);
                        newOrientedPanel.Children.Add(_manager.Layout.RootPanel);

                        _manager.Layout.RootPanel = newOrientedPanel;
                    }
                }
                    break;

                #endregion

                case DropTargetType.DockingManagerDockBottom:

                    #region DropTargetType.DockingManagerDockBottom

                {
                    if (_manager.Layout.RootPanel.Orientation != Orientation.Vertical &&
                        _manager.Layout.RootPanel.Children.Count == 1)
                        _manager.Layout.RootPanel.Orientation = Orientation.Vertical;

                    if (_manager.Layout.RootPanel.Orientation == Orientation.Vertical)
                    {
                        var layoutAnchorablePaneGroup = floatingWindow.RootPanel;
                        if (layoutAnchorablePaneGroup is not null &&
                            layoutAnchorablePaneGroup.Orientation == Orientation.Vertical)
                        {
                            var childrenToTransfer = layoutAnchorablePaneGroup.Children.ToArray();
                            for (var i = 0; i < childrenToTransfer.Length; i++)
                                _manager.Layout.RootPanel.Children.Add(childrenToTransfer[i]);
                        }
                        else
                        {
                            _manager.Layout.RootPanel.Children.Add(floatingWindow.RootPanel);
                        }
                    }
                    else
                    {
                        var newOrientedPanel = new LayoutPanel
                        {
                            Orientation = Orientation.Vertical
                        };

                        newOrientedPanel.Children.Add(floatingWindow.RootPanel);
                        newOrientedPanel.Children.Insert(0, _manager.Layout.RootPanel);

                        _manager.Layout.RootPanel = newOrientedPanel;
                    }
                }
                    break;

                #endregion
            }


            base.Drop(floatingWindow);
        }

        public override Geometry GetPreviewPath(OverlayWindow overlayWindow, LayoutFloatingWindow floatingWindowModel)
        {
            var anchorableFloatingWindowModel = floatingWindowModel as LayoutAnchorableFloatingWindow;
            var layoutAnchorablePane = anchorableFloatingWindowModel.RootPanel as ILayoutPositionableElement;
            var layoutAnchorablePaneWithActualSize =
                anchorableFloatingWindowModel.RootPanel as ILayoutPositionableElementWithActualSize;

            var targetScreenRect = TargetElement.GetScreenArea();

            switch (Type)
            {
                case DropTargetType.DockingManagerDockLeft:
                {
                    var desideredWidth = layoutAnchorablePane.DockWidth.IsAbsolute
                        ? layoutAnchorablePane.DockWidth.Value
                        : layoutAnchorablePaneWithActualSize.ActualWidth;
                    var previewBoxRect = new Rect(
                        targetScreenRect.Left - overlayWindow.Left,
                        targetScreenRect.Top - overlayWindow.Top,
                        Math.Min(desideredWidth, targetScreenRect.Width / 2.0),
                        targetScreenRect.Height);

                    return new RectangleGeometry(previewBoxRect);
                }
                case DropTargetType.DockingManagerDockTop:
                {
                    var desideredHeight = layoutAnchorablePane.DockHeight.IsAbsolute
                        ? layoutAnchorablePane.DockHeight.Value
                        : layoutAnchorablePaneWithActualSize.ActualHeight;
                    var previewBoxRect = new Rect(
                        targetScreenRect.Left - overlayWindow.Left,
                        targetScreenRect.Top - overlayWindow.Top,
                        targetScreenRect.Width,
                        Math.Min(desideredHeight, targetScreenRect.Height / 2.0));

                    return new RectangleGeometry(previewBoxRect);
                }
                case DropTargetType.DockingManagerDockRight:
                {
                    var desideredWidth = layoutAnchorablePane.DockWidth.IsAbsolute
                        ? layoutAnchorablePane.DockWidth.Value
                        : layoutAnchorablePaneWithActualSize.ActualWidth;
                    var previewBoxRect = new Rect(
                        targetScreenRect.Right - overlayWindow.Left -
                        Math.Min(desideredWidth, targetScreenRect.Width / 2.0),
                        targetScreenRect.Top - overlayWindow.Top,
                        Math.Min(desideredWidth, targetScreenRect.Width / 2.0),
                        targetScreenRect.Height);

                    return new RectangleGeometry(previewBoxRect);
                }
                case DropTargetType.DockingManagerDockBottom:
                {
                    var desideredHeight = layoutAnchorablePane.DockHeight.IsAbsolute
                        ? layoutAnchorablePane.DockHeight.Value
                        : layoutAnchorablePaneWithActualSize.ActualHeight;
                    var previewBoxRect = new Rect(
                        targetScreenRect.Left - overlayWindow.Left,
                        targetScreenRect.Bottom - overlayWindow.Top -
                        Math.Min(desideredHeight, targetScreenRect.Height / 2.0),
                        targetScreenRect.Width,
                        Math.Min(desideredHeight, targetScreenRect.Height / 2.0));

                    return new RectangleGeometry(previewBoxRect);
                }
            }


            throw new InvalidOperationException();
        }

        #endregion
    }
}