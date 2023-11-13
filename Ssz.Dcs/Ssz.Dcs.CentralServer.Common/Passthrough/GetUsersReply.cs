using System.Collections.Generic;
using Ssz.Dcs.CentralServer.Common.EntityFramework;
using Ssz.Utils;
using Ssz.Utils.Serialization;

namespace Ssz.Dcs.CentralServer.Common.Passthrough
{
    /// <summary>
    ///     Request is empty.
    /// </summary>
    public class GetUsersReply : OwnedDataSerializable
    {
        #region public functions

        public List<User> UsersCollection { get; set; } = null!;

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                writer.WriteListOfOwnedDataSerializable(UsersCollection, context);                
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        UsersCollection = reader.ReadListOfOwnedDataSerializable(() => new User(), context);
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        #endregion
    }
}