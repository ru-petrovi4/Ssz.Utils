using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Runtime.Serialization
{
    internal class SszArray
    {
        internal static object UnsafeCreateInstance(Type pRarrayElementType, int length)
        {
            return Array.CreateInstance(pRarrayElementType, length);
        }

        internal static object UnsafeCreateInstance(Type pRarrayElementType, int[] pRlengthA, int[] pRlowerBoundA)
        {
            return Array.CreateInstance(pRarrayElementType, pRlengthA, pRlowerBoundA);
        }

        internal static object UnsafeCreateInstance(Type pRarrayElementType, int[] pRlengthA)
        {
            return Array.CreateInstance(pRarrayElementType, pRlengthA);
        }
    }
}
