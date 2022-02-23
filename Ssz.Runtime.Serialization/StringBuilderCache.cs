using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Runtime.Serialization
{
    internal class StringBuilderCache
    {
        internal static StringBuilder Acquire(int v)
        {
            return new StringBuilder();
        }

        internal static object GetStringAndRelease(StringBuilder sb)
        {
            return sb.ToString();
        }
    }
}
