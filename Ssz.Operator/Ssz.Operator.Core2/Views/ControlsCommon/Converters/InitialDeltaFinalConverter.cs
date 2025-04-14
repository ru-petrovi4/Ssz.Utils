using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Ssz.Operator.Core.ControlsCommon.Converters
{
    public class InitialDeltaFinalConverter : IMultiValueConverter
    {
        #region public functions

        public static readonly IMultiValueConverter Instance = new InitialDeltaFinalConverter();

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values is null) return BindingOperations.DoNothing;
            if (values.Count != 3) return BindingOperations.DoNothing;
            if (targetType != typeof(double)) return BindingOperations.DoNothing;
            if (values[0] == AvaloniaProperty.UnsetValue || values[1] == AvaloniaProperty.UnsetValue ||
                values[2] == AvaloniaProperty.UnsetValue) return BindingOperations.DoNothing;

            try
            {
                var initial = (double) (values[0] ?? throw new InvalidOperationException());
                var delta = (double) (values[1] ?? throw new InvalidOperationException());
                var final = (double) (values[2] ?? throw new InvalidOperationException());

                if (double.IsNaN(delta) || double.IsInfinity(delta)) return BindingOperations.DoNothing;

                if (delta < 0) delta = 0;
                else if (delta > 1) delta = 1;

                return initial + (final - initial) * delta;
            }
            catch (Exception)
            {
                return BindingOperations.DoNothing;
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