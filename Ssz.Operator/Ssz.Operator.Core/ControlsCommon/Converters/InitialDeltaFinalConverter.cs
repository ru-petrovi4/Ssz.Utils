using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Ssz.Operator.Core.ControlsCommon.Converters
{
    public class InitialDeltaFinalConverter : IMultiValueConverter
    {
        #region public functions

        public static readonly IMultiValueConverter Instance = new InitialDeltaFinalConverter();

        public object? Convert(object?[]? values, Type? targetType, object? parameter, CultureInfo culture)
        {
            if (values is null) return Binding.DoNothing;
            if (values.Length != 3) return Binding.DoNothing;
            if (targetType != typeof(double)) return Binding.DoNothing;
            if (values[0] == DependencyProperty.UnsetValue || values[1] == DependencyProperty.UnsetValue ||
                values[2] == DependencyProperty.UnsetValue) return Binding.DoNothing;

            try
            {
                var initial = (double) (values[0] ?? throw new InvalidOperationException());
                var delta = (double) (values[1] ?? throw new InvalidOperationException());
                var final = (double) (values[2] ?? throw new InvalidOperationException());

                if (double.IsNaN(delta) || double.IsInfinity(delta)) return Binding.DoNothing;

                if (delta < 0) delta = 0;
                else if (delta > 1) delta = 1;

                return initial + (final - initial) * delta;
            }
            catch (Exception)
            {
                return Binding.DoNothing;
            }
        }

        public object[]? ConvertBack(object? value, Type?[] targetTypes, object? parameter, CultureInfo culture)
        {
            /*
            if (!(value is double)) return null;
            if (targetTypes[0] != typeof(double)) return null;

            try
            {
                return new[] { value };
            }
            catch (Exception)
            {
                return null;
            }*/
            return null;
        }

        #endregion
    }
}