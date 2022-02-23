using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Runtime.Serialization
{    
    internal static class FieldInfoExtensions
    {
        internal static void CheckConsistency(this FieldInfo fieldInfo, object target)
        {
            
        }

        internal static void UnsafeSetValue(this FieldInfo fieldInfo, object target, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
        {
            fieldInfo.SetValue(target, value, invokeAttr, binder, culture);
        }

        internal static object UnsafeGetValue(this FieldInfo fieldInfo, object target)
        {
            return fieldInfo.GetValue(target);
        }
    }
}
