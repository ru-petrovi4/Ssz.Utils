using System;
using System.IO;
using System.Security.Cryptography;
using Ssz.Utils.Net4.Serialization;

namespace Ssz.Utils.Net4
{
    public static class CacheService
    {
        #region public functions

        public static object GetCacheObject(FileInfo sourceFileInfo)
        {
            if (!sourceFileInfo.Exists) return null;

            byte[] sourceFileHash;
			using (var sha = SHA256.Create())
			{
				using (FileStream stream = File.OpenRead(sourceFileInfo.FullName))
				{
					sourceFileHash = sha.ComputeHash(stream);
				}
			}

            FileInfo cacheFileInfo =
                GetCacheFileInfo(sourceFileInfo.FullName, sourceFileHash);

            if (!cacheFileInfo.Exists) return null;

            using (FileStream fileStream = File.OpenRead(cacheFileInfo.FullName))
            {
                using (var reader = new SerializationReader(fileStream))
				{
					return reader.ReadObject();
				}
			}
        }

        public static void SetCacheObject(FileInfo sourceFileInfo, object obj)
        {
            if (!sourceFileInfo.Exists) return;

            byte[] sourceFileHash;
			using (var sha = SHA256.Create())
			{
				using (FileStream stream = File.OpenRead(sourceFileInfo.FullName))
				{
					sourceFileHash = sha.ComputeHash(stream);
				}
			}

            FileInfo cacheFileInfo =
                GetCacheFileInfo(sourceFileInfo.FullName, sourceFileHash);

            using (var memoryStream = new MemoryStream(1024))
            {
                using (var writer = new SerializationWriter(memoryStream))
                {
                    writer.WriteObject(obj);
                }
                using (FileStream fileStream = File.Create(cacheFileInfo.FullName))                
                {
                    memoryStream.WriteTo(fileStream);
                }
            }            
        }

        #endregion

        #region private functions

        private static FileInfo GetCacheFileInfo(string sourceFileInfoFullName, byte[] sourceFileHash)
        {
            string postfix = Convert.ToBase64String(sourceFileHash);
            foreach (char ch in Path.GetInvalidFileNameChars())
            {
                postfix = postfix.Replace(ch, '_');
            }
            return new FileInfo(sourceFileInfoFullName + "." + postfix);
        }

        #endregion
    }
}