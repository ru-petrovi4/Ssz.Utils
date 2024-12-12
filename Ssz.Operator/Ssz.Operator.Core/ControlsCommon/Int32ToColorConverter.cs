using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Ssz.Operator.Core.ControlsCommon
{
    public class Int32ToColorConverter : IValueConverter
    {
        #region public functions

        public static readonly Int32ToColorConverter Instanse = new Int32ToColorConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var colors = parameter as Color[];
            if (value is int && colors != null)
            {
                var idx = (int) value;
                if (idx >= 0 && idx < colors.Length)
                    return colors[idx];
            }
            return Colors.Red;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}