using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.ControlEngine
{
    public class JournalDsBlock : DsBlockBase
    {
        #region construction and destruction

        static JournalDsBlock()
        {
            _majorConstParamInfos = DsParamInfo.EmptyParamsArray;
            _constParamInfos = new[]
            {
                new DsParamInfo { Name = @"UNITS", Desc = @"Units of the value" },
                new DsParamInfo { Name = @"HI_LIM", Desc = @"Value for upper upper limit of value" },
                new DsParamInfo { Name = @"LO_LIM", Desc = @"Value for lower limit of value" },
                new DsParamInfo { Name = @"SETTINGS", Desc = @"Sampling interval" },
            };
            _majorParamInfos = DsParamInfo.EmptyParamsArray;
            _paramInfos = new[]
            {
                new DsParamInfo { Name = @"VALUE", Desc = @"Main measurement value as a result of the Measurement FB" },                
                new DsParamInfo { Name = @"MODE", Desc = @"Operation mode of the block (e.g. Manual, Automatic, Remote, Cascade)" },                
            };
            _paramInfosVersion = 1;
        }

        public JournalDsBlock(string blockTypeString, UInt16 blockType, string tag, DsModule parentModule, ComponentDsBlock? parentComponentDsBlock) :
            base(blockTypeString, blockType, tag, parentModule, parentComponentDsBlock)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                if (_journalItemHandle != 0)
                    ParentModule.Device.HistoryValues.RemoveItem(_journalItemHandle);
            }

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public override DsParamInfo[] MajorConstParamInfos => _majorConstParamInfos;

        public override DsParamInfo[] ConstParamInfos => _constParamInfos;

        public override DsParamInfo[] MajorParamInfos => _majorParamInfos;

        public override DsParamInfo[] ParamInfos => _paramInfos;

        public override UInt16 ParamInfosVersion => _paramInfosVersion;

        public ref DsParam UNITS => ref Params[0];

        public ref DsParam HI_LIM => ref Params[1];

        public ref DsParam LO_LIM => ref Params[2];

        public ref DsParam SETTINGS => ref Params[3];

        public ref DsParam VALUE => ref Params[4];

        public ref DsParam MODE => ref Params[5];

        public override void Compute(uint dtMs)
        {
            base.Compute(dtMs);

            string settings = SETTINGS.Value.ValueAsString(false);
            if (settings == @"") return;

            if (_journalItemHandle == 0)
            {
                _journalItemHandle = ParentModule.Device.HistoryValues.AddItem(DsDeviceHelper.GetDsBlockFullName(this) + ".VALUE", settings);
            }
            ParentModule.Device.HistoryValues.WriteValue(_journalItemHandle, ParentModule.Device.ModelTimeMs, 
                VALUE.Value.ValueAsDouble(false),
                LO_LIM.Value.ValueAsDouble(false),
                HI_LIM.Value.ValueAsDouble(false));
        }

        #endregion

        #region private fields

        private static readonly DsParamInfo[] _majorConstParamInfos;

        private static readonly DsParamInfo[] _constParamInfos;

        private static readonly DsParamInfo[] _majorParamInfos;

        private static readonly DsParamInfo[] _paramInfos;

        private static readonly UInt16 _paramInfosVersion;

        private UInt32 _journalItemHandle;

        #endregion
    }
}
