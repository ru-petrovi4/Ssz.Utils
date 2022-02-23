using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Runtime.Serialization
{
    // TODO
    internal static class ConstructorInfoExtensions
    {
        internal static void SerializationInvoke(this ConstructorInfo constructorInfo, object obj, SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }
    }
}
