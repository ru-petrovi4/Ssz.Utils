using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Runtime.Serialization
{
    internal class SszAssembly
    {
        internal static Type GetType_Compat(string assemblyFullName, string type)
        {
            var assemblyName = assemblyFullName.Split(',')[0];
            var assembly = AppDomain.CurrentDomain.GetAssemblies().
                First(a => a.GetName().Name == assemblyName);
            return assembly.GetType(type);            
            //if (t == null)
            //{
            //}            
        }
    }
}
