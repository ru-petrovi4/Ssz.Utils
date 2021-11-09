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
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Ssz.Xceed.Wpf.Toolkit.Core.Converters
{
  /// <summary>
  ///     This converter allow to blend two colors into one based on a specified ratio
  /// </summary>
  public class ColorBlendConverter : IValueConverter
    {
        private double _blendedColorRatio;

        /// <summary>
        ///     The ratio of the blended color. Must be between 0 and 1.
        /// </summary>
        public double BlendedColorRatio
        {
            get => _blendedColorRatio;

            set
            {
                if (value < 0d || value > 1d)
                    throw new ArgumentException(
                        "BlendedColorRatio must greater than or equal to 0 and lower than or equal to 1 ");

                _blendedColorRatio = value;
            }
        }

        /// <summary>
        ///     The color to blend with the source color
        /// </summary>
        public Color BlendedColor { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null || value.GetType() != typeof(Color))
                return null;

            var color = (Color) value;
            return new Color
            {
                A = BlendValue(color.A, BlendedColor.A),
                R = BlendValue(color.R, BlendedColor.R),
                G = BlendValue(color.G, BlendedColor.G),
                B = BlendValue(color.B, BlendedColor.B)
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private byte BlendValue(byte original, byte blend)
        {
            var blendRatio = BlendedColorRatio;
            var sourceRatio = 1 - blendRatio;

            var result = original * sourceRatio + blend * blendRatio;
            result = Math.Round(result);
            result = Math.Min(255d, Math.Max(0d, result));
            return System.Convert.ToByte(result);
        }
    }
}