using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Ssz.Dcs.CentralServer.Common;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer_ClientWindowsService
{
    public partial class Worker
    {
        #region private functions

        private async Task PrepareAndRunOperatorExeAsync(string textMessage, IDataAccessProvider utilityDataAccessProvider)
        {
            var parts = CsvHelper.ParseCsvLine(",", textMessage);
            if (parts.Length < 5)
            {
                Logger.LogError("Invalid textMessage = " + textMessage);
                return;
            }

            string jobId = parts[0] ?? "";
            try
            {  
                string operatorSessionId = parts[1] ?? "";
                string processModelName = parts[2] ?? "";
                string dsProjectPathRelativeToDataDirectory = parts[3] ?? "";                
                string operatorSessionDescription = parts[4] ?? "";                                               

                var progressInfo = new ProgressInfo(jobId, 0, 95)
                {
                    ProgressLabelResourceName = Ssz.Dcs.CentralServer.Properties.ResourceStrings.LaunchingOperatorProgressLabel
                };

                var workingDirectoriesOptional = await PrepareWorkingDirectoriesAsync(progressInfo, processModelName,
                    DsFilesStoreDirectoryType.OperatorBin, DsFilesStoreDirectoryType.OperatorData, utilityDataAccessProvider, dsProjectPathRelativeToDataDirectory);
                if (workingDirectoriesOptional is null)
                {
                    throw new NotifyProgressException(@"Invalid textMessage = " + textMessage)
                    {
                        PprogressLabelResourceName = Ssz.Dcs.CentralServer.Properties.ResourceStrings.OperatorDirectoryErrorProgressLabel
                    };
                }

                var t = await utilityDataAccessProvider.LongrunningPassthroughAsync(@"", LongrunningPassthroughConstants.ProcessModelingSession_RunOperatorExe,
                    Encoding.UTF8.GetBytes(CsvHelper.FormatForCsv(operatorSessionId, 
                    workingDirectoriesOptional.Value.BinDirectoryInfo.FullName, 
                    workingDirectoriesOptional.Value.DataDirectoryInfo.FullName,
                    utilityDataAccessProvider.ServerAddress)), null);
                      
            }
            catch (NotifyProgressException ex)
            {
                Logger.LogError(ex, "LaunchOperator Failed.");

                await SetJobProgressAsync(jobId, 100, ex.PprogressLabelResourceName, ex.ProgressDetails, StatusCodes.BadInvalidState, utilityDataAccessProvider);
            }
            catch (RpcException ex)
            {
                Logger.LogError(ex, "LaunchOperator Failed.");

                await SetJobProgressAsync(jobId, 100, Ssz.Dcs.CentralServer.Properties.ResourceStrings.LaunchedOperatorProgressLabel, ex.Status.Detail, StatusCodes.BadInvalidState, utilityDataAccessProvider);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "LaunchOperator Failed.");

                await SetJobProgressAsync(jobId, 100, Ssz.Dcs.CentralServer.Properties.ResourceStrings.LaunchedOperatorProgressLabel, ex.Message, StatusCodes.BadInvalidState, utilityDataAccessProvider);
            }            
        }

        #endregion
    }
}


//string commandLine;
//if (runLauncherExe)
//{
//    string launcherExeDirectoryFullName = Path.Combine(AppContext.BaseDirectory, @"..\DeltaSim.Launcher");
//    string launcherExeFileFullName = Path.Combine(launcherExeDirectoryFullName, DataAccessConstants.Launcher_ClientApplicationName + @".exe");
//    var launcherExeFileInfo = new FileInfo(launcherExeFileFullName);

//    if (launcherExeFileInfo.Exists)
//    {
//        commandLine = "-m Operator --OperatorSessionId=" + operatorSessionId;
//        if (operatorSessionDescription != @"")
//            commandLine += " -md \"" + operatorSessionDescription + "\"";
//        Logger.LogDebug("DeltaSim.Launcher is starting.. " + launcherExeFileInfo.FullName + commandLine);
//        ProcessHelper.StartProcessAsCurrentUser(launcherExeFileInfo.FullName, @" " + commandLine, launcherExeDirectoryFullName, true);
//    }
//}

/*
                if (!string.IsNullOrEmpty(localModel.User))
                {
                    procInfo.UserName = localModel.User;
                    procInfo.Password = new SecureString();
                    foreach (char c in localModel.Password)
                        procInfo.Password.AppendChar(c);
                }*/