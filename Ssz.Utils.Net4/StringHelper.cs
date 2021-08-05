using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    public static class StringHelper
    {
        #region public functions

        /// <summary>
        ///     Object is null or String.Empty
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsNullOrEmptyString(object value)
        {
#pragma warning disable CS0252 // Possible unintended reference comparison; left hand side needs cast
            return value == null || value == @"";
#pragma warning restore CS0252 // Possible unintended reference comparison; left hand side needs cast
        }

        /// <summary>
        ///     Strings can be null
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool CompareIgnoreCase(string left, string right)
        {
            if (left == null && right == null) return true;
            if (left == null || right == null) return false;
            return String.Equals(left, right, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        ///     returns replaces count.
        ///     oldValue != String.Empty
        /// </summary>
        public static int ReplaceIgnoreCase(ref string str, string oldValue, string newValue)
        {
            if (oldValue == String.Empty) throw new ArgumentNullException(@"oldValue");

            if (String.IsNullOrEmpty(str)) return 0;

            int index = str.IndexOf(oldValue, StringComparison.InvariantCultureIgnoreCase);
            if (index == -1) return 0;

            int replacesCount = 0;
            var sb = new StringBuilder();
            int previousIndex = 0;

            while (index != -1)
            {
                sb.Append(str.Substring(previousIndex, index - previousIndex));
                sb.Append(newValue ?? "");
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
        ///     value != null
        /// </summary>
        /// <param name="str"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool ContainsIgnoreCase(string str, string value)
        {
            if (value == null) throw new ArgumentNullException(@"value");

            if (str == null) return false;
            return str.IndexOf(value, StringComparison.InvariantCultureIgnoreCase) >= 0;
        }

        /// <summary>
        ///     value != null
        /// </summary>
        /// <param name="str"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool StartsWithIgnoreCase(string str, string value)
        {
            if (value == null) throw new ArgumentNullException(@"value");

            if (str == null) return false;
            return str.StartsWith(value, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        ///     value != null
        /// </summary>
        /// <param name="str"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool EndsWithIgnoreCase(string str, string value)
        {
            if (value == null) throw new ArgumentNullException(@"value");

            if (str == null) return false;
            return str.EndsWith(value, StringComparison.InvariantCultureIgnoreCase);
        }

        #endregion
    }
}
