using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.ServerBase
{
    public static class Constants
    {
        /// <summary>
        ///     Size in bytes.
        /// </summary>
        public const int MaxReplyObjectSize = 1024 * 1024;

        public const int MaxEventMessagesCount = 1024;

        /// <summary>
        ///     Is addon contains resource monitoring data.
        /// </summary>
        public const string ParamName_IsResourceMonitorAddon = @"IsResourceMonitorAddon";

        /// <summary>
        ///     The CPU utilization percentage..
        /// </summary>
        public const string ParamName_CpuUsedPercentage = @"CpuUsedPercentage";

        /// <summary>
        ///     The memory utilization percentage.
        /// </summary>
        public const string ParamName_MemoryUsedPercentage = @"MemoryUsedPercentage";

        /// <summary>
        ///     The total memory used.
        /// </summary>
        public const string ParamName_MemoryUsedInBytes = @"MemoryUsedInBytes";

        //public const string ParamName_OSPlatform = @"OSPlatform";

        //public const string ParamName_OSVersion = @"OSVersion";

        /// <summary>
        ///     Total Physical Memory.
        /// </summary>
        public const string ParamName_TotalPhysicalMemory = @"TotalPhysicalMemory";

        /// <summary>
        ///     Available Physical Memory.
        /// </summary>
        public const string ParamName_AvailablePhysicalMemory = @"AvailablePhysicalMemory";

        /// <summary>
        ///     Total Virtual Memory.
        /// </summary>
        public const string ParamName_TotalVirtualMemory = @"TotalVirtualMemory";

        /// <summary>
        ///     Available Virtual Memory.
        /// </summary>
        public const string ParamName_AvailableVirtualMemory = @"AvailableVirtualMemory";

        /// <summary>
        ///     Volume Label.
        /// </summary>
        public const string ParamName_VolumeLabel = @"VolumeLabel";

        /// <summary>
        ///     Drive Format.
        /// </summary>
        public const string ParamName_DriveFormat = @"DriveFormat";

        /// <summary>
        ///     Drive Available Free Space.
        /// </summary>
        public const string ParamName_AvailableFreeSpace = @"AvailableFreeSpace";

        /// <summary>
        ///     Drive Total Free Space.
        /// </summary>
        public const string ParamName_TotalFreeSpace = @"TotalFreeSpace";

        /// <summary>
        ///     Drive Total Size.
        /// </summary>
        public const string ParamName_TotalSize = @"TotalSize";

        /// <summary>
        ///     Drives info.
        /// </summary>
        public const string ParamName_DrivesInfo = @"DrivesInfo";
    }
}
