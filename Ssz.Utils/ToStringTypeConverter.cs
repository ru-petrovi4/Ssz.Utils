using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace Ssz.Utils
{
    public class ToStringTypeConverter : TypeConverter
    {
        #region public functions

        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        {
            if (destinationType == typeof(string))
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value,
            Type destinationType)
        {
            if (destinationType == typeof(string))
                return value?.ToString() ?? @"";

            return base.ConvertTo(context, culture, value, destinationType);
        }

        #endregion
    }
}
