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
using Ssz.Utils.Addons;

namespace Ssz.Dcs.CentralServer
{
    public partial class ServerWorker : DataAccessServerWorkerBase
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
            DsFilesStoreDirectory? rootDsFilesStoreDirectory = DsFilesStoreHelper.CreateDsFilesStoreDirectoryObject(FilesStoreDirectoryInfo, @"", 1);

            var dsFileStoreDescriptors = DsFilesStoreHelper.GetDsFilesStoreDescriptors(rootDsFilesStoreDirectory);
            DsFilesStoreDescriptor? dsFilesStoreDescriptor = dsFileStoreDescriptors.FirstOrDefault(i => String.Equals(Path.GetFileName(i.RelativeToDescriptorFileOrDirectoryPath), processModelName, StringComparison.InvariantCultureIgnoreCase));
            DsFilesStoreItem? dsFilesStoreItem = null;
            if (dsFilesStoreDescriptor is not null)
            {
                dsFilesStoreItem = DsFilesStoreHelper.FindDsFilesStoreItem(rootDsFilesStoreDirectory, dsFilesStoreDescriptor.RelativeToDescriptorFileOrDirectoryPath);
                if (dsFilesStoreItem is null)
                    dsFilesStoreDescriptor = null;
            }            
            if (dsFilesStoreDescriptor is null)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid processModelName: " + processModelName));            

            CaseInsensitiveDictionary<List<string?>> data = CsvHelper.LoadCsvFile(Path.Combine(FilesStoreDirectoryInfo.FullName, dsFilesStoreDescriptor.DescriptorDsFileInfo.Name), true);

            var processModelingSession = new ProcessModelingSession(
                Guid.NewGuid().ToString(),
                clientApplicationName,
                clientWorkstationName,
                processModelName, 
                dsFilesStoreDescriptor.Title, 
                instructorUserName, 
                instructorAccessFlags, 
                mode)
            {
                ProcessModelingSessionStatus = ProcessModelingSessionConstants.Initiated,  
                ForTimeout_LastDateTimeUtc = DateTime.UtcNow,
            };
            _processModelingSessionsCollection.Add(processModelingSession.ProcessModelingSessionId, processModelingSession);

