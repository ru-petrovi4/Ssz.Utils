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

        private const uint InstructorLaunchingMaxPercent = 85; // 100% when all DataProviders connected to Engines

        private async Task PrepareAndRunInstructorExeAsync(string textMessage, IDataAccessProvider utilityDataAccessProvider)
        {
            var parts = CsvHelper.ParseCsvLine(",", textMessage);
            if (parts.Length < 4)
            {
                Logger.LogError("Invalid textMessage = " + textMessage);                
                return;
            }

            string jobId = parts[0] ?? "";
            try
            {                
                string processModelingSessionId = parts[1] ?? "";
                string processModelName = parts[2] ?? "";
                string instructorUserName = parts[3] ?? "";
                var binDsFilesStoreDirectoryType = DsFilesStoreDirectoryType.InstructorBin;
                var dataDsFilesStoreDirectoryType = DsFilesStoreDirectoryType.InstructorData;

                var progressInfo = new ProgressInfo(jobId, 0, InstructorLaunchingMaxPercent - 5)
                {
                    ProgressLabelResourceName = Ssz.Dcs.CentralServer.Properties.ResourceStrings.LaunchingInstructorProgressLabel
                };

                var workingDirectoriesOptional = await PrepareWorkingDirectoriesAsync(
                    progressInfo, 
                    processModelName,
                    binDsFilesStoreDirectoryType,                    
                    dataDsFilesStoreDirectoryType,
                    utilityDataAccessProvider);                
                if (workingDirectoriesOptional is null)
                {
                    throw new Exception(@"Invalid textMessage = " + textMessage);
                }
                var workingDirectories = workingDirectoriesOptional.Value;

                RunInstructorExe(processModelingSessionId, workingDirectories.ProcessModelDirectoryInfo, workingDirectories.BinDirectoryInfo, utilityDataAccessProvider);

                await SetJobProgressAsync(jobId, InstructorLaunchingMaxPercent, Ssz.Dcs.CentralServer.Properties.ResourceStrings.LaunchedInstructorProgressLabel, null, StatusCodes.Good, utilityDataAccessProvider);
            }
            catch (RpcException ex)
            {
                Logger.LogError(ex, "Launch Engine Failed.");

                await SetJobProgressAsync(jobId, 100, null, ex.Status.Detail, StatusCodes.BadInvalidState, utilityDataAccessProvider);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Launch Engine Failed.");

                await SetJobProgressAsync(jobId, 100, null, ex.Message, StatusCodes.BadInvalidState, utilityDataAccessProvider);
            }            
        }

        private void RunInstructorExe(string processModelingSessionId, DirectoryInfo processModelDirectoryInfo, DirectoryInfo binDirectoryInfo, IDataAccessProvider utilityDataAccessProvider)
        {
            string binDirectoryFullName = binDirectoryInfo.FullName;
            string arguments = "-d \"" + processModelDirectoryInfo.FullName +
                "\" --CentralServerAddress=" + utilityDataAccessProvider.ServerAddress +
                " -s \"" + processModelingSessionId + "\"";

            Logger.LogDebug("Ssz.Dcs.Instructor.exe is starting.. " + binDirectoryFullName + @" " + arguments);

            var t = utilityDataAccessProvider.PassthroughAsync(@"", PassthroughConstants.ProcessModelingSession_RunInstructorExe,
                   Encoding.UTF8.GetBytes(CsvHelper.FormatForCsv(processModelingSessionId,
                   binDirectoryFullName,
                   arguments)));            
        }

        #endregion
    }
}