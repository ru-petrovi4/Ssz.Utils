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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors
{
    public class EnumComboBoxEditor : ComboBoxEditor
    {
        protected override IEnumerable CreateItemsSource(PropertyItem propertyItem)
        {
            return GetValues(propertyItem.PropertyType);
        }

        private static object[] GetValues(Type enumType)
        {
            var values = new List<object>();

            if (enumType is not null)
            {
                var fields = enumType.GetFields().Where(x => x.IsLiteral);
                foreach (var field in fields)
                {
                    // Get array of BrowsableAttribute attributes
                    var attrs = field.GetCustomAttributes(typeof(BrowsableAttribute), false);
                    if (attrs.Length == 1)
                    {
                        // If attribute exists and its value is false continue to the next field...
                        var brAttr = (BrowsableAttribute) attrs[0];
                        if (brAttr.Browsable == false)
                            continue;
                    }

                    values.Add(field.GetValue(enumType));
                }
            }

            return values.ToArray();
        }
    }
}