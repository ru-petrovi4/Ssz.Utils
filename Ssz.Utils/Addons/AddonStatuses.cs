using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ssz.Utils.Addons
{
    public class AddonStatuses : IOwnedDataSerializable
    {
        #region public functions        

        public List<AddonStatus> AddonStatusesCollection { get; set; } = new();        

        public void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                writer.WriteListOfOwnedDataSerializable(AddonStatusesCollection, context);
            }
        }

        public virtual void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        AddonStatusesCollection = reader.ReadListOfOwnedDataSerializable(() => new AddonStatus(), context);
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        #endregion
    }
}
