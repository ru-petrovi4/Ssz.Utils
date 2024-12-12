using System;
using System.Globalization;
using System.Windows.Data;
using Ssz.Operator.Core.Properties;

namespace Ssz.Operator.Core.VisualEditors.ValueConverters
{
    public class ValueConverterToTextConverter : IValueConverter
    {
        #region public functions

        public static ValueConverterToTextConverter Instance = new();

        public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            if (value is null) return Resources.NoConverter;
            return Resources.Converter;
        }

        public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}