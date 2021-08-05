using System;
using System.Collections.Generic;

namespace Ssz.Utils
{
    public class CaseInsensitiveDictionary<T> : Dictionary<string, T>
    {
        #region construction and destruction

        public CaseInsensitiveDictionary() : base(StringComparer.InvariantCultureIgnoreCase)
        {
        }

        public CaseInsensitiveDictionary(int capacity)
            : base(capacity, StringComparer.InvariantCultureIgnoreCase)
        {
        }

        public CaseInsensitiveDictionary(Dictionary<string, T> dictionary)
            : base(dictionary, StringComparer.InvariantCultureIgnoreCase)
        {
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Returns default value (null), if not found.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public T TryGetValue(string key)
        {
            if (key == null) return default(T);
            T result;
            TryGetValue(key, out result);
            return result;
        }

        #endregion
    }
}