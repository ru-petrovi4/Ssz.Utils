using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer.Common
{
    public class DsFilesStoreFile : IOwnedDataSerializable
    {
        #region public functions

        public string Name { get; set; } = @"";

        /// <summary>
        ///     FileInfo.LastWriteTimeUtc
        /// </summary>
        public DateTime LastWriteTimeUtc { get; set; } = DateTime.MinValue;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="context"></param>
        public void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            writer.Write(Name);
            writer.Write(LastWriteTimeUtc);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="context"></param>
        public void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            Name = reader.ReadString();
            LastWriteTimeUtc = reader.ReadDateTime();
        }

        #endregion
    }
}
