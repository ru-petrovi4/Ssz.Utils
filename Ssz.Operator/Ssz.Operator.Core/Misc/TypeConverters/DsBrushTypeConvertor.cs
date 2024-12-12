using System;
using System.ComponentModel;
using System.Globalization;


namespace Ssz.Operator.Core
{
    public class DsBrushTypeConvertor : TypeConverter
    {
        #region public functions

        public static readonly DsBrushTypeConvertor Instance = new();


        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            if (sourceType == typeof(string)) return true;

            return base.CanConvertFrom(context, sourceType);
        }


        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
        {
            if (value is null) throw GetConvertFromException(value);

            var source = value as string;

            if (source is not null)
            {
                if (string.IsNullOrWhiteSpace(source)) return null;
#if NET5_0_OR_GREATER
                if (source.Contains(';'))
#else
                if (source.Contains(";"))
#endif                
                {
                    string[] colorStrings = source.Split(';');
                    return new BlinkingDsBrush
                    {
                        FirstColorString = colorStrings[0],
                        SecondColorString = colorStrings[1]
                    };
                }

                return new SolidDsBrush {ColorString = source};
            }

            return base.ConvertFrom(context, culture, value);
        }

        #endregion
    }
}