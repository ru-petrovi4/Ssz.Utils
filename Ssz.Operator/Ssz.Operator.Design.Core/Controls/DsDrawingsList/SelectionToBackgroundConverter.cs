using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Ssz.Operator.Design.Core.Controls
{
    public class SelectionToBackgroundConverter : IMultiValueConverter
    {
        #region public functions

        public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
        {            
            if (values[1] is bool boolValue1 && boolValue1) return SystemColors.HighlightBrush;
            if (values[0] is bool boolValue0 && boolValue0) return Brushes.LightSkyBlue;
            return Brushes.White;
        }

        public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }        

        #endregion
    }
}
