using Ssz.Utils;
using System;
using System.Globalization;
using Avalonia.Data.Converters;
using System.Collections.Generic;
using Avalonia.Data;

namespace Ssz.Operator.Core.ControlsCommon.Converters
{
    public class NumberWithFormatConverter : IMultiValueConverter
    {
        #region public functions

        public static readonly IMultiValueConverter Instance = new NumberWithFormatConverter();

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values is null || values.Count == 0)
                return BindingOperations.DoNothing;

            string? valueFormat;
            if (values.Count > 1)
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