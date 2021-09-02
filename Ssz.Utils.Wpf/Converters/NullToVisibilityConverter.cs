﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Ssz.Utils.Wpf.Converters
{
    public class NullToVisibilityConverter : IValueConverter
    {
        #region public functions

        public static readonly NumberToColorConverter Instanse = new NumberToColorConverter();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return ReferenceEquals(value, null) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}