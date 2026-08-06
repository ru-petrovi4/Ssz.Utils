using System;
using System.Globalization;
using Avalonia.Data.Converters;
using OxyPlot.Axes;

namespace Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends
{
    public class StartDateConverter : IValueConverter
    {
        #region public functions

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not double val)
                return value;

            DateTime dateTime = DateTimeAxis.ToDateTime(val, TimeSpan.FromSeconds(1));
            return dateTime.ToString("ddd d MMM yyyy");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
