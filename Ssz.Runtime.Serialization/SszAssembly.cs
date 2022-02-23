using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Runtime.Serialization
{
    internal class SszAssembly
    {
        internal static Type GetType_Compat(string assembly, string type)
        {
            return Type.GetType(type);
        }
    }
}
