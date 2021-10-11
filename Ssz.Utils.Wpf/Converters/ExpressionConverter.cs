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
    public class ExpressionConverter : DependencyObject, IMultiValueConverter
    {
        #region public functions

        public static readonly DependencyProperty ExpressionProperty =
            DependencyProperty.Register("Expression", typeof(string), typeof(ExpressionConverter),
                new PropertyMetadata(@""));

        public string Expression
        {
            get { return (string)GetValue(ExpressionProperty); }
            set { SetValue(ExpressionProperty, value); }
        }

        public object? Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        {            
            return new SszExpression(Expression).Evaluate(values, null, null);
        }

        public object[]? ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion        
    }
}
