using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Runtime.Serialization
{
    internal class SszDelegate
    {
        internal static SerializationEventHandler CreateDelegateNoSecurityCheck(Type type, object obj, MethodInfo m)
        {
            throw new NotImplementedException();
        }
    }
}
