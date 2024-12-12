using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Operator.Design.License
{
    internal class LicenseInfo : IOwnedDataSerializable
    {
        public Guid Guid;

        public string Reserved = @"";

        public DateTime ValidUpTo;

        public void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            writer.Write(Guid);
            writer.Write(Reserved);
            writer.Write(ValidUpTo);
        }

        public void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            if (context is int version)
            {
                if (version == 1)
                {
                    Guid = reader.ReadGuid();
                    Reserved = reader.ReadString();
                    ValidUpTo = reader.ReadDateTime();
                }
                else
                {
                    throw new BlockUnsupportedVersionException();
                }
            }
            else
            {
                throw new BlockUnsupportedVersionException();
            }
        }
    }
}
