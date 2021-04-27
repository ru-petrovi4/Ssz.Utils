﻿/*************************************************************************************
   
   Toolkit for WPF

   Copyright (C) 2007-2018 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at https://xceed.com/xceed-toolkit-plus-for-wpf/

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

namespace Xceed.Wpf.AvalonDock.Layout
{
  public interface ILayoutContentSelector
  {
    #region Properties

    int SelectedContentIndex
    {
      get; set;
    }

    LayoutContent SelectedContent
    {
      get;
    }

    #endregion

    #region Methods

    int IndexOf( LayoutContent content );

    #endregion
  }
}
