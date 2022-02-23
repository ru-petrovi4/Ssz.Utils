﻿using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Ssz.Utils.Wpf.Converters
{
    public class IntToObjectConverter : IValueConverter
    {
        #region public functions

        public static readonly IntToObjectConverter Instance = new IntToObjectConverter();

        public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            var colors = parameter as Array;            
            if (colors is not null)
            {
                var intValue = new Any(value).ValueAsInt32(false);                
                if (intValue >= 0 && intValue < colors.Length)
                    return colors.GetValue(intValue);
            }
            return Colors.Transparent;
        }

        public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}