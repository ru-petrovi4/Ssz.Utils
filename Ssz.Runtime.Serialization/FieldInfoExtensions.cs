using System;
using System.Collections;
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
        internal static void CheckConsistency(this System.Reflection.FieldInfo fieldInfo, object target)
        {
            
        }

        internal static void UnsafeSetValue(this System.Reflection.FieldInfo fieldInfo, object target, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
        {                
            try 
            {                
                fieldInfo.SetValue(target, value, invokeAttr, binder, culture);
            }
            catch //(Exception ex)
            {
            }            
        }

        internal static object UnsafeGetValue(this System.Reflection.FieldInfo fieldInfo, object target)
        {
            return fieldInfo.GetValue(target);
        }
    }
}
