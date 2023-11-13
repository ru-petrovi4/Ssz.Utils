using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.ControlEngine.Pcs
{    
    public class AnalogInputDsBlock : DsBlockBase
    {
        #region construction and destruction

        static AnalogInputDsBlock()
        {
            _majorConstParamInfos = DsParamInfo.EmptyParamsArray;
            _constParamInfos = new[]
            {                
                new DsParamInfo { Name = @"UNITS", Desc = @"Units of the main measurement value" },
                new DsParamInfo { Name = @"HIGH_HIGH_ALARM_LIMIT", Desc = @"Value for upper upper limit of alarms" },
                new DsParamInfo { Name = @"HIGH_ALARM_LIMIT", Desc = @"Value for upper limit of alarms" },
                new DsParamInfo { Name = @"LOW_ALARM_LIMIT", Desc = @"Value for lower limit of alarms" },
                new DsParamInfo { Name = @"LOW_LOW_ALARM_LIMIT", Desc = @"Value for lower limit of alarms" },
                new DsParamInfo { Name = @"CHANNEL", Desc = @"Logical reference to the Technology DsBlock measurement" },                
            };
            _majorParamInfos = DsParamInfo.EmptyParamsArray;
            _paramInfos = new[]
            {
                new DsParamInfo { Name = @"MEASUREMENT_VALUE", Desc = @"Main measurement value as a result of the Measurement FB" },
                new DsParamInfo { Name = @"MEASUREMENT_STATUS", Desc = @"Status of the MEASUREMENT_VALUE" },
                new DsParamInfo { Name = @"PRIMARY_MEASUREMENT_VALUE", Desc = @"Primary measurement value as a result of the measurement Technology DsBlock" },
                new DsParamInfo { Name = @"PRIMARY_MEASUREMENT_STATUS", Desc = @"Status of the PRIMARY_MEASUREMENT_VALUE parameter" },                
                new DsParamInfo { Name = @"MODE", Desc = @"Operation mode of the block (e.g. Manual, Automatic, Remote, Cascade)" },                
                new DsParamInfo { Name = @"SIMULATE", Desc = @"Used to carry out internal tests" }
            };
            _paramInfosVersion = 1;
        }

        public AnalogInputDsBlock(string blockTypeString, UInt16 blockType, string tag, DsModule parentModule, ComponentDsBlock? parentComponentDsBlock) :
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

        public ref DsParam UNITS => ref Params[0];

        public ref DsParam HIGH_HIGH_ALARM_LIMIT => ref Params[1];

        public ref DsParam HIGH_ALARM_LIMIT => ref Params[2];

        public ref DsParam LOW_ALARM_LIMIT => ref Params[3];

        public ref DsParam LOW_LOW_ALARM_LIMIT => ref Params[4];

        public ref DsParam CHANNEL => ref Params[5];

        public ref DsParam MEASUREMENT_VALUE => ref Params[6];

        public ref DsParam MEASUREMENT_STATUS => ref Params[7];

        public ref DsParam PRIMARY_MEASUREMENT_VALUE => ref Params[8];

        public ref DsParam PRIMARY_MEASUREMENT_STATUS => ref Params[9];

        public ref DsParam MODE => ref Params[10];

        public ref DsParam SIMULATE => ref Params[11];

        public override void Compute(uint dtMs)
        {
            base.Compute(dtMs);

            MEASUREMENT_VALUE.Value.Set(PRIMARY_MEASUREMENT_VALUE.Value);
            MEASUREMENT_STATUS.Value.Set(PRIMARY_MEASUREMENT_STATUS.Value);
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
