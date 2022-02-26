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
        internal static void CheckConsistency(this FieldInfo fieldInfo, object target)
        {
            
        }

        internal static void UnsafeSetValue(this FieldInfo fieldInfo, object target, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
        {
            if (value != null && !fieldInfo.FieldType.IsAssignableFrom(value.GetType()))
            {
                if (fieldInfo.DeclaringType.Name == @"Font")
                {
                    return;
                    //if (value is int intValue)
                    //{
                    //    ((Font)target).Size = intValue;
                    //    return;
                    //}
                }
                //if (fieldInfo.FieldType == typeof(System.Collections.Hashtable))
                //{
                //    var newValue = new System.Collections.Hashtable();
                //    foreach (DictionaryEntry kv in ((Ssz.Collections.Hashtable)value))
                //        newValue.Add(kv.Key, kv.Value);
                //    value = newValue;
                //}               
            }            
            if (fieldInfo.FieldType == typeof(Font))
            {
                value = new Font("Arial", 12);
                //var oldValue = (Font)value;
                //value = new Font(oldValue.FontFamily, oldValue.Size, oldValue.Style);
            }
            try 
            {                
                fieldInfo.SetValue(target, value, invokeAttr, binder, culture);
            }
            catch (Exception ex)
            {
            }            
        }

        internal static object UnsafeGetValue(this FieldInfo fieldInfo, object target)
        {
            return fieldInfo.GetValue(target);
        }
    }
}
