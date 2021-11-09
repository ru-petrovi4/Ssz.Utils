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
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Ssz.Xceed.Wpf.Toolkit.Core.Utilities;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Ssz.Xceed.Wpf.Toolkit.PropertyGrid
{
    internal class PropertyGridUtilities
    {
        internal static T GetAttribute<T>(PropertyDescriptor property) where T : Attribute
        {
            return property.Attributes.OfType<T>().FirstOrDefault();
        }


        internal static ITypeEditor CreateDefaultEditor(Type propertyType, TypeConverter typeConverter)
        {
            ITypeEditor editor = null;

            if (propertyType == typeof(string))
            {
                editor = new TextBoxEditor();
            }
            else if (propertyType == typeof(bool) || propertyType == typeof(bool?))
            {
                editor = new CheckBoxEditor();
            }
            else if (propertyType == typeof(decimal) || propertyType == typeof(decimal?))
            {
                editor = new DecimalUpDownEditor();
            }
            else if (propertyType == typeof(double) || propertyType == typeof(double?))
            {
                editor = new DoubleUpDownEditor();
            }
            else if (propertyType == typeof(int) || propertyType == typeof(int?))
            {
                editor = new IntegerUpDownEditor();
            }
            else if (propertyType == typeof(short) || propertyType == typeof(short?))
            {
                editor = new ShortUpDownEditor();
            }
            else if (propertyType == typeof(long) || propertyType == typeof(long?))
            {
                editor = new LongUpDownEditor();
            }
            else if (propertyType == typeof(float) || propertyType == typeof(float?))
            {
                editor = new SingleUpDownEditor();
            }
            else if (propertyType == typeof(byte) || propertyType == typeof(byte?))
            {
                editor = new ByteUpDownEditor();
            }
            else if (propertyType == typeof(sbyte) || propertyType == typeof(sbyte?))
            {
                editor = new SByteUpDownEditor();
            }
            else if (propertyType == typeof(uint) || propertyType == typeof(uint?))
            {
                editor = new UIntegerUpDownEditor();
            }
            else if (propertyType == typeof(ulong) || propertyType == typeof(ulong?))
            {
                editor = new ULongUpDownEditor();
            }
            else if (propertyType == typeof(ushort) || propertyType == typeof(ushort?))
            {
                editor = new UShortUpDownEditor();
            }
            else if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
            {
                editor = new DateTimeUpDownEditor();
            }
            else if (propertyType == typeof(Color))
            {
                editor = new ColorEditor();
            }
            else if (propertyType.IsEnum)
            {
                editor = new EnumComboBoxEditor();
            }
            else if (propertyType == typeof(TimeSpan))
            {
                editor = new TimeSpanEditor();
            }
            else if (propertyType == typeof(FontFamily) || propertyType == typeof(FontWeight) ||
                     propertyType == typeof(FontStyle) || propertyType == typeof(FontStretch))
            {
                editor = new FontComboBoxEditor();
            }
            else if (propertyType == typeof(object))
                // If any type of object is possible in the property, default to the TextBoxEditor.
                // Useful in some case (e.g., Button.Content).
                // Can be reconsidered but was the legacy behavior on the PropertyGrid.
            {
                editor = new TextBoxEditor();
            }
            else
            {
                var listType = ListUtilities.GetListItemType(propertyType);

                if (listType is not null)
                {
                    if (!listType.IsPrimitive && !listType.Equals(typeof(string)))
                        editor = new CollectionEditor();
                    else
                        editor = new PrimitiveTypeCollectionEditor();
                }
                else
                {
                    // If the type is not supported, check if there is a converter that supports
                    // string conversion to the object type. Use TextBox in theses cases.
                    // Otherwise, return a TextBlock editor since no valid editor exists.
                    editor = typeConverter is not null && typeConverter.CanConvertFrom(typeof(string))
                        ? new TextBoxEditor()
                        : (ITypeEditor) new TextBlockEditor();
                }
            }

            return editor;
        }
    }
}