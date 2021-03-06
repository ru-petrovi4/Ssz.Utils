﻿/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System.Windows;

namespace Ssz.Xceed.Wpf.Toolkit
{
    public class ItemEventArgs : RoutedEventArgs
    {
        #region Protected Members

        #endregion

        #region Constructor

        internal ItemEventArgs(RoutedEvent routedEvent, object newItem)
            : base(routedEvent)
        {
            Item = newItem;
        }

        #endregion

        #region Property Item

        public object Item { get; }

        #endregion
    }
}