using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer.Common
{
    public class DsFilesStoreDirectory : IOwnedDataSerializable
    {
        #region public functions

        /// <summary>
        ///     String.Empty for the Files Store root directory.
        /// </summary>
        public string Name => InvariantPathRelativeToRootDirectory.Substring(InvariantPathRelativeToRootDirectory.LastIndexOf('/') + 1);

        /// <summary>    
        ///     !!! Warning: always '/' as path separator !!!
        ///     Path relative to the root of the Files Store.
        ///     No '/' at the begin, no '/' at the end.
        ///     String.Empty for the Files Store root directory.
        /// </summary>
        public string InvariantPathRelativeToRootDirectory { get; set; } = @"";

        public string PathRelativeToRootDirectory
        {
            get
            {
                return InvariantPathRelativeToRootDirectory.Replace('/', Path.DirectorySeparatorChar);
            }
            set
            {
                InvariantPathRelativeToRootDirectory = value.Replace(Path.DirectorySeparatorChar, '/');
            }
        }

        public List<DsFilesStoreDirectory> ChildDsFilesStoreDirectoriesCollection { get; set; } = new List<DsFilesStoreDirectory>();

        public List<DsFilesStoreFile> DsFilesStoreFilesCollection { get; set; } = new List<DsFilesStoreFile>();

        /// <summary>
        ///     Store internal data directly into a SerializationWriter.
        ///     Uses JsonSerializer.Serialize internally.
        ///     You can specify JsonSerializerOptions as context.
        /// </summary>
        /// <param name="writer"> The SerializationWriter to use </param>
        /// <param name="context"> Optional context to use as a hint as to what to store (BitVector32 is useful) </param>
        public void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                writer.Write(InvariantPathRelativeToRootDirectory);
                writer.WriteListOfOwnedDataSerializable(ChildDsFilesStoreDirectoriesCollection, context);
                writer.WriteListOfOwnedDataSerializable(DsFilesStoreFilesCollection, context);
            }
        }

        /// <summary>
        ///     Retrieve internal data directly from a SerializationReader.
        ///     Uses JsonSerializer.Deserialize internally.
        ///     You can specify JsonSerializerOptions as context.
        /// </summary>
        /// <param name="reader"> The SerializationReader to use </param>
        /// <param name="context"> Optional context to use as a hint as to what to retrieve (BitVector32 is useful) </param>
        public virtual void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        InvariantPathRelativeToRootDirectory = reader.ReadString();
                        ChildDsFilesStoreDirectoriesCollection = reader.ReadListOfOwnedDataSerializable(() => new DsFilesStoreDirectory(), context);
                        DsFilesStoreFilesCollection = reader.ReadListOfOwnedDataSerializable(() => new DsFilesStoreFile(), context);
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        #endregion
    }
}
