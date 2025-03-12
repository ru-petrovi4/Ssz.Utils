using System;
using Ssz.Operator.Core.Utils.Serialization;

namespace Ssz.Operator.Core.Utils
{
    public class GuidAndName : IOwnedDataSerializable
    {
        #region public functions

        public Guid Guid { get; set; }

        public string? Name { get; set; }

        public void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                writer.Write(Guid);
                writer.Write(Name);
            }
        }

        public void DeserializeOwnedDataAsync(SerializationReader reader, object? context)
        {
            using (var block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        Guid = reader.ReadGuid();
                        Name = reader.ReadString();
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }


        public override bool Equals(object? obj)
        {
            var other = obj as GuidAndName;
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Guid == other.Guid;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public override string ToString()
        {
            return @"";
        }

        #endregion
    }
}