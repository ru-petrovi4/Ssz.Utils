using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Runtime.Serialization
{    
    internal static class MethodInfoExtensions
    {
        internal static void SerializationInvoke(this ConstructorInfo constructorInfo, object obj, SerializationInfo info, StreamingContext context)
        {
            constructorInfo.Invoke(obj, new object[] { info, context });
        }

        internal static string SerializationToString(this PropertyInfo propertyInfo)
        {
            // VALFIX
            return propertyInfo.ToString();
        }

        internal static string SerializationToString(this ConstructorInfo constructorInfo)
        {
            // VALFIX
            return constructorInfo.ToString();
        }

        internal static string SerializationToString(this MethodInfo methodInfo)
        {
            // VALFIX
            return methodInfo.ToString();
        }
    }
}
