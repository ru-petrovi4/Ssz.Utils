using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;

namespace Ssz.Operator.Core.VisualEditors.ValueConverters
{
    /// <summary>
    ///     Turns the brush of a property into what its row in the grid shows.
    /// </summary>
    public class DsBrushToContentConverter : IValueConverter
    {
        #region public functions

        public static readonly DsBrushToContentConverter Instance = new();

        public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            if (value is int paramNum) return @"o[" + paramNum + @"]";

            var dsBrush = value as DsBrushBase;
            if (dsBrush is null) return Resources.DefaultValue;

            return new BrushAndNameControl(dsBrush);
        }

        public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
