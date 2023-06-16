using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils.Addons
{
    /// <summary>
    ///     pathRelativeToRootDirectory - Path separator is always '/'. No '/' at the begin, no '/' at the end. 
    /// </summary>    
    public class ConfigurationCsvFile : IOwnedDataSerializable
    {
        #region public functions

        /// <summary>
        ///     pathRelativeToRootDirectory - Path separator is always '/'. No '/' at the begin, no '/' at the end. .
        /// </summary>
        /// <param name="pathRelativeToRootDirectory"></param>
        /// <param name="csvFileInfo"></param>
        /// <returns></returns>
        public static ConfigurationCsvFile CreateFromFileInfo(string pathRelativeToRootDirectory, FileInfo csvFileInfo)
        {
            pathRelativeToRootDirectory = pathRelativeToRootDirectory.Replace(Path.DirectorySeparatorChar, '/');

            var addonCsvFile = new ConfigurationCsvFile
            {
                PathRelativeToRootDirectory = pathRelativeToRootDirectory != @"" ? 
                    pathRelativeToRootDirectory + "/" + csvFileInfo.Name :
                    csvFileInfo.Name,
                LastWriteTimeUtc = csvFileInfo.LastWriteTimeUtc,
            };

            string fileData;
            using (var reader = new StreamReader(csvFileInfo.FullName, Encoding.Unicode, true))
            {
                fileData = reader.ReadToEnd();
            }

            addonCsvFile.FileData = TextFileHelper.NormalizeNewLine(fileData);

            return addonCsvFile;
        }

        /// <summary>        
        /// </summary>
        public string Name => PathRelativeToRootDirectory.Substring(PathRelativeToRootDirectory.LastIndexOf('/') + 1);

        /// <summary>        
        ///     Path relative to the root of the Files Store.
        ///     Path separator is always '/'. No '/' at the begin, no '/' at the end.        
        /// </summary>        
        public string PathRelativeToRootDirectory { get; set; } = @"";

        /// <summary>
        ///     Substitites to PathRelativeToRootDirectory a platform-specific character used to separate directory levels.
        /// </summary>
        public string PathRelativeToRootDirectory_PlatformSpecific => PathRelativeToRootDirectory.Replace('/', Path.DirectorySeparatorChar);

        /// <summary>        
        ///     
        /// </summary>        
        public string SourceId { get; set; } = @"";

        /// <summary>        
        ///     
        /// </summary>        
        public string SourceIdToDisplay { get; set; } = @"";

        /// <summary>
        ///     FileInfo.LastWriteTimeUtc
        /// </summary>        
        public DateTime LastWriteTimeUtc { get; set; } = DateTime.MinValue;

        /// <summary>
        ///     File content. Always \n as new line char.
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
            writer.Write(SourceIdToDisplay);
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
            SourceIdToDisplay = reader.ReadString();
            LastWriteTimeUtc = reader.ReadDateTime();
            FileData = reader.ReadString();
        }

        #endregion
    }
}
