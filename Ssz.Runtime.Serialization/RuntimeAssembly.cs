using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Runtime.Serialization
{
    internal class RuntimeAssembly
    {
        internal static Assembly LoadWithPartialNameInternal(string assemblyName, object p)
        {
            return Assembly.Load(assemblyName);
        }
    }
}
