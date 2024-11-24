using Microsoft.Extensions.Diagnostics.ResourceMonitoring;
using Ssz.Utils;
using Ssz.Utils.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.ServerBase
{
    public static class SystemInfoHelper
    {
        public static void GetSystemParams(IResourceMonitor resourceMonitor, CaseInsensitiveDictionary<Any> systemParams)
        {
            systemParams[Constants.ParamName_IsResourceMonitorAddon] = new Any(true);

            ResourceUtilization resourceUtilization = resourceMonitor.GetUtilization(TimeSpan.FromSeconds(1));
            systemParams[Constants.ParamName_CpuUsedPercentage] = new Any((long)resourceUtilization.CpuUsedPercentage);
            systemParams[Constants.ParamName_TotalMemoryInBytes] = new Any((long)(100 * resourceUtilization.MemoryUsedInBytes / resourceUtilization.MemoryUsedPercentage));
            systemParams[Constants.ParamName_MemoryUsedInBytes] = new Any((long)resourceUtilization.MemoryUsedInBytes);

            //ComputerInfo computerInfo = new();
            //addonStatusParams[Constants.ParamName_OSPlatform] = new Any(computerInfo.OSPlatform);
            //addonStatusParams[Constants.ParamName_OSVersion] = new Any(computerInfo.OSVersion);
            //systemParams[Constants.ParamName_TotalPhysicalMemory] = new Any(computerInfo.TotalPhysicalMemory);
            //systemParams[Constants.ParamName_AvailablePhysicalMemory] = new Any(computerInfo.AvailablePhysicalMemory);
            //systemParams[Constants.ParamName_TotalVirtualMemory] = new Any(computerInfo.TotalVirtualMemory);
            //systemParams[Constants.ParamName_AvailableVirtualMemory] = new Any(computerInfo.AvailableVirtualMemory);

            Dictionary<string, Any> drivesInfo = new();
            foreach (DriveInfo di in DriveInfo.GetDrives())
            {
                if (di.IsReady == true && di.DriveType == DriveType.Fixed)
                {
                    Dictionary<string, Any> driveInfo = new();

                    driveInfo[Constants.ParamName_VolumeLabel] = new Any(di.VolumeLabel);
                    driveInfo[Constants.ParamName_DriveFormat] = new Any(di.DriveFormat);
                    driveInfo[Constants.ParamName_Drive_TotalSizeInBytes] = new Any(di.TotalSize);
                    driveInfo[Constants.ParamName_Drive_SpaceUsedInBytes] = new Any(di.TotalSize - di.AvailableFreeSpace);

                    drivesInfo[di.Name] = new Any(driveInfo);
                }
            }
            systemParams[Constants.ParamName_DrivesInfo] = new Any(drivesInfo);
        }
    }
}
