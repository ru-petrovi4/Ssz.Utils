#nullable enable
using System;
using System.ComponentModel;
using System.Globalization;

namespace Ssz.Utils.Wpf
{
    public class NameValueCollectionTypeConverter<T> : TypeConverter
        where T : notnull, new()
    {
        #region public functions

        public static readonly NameValueCollectionTypeConverter<T> Instance = new NameValueCollectionTypeConverter<T>();

        /// <summary>
        ///     Returns true if this type converter can convert from a given type.
        /// </summary>
        /// <returns>
        ///     bool - True if this converter can convert from the provided type, false if not.
        /// </returns>
        /// <param name="context"> The ITypeDescriptorContext for this call. </param>
        /// <param name="sourceType"> The Type being queried for support. </param>
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            if (sourceType == typeof (string))
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        ///     Attempts to convert to a DoubleDataSourceItemInfo from the given object.
        /// </summary>
        /// <returns>
        ///     The DoubleDataSourceItemInfo which was constructed.
        /// </returns>
        /// <exception cref="NotSupportedException">
        ///     A NotSupportedException is thrown if the example object is null or is not a valid type
        ///     which can be converted to a DoubleDataSourceItemInfo.
        /// </exception>
        /// <param name="context"> The ITypeDescriptorContext for this call. </param>
        /// <param name="culture"> The requested CultureInfo.  Note that conversion uses "en-US" rather than this parameter. </param>
        /// <param name="value"> The object to convert to an instance of DoubleDataSourceItemInfo. </param>
        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is null)
            {
                throw GetConvertFromException(null);
            }

            var source = value as string;

            if (source is not null)
            {
                object result = Activator.CreateInstance<T>();
                NameValueCollectionHelper.SetNameValueCollection(ref result, NameValueCollectionHelper.Parse(source));
                return result;                
            }

            return base.ConvertFrom(context, culture, value);
        }

        #endregion
    }
}