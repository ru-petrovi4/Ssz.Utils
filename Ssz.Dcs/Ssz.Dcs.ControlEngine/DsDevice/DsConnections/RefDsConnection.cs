using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.ControlEngine
{
    public class RefDsConnection : DsConnectionBase
    {
        #region construction and destruction        

        public RefDsConnection(string connectionTypeString, byte connectionType, DsModule parentModule, ComponentDsBlock? parentComponentDsBlock) :
            base(connectionTypeString, connectionType, parentModule,  parentComponentDsBlock)
        {
        }

        #endregion

        #region public function        

        /// <summary>
        ///     Component_DsBlock_Tag.Tag
        /// </summary>
        public string DsBlockFullName = @"";

        /// <summary>
        ///     Must be Upper-Case
        /// </summary>
        public string ParamName = @"";

        /// <summary>
        ///     Index in module's descendant blocks array.
        /// </summary>
        public UInt16 DsBlockIndexInModule = IndexConstants.DsBlockIndexInModule_InitializationNeeded;

        public UInt16 DsBlockType;

        public UInt16 DsBlockParamInfosVersion;

        public byte ParamInfoType;

        public byte ParamInfoIndex;

        public byte ParamValueIndex;

        /// <summary>
        ///     Temp Runtime Data field.        
        /// </summary>
        public int? ParamIndex;

        public bool IsRefToMajorParam()
        {
            return ParamInfoType == 0 || ParamInfoType == 2;
        }

        public override string ConnectionString
        {
            get
            {                
                return DsDeviceHelper.GetDsBlockFullNameWithParamFullName(DsBlockFullName, ParamName, ParamValueIndex);
            }
            set
            {
                DsDeviceHelper.SplitDsBlockFullNameWithParamFullName(value, out DsBlockFullName, out ParamName, out ParamValueIndex);
            }
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            writer.Write(DsBlockFullName);
            writer.Write(ParamName);
            writer.Write(DsBlockIndexInModule);
            writer.Write(DsBlockType);
            writer.Write(DsBlockParamInfosVersion);
            writer.Write(ParamInfoType);
            writer.Write(ParamInfoIndex);
            writer.Write(ParamValueIndex);
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            DsBlockFullName = reader.ReadString();
            ParamName = reader.ReadString();
            DsBlockIndexInModule = reader.ReadUInt16();
            DsBlockType = reader.ReadUInt16();
            DsBlockParamInfosVersion = reader.ReadUInt16(); ;
            ParamInfoType = reader.ReadByte();
            ParamInfoIndex = reader.ReadByte();
            ParamValueIndex = reader.ReadByte();
        }

        public override Any GetValue()
        {
            return ParentModule.GetParamValue(this);
        }

        /// <summary>
        ///     Returns Status Code (see Ssz.Utils.StatusCodes)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Task<ResultInfo> SetValueAsync(Any value)
        {
            return Task.FromResult(ParentModule.SetParamValue(this, value));
        }

        #endregion
    }
}
