/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System.ComponentModel;

namespace Ssz.Xceed.Wpf.Toolkit
{
    public class AutoCompletingMaskEventArgs : CancelEventArgs
    {
        public AutoCompletingMaskEventArgs(MaskedTextProvider maskedTextProvider, int startPosition,
            int selectionLength, string input)
        {
            AutoCompleteStartPosition = -1;

            MaskedTextProvider = maskedTextProvider;
            StartPosition = startPosition;
            SelectionLength = selectionLength;
            Input = input;
        }

        #region MaskedTextProvider PROPERTY

        public MaskedTextProvider MaskedTextProvider { get; }

        #endregion MaskedTextProvider PROPERTY

        #region StartPosition PROPERTY

        public int StartPosition { get; }

        #endregion StartPosition PROPERTY

        #region SelectionLength PROPERTY

        public int SelectionLength { get; }

        #endregion SelectionLength PROPERTY

        #region Input PROPERTY

        public string Input { get; }

        #endregion Input PROPERTY


        #region AutoCompleteStartPosition PROPERTY

        public int AutoCompleteStartPosition { get; set; }

        #endregion AutoCompleteStartPosition PROPERTY

        #region AutoCompleteText PROPERTY

        public string AutoCompleteText { get; set; }

        #endregion AutoCompleteText PROPERTY
    }
}