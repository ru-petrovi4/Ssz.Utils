using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ssz.Utils;
using Ssz.Dcs.CentralServer.Common;
using Ssz.Dcs.CentralServer.Properties;
using Ssz.Utils.DataAccess;
using Ssz.DataAccessGrpc.ServerBase;
using System.Threading;
using Grpc.Core;
using Google.Protobuf.WellKnownTypes;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Ssz.Dcs.CentralServer
{    
    public partial class ServerWorker : ServerWorkerBase
    {
        #region public functions 

        public void ConcludeOperatorSession(string operatorSessionId)
        {
            OperatorSession? operatorSession = OperatorSessionsCollection.TryGetValue(operatorSessionId);
            if (operatorSession is null) return;
            OperatorSessionsCollection.Remove(operatorSessionId);

            ServerContextsAbort(operatorSession.OperatorSession_ProcessServerContextsCollection.ToArray());

            _utilityItemsDoWorkNeeded = true;
        }

        public void SetOperatorSessionProps(string operatorSessionId, string operatorUserName)
        {
            OperatorSession? operatorSession = OperatorSessionsCollection.TryGetValue(operatorSessionId);
            if (operatorSession is null)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid operatorSession: " + operatorSessionId));

            operatorSession.OperatorUserName = operatorUserName;                                   

            _utilityItemsDoWorkNeeded = true;
        }

        #endregion

        #region internal functions

        /// <summary>
        ///     [OperatorSessionId, OperatorSession]
        /// </summary>
        internal CaseInsensitiveDictionary<OperatorSession> OperatorSessionsCollection { get; } = new();

        #endregion

        #region private functions

        private string ProcessModelingSession_PrepareAndRunOperatorExe_LongrunningPassthrough(ServerContext serverContext, ReadOnlyMemory<byte> dataToSend)
        {
            string?[] args = CsvHelper.ParseCsvLine(@",", Encoding.UTF8.GetString(dataToSend.Span));
            if (args.Length < 8)
                throw new InvalidOperationException();

            string operatorSessionId = args[0] ?? @"";
            string processModelingSessionId = args[1] ?? @"";
            string operatorWorkstationName = args[2] ?? @"";
            string operatorRoleId = args[3] ?? @"";
            string operatorRoleName = args[4] ?? @"";
            string dsProject_PathRelativeToDataDirectory = args[5] ?? @"";
            string interface_NameToDisplay = args[6] ?? @"";
            string operatorPlay_AdditionalCommandLine = args[7] ?? @"";

            ProcessModelingSession? processModelingSession = _processModelingSessionsCollection.TryGetValue(processModelingSessionId);
            if (processModelingSession is null)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid processModelingSessionId: " + processModelingSessionId));

            OperatorSession? operatorSession = OperatorSessionsCollection.TryGetValue(operatorSessionId);
            if (operatorSession is null)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid operatorSessionId: " + operatorSessionId));

            operatorSession.ProcessModelingSession = processModelingSession;
            operatorSession.OperatorRoleId = operatorRoleId;
            operatorSession.OperatorRoleName = operatorRoleName;
            operatorSession.DsProject_PathRelativeToDataDirectory = dsProject_PathRelativeToDataDirectory;
            operatorSession.Interface_NameToDisplay = interface_NameToDisplay;
            operatorSession.OperatorPlay_AdditionalCommandLine = operatorPlay_AdditionalCommandLine;
            SetOperatorSessionStatus(operatorSession, OperatorSessionConstants.LaunchingOperator);

            _utilityItemsDoWorkNeeded = true;

            string jobId;
            JobProgress? jobProgress;
            if (operatorSession.LaunchOperatorJobId is null)
            {
                jobId = Guid.NewGuid().ToString();
                operatorSession.LaunchOperatorJobId = jobId;

                jobProgress = SubscribeForNewJobProgress(jobId, serverContext);
            }
            else
            {
                jobId = operatorSession.LaunchOperatorJobId;

                jobProgress = SubscribeForExistingJobProgress(jobId, serverContext);
            }

            if (jobProgress is not null)
            {
                jobProgress.ForTimeout_LastDateTimeUtc = DateTime.UtcNow;
                jobProgress.JobTimeout_ProgressLabel = Resources.ResourceManager.GetString(ResourceStrings.LaunchingOperatorProgressErrorMessage, serverContext.CultureInfo);
            }

            Generate_PrepareAndRunOperatorExe_UtilityEvent(operatorSession.OperatorWorkstationName, operatorSession);

            return jobId;
        }        

        private string ProcessModelingSession_RunOperatorExe_LongrunningPassthrough(ServerContext serverContext, ReadOnlyMemory<byte> dataToSend)
        {
            string?[] parts = CsvHelper.ParseCsvLine(@",", Encoding.UTF8.GetString(dataToSend.Span));
            if (parts.Length < 4)
                throw new InvalidOperationException();
            
            OperatorSession? operatorSession = OperatorSessionsCollection.TryGetValue(parts[0]);
            if (operatorSession is null) 
                return @"";

            Generate_RunOperatorExe_UtilityEvent(operatorSession.OperatorWorkstationName, operatorSession, parts[1]!, parts[2]!, parts[3]!);

            return operatorSession.LaunchOperatorJobId!;
        }

        private void ProcessModelingSession_RunInstructorExe_Passthrough(ServerContext serverContext, ReadOnlyMemory<byte> dataToSend, out byte[] returnData)
        {
            returnData = new byte[0];

            string?[] parts = CsvHelper.ParseCsvLine(@",", Encoding.UTF8.GetString(dataToSend.Span));
            if (parts.Length < 3)
                throw new InvalidOperationException();

            ProcessModelingSession? processModelingSession = GetProcessModelingSessionOrNull(parts[0]);
            
            if (processModelingSession is null)
                return;

            Generate_RunInstructorExe_UtilityEvent(processModelingSession.InitiatorClientWorkstationName, processModelingSession, parts[1]!, parts[2]!);
        }

        private string ProcessModelingSession_SubscribeForLaunchOperatorProgress_LongrunningPassthrough(ServerContext serverContext, ReadOnlyMemory<byte> dataToSend)
        {
            string operatorSessionId = Encoding.UTF8.GetString(dataToSend.Span);            
            OperatorSession? operatorSession = OperatorSessionsCollection.TryGetValue(operatorSessionId);
            if (operatorSession is null)
                return @"";

            string jobId;
            if (operatorSession.LaunchOperatorJobId is null)
            {
                jobId = Guid.NewGuid().ToString();
                operatorSession.LaunchOperatorJobId = jobId;

                SubscribeForNewJobProgress(jobId, serverContext);
            }
            else
            {
                jobId = operatorSession.LaunchOperatorJobId;

                SubscribeForExistingJobProgress(jobId, serverContext);
            }            

            return jobId;
        }

        /// <summary>
        ///     processServerContext with ContextParams['OperatorSessionId'] != String.Empty
        /// </summary>
        /// <param name="processServerContext"></param>
        /// <param name="added"></param>
        /// <param name="systemNameToConnect"></param>
        /// <exception cref="RpcException"></exception>
        private void OnProcessServerContext_AddedOrRemoved(ServerContext processServerContext, bool added, string systemNameToConnect)
        {
            if (!String.Equals(systemNameToConnect, DataAccessConstants.Dcs_SystemName, StringComparison.InvariantCultureIgnoreCase))
            {            
                ProcessModelingSession? processModelingSession = GetProcessModelingSessionOrNull(systemNameToConnect);
                if (added)
                {
                    if (processModelingSession is null)
                    {
                        throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid processModelingSessionId: " + systemNameToConnect));
                    }
                    if (processServerContext.ClientApplicationName == DataAccessConstants.Instructor_ClientApplicationName &&
                        processModelingSession.ProcessServerContextsCollection.Count(sc =>
                            String.Equals(sc.ClientApplicationName, DataAccessConstants.Instructor_ClientApplicationName, StringComparison.InvariantCultureIgnoreCase) 
                            ) == 0)
                    {
                        processModelingSession.ForTimeout_LastDateTimeUtc = null; // Reset timeout, instruxtor connected.
                        processModelingSession.ProcessModelingSessionStatus = ProcessModelingSessionConstants.InstructorConnected;
                    }
                    processModelingSession.ProcessServerContextsCollection.Add(processServerContext);
                }
                else
                {
                    if (processModelingSession is not null)
                    {
                        processModelingSession.ProcessServerContextsCollection.Remove(processServerContext);
                        if (processServerContext.ClientApplicationName == DataAccessConstants.Instructor_ClientApplicationName &&
                            processModelingSession.ProcessServerContextsCollection.Count(sc =>
                                String.Equals(sc.ClientApplicationName, DataAccessConstants.Instructor_ClientApplicationName, StringComparison.InvariantCultureIgnoreCase)
                                ) == 0)
                        {
                            processModelingSession.ForTimeout_LastDateTimeUtc = DateTime.UtcNow; // Set timeout, last instruxtor disconnected.
                            processModelingSession.ProcessModelingSessionStatus = ProcessModelingSessionConstants.InstructorDisconnected;
                        }
                    }
                }
            }

            string? operatorSessionId = processServerContext.ContextParams.TryGetValue(@"OperatorSessionId");
            if (operatorSessionId == null || operatorSessionId == @"") // Context with operatorSessionId.
                return;
            OperatorSession? operatorSession = OperatorSessionsCollection.TryGetValue(operatorSessionId);
            if (operatorSession is null)
                return;
            if (added)
            {
                operatorSession.OperatorSession_ProcessServerContextsCollection.Add(processServerContext);                
                if (!operatorSession.OperatorInterfaceConnected)
                {
                    operatorSession.OperatorInterfaceConnected = true;
                    operatorSession.ForTimeout_LastDateTimeUtc = null;
                    SetOperatorSessionStatus(operatorSession, OperatorSessionConstants.LaunchedOperator);                                                         
                    _utilityItemsDoWorkNeeded = true;
                }   
            }
            else
            {
                operatorSession.OperatorSession_ProcessServerContextsCollection.Remove(processServerContext);                
                if (operatorSession.OperatorSession_ProcessServerContextsCollection.Count == 0)
                {
                    if (!processServerContext.IsConcludeCalled)
                    {
                        operatorSession.ForTimeout_LastDateTimeUtc = DateTime.UtcNow;
                    }
                    else
                    {
                        OperatorSessionsCollection.Remove(operatorSession.OperatorSessionId);
                        SetOperatorSessionStatus(operatorSession, OperatorSessionConstants.ShutdownedOperator);
                        _utilityItemsDoWorkNeeded = true;
                    }                    
                }
            }
        }

        private void SetOperatorSessionStatus(OperatorSession operatorSession, int operatorSessionStatus)
        {
            operatorSession.OperatorSessionStatus = operatorSessionStatus;
            switch (operatorSessionStatus)
            {
                case OperatorSessionConstants.LaunchedOperator:
                    if (operatorSession.ProcessModelingSession is not null &&
                        operatorSession.ProcessModelingSession.DbEnity_ProcessModelingSessionId is not null)
                    {
                        try
                        {
                            using (var dbContext = _dbContextFactory.CreateDbContext())
                            {
                                if (dbContext.IsConfigured) 
                                {
                                    var operatorUser = dbContext.Users.FirstOrDefault(u => u.UserName == operatorSession.OperatorUserName);
                                    if (operatorUser is not null)
                                    {
                                        var dbEnity_OperatorSession = new Common.EntityFramework.OperatorSession
                                        {
                                            OperatorUser = operatorUser,
                                            StartDateTimeUtc = DateTime.UtcNow,
                                            ProcessModelingSessionId = operatorSession.ProcessModelingSession.DbEnity_ProcessModelingSessionId.Value,
                                        };
                                        dbContext.OperatorSessions.Add(dbEnity_OperatorSession);
                                        dbContext.SaveChanges();
                                        operatorSession.DbEnity_OperatorSessionId = dbEnity_OperatorSession.Id;
                                    }
                                }                                
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, @"LaunchedOperator dbContext error.");
                        }
                    }
                    break;
                case OperatorSessionConstants.ShutdownedOperator:
                    try
                    {
                        if (operatorSession.DbEnity_OperatorSessionId is not null)
                        {
                            using (var dbContext = _dbContextFactory.CreateDbContext())
                            {
                                if (dbContext.IsConfigured)
                                {
                                    var dbEnity_OperatorSession = dbContext.OperatorSessions.FirstOrDefault(pms => pms.Id == operatorSession.DbEnity_OperatorSessionId.Value);
                                    if (dbEnity_OperatorSession is not null)
                                    {
                                        dbEnity_OperatorSession.FinishDateTimeUtc = DateTime.UtcNow;
                                        dbContext.SaveChanges();
                                    };
                                }                                
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, @"ShutdownedOperator dbContext error.");
                    }
                    break;
            }
            
        }

        #endregion

        internal class OperatorSession
        {
            #region construction and destruction

            public OperatorSession(string operatorSessionId, string operatorWorkstationName)
            {
                OperatorSessionId = operatorSessionId;
                OperatorWorkstationName = operatorWorkstationName;                
            }

            #endregion

            #region public functions

            public string OperatorSessionId { get; }

            public string OperatorWorkstationName { get; }

            public string?[] ProcessModelNames { get; set; } = null!;

            public List<ServerContext> UtilityServerContexts { get; } = new();

            public ProcessModelingSession? ProcessModelingSession { get; set; }

            /// <summary>
            ///     'UserDomainName\UserName'
            /// </summary>
            public string WindowsUserName { get; set; } = "";

            /// <summary>
            ///     TODO
            /// </summary>
            public string WindowsUserNameToDisplay { get; set; } = "";

            public string OperatorUserName { get; set; } = "";

            public string OperatorRoleId { get; set; } = "";

            public string OperatorRoleName { get; set; } = "";

            public string DsProject_PathRelativeToDataDirectory { get; set; } = "";

            public string Interface_NameToDisplay { get; set; } = "";

            public string OperatorPlay_AdditionalCommandLine { get; set; } = "";

            public int OperatorSessionStatus { get; set; }

            public bool OperatorInterfaceConnected { get; set; }

            public string? LaunchOperatorJobId { get; set; }

            /// <summary>
            ///     ProcessServerContexts with ContextParams['OperatorSessionId'] != String.Empty
            /// </summary>
            public List<ServerContext> OperatorSession_ProcessServerContextsCollection { get; } = new List<ServerContext>();

            /// <summary>
            ///     Time for inactivity time-out check.
            /// </summary>
            public DateTime? ForTimeout_LastDateTimeUtc { get; set; }

            public long? DbEnity_OperatorSessionId { get; set; }

            #endregion
        }
    }
}


//private string? GetModelName(OperatorSession operatorSession)
//{
//    if (operatorSession.ProcessModelingSessionId != "")
//    {
//        var processModelingSession = _processModelingSessionsCollection.TryGetValue(operatorSession.ProcessModelingSessionId);
//        if (processModelingSession is null) return null;
//        return processModelingSession.ModelName;
//    }
//    if (String.IsNullOrEmpty(operatorSession.ModelName)) return ""; // All models
//    string[] systemNameParts = operatorSession.ModelName.Split('@');
//    if (systemNameParts.Length == 2)
//    {
//        string[] parts = systemNameParts[0].Split('|');
//        if (parts.Length == 2)
//        {
//            return parts[0];
//        }
//    }
//    return operatorSession.ModelName;
//}

//private void GenerateConnectOperatorUtilityEvent(string operatorSessionId, string systemName)
//{
//    Action<Ssz.Utils.DataAccess.EventMessage>? utilityEventMessageNotification = UtilityEventMessageNotification;
//    if (utilityEventMessageNotification is not null)
//    {
//        var eventMessage = new Ssz.Utils.DataAccess.EventMessage(
//            new Ssz.Utils.DataAccess.EventId
//            {
//                Conditions = new List<Ssz.Utils.DataAccess.TypeId> { EventMessageConstants.ConnectOperatorTypeId }
//            }
//            );

//        eventMessage.EventType = EventType.SystemEvent;
//        eventMessage.OccurrenceTime = DateTime.UtcNow;
//        eventMessage.TextMessage = operatorSessionId + OperatorSessionConstants.FieldSeparator + systemName;

//        utilityEventMessageNotification(eventMessage);
//    }
//}

//var rootDsFilesStoreDirectory = GetFilesStoreDsFilesStoreDirectory();
//DsFilesStoreDirectory? processDsFilesStoreDirectory = rootDsFilesStoreDirectory.ChildDsFilesStoreDirectorysCollection
//    .FirstOrDefault(di => String.Equals(di.Name, processModelingSession.ProcessModelName, StringComparison.InvariantCultureIgnoreCase));

//if (processDsFilesStoreDirectory is null || DsFilesStoreHelper.GetDsFilesStoreDirectoryType(processDsFilesStoreDirectory) != DsFilesStoreDirectoryType.General)
//    throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid Process Model Name: " + processModelingSession.ProcessModelName));