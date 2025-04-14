using System;
using System.Globalization;
using Avalonia.Data.Converters;


namespace Ssz.Operator.Core.VisualEditors.ValueConverters
{
    public class XamlToContentConverter : IValueConverter
    {
        #region public functions

        public static XamlToContentConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var dsXaml = value as DsXaml;            
            return XamlHelper.GetContentPreviewSmall(dsXaml is not null ? dsXaml.Xaml : null);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}