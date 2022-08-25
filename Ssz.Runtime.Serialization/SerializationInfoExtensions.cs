using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Runtime.Serialization
{
    internal static class SerializationInfoExtensions
    {
        internal static void GetValueNoThrow(this System.Reflection.FieldInfo serializationInfo, object target)
        {
            //serializationInfo.GetValue()
        }
    }
}
