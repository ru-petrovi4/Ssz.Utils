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
using System.Windows;

namespace Ssz.Xceed.Wpf.AvalonDock.Themes
{
    public abstract class DictionaryTheme : Theme
    {
        #region Properties

        public ResourceDictionary ThemeResourceDictionary { get; }

        #endregion

        #region Overrides

        public override Uri GetResourceUri()
        {
            return null;
        }

        #endregion

        #region Constructors

        public DictionaryTheme()
        {
        }

        public DictionaryTheme(ResourceDictionary themeResourceDictionary)
        {
            ThemeResourceDictionary = themeResourceDictionary;
        }

        #endregion
    }
}