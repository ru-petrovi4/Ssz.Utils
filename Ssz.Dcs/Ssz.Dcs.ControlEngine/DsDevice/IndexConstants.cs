using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.ControlEngine
{
    public static class IndexConstants
    {
        public const UInt16 DsBlockIndexInModule_InitializationNeeded = UInt16.MaxValue;

        public const UInt16 DsBlockIndexInModule_IncorrectDsBlockFullTagName = UInt16.MaxValue - 1;

        public const int ParamIndex_ParamDoesNotExist = -1;

        public const byte ParamValueIndex_IsNotArray = Byte.MaxValue;
    }
}
