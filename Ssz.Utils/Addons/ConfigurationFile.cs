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
    public class ConfigurationFile : IOwnedDataSerializable
    {
        #region public functions

        /// <summary>
        ///     pathRelativeToRootDirectory - Path separator is always '/'. No '/' at the begin, no '/' at the end. .
        /// </summary>
        /// <param name="pathRelativeToRootDirectory"></param>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        public static ConfigurationFile CreateFromFileInfo(string pathRelativeToRootDirectory, FileInfo fileInfo)
        {
            pathRelativeToRootDirectory = pathRelativeToRootDirectory.Replace(Path.DirectorySeparatorChar, '/');

            ConfigurationFile configurationFile = new()
            {
                PathRelativeToRootDirectory = pathRelativeToRootDirectory != @"" ? 
                    pathRelativeToRootDirectory + "/" + fileInfo.Name :
                    fileInfo.Name,
                LastWriteTimeUtc = fileInfo.LastWriteTimeUtc,
            };            

            if (fileInfo.Name.EndsWith(@".csv", StringComparison.InvariantCultureIgnoreCase))
            {
                string fileDataString;
                using (var reader = CharsetDetectorHelper.GetStreamReader(fileInfo.FullName, Encoding.UTF8))
                {
                    fileDataString = reader.ReadToEnd();
                }
                configurationFile.FileData = Encoding.UTF8.GetBytes(TextFileHelper.NormalizeNewLine(fileDataString));                
            }
            else
            {
                // TEMPCODE
                //int bytesCount = (int)fileInfo.Length;
                //byte[] fileData = new byte[bytesCount];
                //using (FileStream fileStream = File.Open(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                //{
                //    fileStream.Read(fileData, 0, bytesCount);
                //}                             
                //configurationFile.FileData = fileData;
                configurationFile.FileData = new byte[0];
            }

            return configurationFile;
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
        ///     File content. Always UTF8-encoded and \n as new line char, if .csv.
        /// </summary>        
        public byte[] FileData { get; set; } = null!;

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
            FileData = reader.ReadByteArray();
            IsDeleted = reader.ReadBoolean();
        }

        /// <summary>
        ///     Substitites to PathRelativeToRootDirectory a platform-specific character used to separate directory levels.
        /// </summary>
        public string GetPathRelativeToRootDirectory_PlatformSpecific() => PathRelativeToRootDirectory.Replace('/', Path.DirectorySeparatorChar);

        #endregion
    }
}
