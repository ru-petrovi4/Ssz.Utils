using System;
using System.ComponentModel;
using System.Globalization;

namespace Ssz.Operator.Core
{
    public class XamlDataBindingTypeConverter : TypeConverter
    {
        #region public functions

        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            if (sourceType == typeof(string)) return true;

            return base.CanConvertFrom(context, sourceType);
        }


        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        {
            if (destinationType == typeof(string)) return false;

            return base.CanConvertTo(context, destinationType);
        }


        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is null) throw GetConvertFromException(value);

            var source = value as string;

            if (source is not null)
            {
                var result = new XamlDataBinding(true, true);
                result.ConstValue.Xaml = source;
                return result;
            }

            return base.ConvertFrom(context, culture, value);
        }

        #endregion
    }
}