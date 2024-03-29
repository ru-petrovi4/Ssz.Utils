﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    /// <summary>
    /// 
    /// </summary>
    public static class StringHelper
    {
        #region public functions

        /// <summary>
        /// Returns <paramref name="str"/> with the minimal concatenation of <paramref name="ending"/> (starting from end) that
        /// results in satisfying .EndsWith(ending).
        /// </summary>
        /// <example>"hel".WithEnding("llo") returns "hello", which is the result of "hel" + "lo".</example>
        public static string WithEnding(string? str, string ending)
        {
            if (str is null)
                return ending;

            if (str.EndsWith(ending))
                return str;
            
            // Right() is 1-indexed, so include these cases
            // * Append no characters
            // * Append up to N characters, where N is ending length
            for (int length = 1; length <= ending.Length; length++)
            {
                string tmp = str + Right(ending, length);
                if (tmp.EndsWith(ending))
                    return tmp;
            }

            return str;
        }

        /// <summary>Gets the rightmost <paramref name="length" /> characters from a string.</summary>
        /// <param name="str">The string to retrieve the substring from.</param>
        /// <param name="length">The number of characters to retrieve.</param>
        /// <returns>The substring.</returns>
        public static string Right(string str, int length)
        {            
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length", length, "Length is less than zero");
            }

            return (length < str.Length) ? str.Substring(str.Length - length) : str;
        }

        /// <summary>
        ///     Object is null or String.Empty
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool IsNullOrEmptyString(object? obj)
        {
#pragma warning disable CS0252 // Possible unintended reference comparison; left hand side needs cast
            return obj is null || obj == @"";
#pragma warning restore CS0252 // Possible unintended reference comparison; left hand side needs cast
        }       
            
        /// <summary>
        ///     returns replaces count.
        ///     oldValue != String.Empty
        /// </summary>
        public static int ReplaceIgnoreCase(ref string str, string oldValue, string newValue)
        {
            if (str == @"") return 0;

            int index = str.IndexOf(oldValue, StringComparison.InvariantCultureIgnoreCase);
            if (index == -1) return 0;

            int replacesCount = 0;
            var sb = new StringBuilder();
            int previousIndex = 0;

            while (index != -1)
            {
                sb.Append(str.Substring(previousIndex, index - previousIndex));
                sb.Append(newValue);
                replacesCount += 1;
                index += oldValue.Length;

                previousIndex = index;
                index = str.IndexOf(oldValue, index, StringComparison.InvariantCultureIgnoreCase);
            }
            sb.Append(str.Substring(previousIndex));

            str = sb.ToString();
            return replacesCount;
        }

        /// <summary>
        ///     value != String.Empty
        /// </summary>
        /// <param name="str"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool ContainsIgnoreCase(string? str, string value)
        {
            if (str is null || value == @"") return false;
            return str.IndexOf(value, StringComparison.InvariantCultureIgnoreCase) >= 0;
        }

        /// <summary>        
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool CompareIgnoreCase(string? left, string? right)
        {
            if (left is null && right is null) return true;
            if (left is null || right is null) return false;
            return String.Equals(left, right, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>        
        /// </summary>
        /// <param name="str"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool StartsWithIgnoreCase(string? str, string value)
        {
            if (str is null) return false;
            return str.StartsWith(value, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>        
        /// </summary>
        /// <param name="str"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool EndsWithIgnoreCase(string? str, string value)
        {
            if (str is null) return false;
            return str.EndsWith(value, StringComparison.InvariantCultureIgnoreCase);
        }        

        public static string JoinNotNullOrEmpty(string separator, params string?[] values)
        {
            return String.Join(separator, values.Where(v => !String.IsNullOrEmpty(v)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string? GetNullForEmptyString(string? value)
        {
            if (value == @"")
                return null;
            return value;
        }

        public static byte[] GetUTF8BytesWithBomPreamble(string? value)
        {
            var data = Encoding.UTF8.GetBytes(value ?? @"");
            return Encoding.UTF8.GetPreamble().Concat(data).ToArray();
        }        

        #endregion
    }
}
