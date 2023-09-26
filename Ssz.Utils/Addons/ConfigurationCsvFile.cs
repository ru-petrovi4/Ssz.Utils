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
            using (var reader = CharsetDetectorHelper.GetStreamReader(csvFileInfo.FullName, Encoding.UTF8))
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
        ///     
        /// </summary>        
        public string SourcePath { get; set; } = @"";

        /// <summary>        
        ///     
        /// </summary>        
        public string SourceId { get; set; } = @"";

        /// <summary>        
        ///     
        /// </summary>        
        public string SourceIdToDisplay { get; set; } = @"";

        /// <summary>        
        ///     Path relative to the root of the Files Store.
        ///     Path separator is always '/'. No '/' at the begin, no '/' at the end.        
        /// </summary>        
        public string PathRelativeToRootDirectory { get; set; } = @"";

        /// <summary>
        ///     FileInfo.LastWriteTimeUtc
        /// </summary>        
        public DateTime LastWriteTimeUtc { get; set; } = DateTime.MinValue;

        /// <summary>
        ///     File content. Always \n as new line char.
        /// </summary>        
        public string FileData { get; set; } = null!;

        /// <summary>
        ///     Whether file must be deleted
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="context"></param>
        public void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            writer.Write(SourcePath);
            writer.Write(SourceId);
            writer.Write(SourceIdToDisplay);
            writer.Write(PathRelativeToRootDirectory);            
            writer.Write(LastWriteTimeUtc);
            writer.Write(FileData);
            writer.Write(IsDeleted);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="context"></param>
        public void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            SourcePath = reader.ReadString();
            SourceId = reader.ReadString();
            SourceIdToDisplay = reader.ReadString();
            PathRelativeToRootDirectory = reader.ReadString();            
            LastWriteTimeUtc = reader.ReadDateTime();
            FileData = reader.ReadString();
            IsDeleted = reader.ReadBoolean();
        }

        /// <summary>
        ///     Substitites to PathRelativeToRootDirectory a platform-specific character used to separate directory levels.
        /// </summary>
        public string GetPathRelativeToRootDirectory_PlatformSpecific() => PathRelativeToRootDirectory.Replace('/', Path.DirectorySeparatorChar);

        #endregion
    }
}
