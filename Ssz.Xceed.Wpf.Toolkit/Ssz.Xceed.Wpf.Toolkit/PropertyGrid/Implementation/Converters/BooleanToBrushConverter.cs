using System;
using System.Collections;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Converters
{
    public class PropsToBrushConverter : IMultiValueConverter
    {
        public Brush NormalBrush { get; set; }
        public Brush ReadonlyValueBrush { get; set; }

        public Brush CompoundValueBrush { get; set; }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values is not null && values.Length == 4)
            {
                var list = values[0] as IList;
                if (list is not null) // Is List
                {
                    if (list.Count > 0) return CompoundValueBrush;
                }
                else
                {
                    if ((bool) values[1]) // Is Expandable
                    {
                        if (!(bool) values[3]) // ValueEditor Is NOT Enabled
                            return CompoundValueBrush;
                    }
                    else
                    {
                        if ((bool) values[2]) // Is Readonly
                            return ReadonlyValueBrush;
                    }
                }
            }

            return NormalBrush;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}