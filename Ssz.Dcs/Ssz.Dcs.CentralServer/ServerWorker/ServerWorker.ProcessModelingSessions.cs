using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Ssz.Dcs.CentralServer.Common;
using Ssz.Dcs.CentralServer.Properties;
using Ssz.Dcs.CentralServer.ServerListItems;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.DataAccessGrpc.Client;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Ssz.Xi.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Security.Cryptography;

namespace Ssz.Dcs.CentralServer
{
    public partial class ServerWorker : ServerWorkerBase
    {
        #region public functions        

        public string InitiateProcessModelingSession(
            string clientApplicationName,
            string clientWorkstationName,
            string processModelName, 
            string instructorUserName,
            InstructorAccessFlags instructorAccessFlags,
            string mode)
        {   
            string processModelingSessionId = Guid.NewGuid().ToString();            

            DsFilesStoreDirectory? rootDsFilesStoreDirectory = DsFilesStoreHelper.CreateDsFilesStoreDirectoryObject(FilesStoreDirectoryInfo, @"", 1);

            var dsFileStoreDescriptors = DsFilesStoreHelper.GetDsFilesStoreDescriptors(rootDsFilesStoreDirectory);
            DsFilesStoreDescriptor? dsFilesStoreDescriptor = dsFileStoreDescriptors.FirstOrDefault(i => String.Equals(Path.GetFileName(i.RelativeToDescriptorFileOrDirectoryPath), processModelName, StringComparison.InvariantCultureIgnoreCase));
            if (dsFilesStoreDescriptor is not null)
            {
                var dsFilesStoreItem = DsFilesStoreHelper.FindDsFilesStoreItem(rootDsFilesStoreDirectory, dsFilesStoreDescriptor.RelativeToDescriptorFileOrDirectoryPath);
                if (dsFilesStoreItem is null)
                {
                    dsFilesStoreDescriptor = null;
                }                
            }            
            if (dsFilesStoreDescriptor is null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid processModelName: " + processModelName));
            }

            CaseInsensitiveDictionary<List<string?>> data = CsvHelper.LoadCsvFile(Path.Combine(FilesStoreDirectoryInfo.FullName, dsFilesStoreDescriptor.DescriptorDsFileInfo.Name), true);

            var processModelingSession = new ProcessModelingSession(processModelingSessionId,
                clientApplicationName,
                clientWorkstationName,
                processModelName, dsFilesStoreDescriptor.Title, instructorUserName, instructorAccessFlags, mode)
            {
                ProcessModelingSessionStatus = ProcessModelingSessionConstants.Initiated,  
                ForTimeout_LastDateTimeUtc = DateTime.UtcNow,
            };
            _processModelingSessionsCollection.Add(processModelingSession.ProcessModelingSessionId, processModelingSession);

            try
            {
                using (var dbContext = _dbContextFactory.CreateDbContext())
                {
                    var instructorUser = dbContext.Users.FirstOrDefault(u => u.UserName == instructorUserName);
                    if (instructorUser is not null)
                    {
                        var dbEnity_ProcessModelingSession = new Common.EntityFramework.ProcessModelingSession
                        {
                            InstructorUser = instructorUser,
                            StartDateTimeUtc = DateTime.UtcNow,
                            ProcessModelName = processModelName,
                            Enterprise = data.TryGetValue(@"Enterprise")?.Skip(1)?.FirstOrDefault() ?? @"",
                            Plant = data.TryGetValue(@"Plant")?.Skip(1)?.FirstOrDefault() ?? @"",
                            Unit = processModelingSession.ProcessModelNameToDisplay
                        };
                        dbContext.ProcessModelingSessions.Add(dbEnity_ProcessModelingSession);
                        dbContext.SaveChanges();
                        processModelingSession.DbEnity_ProcessModelingSessionId = dbEnity_ProcessModelingSession.Id;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, @"dbContext error.");
            }

            return processModelingSession.ProcessModelingSessionId;
        }        

        public async void ConcludeProcessModelingSession(string processModelingSessionId)
        {
            ProcessModelingSession? processModelingSession = _processModelingSessionsCollection.TryGetValue(processModelingSessionId);
            if (processModelingSession is null) return;
            _processModelingSessionsCollection.Remove(processModelingSessionId);

            try
            {
                if (processModelingSession.DbEnity_ProcessModelingSessionId is not null)
                {
                    using (var dbContext = _dbContextFactory.CreateDbContext())
                    {
                        var dbEnity_ProcessModelingSession = dbContext.ProcessModelingSessions.FirstOrDefault(pms => pms.Id == processModelingSession.DbEnity_ProcessModelingSessionId.Value);
                        if (dbEnity_ProcessModelingSession is not null)
                        {
                            dbEnity_ProcessModelingSession.FinishDateTimeUtc = DateTime.UtcNow;
                            dbContext.SaveChanges();
                        };
                    }                    
                }                
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, @"dbContext error.");
            }

            var tasks = new List<Task>();
            foreach (EngineSession engineSession in processModelingSession.EngineSessions)
            {
                try
                {
                    tasks.Add(engineSession.DataAccessProvider.PassthroughAsync(@"", PassthroughConstants.Shutdown, new byte[0]));
                }
                catch
                {
                }
            }
            foreach (var t in tasks) 
            {
                try
                {
                    await t;
                }
                catch
                {
                }
            }            

            ThreadSafeDispatcher.BeginInvoke(ct =>
            {
                for (int collectionIndex = processModelingSession.EngineSessions.Count - 1; collectionIndex >= 0; collectionIndex -= 1)
                {
                    var o = processModelingSession.EngineSessions[collectionIndex];
                    processModelingSession.EngineSessions.RemoveAt(collectionIndex);
                    o.Dispose();
                }

                bool utilityItemsProcessingNeeded = false;
                foreach (OperatorSession operatorSession in OperatorSessionsCollection.Values
                    .Where(t => String.Equals(t.ProcessModelingSession?.ProcessModelingSessionId, processModelingSessionId, StringComparison.InvariantCultureIgnoreCase)).ToArray())
                {
                    OperatorSessionsCollection.Remove(operatorSession.OperatorSessionId);
                    utilityItemsProcessingNeeded = true;
                }

                ServerContextsAbort(processModelingSession.ProcessServerContextsCollection.ToArray());

                if (utilityItemsProcessingNeeded)
                    _utilityItemsDoWorkNeeded = true;
            });            
        }

