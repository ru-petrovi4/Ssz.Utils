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
            if (Ssz.Runtime.Serialization.Settings.IsDeserializingFromNet4)
            {
                if (value != null && !fieldInfo.FieldType.IsAssignableFrom(value.GetType()))
                {
                    if (fieldInfo.DeclaringType.Name == @"Font")
                    {
                        return;
                    }
                }
                if (fieldInfo.FieldType == typeof(Font))
                {
                    value = new Font("Arial", 12);
                }
            }            
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
