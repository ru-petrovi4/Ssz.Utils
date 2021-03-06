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
    public abstract class OverlayArea : IOverlayWindowArea
    {
        #region Constructors

        internal OverlayArea(IOverlayWindow overlayWindow)
        {
            _overlayWindow = overlayWindow;
        }

        #endregion

        #region IOverlayWindowArea

        Rect IOverlayWindowArea.ScreenDetectionArea => _screenDetectionArea.Value;

        #endregion

        #region Internal Methods

        protected void SetScreenDetectionArea(Rect rect)
        {
            _screenDetectionArea = rect;
        }

        #endregion

        #region Members

        private IOverlayWindow _overlayWindow;
        private Rect? _screenDetectionArea;

        #endregion
    }
}