using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Ssz.Operator.Core.ControlsCommon.Converters
{
    public class LeftTopConverter : IMultiValueConverter
    {
        #region public functions

        public static readonly IMultiValueConverter Instance = new LeftTopConverter();

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values is null) return BindingOperations.DoNothing;
            if (values.Count != 7) return BindingOperations.DoNothing;
            if (targetType != typeof(double)) return BindingOperations.DoNothing;

            try
            {
                var initial = (double) (values[0] ?? throw new InvalidOperationException());
                var delta = (double) (values[1] ?? throw new InvalidOperationException());
                var final = (double) (values[2] ?? throw new InvalidOperationException());
                var relative = (double) (values[3] ?? throw new InvalidOperationException());
                var lengthInitial = (double) (values[4] ?? throw new InvalidOperationException());
                var lengthDelta = (double) (values[5] ?? throw new InvalidOperationException());
                var lengthFinal = (double) (values[6] ?? throw new InvalidOperationException());

                if (lengthDelta < 0) lengthDelta = 0;
                else if (lengthDelta > 1) lengthDelta = 1;

                var length = lengthInitial + (lengthFinal - lengthInitial) * lengthDelta;

                if (double.IsNaN(delta) || double.IsInfinity(delta)) return BindingOperations.DoNothing;

                if (delta < 0) delta = 0;
                else if (delta > 1) delta = 1;

                var pos = initial + (final - initial) * delta;

                return pos - length * relative;
            }
            catch (Exception)
            {
                return BindingOperations.DoNothing;
            }
        }

        public object[]? ConvertBack(object? value, Type?[] targetTypes, object? parameter, CultureInfo culture)
        {
            return null;
        }

        #endregion
    }
}