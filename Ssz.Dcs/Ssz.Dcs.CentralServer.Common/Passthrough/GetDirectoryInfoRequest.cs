using Ssz.Utils;
using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer.Common.Passthrough
{
    /// <summary>
    ///     Reply is DsFilesStoreDirectory
    /// </summary>
    public class GetDirectoryInfoRequest : IOwnedDataSerializable
    {
        #region public functions

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

        public int FilesAndDirectoriesIncludeLevel { get; set; }

        public void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                writer.Write(InvariantPathRelativeToRootDirectory);
                writer.Write(FilesAndDirectoriesIncludeLevel);
            }
        }

        public void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        InvariantPathRelativeToRootDirectory = reader.ReadString();
                        FilesAndDirectoriesIncludeLevel = reader.ReadInt32();
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        #endregion
    }
}