        /// <summary>
        ///     Throws RpcException if incorrect processModelingSessionId
        /// </summary>
        /// <param name="processModelingSessionId"></param>
        /// <returns></returns>
        public ProcessModelingSession GetProcessModelingSession(string? processModelingSessionId)
        {
            ProcessModelingSession? processModelingSession = GetProcessModelingSessionOrNull(processModelingSessionId);            
            
            if (processModelingSession is null)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid processModelingSessionId: " + processModelingSessionId));

            return processModelingSession;
        }

        public ProcessModelingSession? GetProcessModelingSessionOrNull(string? processModelingSessionId)
        {
            ProcessModelingSession? processModelingSession = null;

            if (!String.IsNullOrEmpty(processModelingSessionId))
            {
                if (String.Equals(processModelingSessionId, DataAccessConstants.DefaultProcessModelingSessionId, StringComparison.InvariantCultureIgnoreCase))
                    processModelingSession = _processModelingSessionsCollection.Values.FirstOrDefault();
                else
                    processModelingSession = _processModelingSessionsCollection.TryGetValue(processModelingSessionId);
            }

            return processModelingSession;
        }        

        #endregion

        #region private functions        

        private string ProcessModelingSession_LaunchEngines_LongrunningPassthrough(ServerContext serverContext, byte[] dataToSend)
        {
            string? processModelingSessionId = Encoding.UTF8.GetString(dataToSend);
            ProcessModelingSession processModelingSession = GetProcessModelingSession(processModelingSessionId);
            string jobId;

            if (processModelingSession.LaunchEnginesJobId is null) // Operation not started yet.
            {
                jobId = Guid.NewGuid().ToString();
                processModelingSession.LaunchEnginesJobId = jobId;

                string targetWorkstationName = processModelingSession.InitiatorClientWorkstationName;

                if (processModelingSession.ProcessModelName == @"")
                {
                    // Connect to PlatInstructor only                    
                    var platInstructorEngineSession = new PlatInstructor_TrainingEngineSession(
                        _serviceProvider,
                        ThreadSafeDispatcher,
                        "http://" + targetWorkstationName + ":60080/SimcodePlatServer/ServerDiscovery",
                        @"",
                        new CaseInsensitiveDictionary<string?>());                    
                    processModelingSession.EngineSessions.Add(platInstructorEngineSession);

                    serverContext.AddCallbackMessage(new ServerContext.LongrunningPassthroughCallbackMessage
                    {
                        JobId = jobId,
                        ProgressPercent = 100,
                        ProgressLabel = Resources.ResourceManager.GetString(ResourceStrings.OperationCompleted_ProgressLabel, serverContext.CultureInfo),
                        JobStatusCode = JobStatusCodes.OK
                    });
                }
                else
                {
                    DsFilesStoreDirectory processDsFilesStoreDirectory = DsFilesStoreHelper.CreateDsFilesStoreDirectoryObject(
                        FilesStoreDirectoryInfo,
                        processModelingSession.ProcessModelName, 2);                    

                    // Launch Instructor

                    var jobProgress = new JobProgress(jobId)
                    {
                        ForTimeout_LastDateTimeUtc = DateTime.UtcNow,
                        JobTimeout_ProgressLabel = Resources.ResourceManager.GetString(ResourceStrings.LaunchingInstructorProgressErrorMessage, serverContext.CultureInfo)
                    };
                    jobProgress.ProgressSubscribers.Add(serverContext);
                    _jobProgressesCollection.Add(jobId, jobProgress);

                    //     Creates all directories and subdirectories in the specified path unless they
                    //     already exist.
                    Directory.CreateDirectory(Path.Combine(FilesStoreDirectoryInfo.FullName, 
                        processDsFilesStoreDirectory.PathRelativeToRootDirectory,
                        DsFilesStoreConstants.InstructorDataDirectoryName));

                    if (processModelingSession.InitiatorClientApplicationName != DataAccessConstants.Instructor_ClientApplicationName)
                    {
                        Generate_LaunchInstructor_SystemEvent(targetWorkstationName, processModelingSession);
                    }
                    else
                    {
                        // Only update Instructor.Data directory.
                        Generate_DownloadChangedFiles_SystemEvent(processModelingSession.InitiatorClientWorkstationName, jobId, 
                            Path.Combine(processDsFilesStoreDirectory.PathRelativeToRootDirectory, DsFilesStoreConstants.InstructorDataDirectoryName),
                            true);
                    }

                    // Launch DscEngine 

                    //     Creates all directories and subdirectories in the specified path unless they
                    //     already exist.
                    Directory.CreateDirectory(Path.Combine(FilesStoreDirectoryInfo.FullName,
                        processDsFilesStoreDirectory.PathRelativeToRootDirectory,
                        DsFilesStoreConstants.ControlEngineDataDirectoryName));

                    string engineSessionId = Guid.NewGuid().ToString();

                    int portNumber;
                    var controlEngineSessions = GetEngineSessions().OfType<Control_TrainingEngineSession>().Where(s => String.Equals(s.WorkstationName, targetWorkstationName)).ToArray();
                    if (controlEngineSessions.Length > 0)
                        portNumber = controlEngineSessions.Max(s => s.PortNumber) + 1;
                    else
                        portNumber = 60061;

                    Generate_LaunchEngine_SystemEvent(targetWorkstationName, processModelingSession, DsFilesStoreDirectoryType.ControlEngineBin, DsFilesStoreDirectoryType.ControlEngineData, @"", new Any(portNumber).ValueAsString(false));
                    
                    var controlEngineSession = new Control_TrainingEngineSession(
                        _serviceProvider,
                        ThreadSafeDispatcher,
                        @"http://" + targetWorkstationName + @":" + portNumber,
                        @"PROCESS",
                        new CaseInsensitiveDictionary<string?>())
                    {
                        PortNumber = portNumber
                    };                    
                    processModelingSession.EngineSessions.Add(controlEngineSession);
                    processModelingSession.SubscribeToMainControlEngine((GrpcDataAccessProvider)controlEngineSession.DataAccessProvider);

                    // Launch PlatInstructor

                    var platInstructorEngineDataDsFilesStoreDirectory = DsFilesStoreHelper.FindDataDsFilesStoreDirectory(processDsFilesStoreDirectory, DsFilesStoreDirectoryType.PlatInstructorData);
                    if (platInstructorEngineDataDsFilesStoreDirectory is not null)
                    {
                        DsFilesStoreFile? mvDsFileInfo = platInstructorEngineDataDsFilesStoreDirectory.DsFilesStoreFilesCollection
                            .FirstOrDefault(fi => fi.Name.EndsWith(@".mv_", StringComparison.InvariantCultureIgnoreCase));
                        if (mvDsFileInfo is not null)
                        {
                            var systemNameBase = Path.GetFileNameWithoutExtension(mvDsFileInfo.Name);
                            engineSessionId = Guid.NewGuid().ToString();
                            string systemName = systemNameBase + @"." + engineSessionId;

                            Generate_LaunchEngine_SystemEvent(targetWorkstationName, processModelingSession, DsFilesStoreDirectoryType.PlatInstructorBin, DsFilesStoreDirectoryType.PlatInstructorData, mvDsFileInfo.Name, systemName);                            
                            var platInstructorEngineSession = new PlatInstructor_TrainingEngineSession(
                                _serviceProvider,
                                ThreadSafeDispatcher,
                                @"http://" + targetWorkstationName + @":60080/SimcodePlatServer/ServerDiscovery",
                                systemName,
                                new CaseInsensitiveDictionary<string?> { { @"XiSystem", systemName } });                            
                            processModelingSession.EngineSessions.Add(platInstructorEngineSession);
                        }
                    }
                }                
            }
            else
            {
                jobId = processModelingSession.LaunchEnginesJobId;
                SubscribeForExistingJobProgress(jobId, serverContext);
            }

            ProcessModelingSession_LaunchEngines_LongrunningPassthroughAsync(serverContext,  processModelingSession, jobId);

            return jobId;
        }

