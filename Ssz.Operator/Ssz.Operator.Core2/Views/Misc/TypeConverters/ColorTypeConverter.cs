using Avalonia.Media;
using System;
using System.ComponentModel;
using System.Globalization;


namespace Ssz.Operator.Core
{
    public class ColorTypeConverter : TypeConverter
    {
        #region public functions

        public static readonly ColorTypeConverter Instance = new();

        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            if (sourceType == typeof(string)) 
                return true;

            return base.CanConvertFrom(context, sourceType);
        }

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
        {
            if (value is null) 
                throw GetConvertFromException(value);
            
            if (value is string source)
            {
                if (string.IsNullOrWhiteSpace(source)) 
                    return null;                

                return Color.Parse(source);
            }

            return base.ConvertFrom(context, culture, value);
        }

        #endregion
    }
}