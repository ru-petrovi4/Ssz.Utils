using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;


namespace Ssz.Operator.Core.VisualEditors.ValueConverters
{
    public class DataSourceIdsListToTextConverter : IValueConverter
    {
        #region public functions

        public static DataSourceIdsListToTextConverter Instance = new();

        public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            if (value is null) throw new InvalidOperationException();
            DataBindingItem[] dataBindingItems = ((IEnumerable<DataBindingItem>) value).ToArray();
            if (dataBindingItems.Length == 0) return "<null>";
            StringBuilder result = new(byte.MaxValue); //255
            foreach (DataBindingItem dataBindingItem in dataBindingItems)
            {
                result.Append(dataBindingItem.IdString);
                result.Append(";");
            }

            return result.ToString();
        }

        public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}