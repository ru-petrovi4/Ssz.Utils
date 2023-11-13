using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.ControlEngine
{
    public struct DsParamAlias : IOwnedDataSerializable
    {
        #region construction and destruction

        /// <summary>
        ///     paramAlias must be in Upper-Case.
        /// </summary>
        /// <param name="paramAliasString"></param>
        /// <param name="blockFullNameWithParamFullName"></param>
        /// <param name="componentDsBlock"></param>
        public DsParamAlias(string paramAliasString, string blockFullNameWithParamFullName, ComponentDsBlock componentDsBlock)
        {
            ParamAliasString = paramAliasString;
            Connection = DsConnectionsFactory.CreateRefConnection(blockFullNameWithParamFullName, componentDsBlock.ParentModule, componentDsBlock);            
        }

        #endregion        

        #region public functions

        public static readonly DsParamAlias[] ParamAliasesEmptyArray = new DsParamAlias[0];

        /// <summary>
        ///     Must be in Upper-Case.
        /// </summary>
        public string ParamAliasString;

        public RefDsConnection Connection;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="context"></param>
        public void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                writer.Write(ParamAliasString);
                Connection.SerializeOwnedData(writer, null);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="context"></param>
        public void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        ParamAliasString = reader.ReadString();
                        Connection.DeserializeOwnedData(reader, null);
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        #endregion

    }
}
