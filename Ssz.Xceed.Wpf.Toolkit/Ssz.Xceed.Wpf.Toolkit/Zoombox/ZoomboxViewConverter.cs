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
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using Ssz.Xceed.Wpf.Toolkit.Core.Utilities;

namespace Ssz.Xceed.Wpf.Toolkit.Zoombox
{
    public sealed class ZoomboxViewConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext typeDescriptorContext, Type type)
        {
            return type == typeof(string)
                   || type == typeof(double)
                   || type == typeof(Point)
                   || type == typeof(Rect)
                   || base.CanConvertFrom(typeDescriptorContext, type);
        }

        public override bool CanConvertTo(ITypeDescriptorContext typeDescriptorContext, Type type)
        {
            return type == typeof(string)
                   || base.CanConvertTo(typeDescriptorContext, type);
        }

        public override object ConvertFrom(
            ITypeDescriptorContext typeDescriptorContext,
            CultureInfo cultureInfo,
            object value)
        {
            ZoomboxView result = null;
            if (value is double)
            {
                result = new ZoomboxView((double) value);
            }
            else if (value is Point)
            {
                result = new ZoomboxView((Point) value);
            }
            else if (value is Rect)
            {
                result = new ZoomboxView((Rect) value);
            }
            else if (value is string)
            {
                if (string.IsNullOrEmpty((value as string).Trim()))
                    result = ZoomboxView.Empty;
                else
                    switch ((value as string).Trim().ToLower())
                    {
                        case "center":
                            result = ZoomboxView.Center;
                            break;

                        case "empty":
                            result = ZoomboxView.Empty;
                            break;

                        case "fill":
                            result = ZoomboxView.Fill;
                            break;

                        case "fit":
                            result = ZoomboxView.Fit;
                            break;

                        default:
                            // parse double values; respect the following separators: ' ', ';', or ','
                            var values = new List<double>();
                            foreach (var token in (value as string).Split(new[] {' ', ';', ','},
                                StringSplitOptions.RemoveEmptyEntries))
                            {
                                double d;
                                if (double.TryParse(token, out d)) values.Add(d);
                                if (values.Count >= 4)
                                    // disregard additional values
                                    break;
                            }

                            switch (values.Count)
                            {
                                case 1: // scale
                                    result = new ZoomboxView(values[0]);
                                    break;

                                case 2: // x, y
                                    result = new ZoomboxView(values[0], values[1]);
                                    break;

                                case 3: // scale, x, y
                                    result = new ZoomboxView(values[0], values[1], values[2]);
                                    break;

                                case 4: // x, y, width, height
                                    result = new ZoomboxView(values[0], values[1], values[2], values[3]);
                                    break;
                            }

                            break;
                    }
            }

            return result is null ? base.ConvertFrom(typeDescriptorContext, cultureInfo, value) : result;
        }

        public override object ConvertTo(
            ITypeDescriptorContext typeDescriptorContext,
            CultureInfo cultureInfo,
            object value,
            Type destinationType)
        {
            object result = null;
            var view = value as ZoomboxView;

            if (view is not null)
                if (destinationType == typeof(string))
                {
                    result = "Empty";
                    switch (view.ViewKind)
                    {
                        case ZoomboxViewKind.Absolute:
                            if (PointHelper.IsEmpty(view.Position))
                            {
                                if (!DoubleHelper.IsNaN(view.Scale)) result = view.Scale.ToString();
                            }
                            else if (DoubleHelper.IsNaN(view.Scale))
                            {
                                result = view.Position.X + "," + view.Position.Y;
                            }
                            else
                            {
                                result = view.Scale + ","
                                                    + view.Position.X + ","
                                                    + view.Position.Y;
                            }

                            break;

                        case ZoomboxViewKind.Center:
                            result = "Center";
                            break;

                        case ZoomboxViewKind.Fill:
                            result = "Fill";
                            break;

                        case ZoomboxViewKind.Fit:
                            result = "Fit";
                            break;

                        case ZoomboxViewKind.Region:
                            result = view.Region.X + ","
                                                   + view.Region.Y + ","
                                                   + view.Region.Width + ","
                                                   + view.Region.Height;
                            break;
                    }
                }

            return result is null ? base.ConvertTo(typeDescriptorContext, cultureInfo, value, destinationType) : result;
        }

        #region Converter Static Property

        internal static ZoomboxViewConverter Converter
        {
            get
            {
                if (_converter is null) _converter = new ZoomboxViewConverter();
                return _converter;
            }
        }

        private static ZoomboxViewConverter _converter; //null

        #endregion
    }
}