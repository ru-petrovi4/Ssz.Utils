using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace Ssz.Utils
{
    /// <summary>
    ///     Case Insensitive Dictionary
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [TypeConverter(typeof(CaseInsensitiveDictionary_TypeConverter))]
    public class CaseInsensitiveDictionary<T> : Dictionary<string, T>        
    {
        #region construction and destruction

        /// <summary>
        /// 
        /// </summary>
        public CaseInsensitiveDictionary() : base(StringComparer.InvariantCultureIgnoreCase)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="capacity"></param>
        public CaseInsensitiveDictionary(int capacity)
            : base(capacity, StringComparer.InvariantCultureIgnoreCase)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dictionary"></param>
        public CaseInsensitiveDictionary(IDictionary<string, T> dictionary)
            : base(dictionary, StringComparer.InvariantCultureIgnoreCase)
        {
        }

#if NET5_0_OR_GREATER
        public CaseInsensitiveDictionary(IEnumerable<KeyValuePair<string, T>> collection)
            : base(collection, StringComparer.InvariantCultureIgnoreCase)
        {
        }   
#else
        public CaseInsensitiveDictionary(IEnumerable<KeyValuePair<string, T>> collection)
            : base(collection.ToDictionary(i => i.Key, i => i.Value, StringComparer.InvariantCultureIgnoreCase), StringComparer.InvariantCultureIgnoreCase)
        {
        }
#endif

        #endregion

        #region public functions

        /// <summary>
        ///     Returns null, if not found.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public T? TryGetValue(string? key)
        {
            if (key is null) return default;
            T? result;
            TryGetValue(key, out result);
            return result;
        }

        public override string ToString()
        {
            return NameValueCollectionHelper.GetNameValueCollectionStringToDisplay(this.Select(kvp => (kvp.Key, (string?)(new Any(kvp.Value).ValueAsString(false)))));
        }

        #endregion
    }

    public class CaseInsensitiveDictionary_TypeConverter : TypeConverter
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
            {
                if (value is null) 
                    return @"";
                else
                    return NameValueCollectionHelper.GetNameValueCollectionString((Dictionary<string, string?>)value);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;

            return base.CanConvertTo(context, sourceType);
        }

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is null || value.GetType() == typeof(string))
                return NameValueCollectionHelper.Parse((string?)value);

            return base.ConvertFrom(context, culture, value);
        }

        #endregion
    }
}