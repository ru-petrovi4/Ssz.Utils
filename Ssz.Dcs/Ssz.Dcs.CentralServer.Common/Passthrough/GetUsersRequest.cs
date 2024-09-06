using System.Collections.Generic;
using Ssz.Dcs.CentralServer.Common.EntityFramework;
using Ssz.Utils;
using Ssz.Utils.Serialization;

namespace Ssz.Dcs.CentralServer.Common.Passthrough
{
    /// <summary>
    ///     Request is empty.
    /// </summary>
    public class GetUsersRequest : OwnedDataSerializable
    {
        #region public functions

        public string ProcessModelNames { get; set; } = @"";

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                writer.Write(ProcessModelNames);                
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        ProcessModelNames = reader.ReadString();
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        #endregion
    }
}