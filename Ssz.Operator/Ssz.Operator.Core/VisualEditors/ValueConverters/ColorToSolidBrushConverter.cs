using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;


namespace Ssz.Operator.Core.VisualEditors.ValueConverters
{
    public class ColorToSolidBrushConverter : IValueConverter
    {
        #region public functions

        public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            if (value is Color)
                return SolidDsBrush.GetSolidColorBrush((Color) value);
            return null;
        }

        public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            var brush = value as SolidColorBrush;
            if (brush is not null)
                return brush.Color;
            return null;
        }

        #endregion
    }
}