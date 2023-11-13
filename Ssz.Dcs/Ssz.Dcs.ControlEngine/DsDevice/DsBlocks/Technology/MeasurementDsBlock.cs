using Ssz.Utils;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.ControlEngine.Technology
{
    public class MeasurementDsBlock : DsBlockBase
    {
        #region construction and destruction

        static MeasurementDsBlock()
        {
            _majorConstParamInfos = DsParamInfo.EmptyParamsArray;
            _constParamInfos = new[]
            {
                new DsParamInfo { Name = @"SENSOR_HI_LIM", Desc = @"Physical upper limit of the sensor" },
                new DsParamInfo { Name = @"SENSOR_LO_LIM", Desc = @"Physical lower limit of the sensor" }
            };
            _majorParamInfos = DsParamInfo.EmptyParamsArray;
            _paramInfos = new[]
            {
                new DsParamInfo { Name = @"RAW_MEASUREMENT_VALUE", Desc = @"Raw measurement value as result of measurement acquisition" },
                new DsParamInfo { Name = @"RAW_MEASUREMENT_STATUS", Desc = @"Status of RAW_MEASUREMENT_VALUE parameter" },
                new DsParamInfo { Name = @"PRIMARY_MEASUREMENT_VALUE", Desc = @"Primary measurement value as result of the transformation function" },
                new DsParamInfo { Name = @"PRIMARY_MEASUREMENT_STATUS", Desc = @"Status of PRIMARY_MEASUREMENT_VALUE parameter" },
            };
            _paramInfosVersion = 1;
        }

        public MeasurementDsBlock(string blockTypeString, UInt16 blockType, string tag, DsModule parentModule, ComponentDsBlock? parentComponentDsBlock) :
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

        public ref DsParam SENSOR_HI_LIM => ref Params[0];

        public ref DsParam SENSOR_LO_LIM => ref Params[1];       

        public ref DsParam RAW_MEASUREMENT_VALUE => ref Params[2];

        public ref DsParam RAW_MEASUREMENT_STATUS => ref Params[3];

        public ref DsParam PRIMARY_MEASUREMENT_VALUE => ref Params[4];

        public ref DsParam PRIMARY_MEASUREMENT_STATUS => ref Params[5];        

        public override void Compute(uint dtMs)
        {
            base.Compute(dtMs);            

            PRIMARY_MEASUREMENT_VALUE.Value.Set(RAW_MEASUREMENT_VALUE.Value);
            PRIMARY_MEASUREMENT_STATUS.Value.Set(RAW_MEASUREMENT_STATUS.Value);
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
