using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Runtime.Serialization
{    
    internal static class TypeExtensions
    {
        public static ConstructorInfo GetSerializationCtor(this Type type)
        {
            return type.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance,
                new Type[] { typeof (SerializationInfo), typeof(StreamingContext) });
        }
    }
}
