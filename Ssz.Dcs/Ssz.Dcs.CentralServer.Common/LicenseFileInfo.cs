using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer.Common
{
    public class LicenseFileInfo
    {
        /// <summary>
        ///     Id сущности LicenseFile
        /// </summary>
        public int LicenseFileId { get; set; }

        public string LicenseOwner { get; set; } = @"";

        public ModuleOrAddonLicense[] ModuleOrAddonLicenses { get; set; } = null!;
    }

    public class ModuleOrAddonLicense
    {
        public Guid ModuleOrAddonGuid { get; set; }

        public string ModuleOrAddonIdentifier { get; set; } = null!;

        public string ModuleOrAddonDesc { get; set; } = null!;

        public DateTime StartTimeUtc { get; set; }

        public DateTime EndTimeUtc { get; set; }

        public uint MaxUsers { get; set; }
    }
}
