using Grpc.Core;
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

        private const uint InstructorLaunchingMaxPercent = 85; // 100% when all DataProviders connected to Engines

        private async Task LaunchInstructorAsync(string textMessage)
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

                var workingDirectoriesOptional = await PrepareWorkingDirectoriesAsync(progressInfo, processModelName,
                    binDsFilesStoreDirectoryType, dataDsFilesStoreDirectoryType);                
                if (workingDirectoriesOptional is null)
                {
                    throw new Exception(@"Invalid textMessage = " + textMessage);
                }
                var workingDirectories = workingDirectoriesOptional.Value;

                LaunchInstructor(processModelingSessionId, workingDirectories.ProcessDirectoryInfo, workingDirectories.BinDirectoryInfo);

                await SetJobProgressAsync(jobId, InstructorLaunchingMaxPercent, Ssz.Dcs.CentralServer.Properties.ResourceStrings.LaunchedInstructorProgressLabel, null, JobStatusCodes.OK);
            }
            catch (RpcException ex)
            {
                Logger.LogError(ex, "Launch Engine Failed.");

                await SetJobProgressAsync(jobId, 100, null, ex.Status.Detail, JobStatusCodes.Aborted);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Launch Engine Failed.");

                await SetJobProgressAsync(jobId, 100, null, ex.Message, JobStatusCodes.Aborted);
            }            
        }

        private void LaunchInstructor(string processModelingSessionId, DirectoryInfo processDirectoryInfo, DirectoryInfo binDirectoryInfo)
        {
            string binDirectoryFullName = binDirectoryInfo.FullName;
            string arguments = "-d \"" + processDirectoryInfo.FullName +
                "\" --CentralServerAddress=" + ConfigurationHelper.GetValue<string>(Configuration, @"CentralServerAddress", @"") +
                " -s \"" + processModelingSessionId + "\"";

            Logger.LogDebug("Ssz.Dcs.Instructor.exe is starting.. " + binDirectoryFullName + @" " + arguments);

            var t = UtilityDataAccessProvider.PassthroughAsync(@"", PassthroughConstants.ProcessModelingSession_RunInstructorExe,
                   Encoding.UTF8.GetBytes(CsvHelper.FormatForCsv(processModelingSessionId,
                   binDirectoryFullName,
                   arguments)));            
        }

        #endregion
    }
}