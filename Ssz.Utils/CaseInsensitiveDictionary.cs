using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

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
            if (key is null) 
                return default;
            T? result;
            TryGetValue(key, out result);
            return result;
        }

        /// <summary>
        ///     Returns null, if not found.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public T? TryRemoveValue(string? key)
        {
            if (key is null)
                return default;
            T? result;
#if NET5_0_OR_GREATER
            Remove(key, out result);
#else
            if (TryGetValue(key, out result))
                Remove(key);
#endif
            return result;
        }

        /// <summary>
        ///     Does not return defaultValue, if object exists in dictionary.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public T GetValue(string? key, T defaultValue)
        {
            if (key is null) 
                return defaultValue;
            T? result;
            if (!TryGetValue(key, out result))
                return defaultValue;
            return result;
        }

        public override string ToString()
        {
            return NameValueCollectionHelper.GetNameValueCollectionStringToDisplay(this.Select(kvp => (kvp.Key, (string?)(new Any(kvp.Value).ValueAsString(false)))));
        }

#endregion
    }
    
    public static class CaseInsensitiveDictionaryExtensions
    {
#if !NET8_0_OR_GREATER
        /// <summary>
        ///     Returns null, if not found.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T? TryGetValue<T>(this IDictionary<string, T> dictionary, string? key)
        {
            if (key is null)
                return default;
            T? result;
            dictionary.TryGetValue(key, out result);
            return result;
        }
#endif

        public static CaseInsensitiveDictionary<TSource> ToCaseInsensitiveDictionary<TSource>(this IEnumerable<TSource> source, Func<TSource, string> keySelector)
        {
            int capacity = 0;
            if (source is TSource[] array)
                capacity = array.Length;
            if (source is List<TSource> list)
                capacity = list.Count;

            CaseInsensitiveDictionary<TSource> d = new CaseInsensitiveDictionary<TSource>(capacity);
            foreach (TSource element in source)
            {
                d[keySelector(element)] = element;
            }

            return d;
        }
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
            if (value.GetType() == typeof(string))
                return NameValueCollectionHelper.Parse((string?)value);

            return base.ConvertFrom(context, culture, value);
        }

        #endregion
    }

    public class CaseInsensitiveDictionaryJsonConverter : JsonConverter<CaseInsensitiveDictionary<string?>>
    {
        public override CaseInsensitiveDictionary<string?> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject token");
            }

            CaseInsensitiveDictionary<string?> dictionary = new();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return dictionary;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected PropertyName token");
                }

                string? key = reader.GetString();

                reader.Read();
                string? value = JsonSerializer.Deserialize<string>(ref reader, options);

                dictionary.Add(key ?? "", value);
            }

            throw new JsonException("Expected EndObject token");
        }

        public override void Write(Utf8JsonWriter writer, CaseInsensitiveDictionary<string?> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            foreach (var kvp in value)
            {
                writer.WritePropertyName(kvp.Key);
                JsonSerializer.Serialize(writer, kvp.Value, options);
            }

            writer.WriteEndObject();
        }
    }
}