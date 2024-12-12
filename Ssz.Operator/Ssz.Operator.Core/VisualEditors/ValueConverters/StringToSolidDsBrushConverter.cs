using System;
using System.Globalization;
using System.Windows.Data;

using Ssz.Operator.Core.Utils;

namespace Ssz.Operator.Core.VisualEditors.ValueConverters
{
    public class StringToSolidDsBrushConverter : IValueConverter
    {
        #region public functions

        public static StringToSolidDsBrushConverter Instance = new();

        public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            var valueString = value as string;
            if (string.IsNullOrEmpty(valueString)) return null;
            return ObsoleteAnyHelper.ConvertTo<DsBrushBase>(valueString, false) as SolidDsBrush;
        }

        public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}