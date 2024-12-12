using System;
using System.Globalization;
using System.Windows.Data;

namespace Ssz.Operator.Core.ControlsCommon.Converters
{
    public class ConstMultiplicationValueConverter : IValueConverter
    {
        #region public functions

        public double Multiplier { get; set; }

        public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            if (value is double) return (double) value * Multiplier;
            return Binding.DoNothing;
        }

        public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}