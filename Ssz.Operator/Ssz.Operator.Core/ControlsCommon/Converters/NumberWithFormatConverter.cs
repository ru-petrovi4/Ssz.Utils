using Ssz.Utils;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Ssz.Operator.Core.ControlsCommon.Converters
{
    public class NumberWithFormatConverter : IMultiValueConverter
    {
        #region public functions

        public static readonly IMultiValueConverter Instance = new NumberWithFormatConverter();

        public object? Convert(object?[]? values, Type? targetType, object? parameter, CultureInfo culture)
        {
            if (values is null || values.Length == 0)
                return Binding.DoNothing;

            string? valueFormat;
            if (values.Length > 1)
                valueFormat = new Any(values[1]).ValueAsString(false);
            else
                valueFormat = null;

            return new Any(values[0]).ValueAsString(false, valueFormat);
        }

        public object[]? ConvertBack(object? value, Type?[] targetTypes, object? parameter, CultureInfo culture)
        {
            return null;
        }

        #endregion
    }
}