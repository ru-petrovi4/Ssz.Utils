using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer.Common
{
    public class SaveFilesRequest : IOwnedDataSerializable
    {
        #region public functions        

        public List<DsFilesStoreFileData> DatasCollection { get; set; } = new();
        
        public void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {                
                writer.WriteListOfOwnedDataSerializable(DatasCollection, context);
            }
        }
        
        public virtual void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:                        
                        DatasCollection = reader.ReadListOfOwnedDataSerializable(() => new DsFilesStoreFileData(), context);
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        #endregion
    }
}
