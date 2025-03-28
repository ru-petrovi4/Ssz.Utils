using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using OxyPlot.Axes;

namespace Ssz.Operator.Core.ControlsCommon.Trends.Converters
{
    public class DateTimeToDoubleConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
                return BindingOperations.DoNothing;

            var dateTime = (DateTime)value;

            return DateTimeAxis.ToDouble(dateTime);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
