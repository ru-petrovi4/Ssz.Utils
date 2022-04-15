using System;
using System.Collections.Generic;

namespace Ssz.Utils
{
    /// <summary>
    ///     Case Insensitive Dictionary
    /// </summary>
    /// <typeparam name="T"></typeparam>
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

#if !NETSTANDARD2_0
        public CaseInsensitiveDictionary(IEnumerable<KeyValuePair<string, T>> collection)
            : base(collection, StringComparer.InvariantCultureIgnoreCase)
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

        #endregion
    }
}