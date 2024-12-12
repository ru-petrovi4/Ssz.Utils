using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;


namespace Ssz.Operator.Core.VisualEditors.ValueConverters
{
    public class ValueConverterToBrushConverter : IValueConverter
    {
        #region public functions

        public static ValueConverterToBrushConverter Instance = new();

        public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            if (value is null) return DependencyProperty.UnsetValue;
            return SolidDsBrush.GetSolidColorBrush(Colors.Blue);
        }

        public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}