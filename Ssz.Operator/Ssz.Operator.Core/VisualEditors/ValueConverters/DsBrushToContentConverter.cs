using System;
using System.Globalization;
using System.Windows.Data;

using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;

namespace Ssz.Operator.Core.VisualEditors.ValueConverters
{
    public class DsBrushToContentConverter : IValueConverter
    {
        #region public functions

        public static DsBrushToContentConverter Instance = new();

        public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            if (value is int) return @"o[" + (int) value + @"]";
            var dsBrush = value as DsBrushBase;
            if (dsBrush is null) return Resources.DefaultValue;
            return new BrushAndNameControl(dsBrush);
        }

        public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}