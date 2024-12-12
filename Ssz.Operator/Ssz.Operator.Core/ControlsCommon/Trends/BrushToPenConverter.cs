using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Ssz.Operator.Core.ControlsCommon.Trends
{
    public class BrushToPenConverter : IValueConverter
    {
        #region public functions

        public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            var brush = value as Brush;
            if (brush is null || parameter is null)
                return null;
            return new Pen(brush, ((Trend) parameter).Thicknes);
        }

        public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            if (value is null) return null;
            return ((Pen) value).Brush;
        }

        #endregion
    }
}