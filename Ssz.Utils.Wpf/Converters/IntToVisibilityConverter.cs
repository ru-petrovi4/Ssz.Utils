using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Ssz.Utils.Wpf.Converters
{
    public class IntToVisibilityConverter : IValueConverter
    {
        #region public functions

        public static readonly IntToVisibilityConverter Instance = new IntToVisibilityConverter();

        public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {            
            int intParameter = new Any(parameter).ValueAsInt32(false);
            int intValue = new Any(value).ValueAsInt32(false);
            return intParameter == intValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
