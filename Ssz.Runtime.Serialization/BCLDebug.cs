using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Runtime.Serialization
{
    internal class BCLDebug
    {
        internal static bool CheckEnabled(string v)
        {
            return false;
        }

        internal static void Trace(string value, params object[] arg)
        {
            System.Diagnostics.Trace.WriteLine(String.Format(value, arg));
        }

        internal static void Assert(bool v1, string v2)
        {            
        }

        internal static void Correctness(bool v1, string v2)
        {            
        }
    }
}
