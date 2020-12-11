using System;
using System.Collections.Generic;

namespace Ssz.Utils
{
    /// <summary>
    ///     Case Insensitive Dictionary
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CaseInsensitiveDictionary<T> : Dictionary<string, T>
        where T : class?
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
        public CaseInsensitiveDictionary(Dictionary<string, T> dictionary)
            : base(dictionary, StringComparer.InvariantCultureIgnoreCase)
        {
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Returns null, if not found.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public T? TryGetValue(string key)
        {
            if (key == null) return null;
            T? result;
            TryGetValue(key, out result);
            return result;
        }

        #endregion
    }
}