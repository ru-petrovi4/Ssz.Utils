using Ssz.Operator.Core.MultiValueConverters;
using System;
using System.ComponentModel;
using System.Globalization;


namespace Ssz.Operator.Core
{
    public class ExpressionTypeConverter : TypeConverter
    {
        #region public functions

        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            if (sourceType == typeof(string)) return true;

            return base.CanConvertFrom(context, sourceType);
        }


        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        {
            if (destinationType == typeof(string)) return true;

            return base.CanConvertTo(context, destinationType);
        }


        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is null) throw GetConvertFromException(value);

            var source = value as string;

            if (source is not null) return new Expression {ExpressionString = source};

            return base.ConvertFrom(context, culture, value);
        }


        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value,
            Type destinationType)
        {
            var expression = value as Expression;
            if (expression is not null)
                if (destinationType == typeof(string))
                    return expression.ExpressionString;

            // Pass unhandled cases to base class (which will throw exceptions for null value or destinationType.)
            return base.ConvertTo(context, culture, value, destinationType);
        }

        #endregion
    }
}