using System;
using System.Collections.Concurrent;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Ssz.Operator.Core.ControlsCommon
{
    /// <summary>
    ///     Turns a Color into a Brush.
    ///     WPF views wrote &lt;Label.Background&gt;&lt;SolidColorBrush Color="{Binding Color}"/&gt;, but an
    ///     Avalonia brush is not a StyledElement, so a binding inside one never gets a DataContext and
    ///     silently resolves to nothing. Binding the brush property itself through this converter works.
    /// </summary>
    public class ColorToBrushConverter : IValueConverter
    {
        #region public functions

        public static readonly ColorToBrushConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Color color)
                return _brushes.GetOrAdd(color, c => new ImmutableSolidColorBrush(c));

            if (value is IBrush brush)
                return brush;

            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is ISolidColorBrush solidColorBrush
                ? solidColorBrush.Color
                : null;
        }

        #endregion

        #region private fields

        private static readonly ConcurrentDictionary<Color, IBrush> _brushes = new();

        #endregion
    }
}