        private async void ProcessModelingSession_LaunchEngines_LongrunningPassthroughAsync(ServerContext serverContext, ProcessModelingSession processModelingSession, string jobId)
        {
            var isConnectedEventWaitHandles = new List<EventWaitHandle>();
            foreach (EngineSession engineSession in processModelingSession.EngineSessions)
            {
                isConnectedEventWaitHandles.Add(engineSession.DataAccessProvider.IsConnectedEventWaitHandle);
            }
            await Task.Run(() =>
            {
                WaitHandle.WaitAll(isConnectedEventWaitHandles.ToArray());
            });

            serverContext.AddCallbackMessage(new ServerContext.LongrunningPassthroughCallbackMessage
            {
                JobId = jobId,
                ProgressPercent = 100,
                ProgressLabel = Resources.ResourceManager.GetString(ResourceStrings.OperationCompleted_ProgressLabel, serverContext.CultureInfo),
                JobStatusCode = JobStatusCodes.OK
            });
        }

        private string ProcessModelingSession_DownloadChangedFiles_LongrunningPassthrough(ServerContext serverContext, byte[] dataToSend)
        {
            string?[] csvLine = CsvHelper.ParseCsvLine(@",", Encoding.UTF8.GetString(dataToSend));
            string directoryPathsRelativeToRootDirectory = CsvHelper.FormatForCsv(@",", csvLine.Skip(1));            
            ProcessModelingSession processModelingSession = GetProcessModelingSession(csvLine[0]);

            string jobId = Guid.NewGuid().ToString();
            var jobProgress = new JobProgress(jobId)
            {
                ForTimeout_LastDateTimeUtc = DateTime.UtcNow,
                JobTimeout_ProgressLabel = @"" //Resources.ResourceManager.GetString(ResourceStrings.LaunchingInstructorProgressErrorMessage, serverContext.CultureInfo)
            };
            jobProgress.ProgressSubscribers.Add(serverContext);
            _jobProgressesCollection.Add(jobId, jobProgress);

            Generate_DownloadChangedFiles_SystemEvent(processModelingSession.InitiatorClientWorkstationName, jobId, directoryPathsRelativeToRootDirectory, false);

            return jobId;
        }

