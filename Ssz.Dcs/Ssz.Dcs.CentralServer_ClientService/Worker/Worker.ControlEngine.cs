using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Ssz.Dcs.CentralServer.Common;
using Ssz.Dcs.CentralServer.Common.EntityFramework;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer_ClientService
{
    public partial class Worker
    {
        #region private functions

        private void RunControlEngineExe(string processModelingSessionId, DirectoryInfo processModelDirectoryInfo, DirectoryInfo binDirectoryInfo, string controlEngineServerAddress, IDataAccessProvider utilityDataAccessProvider, string instanceInfo)
        {
            string exeFileFullName;
            string arguments;
            if (OperatingSystem.IsWindows())
            {
                exeFileFullName = Path.Combine(binDirectoryInfo.FullName, @"Ssz.Dcs.ControlEngine.exe");
                arguments = "-d \"" + processModelDirectoryInfo.FullName +
                    "\" --CentralServerAddress=" + utilityDataAccessProvider.ServerAddress +
                    " --CentralServerSystemName=\"" + processModelingSessionId + "\"" +
                    " --ControlEngineServerAddress=" + controlEngineServerAddress +
                    " --EngineSessionId=" + instanceInfo;
            }
            else
            {
                exeFileFullName = @"/usr/bin/dotnet";
                arguments = Path.Combine(binDirectoryInfo.FullName, @"Ssz.Dcs.ControlEngine.dll") + " -d \"" + processModelDirectoryInfo.FullName +
                    "\" --CentralServerAddress=" + utilityDataAccessProvider.ServerAddress +
                    " --CentralServerSystemName=\"" + processModelingSessionId + "\"" +
                    " --ControlEngineServerAddress=" + controlEngineServerAddress +
                    " --EngineSessionId=" + instanceInfo;
            }            

            Logger.LogDebug("Dcs.ControlEngine is starting.. " + exeFileFullName + @" " + arguments);

            var processStartInfo = new ProcessStartInfo(exeFileFullName, arguments)
            {
                WorkingDirectory = binDirectoryInfo.FullName,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            var controlEngineProcess = Process.Start(processStartInfo);

            if (controlEngineProcess is not null) 
            {
                Task.Run(() =>
                {
                    _threadSafeDispatcher.BeginInvokeEx(async ct =>
                    {
                        _runningControlEngineServerAddresses.Add(controlEngineServerAddress);
                        await UtilityDataAccessProviderHolders_UpdateContextParams();
                    });

                    controlEngineProcess.WaitForExit();

                    _threadSafeDispatcher.BeginInvokeEx(async ct =>
                    {
                        _runningControlEngineServerAddresses.Remove(controlEngineServerAddress);
                        await UtilityDataAccessProviderHolders_UpdateContextParams();
                    });
                });
            }            
        }

        #endregion
    }
}
