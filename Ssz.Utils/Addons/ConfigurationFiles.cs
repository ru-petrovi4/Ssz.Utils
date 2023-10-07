using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ssz.Utils.Addons
{
    public class ConfigurationFiles : IOwnedDataSerializable
    {
        #region public functions        

        public List<ConfigurationFile> ConfigurationFilesCollection { get; set; } = new();        

        public void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                writer.WriteListOfOwnedDataSerializable(ConfigurationFilesCollection, context);
            }
        }

        public virtual void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        ConfigurationFilesCollection = reader.ReadListOfOwnedDataSerializable(() => new ConfigurationFile(), context);
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        #endregion
    }
}
