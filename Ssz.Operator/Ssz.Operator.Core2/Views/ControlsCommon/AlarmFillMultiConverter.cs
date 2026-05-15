using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Ssz.Operator.Core.ControlsCommon;

public sealed class AlarmFillMultiConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count != 2)
            return BindingOperations.DoNothing;

        if (values[0] is not bool isActive)
            return BindingOperations.DoNothing;

        if (values[1] is not IBrush brush)
            return BindingOperations.DoNothing;

        return isActive ? brush : Brushes.Transparent;
    }
}