            try
            {
                using (var dbContext = _dbContextFactory.CreateDbContext())
                {
                    if (dbContext.IsConfigured)
                    {
                        string enterprise = CsvDb.GetValue(data, "Enterprise", 1) ?? @"";
                        string plant = CsvDb.GetValue(data, "Plant", 1) ?? @"";
                        string unit = processModelingSession.ProcessModelNameToDisplay;

                        var processModel = dbContext.ProcessModels.AsEnumerable().FirstOrDefault(pm => String.Equals(pm.ProcessModelName, processModelName, StringComparison.InvariantCultureIgnoreCase));
                        if (processModel is null)
                        {
                            processModel = new Common.EntityFramework.ProcessModel();
                            dbContext.ProcessModels.Add(processModel);
                        }
                        processModel.ProcessModelName = processModelName; // For case-sensivity issues
                        processModel.Enterprise = enterprise;
                        processModel.Plant = plant;
                        processModel.Unit = unit;

                        var originalScenarios = dbContext.Scenarios.Where(s => s.ProcessModel == processModel).ToList();
                        foreach (string fullFileName in Directory.EnumerateFiles(Path.Combine(FilesStoreDirectoryInfo.FullName, dsFilesStoreItem!.DsFilesStoreDirectory.Name, DsFilesStoreConstants.InstructorDataDirectoryName))
                            .Where(ffn => ffn.EndsWith(@".scenario.csv", StringComparison.InvariantCultureIgnoreCase)))
                        {
                            CaseInsensitiveDictionary<List<string?>> scenarioData = CsvHelper.LoadCsvFile(fullFileName, true);
                            string fileName = Path.GetFileName(fullFileName);
                            string scenarioName = fileName.Substring(0, fileName.Length - @".scenario.csv".Length);
                            var scenario = originalScenarios.FirstOrDefault(s => String.Equals(s.ScenarioName, scenarioName, StringComparison.InvariantCultureIgnoreCase));
                            if (scenario is null)
                            {
                                scenario = new Common.EntityFramework.Scenario()
                                {
                                    ProcessModel = processModel
                                };
                                dbContext.Scenarios.Add(scenario);
                            }
                            else
                            {
                                originalScenarios.Remove(scenario);
                            }
                            scenario.ScenarioName = scenarioName; // For case-sensivity issues
                            scenario.InitialConditionName = CsvDb.GetValue(scenarioData, "%(State)", 1) ?? @"";
                            scenario.MaxPenalty = new Any(CsvDb.GetValue(scenarioData, "%(MaxPenalty)", 1) ?? @"").ValueAsInt32(false);
                            scenario.ScenarioMaxProcessModelTimeSeconds = new Any(CsvDb.GetValue(scenarioData, "%(ScenarioMaxTimeSeconds)", 1) ?? @"").ValueAsUInt64(false);
                        }
                        foreach (var originalScenario in originalScenarios)
                        {
                            dbContext.Remove(originalScenario);
                        }

                        Common.EntityFramework.ProcessModelingSession? dbEnity_ProcessModelingSession = null;
                        var instructorUser = dbContext.Users.FirstOrDefault(u => u.UserName == instructorUserName);
                        if (instructorUser is not null)
                        {
                            dbEnity_ProcessModelingSession = new Common.EntityFramework.ProcessModelingSession
                            {
                                InstructorUser = instructorUser,
                                StartDateTimeUtc = DateTime.UtcNow,
                                ProcessModel = processModel,                                
                            };
                            dbContext.ProcessModelingSessions.Add(dbEnity_ProcessModelingSession);
                        }

                        dbContext.SaveChanges();

                        if (dbEnity_ProcessModelingSession is not null)
                            processModelingSession.DbEnity_ProcessModelingSessionId = dbEnity_ProcessModelingSession.Id;
                    }                    
                }                
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, @"dbContext error.");
            }
            Logger.LogInformation($"Process modeling session initiated. Instructor workstation: {clientWorkstationName}");

            return processModelingSession.ProcessModelingSessionId;
        }        

        public async void ConcludeProcessModelingSession(string processModelingSessionId)
        {
            _processModelingSessionsCollection.Remove(processModelingSessionId, out ProcessModelingSession? processModelingSession);
            if (processModelingSession is null) 
                return;            

            try
            {
                if (processModelingSession.DbEnity_ProcessModelingSessionId is not null)
                {
                    using (var dbContext = _dbContextFactory.CreateDbContext())
                    {
                        if (dbContext.IsConfigured)
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
                    var engineSession = processModelingSession.EngineSessions[collectionIndex];
                    processModelingSession.EngineSessions.RemoveAt(collectionIndex);
                    engineSession.DataAccessProviderGetter_Addon.Close();
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
                {
                    processModelingSession = _processModelingSessionsCollection.Values.FirstOrDefault();
                }
                else
                {
                    processModelingSession = _processModelingSessionsCollection.TryGetValue(processModelingSessionId);

                    if (processModelingSession is null && processModelingSessionId.StartsWith(DataAccessConstants.PlatformXiProcessModelingSessionId, StringComparison.InvariantCultureIgnoreCase))
                    {
                        string systemName = processModelingSessionId.Substring(DataAccessConstants.PlatformXiProcessModelingSessionId.Length);

                        processModelingSession = new ProcessModelingSession(
                            processModelingSessionId,
                            @"",
                            @"",
                            @"",
                            @"",
                            @"",
                            0,
                            @"")
                        {
                            ProcessModelingSessionStatus = ProcessModelingSessionConstants.Initiated                            
                        };

                        DataAccessProviderGetter_AddonBase dataAccessProviderGetter_Addon = GetNewInitializedDataAccessProviderAddon(
                            _serviceProvider,
                            $"http://localhost:60080/PlatServer/ServerDiscovery",
                            systemName,
                            new CaseInsensitiveDictionary<string?> { { @"XiSystem", systemName } },
                            ThreadSafeDispatcher);
                        var platInstructorEngineSession = new EngineSession(Guid.NewGuid().ToString(), dataAccessProviderGetter_Addon);
                        processModelingSession.EngineSessions.Add(platInstructorEngineSession);

                        _processModelingSessionsCollection.Add(processModelingSession.ProcessModelingSessionId, processModelingSession);
                    }
                    else if (processModelingSession is null && processModelingSessionId.StartsWith(DataAccessConstants.UsoXiProcessModelingSessionId, StringComparison.InvariantCultureIgnoreCase))
                    {
                        string systemName = processModelingSessionId.Substring(DataAccessConstants.UsoXiProcessModelingSessionId.Length);

                        processModelingSession = new ProcessModelingSession(
                            processModelingSessionId,
                            @"",
                            @"",
                            @"",
                            @"",
                            @"",
                            0,
                            @"")
                        {
                            ProcessModelingSessionStatus = ProcessModelingSessionConstants.Initiated,                            
                        };

                        DataAccessProviderGetter_AddonBase dataAccessProviderGetter_Addon = GetNewInitializedDataAccessProviderAddon(
                            _serviceProvider,
                            $"http://localhost:60080/HoneywellUsoXiServer/ServerDiscovery",
                            systemName,
                            new CaseInsensitiveDictionary<string?> { { @"XiSystem", systemName } },
                            ThreadSafeDispatcher);
                        var platInstructorEngineSession = new EngineSession(Guid.NewGuid().ToString(), dataAccessProviderGetter_Addon);
                        processModelingSession.EngineSessions.Add(platInstructorEngineSession);

                        _processModelingSessionsCollection.Add(processModelingSession.ProcessModelingSessionId, processModelingSession);
                    }
                }                                
            }

            return processModelingSession;
        }        

        #endregion

        #region private functions        

        private string ProcessModelingSession_PrepareAndRunInstructorAndEngines_LongrunningPassthrough(ServerContext serverContext, ReadOnlyMemory<byte> dataToSend)
        {
            string? processModelingSessionId = Encoding.UTF8.GetString(dataToSend.Span);
            ProcessModelingSession processModelingSession = GetProcessModelingSession(processModelingSessionId);
            string jobId;            

            if (processModelingSession.LaunchEnginesJobId is null) // Operation not started yet.
            {
                jobId = Guid.NewGuid().ToString();
                processModelingSession.LaunchEnginesJobId = jobId;                

                DsFilesStoreDirectory processModelDsFilesStoreDirectory = DsFilesStoreHelper.CreateDsFilesStoreDirectoryObject(
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
                    processModelDsFilesStoreDirectory.PathRelativeToRootDirectory,
                    DsFilesStoreConstants.InstructorDataDirectoryName));

                if (processModelingSession.InitiatorClientApplicationName != DataAccessConstants.Instructor_ClientApplicationName) // Production
                {
                    Generate_PrepareAndRunInstructorExe_SystemEvent(processModelingSession.InitiatorClientWorkstationName, processModelingSession);
                }
                else // Debug only
                {
                    // Only update Instructor.Data directory.
                    Generate_DownloadChangedFiles_SystemEvent(processModelingSession.InitiatorClientWorkstationName, jobId,
                        processModelDsFilesStoreDirectory.InvariantPathRelativeToRootDirectory + "/" + DsFilesStoreConstants.InstructorDataDirectoryName,
                        true);
                }

                var enginesHostInfosCollection = _enginesHostInfosCollection.Values.Where(ehi =>                
                    ehi.ProcessModelNames.Contains(@"*", StringComparer.InvariantCultureIgnoreCase) ||
                    ehi.ProcessModelNames.Contains(processModelingSession.ProcessModelName, StringComparer.InvariantCultureIgnoreCase)
                );
                processModelingSession.EnginesHostInfo = enginesHostInfosCollection
                    .FirstOrDefault(ehi => String.Equals(ehi.WorkstationName, processModelingSession.InitiatorClientWorkstationName, StringComparison.InvariantCultureIgnoreCase));
                if (processModelingSession.EnginesHostInfo is null)
                {
                    int minEnginesCount = Int32.MaxValue;
                    foreach (var ehi in enginesHostInfosCollection)
                    {
                        if (ehi.EnginesCount < minEnginesCount)
                        {
                            processModelingSession.EnginesHostInfo = ehi;
                            minEnginesCount = ehi.EnginesCount;
                        }
                    }
                }
                if (processModelingSession.EnginesHostInfo is null)
                    processModelingSession.EnginesHostInfo = _localEnginesHostInfo;

                // Launch DscEngine 

                //     Creates all directories and subdirectories in the specified path unless they
                //     already exist.
                Directory.CreateDirectory(Path.Combine(FilesStoreDirectoryInfo.FullName,
                    processModelDsFilesStoreDirectory.PathRelativeToRootDirectory,
                    DsFilesStoreConstants.ControlEngineDataDirectoryName));

                string engineSessionId = Guid.NewGuid().ToString();

                Generate_LaunchEngine_SystemEvent(processModelingSession.EnginesHostInfo.WorkstationName, processModelingSession, DsFilesStoreDirectoryType.ControlEngineBin, DsFilesStoreDirectoryType.ControlEngineData, @"", engineSessionId);

                DataAccessProviderGetter_AddonBase dataAccessProviderGetter_Addon = GetNewInitializedDataAccessProviderAddon(
                        _serviceProvider,
                        @"",
                        @"PROCESS",
                        new CaseInsensitiveDictionary<string?>(),
                        ThreadSafeDispatcher);
                var controlEngineSession = new ControlEngine_TrainingEngineSession(engineSessionId, dataAccessProviderGetter_Addon);
                processModelingSession.EngineSessions.Add(controlEngineSession);
                processModelingSession.SubscribeToMainControlEngine((GrpcDataAccessProvider)controlEngineSession.DataAccessProvider);

                // Launch PlatInstructor

                var platInstructorEngineDataDsFilesStoreDirectory = DsFilesStoreHelper.FindDataDsFilesStoreDirectory(processModelDsFilesStoreDirectory, DsFilesStoreDirectoryType.PlatInstructorData);
                if (platInstructorEngineDataDsFilesStoreDirectory is not null)
                {
                    DsFilesStoreFile? mvDsFileInfo = platInstructorEngineDataDsFilesStoreDirectory.DsFilesStoreFilesCollection
                        .FirstOrDefault(fi => fi.Name.EndsWith(@".mv_", StringComparison.InvariantCultureIgnoreCase));
                    if (mvDsFileInfo is not null)
                    {
                        engineSessionId = Guid.NewGuid().ToString();
                        var systemNameBase = Path.GetFileNameWithoutExtension(mvDsFileInfo.Name);                        
                        string xiSystemName = systemNameBase + @"." + engineSessionId;

                        Generate_LaunchEngine_SystemEvent(processModelingSession.EnginesHostInfo.WorkstationName, processModelingSession, DsFilesStoreDirectoryType.PlatInstructorBin, DsFilesStoreDirectoryType.PlatInstructorData, mvDsFileInfo.Name, xiSystemName);

                        DataAccessProviderGetter_AddonBase dataAccessProviderGetter_Addon2;
                        if (String.Equals(processModelingSession.EnginesHostInfo.WorkstationName, @"localhost", StringComparison.InvariantCultureIgnoreCase))
                        {
                            dataAccessProviderGetter_Addon2 = GetNewInitializedDataAccessProviderAddon(
                                _serviceProvider,
                                $"http://localhost:60080/PlatServer/ServerDiscovery",
                                xiSystemName,
                                new CaseInsensitiveDictionary<string?> { { @"XiSystem", xiSystemName } },
                                ThreadSafeDispatcher);
                        }
                        else
                        {
                            dataAccessProviderGetter_Addon2 = GetNewInitializedDataAccessProviderAddon(
                                _serviceProvider,
                                $"https://{processModelingSession.EnginesHostInfo.WorkstationName}:60060",
                                DataAccessConstants.PlatformXiProcessModelingSessionId + xiSystemName,
                                new CaseInsensitiveDictionary<string?>(),
                                ThreadSafeDispatcher);
                        }
                        var platInstructorEngineSession = new EngineSession(engineSessionId, dataAccessProviderGetter_Addon2);
                        processModelingSession.EngineSessions.Add(platInstructorEngineSession);                        
                    }
                }
            }
            else
            {
                jobId = processModelingSession.LaunchEnginesJobId;
                SubscribeForExistingJobProgress(jobId, serverContext);
            }

            Task.Run(async () =>
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

                serverContext.AddCallbackMessage(new LongrunningPassthroughCallbackMessage
                {
                    JobId = jobId,
                    ProgressPercent = 100,
                    ProgressLabel = Resources.ResourceManager.GetString(ResourceStrings.OperationCompleted_ProgressLabel, serverContext.CultureInfo),
                    StatusCode = StatusCodes.Good
                });
            });

            return jobId;
        }        

        private string ProcessModelingSession_DownloadChangedFiles_LongrunningPassthrough(ServerContext serverContext, ReadOnlyMemory<byte> dataToSend)
        {
            string?[] csvLine = CsvHelper.ParseCsvLine(@",", Encoding.UTF8.GetString(dataToSend.Span));
            string invariantDirectoryPathsRelativeToRootDirectory = CsvHelper.FormatForCsv(@",", csvLine.Skip(1));            
            ProcessModelingSession processModelingSession = GetProcessModelingSession(csvLine[0]);

            string jobId = Guid.NewGuid().ToString();
            var jobProgress = new JobProgress(jobId)
            {
                ForTimeout_LastDateTimeUtc = DateTime.UtcNow,
                JobTimeout_ProgressLabel = @"" //Resources.ResourceManager.GetString(ResourceStrings.LaunchingInstructorProgressErrorMessage, serverContext.CultureInfo)
            };
            jobProgress.ProgressSubscribers.Add(serverContext);
            _jobProgressesCollection.Add(jobId, jobProgress);

            Generate_DownloadChangedFiles_SystemEvent(processModelingSession.InitiatorClientWorkstationName, @"", invariantDirectoryPathsRelativeToRootDirectory, false);
            Generate_DownloadChangedFiles_SystemEvent(processModelingSession.EnginesHostInfo!.WorkstationName, jobId, invariantDirectoryPathsRelativeToRootDirectory, false);

            return jobId;
        }

        private string ProcessModelingSession_UploadChangedFiles_LongrunningPassthrough(ServerContext serverContext, ReadOnlyMemory<byte> dataToSend)
        {
            string?[] csvLine = CsvHelper.ParseCsvLine(@",", Encoding.UTF8.GetString(dataToSend.Span));
            string invariantDirectoryPathsRelativeToRootDirectory = CsvHelper.FormatForCsv(@",", csvLine.Skip(1));
            ProcessModelingSession processModelingSession = GetProcessModelingSession(csvLine[0]);

            string jobId = Guid.NewGuid().ToString();
            var jobProgress = new JobProgress(jobId)
            {
                ForTimeout_LastDateTimeUtc = DateTime.UtcNow,
                JobTimeout_ProgressLabel = @"" //Resources.ResourceManager.GetString(ResourceStrings.LaunchingInstructorProgressErrorMessage, serverContext.CultureInfo)
            };
            jobProgress.ProgressSubscribers.Add(serverContext);
            _jobProgressesCollection.Add(jobId, jobProgress);

            Generate_UploadChangedFiles_SystemEvent(processModelingSession.InitiatorClientWorkstationName, @"", invariantDirectoryPathsRelativeToRootDirectory);
            Generate_UploadChangedFiles_SystemEvent(processModelingSession.EnginesHostInfo!.WorkstationName, jobId, invariantDirectoryPathsRelativeToRootDirectory);

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

            /// <summary>
            ///     Production DataAccessConstants.Launcher_ClientApplicationName.
            ///     Debug only DataAccessConstants.Instructor_ClientApplicationName
            /// </summary>
            public string InitiatorClientApplicationName { get; }

            /// <summary>            
            ///     Production WorkstationName of DataAccessConstants.Launcher_ClientApplicationName.
            ///     Debug only WorkstationName of DataAccessConstants.Instructor_ClientApplicationName
            /// </summary>
            public string InitiatorClientWorkstationName { get; }

            public string ProcessModelName { get; }

            public string ProcessModelNameToDisplay { get; }

            public string InstructorUserName { get; }

            public InstructorAccessFlags InstructorAccessFlags { get; }

            public string Mode { get; }

            public long? DbEnity_ProcessModelingSessionId { get; set; }

            public ObservableCollection<EngineSession> EngineSessions { get; } = new();

            public EnginesHostInfo? EnginesHostInfo { get; set; }

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
                            if (StatusCodes.IsGood(args.NewValueStatusTimestamp.StatusCode))
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


//if (processModelingSession.ProcessModelName == @"")
//{
//    // Connect to PlatInstructor only                    
//    var platInstructorEngineSession = new PlatInstructor_TrainingEngineSession(
//        _serviceProvider,
//        ThreadSafeDispatcher,
//        "http://" + targetWorkstationName + ":60080/PlatServer/ServerDiscovery",
//        @"",
//        new CaseInsensitiveDictionary<string?>());
//    processModelingSession.EngineSessions.Add(platInstructorEngineSession);

//    serverContext.AddCallbackMessage(new ServerContext.LongrunningPassthroughCallbackMessage
//    {
//        JobId = jobId,
//        ProgressPercent = 100,
//        ProgressLabel = Resources.ResourceManager.GetString(ResourceStrings.OperationCompleted_ProgressLabel, serverContext.CultureInfo),
//        StatusCode = StatusCodes.Good
//    });
//}