        private string ProcessModelingSession_UploadChangedFiles_LongrunningPassthrough(ServerContext serverContext, byte[] dataToSend)
        {
            string?[] csvLine = CsvHelper.ParseCsvLine(@",", Encoding.UTF8.GetString(dataToSend));
            string directoryPathsRelativeToRootDirectory = CsvHelper.FormatForCsv(@",", csvLine.Skip(1));
            ProcessModelingSession processModelingSession = GetProcessModelingSession(csvLine[0]);

            string jobId = Guid.NewGuid().ToString();
            var jobProgress = new JobProgress(jobId)
            {
                ForTimeout_LastDateTimeUtc = DateTime.UtcNow,
                JobTimeout_ProgressLabel = @"" //Resources.ResourceManager.GetString(ResourceStrings.LaunchingInstructorProgressErrorMessage, serverContext.CultureInfo)
            };
            jobProgress.ProgressSubscribers.Add(serverContext);
            _jobProgressesCollection.Add(jobId, jobProgress);

            Generate_UploadChangedFiles_SystemEvent(processModelingSession.InitiatorClientWorkstationName, jobId, directoryPathsRelativeToRootDirectory);

            return jobId;
        }        

        #endregion

        public class ProcessModelingSession
        {
            #region construction and destruction

            public ProcessModelingSession(string processModelingSessionId,
                string initiatorClientApplicationName,
                string initiatorClientWorkstationName,
                string processModelName, 
                string processModelNameToDisplay, 
                string instructorUserName,
                InstructorAccessFlags instructorAccessFlags,
                string mode)
            {
                ProcessModelingSessionId = processModelingSessionId;                
                InitiatorClientApplicationName = initiatorClientApplicationName;
                InitiatorClientWorkstationName = initiatorClientWorkstationName;
                ProcessModelName = processModelName;
                ProcessModelNameToDisplay = processModelNameToDisplay;
                InstructorUserName = instructorUserName;
                InstructorAccessFlags = instructorAccessFlags;
                Mode = mode;
            }

