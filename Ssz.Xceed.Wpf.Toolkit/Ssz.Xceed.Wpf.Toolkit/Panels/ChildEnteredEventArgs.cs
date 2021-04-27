/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System.Windows;

namespace Ssz.Xceed.Wpf.Toolkit.Panels
{
    public class ChildEnteredEventArgs : RoutedEventArgs
    {
        #region Constructors

        public ChildEnteredEventArgs(UIElement child, Rect arrangeRect)
        {
            Child = child;
            ArrangeRect = arrangeRect;
        }

        #endregion

        #region ArrangeRect Property

        public Rect ArrangeRect { get; }

        #endregion

        #region Child Property

        public UIElement Child { get; }

        #endregion
    }
}