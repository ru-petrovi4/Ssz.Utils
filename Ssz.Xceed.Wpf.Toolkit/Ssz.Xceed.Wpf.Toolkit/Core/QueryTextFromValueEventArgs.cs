/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System;

namespace Ssz.Xceed.Wpf.Toolkit.Core
{
    public class QueryTextFromValueEventArgs : EventArgs
    {
        public QueryTextFromValueEventArgs(object value, string text)
        {
            Value = value;
            Text = text;
        }

        #region Value Property

        public object Value { get; }

        #endregion Value Property

        #region Text Property

        public string Text { get; set; }

        #endregion Text Property
    }
}