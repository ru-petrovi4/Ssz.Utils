#if NET5_0_OR_GREATER

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text;

namespace Ssz.Utils
{
    public static class FrozenDictionaryExtensions
    {
        /// <summary>
        ///     Returns null, if not found.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="frozenDictionary"></param>
        /// <param name="key"></param>
        public static T? TryGetValue<T>(this FrozenDictionary<string, T> frozenDictionary, string? key)
        {
            if (key is null)
                return default;
            T? result;
            frozenDictionary.TryGetValue(key, out result);
            return result;
        }
    }
}

#endif
