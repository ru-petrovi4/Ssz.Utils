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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Converters
{
    internal class ListConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return true;
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is null)
                return null;

            var names = value as string;

            var list = new List<object>();
            if (names is null && value is not null)
            {
                list.Add(value);
            }
            else
            {
                if (names is null)
                    return null;

                foreach (var name in names.Split(',')) list.Add(name.Trim());
            }

            return new ReadOnlyCollection<object>(list);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
            Type destinationType)
        {
            if (destinationType != typeof(string))
                throw new InvalidOperationException("Can only convert to string.");


            var strs = (IList) value;

            if (strs is null)
                return null;

            var sb = new StringBuilder();
            var first = true;
            foreach (var o in strs)
            {
                if (o is null)
                    throw new InvalidOperationException("Property names cannot be null.");

                var s = o as string;
                if (s is null)
                    throw new InvalidOperationException("Does not support serialization of non-string property names.");

#if NET5_0_OR_GREATER
                if (s.Contains(','))
#else
                if (s.Contains(","))
#endif

                    throw new InvalidOperationException("Property names cannot contain commas.");

                if (s.Trim().Length != s.Length)
                    throw new InvalidOperationException(
                        "Property names cannot start or end with whitespace characters.");

                if (!first) sb.Append(", ");
                first = false;

                sb.Append(s);
            }

            return sb.ToString();
        }
    }
}