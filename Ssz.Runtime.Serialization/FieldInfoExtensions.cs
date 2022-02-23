using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Runtime.Serialization
{
    // TODO
    internal static class FieldInfoExtensions
    {
        internal static void CheckConsistency(this FieldInfo fieldInfo, object target)
        {
            
        }

        internal static void UnsafeSetValue(this FieldInfo fieldInfo, object target, object value, BindingFlags @default, Binder s_binder, object p)
        {
            throw new NotImplementedException();
        }

        internal static object UnsafeGetValue(this FieldInfo fieldInfo, object target)
        {
            throw new NotImplementedException();
        }
    }
}
