using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Ssz.Operator.Core.VisualEditors.ValueConverters
{
    public class ExpressionIsValidToTextBrushConverter : IValueConverter
    {
        #region public functions

        public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            if (value is null) return null;
            var isValid = (bool) value;
            if (!isValid) return Brushes.Red;
            return Brushes.Black;
        }

        public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class ExpressionIsValidToTextBrushMultiValueConverter : IMultiValueConverter
    {
        #region public functions

        public object? Convert(object?[]? values, Type? targetType, object? parameter,
            CultureInfo culture)
        {
            if (values is null || values.Length != 2) return Binding.DoNothing;
            var isValid = (bool) (values[0] ?? throw new InvalidOperationException());
            if (!isValid) return Brushes.Red;
            if ((bool) (values[1] ?? throw new InvalidOperationException())) return Brushes.DarkViolet;
            return Brushes.Black;
        }

        public object?[] ConvertBack(object? value, Type?[] targetTypes, object? parameter,
            CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}