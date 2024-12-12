using System;
using System.ComponentModel;
using System.Globalization;
using Ssz.Operator.Core.Utils;

namespace Ssz.Operator.Core
{
    public class StructDataBindingTypeConverter<T, TDataBinding> : TypeConverter
        where T : struct
        where TDataBinding : StructDataBinding<T>, new()
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
                return new TDataBinding
                {
                    ConstValue = ObsoleteAnyHelper.ConvertTo<T>(source, false)
                };

            return base.ConvertFrom(context, culture, value);
        }

        #endregion
    }
}