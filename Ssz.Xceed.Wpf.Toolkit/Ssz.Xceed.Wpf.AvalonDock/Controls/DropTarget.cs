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
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Ssz.Xceed.Wpf.AvalonDock.Layout;

namespace Ssz.Xceed.Wpf.AvalonDock.Controls
{
    internal abstract class DropTarget<T> : DropTargetBase, IDropTarget where T : FrameworkElement
    {
        #region Members

        #endregion

        #region Constructors

        protected DropTarget(T targetElement, Rect detectionRect, DropTargetType type)
        {
            TargetElement = targetElement;
            DetectionRects = new[] {detectionRect};
            Type = type;
        }

        protected DropTarget(T targetElement, IEnumerable<Rect> detectionRects, DropTargetType type)
        {
            TargetElement = targetElement;
            DetectionRects = detectionRects.ToArray();
            Type = type;
        }

        #endregion

        #region Properties

        public Rect[] DetectionRects { get; }

        public T TargetElement { get; }

        public DropTargetType Type { get; }

        #endregion

        #region Overrides

        protected virtual void Drop(LayoutAnchorableFloatingWindow floatingWindow)
        {
        }

        protected virtual void Drop(LayoutDocumentFloatingWindow floatingWindow)
        {
        }

        #endregion

        #region Public Methods

        public void Drop(LayoutFloatingWindow floatingWindow)
        {
            var root = floatingWindow.Root;
            var currentActiveContent = floatingWindow.Root.ActiveContent;
            var fwAsAnchorable = floatingWindow as LayoutAnchorableFloatingWindow;

            if (fwAsAnchorable is not null)
            {
                Drop(fwAsAnchorable);
            }
            else
            {
                var fwAsDocument = floatingWindow as LayoutDocumentFloatingWindow;
                Drop(fwAsDocument);
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                currentActiveContent.IsSelected = false;
                currentActiveContent.IsActive = false;
                currentActiveContent.IsActive = true;
            }), DispatcherPriority.Background);
        }

        public virtual bool HitTest(Point dragPoint)
        {
            return DetectionRects.Any(dr => dr.Contains(dragPoint));
        }

        public abstract Geometry GetPreviewPath(OverlayWindow overlayWindow, LayoutFloatingWindow floatingWindow);

        public void DragEnter()
        {
            SetIsDraggingOver(TargetElement, true);
        }

        public void DragLeave()
        {
            SetIsDraggingOver(TargetElement, false);
        }

        #endregion
    }
}