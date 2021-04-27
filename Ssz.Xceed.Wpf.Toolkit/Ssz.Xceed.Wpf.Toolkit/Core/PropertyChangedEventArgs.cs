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
using System.Windows;

namespace Ssz.Xceed.Wpf.Toolkit.Core
{
    public class PropertyChangedEventArgs<T> : RoutedEventArgs
    {
        #region Constructors

        public PropertyChangedEventArgs(RoutedEvent Event, T oldValue, T newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
            RoutedEvent = Event;
        }

        #endregion

        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            var handler = (PropertyChangedEventHandler<T>) genericHandler;
            handler(genericTarget, this);
        }

        #region NewValue Property

        public T NewValue { get; }

        #endregion

        #region OldValue Property

        public T OldValue { get; }

        #endregion
    }
}