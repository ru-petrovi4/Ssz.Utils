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
    public enum DropAreaType
    {
        DockingManager,
        DocumentPane,
        DocumentPaneGroup,
        AnchorablePane
    }


    public interface IDropArea
    {
        Rect DetectionRect { get; }

        DropAreaType Type { get; }
    }

    public class DropArea<T> : IDropArea where T : FrameworkElement
    {
        #region Constructors

        internal DropArea(T areaElement, DropAreaType type)
        {
            AreaElement = areaElement;
            DetectionRect = areaElement.GetScreenArea();
            Type = type;
        }

        #endregion

        #region Members

        #endregion

        #region Properties

        public Rect DetectionRect { get; }

        public DropAreaType Type { get; }

        public T AreaElement { get; }

        #endregion
    }
}