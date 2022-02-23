using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Runtime.Serialization
{
    // TODO
    internal static class TypeExtensions
    {
        public static ConstructorInfo GetSerializationCtor(this Type type)
        {
            return type.GetConstructor(new Type[0]);
        }
    }
}
