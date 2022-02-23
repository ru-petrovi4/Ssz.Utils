using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Runtime.Serialization
{
    internal abstract class RtFieldInfo : FieldInfo
    {
        internal void CheckConsistency(object target)
        {
            
        }

        internal void UnsafeSetValue(object target, object value, BindingFlags @default, Binder s_binder, object p)
        {
            throw new NotImplementedException();
        }
    }
}
