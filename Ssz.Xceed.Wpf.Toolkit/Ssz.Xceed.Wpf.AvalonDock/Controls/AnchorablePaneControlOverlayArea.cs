/*************************************************************************************
   
   Toolkit for WPF

   Copyright (C) 2007-2018 Ssz.Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at https://xceed.com/xceed-toolkit-plus-for-wpf/

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System.Windows;

namespace Ssz.Xceed.Wpf.AvalonDock.Controls
{
    public class AnchorablePaneControlOverlayArea : OverlayArea
    {
        #region Members

        private readonly LayoutAnchorablePaneControl _anchorablePaneControl;

        #endregion

        #region constructors

        internal AnchorablePaneControlOverlayArea(
            IOverlayWindow overlayWindow,
            LayoutAnchorablePaneControl anchorablePaneControl)
            : base(overlayWindow)
        {
            _anchorablePaneControl = anchorablePaneControl;
            SetScreenDetectionArea(new Rect(
                _anchorablePaneControl.PointToScreenDPI(new Point()),
                _anchorablePaneControl.TransformActualSizeToAncestor()));
        }

        #endregion
    }
}