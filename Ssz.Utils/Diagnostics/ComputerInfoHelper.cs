using Ssz.Utils;
using Ssz.Utils.Addons;
using Ssz.Utils.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Utils.Diagnostics
{
    public static class ComputerInfoHelper
    {
        /// <summary>
        ///     The CPU utilization percentage..
        /// </summary>
        public const string ParamName_CpuUsedPercentage = @"CpuUsedPercentage";

        /// <summary>
        ///     Total Physical Memory
        /// </summary>
        public const string ParamName_TotalMemoryInBytes = @"TotalMemoryInBytes";

        /// <summary>
        ///     The memory utilization.
        /// </summary>
        public const string ParamName_MemoryUsedInBytes = @"MemoryUsedInBytes";

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
        ///     Space Used.
        /// </summary>
        public const string ParamName_Drive_SpaceUsedInBytes = @"Drive_SpaceUsedInBytes";

        /// <summary>
        ///     Drives info.
        /// </summary>
        public const string ParamName_DrivesInfo = @"DrivesInfo";

        public static async Task<CaseInsensitiveDictionary<Any>> GetSystemParamsAsync()
        {
            CaseInsensitiveDictionary<Any> systemParams = new();

            systemParams[AddonBase.ParamName_IsResourceMonitoringAddon] = new Any(true);

            Process currentProcess = Process.GetCurrentProcess();
            ComputerInfo computerInfo = new();

            //ResourceUtilization resourceUtilization = resourceMonitor.GetUtilization(TimeSpan.FromSeconds(1));
            //systemParams[ParamName_CpuUsedPercentage] = new Any((long)resourceUtilization.CpuUsedPercentage);
            //systemParams[ParamName_TotalMemoryInBytes] = new Any((long)(100 * resourceUtilization.MemoryUsedInBytes / resourceUtilization.MemoryUsedPercentage));
            //systemParams[ParamName_MemoryUsedInBytes] = new Any((long)resourceUtilization.MemoryUsedInBytes);

            systemParams[ParamName_CpuUsedPercentage] = new Any((long)await GetCpuUsedPercentageAsync(currentProcess));
            systemParams[ParamName_TotalMemoryInBytes] = new Any((long)computerInfo.TotalPhysicalMemory);
            systemParams[ParamName_MemoryUsedInBytes] = new Any((long)currentProcess.WorkingSet64);
           
            //addonStatusParams[ParamName_OSPlatform] = new Any(computerInfo.OSPlatform);
            //addonStatusParams[ParamName_OSVersion] = new Any(computerInfo.OSVersion);
            //systemParams[ParamName_TotalPhysicalMemory] = new Any(computerInfo.TotalPhysicalMemory);
            //systemParams[ParamName_AvailablePhysicalMemory] = new Any(computerInfo.AvailablePhysicalMemory);
            //systemParams[ParamName_TotalVirtualMemory] = new Any(computerInfo.TotalVirtualMemory);
            //systemParams[ParamName_AvailableVirtualMemory] = new Any(computerInfo.AvailableVirtualMemory);

            Dictionary<string, Any> drivesInfo = new();
            foreach (DriveInfo di in DriveInfo.GetDrives())
            {
                if (di.IsReady == true && di.DriveType == DriveType.Fixed)
                {
                    Dictionary<string, Any> driveInfo = new();

                    driveInfo[ParamName_VolumeLabel] = new Any(di.VolumeLabel);
                    driveInfo[ParamName_DriveFormat] = new Any(di.DriveFormat);
                    driveInfo[ParamName_Drive_TotalSizeInBytes] = new Any(di.TotalSize);
                    driveInfo[ParamName_Drive_SpaceUsedInBytes] = new Any(di.TotalSize - di.AvailableFreeSpace);

                    drivesInfo[di.Name] = new Any(driveInfo);
                }
            }
            systemParams[ParamName_DrivesInfo] = new Any(drivesInfo);

            return systemParams;
        }

        private static async Task<double> GetCpuUsedPercentageAsync(Process process)
        {
            TimeSpan prevCpuTime = process.TotalProcessorTime;
            DateTime prevTime = DateTime.UtcNow;

            await Task.Delay(100);

            TimeSpan curCpuTime = process.TotalProcessorTime;
            DateTime curTime = DateTime.UtcNow;
            
            return 100.0 * (curCpuTime - prevCpuTime).TotalMilliseconds / ((curTime - prevTime).TotalMilliseconds * Environment.ProcessorCount);
        }
    }
}
