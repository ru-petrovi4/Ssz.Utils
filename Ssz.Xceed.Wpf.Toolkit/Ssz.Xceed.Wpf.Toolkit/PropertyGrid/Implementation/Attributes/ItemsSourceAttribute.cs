﻿/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System;

namespace Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes
{
    public class ItemsSourceAttribute : Attribute
    {
        public ItemsSourceAttribute(Type type, bool isEditable = false)
        {
            var valueSourceInterface = type.GetInterface(typeof(IItemsSource).FullName);
            if (valueSourceInterface is null)
                throw new ArgumentException("Type must implement the IItemsSource interface.", "type");

            Type = type;
            IsEditable = isEditable;
        }

        public Type Type { get; set; }

        public bool IsEditable { get; set; }
    }
}