using System;
using System.ComponentModel;
using System.Globalization;

namespace Ssz.Operator.Core
{
    public class TextDataBindingTypeConverter : TypeConverter
    {
        #region public functions

        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            if (sourceType == typeof(string)) return true;

            return base.CanConvertFrom(context, sourceType);
        }


        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is null) throw GetConvertFromException(value);

            var source = value as string;

            if (source is not null)
                return new TextDataBinding(true, true)
                {
                    ConstValue = source
                };

            return base.ConvertFrom(context, culture, value);
        }

        #endregion
    }
}