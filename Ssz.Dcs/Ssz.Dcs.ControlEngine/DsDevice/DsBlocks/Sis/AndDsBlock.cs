using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.ControlEngine.Sis
{
    public class AndDsBlock : DsBlockBase
    {
        #region construction and destruction

        static AndDsBlock()
        {
            _majorConstParamInfos = new[]
            {
                new DsParamInfo { Name = @"COUNT", Desc = @"Inputs count" }                
            };
            _constParamInfos = new[]
            {                
                new DsParamInfo { Name = @"INPTINVSTS", Desc = @"Inputs inversion flag", IsArray = true }                
            };
            _majorParamInfos = DsParamInfo.EmptyParamsArray;
            _paramInfos = new[]
            {                
                new DsParamInfo { Name = @"IN", Desc = @"Inputs", IsArray = true },
                new DsParamInfo { Name = @"OUT", Desc = @"Output; True if all connected inputs are True" }
            };
            _paramInfosVersion = 1;
        }

        public AndDsBlock(string blockTypeString, UInt16 blockType, string tag, DsModule parentModule, ComponentDsBlock? parentComponentDsBlock) :
            base(blockTypeString, blockType, tag, parentModule, parentComponentDsBlock)
        {
        }

        #endregion

        #region public functions

        public override DsParamInfo[] MajorConstParamInfos => _majorConstParamInfos;

        public override DsParamInfo[] ConstParamInfos => _constParamInfos;

        public override DsParamInfo[] MajorParamInfos => _majorParamInfos;

        public override DsParamInfo[] ParamInfos => _paramInfos;

        public override UInt16 ParamInfosVersion => _paramInfosVersion;

        public ref DsParam COUNT => ref Params[0];

        public ref DsParam INPTINVSTS => ref Params[1];

        public ref DsParam IN => ref Params[2];

        public ref DsParam OUT => ref Params[3];

        public override void OnMajorParamsChanged()
        {
            int count = COUNT.Value.ValueAsInt32(false);
            if (count < 0) count = 0;
            if (count > 0xFFFF) count = 0xFFFF;
            INPTINVSTS.Resize(count);
            IN.Resize(count);
        }

        public override void Compute(uint dtMs)
        {
            base.Compute(dtMs);
              
            bool _out = true;
            foreach (int i in Enumerable.Range(0, COUNT.Value.ValueAsInt32(false)))
            {
                if (IN.Connections[i] is not null)
                {
                    bool _in = IN.Values[i].ValueAsBoolean(false);
                    if (INPTINVSTS.Values[i].ValueAsBoolean(false))
                        _in = !_in;
                    if (!_in)
                    {
                        _out = false;
                        break;
                    }
                }
            }
            OUT.Value.Set(_out);
        }

        #endregion

        #region private fields

        private static readonly DsParamInfo[] _majorConstParamInfos;

        private static readonly DsParamInfo[] _constParamInfos;

        private static readonly DsParamInfo[] _majorParamInfos;

        private static readonly DsParamInfo[] _paramInfos;

        private static readonly UInt16 _paramInfosVersion;

        #endregion
    }
}
