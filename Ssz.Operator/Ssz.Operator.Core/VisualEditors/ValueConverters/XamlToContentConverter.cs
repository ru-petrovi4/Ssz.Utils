using System;
using System.Globalization;
using System.Windows.Data;


namespace Ssz.Operator.Core.VisualEditors.ValueConverters
{
    public class XamlToContentConverter : IValueConverter
    {
        #region public functions

        public static XamlToContentConverter Instance = new();

        public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            var dsXaml = value as DsXaml;
            // AF: XamlHelper.GetContentPreviewSmall expected as parameter XAML string or null
            // you do not need to convert all other types axcept string to text parameter
            return XamlHelper.GetContentPreviewSmall(dsXaml is not null ? dsXaml.Xaml : null);
        }

        public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}