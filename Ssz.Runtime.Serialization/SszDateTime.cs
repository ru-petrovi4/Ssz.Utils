using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Runtime.Serialization
{
    internal static class SszDateTime
    {
        internal static DateTime FromBinaryRaw(long v)
        {
            return DateTime.FromBinary(v);
        }

        internal static long ToBinaryRaw(this DateTime dt)
        {
            return dt.ToBinary();
        }
    }
}
