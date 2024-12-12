using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Operator.Design.License
{
    internal static class LicenseHelper
    {
        #region public functions

        public static byte[] GetHostIdFileBytes()
        {
            var hostIdFileBytes = System.Text.Encoding.UTF8.GetBytes(GetHostId());
            Crypt(hostIdFileBytes);
            return hostIdFileBytes;
        }

        public static List<Guid> GetAvailableGuids()
        {
            var result = new List<Guid>();
            var directoryInfo = new DirectoryInfo(Environment.ExpandEnvironmentVariables(@"%homedrive%%homepath%"));
            foreach (var fi in directoryInfo.EnumerateFiles("*.dsLic"))
            {
                String hostId = GetHostId();
                byte[] licFileBytes = File.ReadAllBytes(fi.FullName);
                result.AddRange(GetAvailableGuidsInternal(hostId, licFileBytes));
            }            
            return result;
        }

        #endregion

        #region private functions

        private static string GetHostId()
        {
            var nameValueCollection = new CaseInsensitiveDictionary<string?>();

            // processor
            try
            {
                var searcher = new ManagementObjectSearcher("root\\CIMV2",
                   "SELECT * FROM Win32_Processor");
                foreach (ManagementObject queryObj in searcher.Get())
                    nameValueCollection.Add("ProcessorId", queryObj["ProcessorId"].ToString());
            }
            catch
            {
            }

            // motherboard
            try
            {
                var searcher = new ManagementObjectSearcher("root\\CIMV2",
                   "SELECT * FROM CIM_Card");
                foreach (ManagementObject queryObj in searcher.Get())
                    nameValueCollection.Add("CardID", queryObj["SerialNumber"].ToString());
            }
            catch
            {
            }

            //UUID
            try
            {
                var searcher = new ManagementObjectSearcher("root\\CIMV2",
                   "SELECT UUID FROM Win32_ComputerSystemProduct");
                foreach (ManagementObject queryObj in searcher.Get())
                    nameValueCollection.Add("UUID", queryObj["UUID"].ToString());
            }
            catch
            {
            }

            return NameValueCollectionHelper.GetNameValueCollectionString(nameValueCollection);
        }

        private static List<Guid> GetAvailableGuidsInternal(String hostId, byte[] licFileBytes)
        {
            var result = new List<Guid>();
            Crypt(licFileBytes);
            byte[] hash;
            var sha512 = System.Security.Cryptography.SHA512.Create();
            hash = sha512.ComputeHash(new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(hostId)));

            bool equals = false;
            for (int indexInLicFile = 0; indexInLicFile <= licFileBytes.Length - hash.Length; indexInLicFile += 1)
            {
                equals = true;
                for (int indexInHash = 0; indexInHash < hash.Length; indexInHash += 1)
                {
                    if (licFileBytes[indexInLicFile + indexInHash] != hash[indexInHash])
                    {
                        equals = false;
                        break;
                    }
                }
                if (equals)
                {
                    List<LicenseInfo> licenseInfos;
                    using (var stream = new System.IO.MemoryStream(licFileBytes, indexInLicFile + hash.Length, licFileBytes.Length - indexInLicFile - hash.Length))
                    using (var reader = new Ssz.Utils.Serialization.SerializationReader(stream))
                    {
                        using (var block = reader.EnterBlock())
                        {
                            switch (block.Version)
                            {
                                case 1:
                                    licenseInfos = reader.ReadListOfOwnedDataSerializable(() => new LicenseInfo(), 1);
                                    break;
                                default:
                                    throw new BlockUnsupportedVersionException();
                            }
                        }
                    }
                    var utcNow = DateTime.UtcNow;
                    result = licenseInfos.Where(li => utcNow <= li.ValidUpTo).Select(li => li.Guid).ToList();
                    break;
                }
            }
            return result;
        }        

        private static void Crypt(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(bytes[i] ^ 123);
            }
        }

        #endregion
    }
}

/*
if (licFileBytes.Length - indexInLicFile - hash.Length < 8)
{
    equals = false;
}
else
{
    DateTime dt = DateTime.FromBinary(BitConverter.ToInt64(new ReadOnlySpan<Byte>(licFileBytes, indexInLicFile + hash.Length, 8)));
    equals = DateTime.UtcNow < dt;
}
*/
