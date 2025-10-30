using Ssz.Utils;
using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.ControlEngine
{
    public class DsBlockFilesCache : IOwnedDataSerializable
    {
        #region public functions

        public const string DataFilesCollectionCacheFileName = @"cache.bin";

        /// <summary>
        ///     [FileName, DataFileCache]
        /// </summary>
        public CaseInsensitiveOrderedDictionary<DsBlockFileCache> DsBlockFileCachesCollection { get; } = new();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="context"></param>
        public void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {                
                writer.Write(DsBlockFileCachesCollection.Count);
                foreach (var kvp in DsBlockFileCachesCollection)
                {
                    writer.Write(kvp.Key);
                    writer.WriteOwnedDataSerializable(kvp.Value, null);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="context"></param>
        public void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:                        
                        int count = reader.ReadInt32();
                        foreach (int _ in Enumerable.Range(0, count))
                        {
                            string key = reader.ReadString();
                            var value = new DsBlockFileCache();
                            reader.ReadOwnedDataSerializable(value, null);
                            DsBlockFileCachesCollection.Add(key, value);
                        }
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public void Save(DirectoryInfo dataDirectoryInfo)
        {
            try
            {
                using (var memoryStream = new MemoryStream(1024 * 1024))
                {
                    using (var writer = new SerializationWriter(memoryStream, true))
                    {
                        SerializeOwnedData(writer, null);
                    }

                    using (FileStream fileStream = File.Create(Path.Combine(dataDirectoryInfo.FullName, DataFilesCollectionCacheFileName)))
                    {
                        memoryStream.WriteTo(fileStream);
                    }
                }
            }
            catch
            {
            }            
        }

        public void Load(DirectoryInfo dataDirectoryInfo)
        {
            DsBlockFileCachesCollection.Clear();
            var fileInfo = new FileInfo(Path.Combine(dataDirectoryInfo.FullName, DataFilesCollectionCacheFileName));
            if (!fileInfo.Exists) return;
            try
            {
                using (var memoryStream = new MemoryStream(File.ReadAllBytes(fileInfo.FullName)))
                using (var reader = new SerializationReader(memoryStream))
                {
                    DeserializeOwnedData(reader, null);
                }
            }
            catch
            {
            }            
        }

        #endregion

        public class DsBlockFileCache : IOwnedDataSerializable
        {
            #region public functions

            public string FileName { get; set; } = @"";

            public DateTime FileLastWriteTimeUtc { get; set; } = DateTime.MinValue;
            
            public List<DsModuleCache> ModuleCachesCollection { get; } = new();

            /// <summary>
            /// 
            /// </summary>
            /// <param name="writer"></param>
            /// <param name="context"></param>
            public void SerializeOwnedData(SerializationWriter writer, object? context)
            {
                using (writer.EnterBlock(1))
                {
                    writer.Write(FileName);
                    writer.Write(FileLastWriteTimeUtc);
                    writer.Write(ModuleCachesCollection.Count);
                    foreach (var dsModuleCache in ModuleCachesCollection)
                    {
                        writer.Write(dsModuleCache.ModuleName);
                        if (dsModuleCache.ModuleRef is not null)
                        {
                            byte[] moduleSerializationData;
                            using (var memoryStream = new MemoryStream(1024))
                            {
                                using (var w = new SerializationWriter(memoryStream))
                                {
                                    dsModuleCache.ModuleRef.SerializeOwnedData(w, null);
                                }
                                moduleSerializationData = memoryStream.ToArray();
                            }
                            writer.WriteArray(moduleSerializationData);
                        }
                        else
                        {
                            writer.WriteArray(dsModuleCache.ModuleSerializationData!);
                        }                        
                    }
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="reader"></param>
            /// <param name="context"></param>
            public void DeserializeOwnedData(SerializationReader reader, object? context)
            {
                using (Block block = reader.EnterBlock())
                {
                    switch (block.Version)
                    {
                        case 1:
                            FileName = reader.ReadString();
                            FileLastWriteTimeUtc = reader.ReadDateTime();
                            ModuleCachesCollection.Clear();
                            int count = reader.ReadInt32();
                            foreach (int i in Enumerable.Range(0, count))
                            {
                                string name = reader.ReadString();
                                byte[] moduleSerializationData = reader.ReadByteArray();
                                ModuleCachesCollection.Add(new DsModuleCache
                                {
                                    ModuleName = name,
                                    ModuleSerializationData = moduleSerializationData
                                });
                            }
                            break;
                        default:
                            throw new BlockUnsupportedVersionException();
                    }
                }
            }

            #endregion
        }

        public class DsModuleCache
        {
            #region public functions

            public string ModuleName { get; set; } = @"";

            /// <summary>
            ///     ModuleSerializationData or ModuleRef is null.
            /// </summary>
            public byte[]? ModuleSerializationData { get; set; }

            /// <summary>
            ///     ModuleSerializationData or ModuleRef is null.
            /// </summary>
            public DsModule? ModuleRef { get; set; }

            #endregion
        }
    }
}
