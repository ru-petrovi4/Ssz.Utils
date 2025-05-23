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
    ///     
    /// </summary>    
    public class ConfigurationFile : IOwnedDataSerializable
    {
        #region public functions

        public static readonly string[] EditableFilesExtensions = [ @".txt", @".csv", @".htm", @".html", @".yml", @".xml" ];

        /// <summary>
        ///     pathRelativeToRootDirectory_NoFIleName - Path separator is always '/'. No '/' at the begin, file name at the end.
        /// </summary>
        /// <param name="invariantPathRelativeToRootDirectory"></param>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        public static ConfigurationFile CreateFromFileInfo(string invariantPathRelativeToRootDirectory, FileInfo fileInfo, bool readBigFileData)
        {
            ConfigurationFile configurationFile = new()
            {
                InvariantPathRelativeToRootDirectory = invariantPathRelativeToRootDirectory,
                LastModified = fileInfo.LastWriteTimeUtc,
                Length = fileInfo.Length,
            };            

            if (EditableFilesExtensions.Any(ext => fileInfo.Name.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase)))
            {
                bool readFileData;
                if (readBigFileData)
                {
                    readFileData = true;
                }
                else
                {
                    int slashCount = configurationFile.InvariantPathRelativeToRootDirectory.Count(f => f == '/');
                    if (slashCount == 0)
                        readFileData = true;
                    else
                        readFileData = String.Equals(fileInfo.Name, AddonBase.OptionsCsvFileName, StringComparison.InvariantCultureIgnoreCase);
                }                

                if (readFileData)
                {
                    string fileDataString;
                    using (var reader = CharsetDetectorHelper.GetStreamReader(fileInfo.FullName, Encoding.UTF8))
                    {
                        fileDataString = reader.ReadToEnd();
                    }
                    fileDataString = TextFileHelper.NormalizeNewLine(fileDataString);
                    configurationFile.FileData = StringHelper.GetUTF8BytesWithBomPreamble(fileDataString);
                }
                else
                {
                    configurationFile.FileData = null;
                }
            }
            else
            {
                if (readBigFileData)
                {                    
                    byte[] fileData;
                    using (FileStream fileStream = File.Open(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (MemoryStream memoryStream = new())
                    {
                        fileStream.CopyTo(memoryStream);
                        fileData = memoryStream.ToArray();
                    }
                    configurationFile.FileData = fileData;
                }
                else
                {
                    configurationFile.FileData = null;
                }                
            }

            return configurationFile;
        }

        /// <summary>        
        /// </summary>
        public string Name => InvariantPathRelativeToRootDirectory.Substring(InvariantPathRelativeToRootDirectory.LastIndexOf('/') + 1);

        /// <summary>        
        ///     String.Empty for in process entities.
        ///     Path separator is always '/'. No '/' at the begin, no '/' at the end.
        /// </summary>        
        public string SourcePath { get; set; } = @"";

        /// <summary>        
        ///     Globally-unique service (process) id.
        /// </summary>        
        public string SourceId { get; set; } = @"";

        /// <summary>        
        ///     Globally-unique service (process) id to display.
        /// </summary>        
        public string SourceIdToDisplay { get; set; } = @"";

        /// <summary>       
        ///     !!! Warning: always '/' as path separator !!!
        ///     Path relative to the root of the Files Store.
        ///     No '/' at the begin, file name at the end.        
        /// </summary>        
        public string InvariantPathRelativeToRootDirectory { get; set; } = @"";

        /// <summary>       
        ///     !!! Warning: always Path.DirectorySeparatorChar as path separator !!!
        ///     Path relative to the root of the Files Store.
        ///     No Path.DirectorySeparatorChar at the begin, file name at the end.        
        /// </summary>   
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
        ///     FileInfo.Length
        /// </summary>  
        public long Length { get; set; }

        /// <summary>
        ///     File content. 
        ///     If .csv., UTF8-encoded with preamble and \n as new line char.
        /// </summary>        
        public byte[]? FileData { get; set; } = null!;

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
            writer.Write(InvariantPathRelativeToRootDirectory);            
            writer.Write(LastModified);
            writer.Write(Length);
            writer.WriteNullableByteArray(FileData);
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
            InvariantPathRelativeToRootDirectory = reader.ReadString();            
            LastModified = reader.ReadDateTimeOffset();
            Length = reader.ReadInt64();
            FileData = reader.ReadNullableByteArray();
            IsDeleted = reader.ReadBoolean();
        }

        /// <summary>
        ///     Substitites to PathRelativeToRootDirectory a platform-specific character used to separate directory levels.
        /// </summary>
        public string GetPathRelativeToRootDirectory_PlatformSpecific() => InvariantPathRelativeToRootDirectory.Replace('/', Path.DirectorySeparatorChar);

        #endregion
    }
}
