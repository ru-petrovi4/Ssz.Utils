// Description: Html Agility Pack - HTML Parsers, selectors, traversors, manupulators.
// Website & Documentation: http://html-agility-pack.net
// Forum & Issues: https://github.com/zzzdsSolutions/html-agility-pack
// License: https://github.com/zzzdsSolutions/html-agility-pack/blob/master/LICENSE
// More dsSolutions: http://www.zzzdsSolutions.com/
// Copyright © ZZZ DsSolutions Inc. 2014 - 2017. All rights reserved.

using System;
using System.Collections.Generic;

namespace HtmlAgilityPack
{
    internal static class Utilities
    {
        public static TValue GetDictionaryValueOrDefault<TKey, TValue>(Dictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default(TValue)) where TKey : class
        {
            TValue value;
            if (!dict.TryGetValue(key, out value))
                return defaultValue;
            return value;
        }
    }
}