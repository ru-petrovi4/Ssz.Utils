using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Ssz.Utils.Wpf.Converters
{
    public class IntToObjectConverter : IValueConverter
    {
        #region public functions

        public static readonly IntToObjectConverter Instance = new IntToObjectConverter();

        public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            var a = parameter as Array;            
            if (a is not null)
            {
                var intValue = new Any(value).ValueAsInt32(false);                
                if (intValue >= 0 && intValue < a.Length)
                    return a.GetValue(intValue);
            }

            return null;
        }

        public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}