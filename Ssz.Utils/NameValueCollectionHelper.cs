using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Windows;
using System.Windows.Markup;

namespace Ssz.Utils
{
    /// <summary>
    /// 
    /// </summary>
    public static class NameValueCollectionHelper
    {
        #region public functions

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nameValueCollectionString"></param>
        /// <returns></returns>
        public static CaseInsensitiveDictionary<string?> Parse(string? nameValueCollectionString)
        {
            var result = new CaseInsensitiveDictionary<string?>();

            if (String.IsNullOrEmpty(nameValueCollectionString)) return result;

            int length = nameValueCollectionString.Length;
            int i = 0;

            while (i <= length)
            {
                // find next & while noting first = on the way (and if there are more) 

                int nameValueBeginIndex = i;
                int equalsCharIndex = -1;

                while (i <= length)
                {
                    char ch;
                    if (i < length) ch = nameValueCollectionString[i];
                    else ch = default(char);

                    if (ch == '=')
                    {
                        if (equalsCharIndex < 0)
                            equalsCharIndex = i;
                    }
                    else if (ch == '&' || ch == default(char))
                    {
                        break;
                    }

                    i++;
                }

                // extract the name / value pair                
                if (equalsCharIndex >= 0) // has '=' char
                {
                    string name = nameValueCollectionString.Substring(nameValueBeginIndex, equalsCharIndex - nameValueBeginIndex);
                    string value = nameValueCollectionString.Substring(equalsCharIndex + 1, i - equalsCharIndex - 1);

                    // add name / value pair to the collection
                    result[UrlDecode(name)] = UrlDecode(value);
                }
                else
                {
                    string name = nameValueCollectionString.Substring(nameValueBeginIndex, i - nameValueBeginIndex);
                    result[UrlDecode(name)] = null;
                }

                i++;
            }

            return result;
        }

        /// <summary>        
        /// </summary>
        /// <param name="nameValueCollection"></param>
        /// <returns></returns>
        public static string GetNameValueCollectionString(CaseInsensitiveDictionary<string?> nameValueCollection)
        {
            if (nameValueCollection.Count == 0) return "";

            var items = new List<string>();

            foreach (var kvp in nameValueCollection.OrderBy(i => i.Key))
            {
                if (kvp.Value == null)
                {
                    items.Add(UrlEncode(kvp.Key));
                }
                else
                {
                    items.Add(UrlEncode(kvp.Key) + @"=" + UrlEncode(kvp.Value));
                }                
            }

            return String.Join("&", items);
        }

        /// <summary>        
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool CanGetNameValueCollection(object? obj)
        {
            if (obj == null) return true;

            PropertyInfo[] props = obj.GetType().GetProperties();
            foreach (PropertyInfo prop in props)
            {
                if (!prop.CanRead || !prop.CanWrite) continue;

                var designerSerializationVisibilityAttribute =
                    prop.GetCustomAttributes(typeof(DesignerSerializationVisibilityAttribute), true)
                        .OfType<DesignerSerializationVisibilityAttribute>().FirstOrDefault();
                if (designerSerializationVisibilityAttribute != null &&
                    designerSerializationVisibilityAttribute.Visibility == DesignerSerializationVisibility.Hidden)
                    continue;

                object? propValue = prop.GetValue(obj, null);
                Type propType = prop.PropertyType;

                var defaultValueAttribute = prop.GetCustomAttributes(typeof(DefaultValueAttribute), true)
                    .OfType<DefaultValueAttribute>().FirstOrDefault();
                if (defaultValueAttribute != null)
                {
                    if (Equals(defaultValueAttribute.Value, propValue))
                        continue;
                }

                //if (propValue != null)
                //{
                //    if (propType.IsClass)
                //    {
                //        //// TODO:
                //        ValueSerializer valueSerializer =
                //            ValueSerializer.GetSerializerFor(propType);
                //        if (valueSerializer == null) return false;
                //        if (!valueSerializer.CanConvertToString(propValue, null)) return false;
                //        return true;
                //    }
                //}
            }

            return true;
        }

        /// <summary>        
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static CaseInsensitiveDictionary<string?> GetNameValueCollection(object? obj)
        {
            var result = new CaseInsensitiveDictionary<string?>();

            if (obj == null) return result;

            PropertyInfo[] props = obj.GetType().GetProperties();
            foreach (PropertyInfo prop in props)
            {
                if (!prop.CanRead || !prop.CanWrite) continue;

                var designerSerializationVisibilityAttribute =
                    prop.GetCustomAttributes(typeof(DesignerSerializationVisibilityAttribute), true)
                        .OfType<DesignerSerializationVisibilityAttribute>().FirstOrDefault();
                if (designerSerializationVisibilityAttribute != null &&
                    designerSerializationVisibilityAttribute.Visibility == DesignerSerializationVisibility.Hidden)
                    continue;

                object? propValue = prop.GetValue(obj, null);

                var defaultValueAttribute = prop.GetCustomAttributes(typeof(DefaultValueAttribute), true)
                    .OfType<DefaultValueAttribute>().FirstOrDefault();
                if (defaultValueAttribute != null)
                {
                    if (Equals(defaultValueAttribute.Value, propValue))
                        continue;
                }

                if (propValue == null)
                {
                    result[prop.Name] = null;
                }
                else
                {
                    result[prop.Name] = new Any(propValue).ValueAsString(false);
                }
            }

            return result;
        }

