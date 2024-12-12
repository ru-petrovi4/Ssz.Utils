using System;
using System.Globalization;
using System.Windows.Data;

namespace Ssz.Operator.Core.ControlsCommon.Converters
{
    public class FlipBoolConverter : IValueConverter
    {
        #region construction and destruction

        private FlipBoolConverter()
        {
        }

        #endregion

        #region public functions

        public static readonly FlipBoolConverter Instance = new();


        public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            if (value is null) return null;
            var b = (bool) value;
            return b ? -1 : 1;
        }

        public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            if (value is null) return null;
            var i = (int) value;
            return i != 1;
        }

        #endregion
    }
}