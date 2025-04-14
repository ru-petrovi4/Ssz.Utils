using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Ssz.Operator.Core.VisualEditors.ValueConverters
{
    public class ValueConverterToBrushConverter : IValueConverter
    {
        #region public functions

        public static ValueConverterToBrushConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null) return AvaloniaProperty.UnsetValue;
            return SolidDsBrush.GetSolidColorBrush(Colors.Blue);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}