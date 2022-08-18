using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils.Addons
{
    public class AddonCsvFile : IOwnedDataSerializable
    {
        #region public functions

        /// <summary>
        ///     pathRelativeToRootDirectory - no path separator at the begin and end.
        /// </summary>
        /// <param name="pathRelativeToRootDirectory"></param>
        /// <param name="csvFileInfo"></param>
        /// <returns></returns>
        public static AddonCsvFile CreateFromFileInfo(string pathRelativeToRootDirectory, FileInfo csvFileInfo)
        {
            var addonCsvFile = new AddonCsvFile
            {
                PathRelativeToRootDirectory = pathRelativeToRootDirectory != @"" ? 
                    pathRelativeToRootDirectory + Path.DirectorySeparatorChar + csvFileInfo.Name :
                    csvFileInfo.Name,
                LastWriteTimeUtc = csvFileInfo.LastWriteTimeUtc,
            };

            using (var reader = new StreamReader(csvFileInfo.FullName, true))
            {
                addonCsvFile.FileData = reader.ReadToEnd();
            }

            return addonCsvFile;
        }

        /// <summary>        
        /// </summary>
        public string Name => PathRelativeToRootDirectory.Substring(PathRelativeToRootDirectory.LastIndexOf(Path.DirectorySeparatorChar) + 1);

        /// <summary>        
        ///     Path relative to the root of the Files Store.
        ///     No '\' at the begin, no '\' at the end.        
        /// </summary>
        public string PathRelativeToRootDirectory { get; set; } = @"";

        /// <summary>        
        ///     You can store here data about source.
        /// </summary>
        public string SourceId { get; set; } = @"";

        /// <summary>
        ///     FileInfo.LastWriteTimeUtc
        /// </summary>
        public DateTime LastWriteTimeUtc { get; set; } = DateTime.MinValue;

        /// <summary>
        ///     File content.
        /// </summary>
        public string FileData { get; set; } = null!;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="context"></param>
        public void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            writer.Write(PathRelativeToRootDirectory);
            writer.Write(SourceId);
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
            SourceId = reader.ReadString();
            LastWriteTimeUtc = reader.ReadDateTime();
            FileData = reader.ReadString();
        }

        #endregion
    }
}
