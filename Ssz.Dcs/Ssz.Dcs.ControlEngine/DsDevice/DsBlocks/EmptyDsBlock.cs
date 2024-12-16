using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.ControlEngine
{    
    public class EmptyDsBlock : DsBlockBase
    {
        #region construction and destruction

        static EmptyDsBlock()
        {
            _paramInfos = DsParamInfo.EmptyParamsArray;
            _paramInfosVersion = 1;
        }

        public EmptyDsBlock() :
            base(@"", 0, @"", null!, null)
        {
        }

        #endregion

        #region public functions

        public override DsParamInfo[] ParamInfos => _paramInfos;

        public override UInt16 ParamInfosVersion => _paramInfosVersion;

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {            
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {            
        }

        public override void Compute(uint dtMs)
        {
        }

        #endregion

        #region private fields        

        private static readonly DsParamInfo[] _paramInfos;

        private static readonly UInt16 _paramInfosVersion;

        #endregion
    }
}
