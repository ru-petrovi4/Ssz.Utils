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
        public string Name => PathRelativeToRootDirectory.Substring(PathRelativeToRootDirectory.LastIndexOf(Path.DirectorySeparatorChar) + 1);

        /// <summary>        
        ///     Path relative to the root of the Files Store.
        ///     No '\' at the begin, no '\' at the end.        
        /// </summary>
        public string PathRelativeToRootDirectory { get; set; } = @"";

        /// <summary>
        ///     FileInfo.LastWriteTimeUtc
        /// </summary>
        public DateTime LastWriteTimeUtc { get; set; } = DateTime.MinValue;

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
            writer.Write(PathRelativeToRootDirectory);
            writer.Write(LastWriteTimeUtc);
            writer.Write(FileData);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="context"></param>
        public void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            PathRelativeToRootDirectory = reader.ReadString();
            LastWriteTimeUtc = reader.ReadDateTime();
            FileData = reader.ReadByteArray();
        }

        #endregion
    }
}
