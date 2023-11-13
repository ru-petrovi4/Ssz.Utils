using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Ssz.Dcs.CentralServer.Common;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer_ClientWindowsService
{
    public partial class MainBackgroundService
    {
        #region private functions

        private void LaunchControlEngine(string processModelingSessionId, DirectoryInfo processDirectoryInfo, DirectoryInfo binDirectoryInfo, string instanceInfo)
        {
            string exeFileFullName = Path.Combine(binDirectoryInfo.FullName, DataAccessConstants.ControlEngine_ClientApplicationName + @".exe");
            string arguments = "-d \"" + processDirectoryInfo.FullName +
                "\" --CentralServerAddress=" + ConfigurationHelper.GetValue<string>(Configuration, @"CentralServerAddress", @"") +
                " -s \"" + processModelingSessionId + "\"" +
                " --LocalServerPortNumber=" + instanceInfo;

            Logger.LogDebug("Dcs.ControlEngine is starting.. " + exeFileFullName + @" " + arguments);

            var processStartInfo = new ProcessStartInfo(exeFileFullName, arguments)
            {
                WorkingDirectory = binDirectoryInfo.FullName,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            Process.Start(processStartInfo);
        }

        #endregion
    }
}
