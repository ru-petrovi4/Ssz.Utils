using System;
using System.ComponentModel;
using System.Globalization;

namespace Ssz.Operator.Core
{
    public class DsCommandTypeConverter : TypeConverter
    {
        #region public functions

        public static readonly DsCommandTypeConverter Instance = new();


        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            if (sourceType == typeof(string)) return true;

            return base.CanConvertFrom(context, sourceType);
        }


        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is null) throw GetConvertFromException(null);

            var source = value as string;

            if (source is not null) 
                return DsCommandValueSerializer.Instance.ConvertFromString(source, null);

            return base.ConvertFrom(context, culture, value);
        }

        #endregion
    }
}