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
        ///     Total Physical Memory
        /// </summary>
        public const string ParamName_TotalMemoryInBytes = @"TotalMemoryInBytes";

        /// <summary>
        ///     The memory utilization percentage.
        /// </summary>
        public const string ParamName_MemoryUsedPercentage = @"MemoryUsedPercentage";

        /// <summary>
        ///     Volume Label.
        /// </summary>
        public const string ParamName_VolumeLabel = @"VolumeLabel";

        /// <summary>
        ///     Drive Format.
        /// </summary>
        public const string ParamName_DriveFormat = @"DriveFormat";

        /// <summary>
        ///     Drive Total Size.
        /// </summary>
        public const string ParamName_Drive_TotalSizeInBytes = @"Drive_TotalSizeInBytes";

        /// <summary>
        ///     Space Used Percentage
        /// </summary>
        public const string ParamName_Drive_SpaceUsedPercentage = @"Drive_SpaceUsedPercentage";

        /// <summary>
        ///     Drives info.
        /// </summary>
        public const string ParamName_DrivesInfo = @"DrivesInfo";
    }
}
