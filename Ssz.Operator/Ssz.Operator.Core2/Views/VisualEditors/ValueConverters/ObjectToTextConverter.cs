using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Ssz.Operator.Core.Properties;

namespace Ssz.Operator.Core.VisualEditors.ValueConverters
{
    public class ObjectToTextConverter : IValueConverter
    {
        #region public functions

        public static ObjectToTextConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null) return Resources.DefaultValue;
            return value.ToString();
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}