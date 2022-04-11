using System;
using Ssz.Utils.Serialization;

namespace Ssz.Utils
{
    public class GuidAndName : OwnedDataSerializable
    {
        #region public functions

        public Guid Guid { get; set; }

        public string Name { get; set; } = @"";

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            writer.Write(Guid);
            writer.Write(Name);
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            Guid = reader.ReadGuid();
            Name = reader.ReadString();
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