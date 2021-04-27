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

namespace Ssz.Xceed.Wpf.Toolkit.Core.Input
{
    public delegate void InputValidationErrorEventHandler(object sender, InputValidationErrorEventArgs e);

    public class InputValidationErrorEventArgs : EventArgs
    {
        #region Constructors

        public InputValidationErrorEventArgs(Exception e)
        {
            Exception = e;
        }

        #endregion

        #region Exception Property

        public Exception Exception { get; private set; }

        #endregion

        #region ThrowException Property

        public bool ThrowException { get; set; }

        #endregion
    }
}