            #endregion

            #region public functions

            public string ProcessModelingSessionId { get; }

            public string InitiatorClientApplicationName { get; }

            public string InitiatorClientWorkstationName { get; }

            public string ProcessModelName { get; }

            public string ProcessModelNameToDisplay { get; }

            public string InstructorUserName { get; }

            public InstructorAccessFlags InstructorAccessFlags { get; }

            public string Mode { get; }

            public long? DbEnity_ProcessModelingSessionId { get; set; }

            public ObservableCollection<EngineSession> EngineSessions { get; } = new();

            /// <summary>
            ///     See ProcessModelingSessionConstants
            /// </summary>
            public int ProcessModelingSessionStatus { get; set; }

            public string? LaunchEnginesJobId { get; set; }

            public List<ServerContext> ProcessServerContextsCollection { get; } = new();

            /// <summary>
            ///     Time for inactivity time-out check.
            /// </summary>
            public DateTime? ForTimeout_LastDateTimeUtc { get; set; }

            public UInt64 ProcessTimeSeconds { get; private set; }            

            /// <summary>
            ///    Must be called once.
            /// </summary>
            /// <param name="dataAccessProvider"></param>
            public void SubscribeToMainControlEngine(GrpcDataAccessProvider dataAccessProvider)
            {
                _processTimeSecondsSubscription = new ValueSubscription(dataAccessProvider, "SYSTEM.MODEL_TIME",
                        (sender, args) =>
                        {
                            if (ValueStatusCodes.IsGood(args.NewValueStatusTimestamp.ValueStatusCode))
                            {
                                ProcessTimeSeconds = args.NewValueStatusTimestamp.Value.ValueAsUInt64(false);
                            }
                            else
                            {
                                ProcessTimeSeconds = 0;
                            }
                        });
            }

            #endregion         

            #region private fields

            public ValueSubscription? _processTimeSecondsSubscription;

            #endregion
        }
    }
}


//EngineSession? engineSession = _engineSessionsCollection.TryGetValue(systemNameToConnect);
//if (engineSession is not null)
//{
//    result.Add(engineSession.DataAccessProvider);
//    return result;
//}

///// <summary>
/////     1 / 10 of Seconds
///// </summary>
//public UInt64 ProcessModelingSessionModelTime { get; set; }