        /// <summary>        
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="nameValueCollection"></param>
        public static void SetNameValueCollection(ref object obj, CaseInsensitiveDictionary<string?>? nameValueCollection)
        {
            if (nameValueCollection == null) return;
            foreach (var kvp in nameValueCollection)
            {
                obj.SetPropertyValue(kvp.Key, kvp.Value);
            }
        }

        #endregion

        #region private functions

        /// <summary>        
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string UrlEncode(string value)
        {
            value = value.Replace(@"%", @"%25");
            value = value.Replace(@"&", @"%26");
            value = value.Replace(@"=", @"%3D");
            value = value.Replace(@"+", @"%2B");
            return value;
        }

        /// <summary>        
        /// </summary>
        private static string UrlDecode(string value)
        {
            if (value == @"") return @"";

            int count = value.Length;
            var urlDecoder = new UrlDecoder(count);

            // go through the string's chars collapsing %XX and %uXXXX and
            // appending each char as char, with exception of %XX constructs 
            // that are appended as bytes
            for (int pos = 0; pos < count; pos++)
            {
                char ch = value[pos];

                if (ch == '+')
                {
                    ch = ' ';
                }
                else if (ch == '%' && pos < count - 2)
                {
                    if (value[pos + 1] == 'u' && pos < count - 5)
                    {
                        int h1 = HexToInt(value[pos + 2]);
                        int h2 = HexToInt(value[pos + 3]);
                        int h3 = HexToInt(value[pos + 4]);
                        int h4 = HexToInt(value[pos + 5]);

                        if (h1 >= 0 && h2 >= 0 && h3 >= 0 && h4 >= 0)
                        {
                            // valid 4 hex chars
                            ch = (char)((h1 << 12) | (h2 << 8) | (h3 << 4) | h4);
                            pos += 5;

                            // only add as char 
                            urlDecoder.AddChar(ch);
                            continue;
                        }
                    }
                    else
                    {
                        int h1 = HexToInt(value[pos + 1]);
                        int h2 = HexToInt(value[pos + 2]);

                        if (h1 >= 0 && h2 >= 0)
                        {
                            // valid 2 hex chars 
                            var b = (byte)((h1 << 4) | h2);
                            pos += 2;

                            // don't add as char
                            urlDecoder.AddByte(b);
                            continue;
                        }
                    }
                }

                if ((ch & 0xFF80) == 0)
                    urlDecoder.AddByte((byte)ch); // 7 bit have to go as bytes because of Unicode 
                else
                    urlDecoder.AddChar(ch);
            }

            return urlDecoder.GetString();
        }

        private static int HexToInt(char h)
        {
            return (h >= '0' && h <= '9')
                ? h - '0'
                : (h >= 'a' && h <= 'f')
                    ? h - 'a' + 10
                    : (h >= 'A' && h <= 'F')
                        ? h - 'A' + 10
                        : -1;
        }

        #endregion

        private class UrlDecoder
        {
            #region construction and destruction

            public UrlDecoder(int bufferSize)
            {
                _bufferSize = bufferSize;

                _charBuffer = new char[bufferSize];
                // byte buffer created on demand 
            }

            #endregion

            #region public functions

            public void AddChar(char ch)
            {
                if (_numBytes > 0)
                    FlushBytes();

                _charBuffer[_numChars] = ch;
                _numChars++;
            }

            public void AddByte(byte b)
            {
                // if there are no pending bytes treat 7 bit bytes as characters
                // this optimization is temp disable as it doesn't work for some encodings
                /* 
                                if (_numBytes == 0 && ((b & 0x80) == 0)) {
                                    AddChar((char)b); 
                                } 
                                else
                */
                {
                    if (_byteBuffer == null)
                        _byteBuffer = new byte[_bufferSize];

                    _byteBuffer[_numBytes] = b;
                    _numBytes++;
                }
            }

            public string GetString()
            {
                if (_numBytes > 0)
                    FlushBytes();

                if (_numChars > 0)
                    return new String(_charBuffer, 0, _numChars);
                else
                    return String.Empty;
            }

            #endregion

            #region private functions

            private void FlushBytes()
            {
                if (_numBytes > 0)
                {
                    if (_byteBuffer == null) throw new InvalidOperationException();
                    _numChars += Encoding.UTF8.GetChars(_byteBuffer, 0, _numBytes, _charBuffer, _numChars);
                    _numBytes = 0;
                }
            }

            #endregion

            #region private fields

            private readonly int _bufferSize;

            // Accumulate characters in a special array 
            private int _numChars;
            private readonly char[] _charBuffer;

            // Accumulate bytes for decoding into characters in a special array
            private int _numBytes;
            private byte[]? _byteBuffer;

            #endregion
        }
    }
}