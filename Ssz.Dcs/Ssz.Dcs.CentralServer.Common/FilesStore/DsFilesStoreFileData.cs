using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer.Common
{
    public class DsFilesStoreFileData : IOwnedDataSerializable
    {
        #region public functions

        /// <summary>        
        /// </summary>
        public string Name => InvariantPathRelativeToRootDirectory.Substring(InvariantPathRelativeToRootDirectory.LastIndexOf('/') + 1);

        /// <summary>  
        ///     !!! Warning: always '/' as path separator !!!
        ///     Path relative to the root of the Files Store.
        ///     No '/' at the begin, no '/' at the end.        
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

        /// <summary>
        ///     FileInfo.LastWriteTimeUtc
        /// </summary>
        public DateTimeOffset LastModified { get; set; }

        /// <summary>
        ///     File content.
        /// </summary>
        public byte[] FileData { get; set; } = null!;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="context"></param>
        public void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            writer.Write(InvariantPathRelativeToRootDirectory);
            writer.Write(LastModified);
            writer.WriteArray(FileData);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="context"></param>
        public void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            InvariantPathRelativeToRootDirectory = reader.ReadString();
            LastModified = reader.ReadDateTimeOffset();
            FileData = reader.ReadByteArray();
        }

        #endregion
    }
}
