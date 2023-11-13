using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.ControlEngine
{
    public class DeviceDsBlock : DsBlockBase
    {
        #region construction and destruction

        static DeviceDsBlock()
        {
            _majorConstParamInfos = DsParamInfo.EmptyParamsArray;
            _constParamInfos = new[]
            {
                new DsParamInfo { Name = @"DEVICE_VENDOR", Desc = @"Company name of the manufacturer" },
                new DsParamInfo { Name = @"DEVICE_MODEL", Desc = @"Name of the device model" },
                new DsParamInfo { Name = @"DEVICE_REVISION", Desc = @"Device revision number" },                
            };
            _majorParamInfos = DsParamInfo.EmptyParamsArray;
            _paramInfos = new[]
            {
                new DsParamInfo { Name = @"DEVICE_STATUS", Desc = @"Status of the device" },
                new DsParamInfo { Name = @"MODEL_TIME", Desc = @"Already calculated Model Time (seconds)" },
                new DsParamInfo { Name = @"MODEL_TIME_MS", Desc = @"Currently calculating Model Time (milliseconds)" },
                new DsParamInfo { Name = @"SCENARIO_NAME", Desc = @"Currently running scenario name" },
                new DsParamInfo { Name = @"SCENARIO_STEP_DONE", Desc = @"Currently completed scenario step" },
            };
            _paramInfosVersion = 1;
        }

        public DeviceDsBlock(string blockTypeString, UInt16 blockType, string tag, DsModule parentModule, ComponentDsBlock? parentComponentDsBlock) :
            base(blockTypeString, blockType, tag, parentModule, parentComponentDsBlock)
        {
            DEVICE_VENDOR.Value.Set(@"Ssz");
            DEVICE_MODEL.Value.Set(@"Dcs DCS Engine");
            DEVICE_REVISION.Value.Set(@"1.0");
        }

        #endregion

        #region public functions

        public override DsParamInfo[] MajorConstParamInfos => _majorConstParamInfos;

        public override DsParamInfo[] ConstParamInfos => _constParamInfos;

        public override DsParamInfo[] MajorParamInfos => _majorParamInfos;

        public override DsParamInfo[] ParamInfos => _paramInfos;

        public override UInt16 ParamInfosVersion => _paramInfosVersion;

        public ref DsParam DEVICE_VENDOR => ref Params[0];

        public ref DsParam DEVICE_MODEL => ref Params[1];

        public ref DsParam DEVICE_REVISION => ref Params[2];

        public ref DsParam DEVICE_STATUS => ref Params[3];

        /// <summary>
        ///     Already calculated Model Time (seconds).
        ///     UInt64
        /// </summary>
        public ref DsParam MODEL_TIME => ref Params[4];

        /// <summary>
        ///     Currently calculating Model Time (milliseconds)
        ///     UInt64
        /// </summary>
        public ref DsParam MODEL_TIME_MS => ref Params[5];

        /// <summary>
        ///     Currently playing scenario name
        /// </summary>
        public ref DsParam SCENARIO_NAME => ref Params[6];

        public ref DsParam SCENARIO_STEP_DONE => ref Params[7];

        //public override void Compute(int dtMs)
        //{
        //    base.Compute(dtMs);


        //}

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
