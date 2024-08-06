using Grpc.Core;
using Ssz.Dcs.CentralServer.Common;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ssz.Utils.Addons;
using Ssz.DataAccessGrpc.Client;

namespace Ssz.Dcs.CentralServer
{
    public partial class ServerWorker : ServerWorkerBase
    {
        #region private functions

        private void On_ServerContextAddedOrRemoved(object? sender, ServerContextAddedOrRemovedEventArgs args)
        {
            ServerContext serverContext = args.ServerContext;
            string systemNameToConnect = serverContext.SystemNameToConnect;
            if (systemNameToConnect == @"") // Utility context            
            {
                if (new Any(serverContext.ContextParams.TryGetValue(DataAccessConstants.ParamName_ConnectionToMain)).ValueAsBoolean(false))
                {
                    if (!String.IsNullOrEmpty(serverContext.ContextParams.TryGetValue(DataAccessConstants.ParamName_AdditionalCentralServerAddress)))
                        On_AdditionalCentralServer_AddedOrRemoved(serverContext, args.Added);
                }
                else
                {
                    if (serverContext.ClientApplicationName == DataAccessConstants.CentralServer_ClientWindowsService_ClientApplicationName)
                    {
                        if (!String.IsNullOrEmpty(serverContext.ContextParams.TryGetValue(DataAccessConstants.ParamName_Engine_ProcessModelNames)))
                            On_EnginesHost_AddedOrRemoved(serverContext, args.Added);
                    }                    
                    else if (serverContext.ClientApplicationName == DataAccessConstants.Launcher_ClientApplicationName)
                    {
                        if (!String.IsNullOrEmpty(serverContext.ContextParams.TryGetValue(@"OperatorSessionId")) &&
                                !String.IsNullOrEmpty(serverContext.ContextParams.TryGetValue(DataAccessConstants.ParamName_Operator_ProcessModelNames)))
                            On_Operator_AddedOrRemoved(serverContext, args.Added);
                    }
                    else if (serverContext.ClientApplicationName == DataAccessConstants.ControlEngine_ClientApplicationName)
                    {
                        if (!String.IsNullOrEmpty(serverContext.ContextParams.TryGetValue(DataAccessConstants.ParamName_EngineSessionId)) &&
                                !String.IsNullOrEmpty(serverContext.ContextParams.TryGetValue(DataAccessConstants.ParamName_ControlEngineServerAddress)))
                            On_ControlEngine_AddedOrRemoved(serverContext, args.Added);
                    }
                }
            }
            else // Process context  
            {
                OnProcessServerContext_AddedOrRemoved(serverContext, args.Added, systemNameToConnect);
            }
        }

        private void On_AdditionalCentralServer_AddedOrRemoved(ServerContext utilityServerContext, bool added)
        {
            var additionalCentralServerAddress = utilityServerContext.ContextParams.TryGetValue(DataAccessConstants.ParamName_AdditionalCentralServerAddress)!;
            var additionalCentralServerInfo = _additionalCentralServerInfosCollection.TryGetValue(additionalCentralServerAddress);
            if (added)
            {
                if (additionalCentralServerInfo is null)
                {
                    additionalCentralServerInfo = new AdditionalCentralServerInfo
                    {
                        ServerAddress = additionalCentralServerAddress
                    };
                    _additionalCentralServerInfosCollection.Add(additionalCentralServerAddress, additionalCentralServerInfo);
                }                
                additionalCentralServerInfo.UtilityServerContexts.Add(utilityServerContext);
            }
            else
            {
                if (additionalCentralServerInfo is not null)
                {
                    additionalCentralServerInfo.UtilityServerContexts.Remove(utilityServerContext);
                    if (additionalCentralServerInfo.UtilityServerContexts.Count == 0)
                        _additionalCentralServerInfosCollection.Remove(additionalCentralServerAddress);
                }
            }

            _utilityItemsDoWorkNeeded = true;
        }

        private void On_EnginesHost_AddedOrRemoved(ServerContext utilityServerContext, bool added)
        {
            var enginesHostInfo = _enginesHostInfosCollection.TryGetValue(utilityServerContext.ClientWorkstationName);
            if (added)
            {
                if (enginesHostInfo is null)
                {
                    enginesHostInfo = new EnginesHostInfo
                    {
                        WorkstationName = utilityServerContext.ClientWorkstationName
                    };
                    _enginesHostInfosCollection.Add(utilityServerContext.ClientWorkstationName, enginesHostInfo);
                }
                enginesHostInfo.ProcessModelNames = CsvHelper.ParseCsvLine(@",", utilityServerContext.ContextParams.TryGetValue(DataAccessConstants.ParamName_Engine_ProcessModelNames));
                enginesHostInfo.UtilityServerContexts.Add(utilityServerContext);                
            }
            else
            {
                if (enginesHostInfo is not null)
                {
                    enginesHostInfo.UtilityServerContexts.Remove(utilityServerContext);
                    if (enginesHostInfo.UtilityServerContexts.Count == 0)
                        _enginesHostInfosCollection.Remove(utilityServerContext.ClientWorkstationName);
                }
            }

            _utilityItemsDoWorkNeeded = true;
        }        

        private void On_Operator_AddedOrRemoved(ServerContext utilityServerContext, bool added)
        {
            string operatorSessionId = utilityServerContext.ContextParams.TryGetValue(@"OperatorSessionId")!;            
            if (added)
            {
                OperatorSession? operatorSession = OperatorSessionsCollection.TryGetValue(operatorSessionId);
                if (operatorSession is null)
                {
                    operatorSession = new OperatorSession(
                        operatorSessionId, 
                        utilityServerContext.ClientWorkstationName);

                    SetOperatorSessionStatus(operatorSession, OperatorSessionConstants.ReadyToLaunchOperator);

                    OperatorSessionsCollection.Add(operatorSession.OperatorSessionId, operatorSession);
                }
                operatorSession.ProcessModelNames = CsvHelper.ParseCsvLine(@",", utilityServerContext.ContextParams.TryGetValue(DataAccessConstants.ParamName_Operator_ProcessModelNames));
                operatorSession.UtilityServerContexts.Add(utilityServerContext);
            }
            else
            {
                OperatorSession? operatorSession = OperatorSessionsCollection.TryGetValue(operatorSessionId);
                if (operatorSession is not null)
                {
                    operatorSession.UtilityServerContexts.Remove(utilityServerContext);
                    if (operatorSession.UtilityServerContexts.Count == 0 &&
                            operatorSession.OperatorSessionStatus == OperatorSessionConstants.ReadyToLaunchOperator)
                        OperatorSessionsCollection.Remove(operatorSession.OperatorSessionId);
                }
            }

            _utilityItemsDoWorkNeeded = true;
        }

        private void On_ControlEngine_AddedOrRemoved(ServerContext utilityServerContext, bool added)
        {
            string engineSessionId = utilityServerContext.ContextParams.TryGetValue(DataAccessConstants.ParamName_EngineSessionId)!;
            try
            {                
                if (added)
                {
                    EngineSession? engineSession = ProcessModeling_EngineSessions.TryGetValue(engineSessionId);
                    if (engineSession is not null)
                        engineSession.DataAccessProvider.ServerAddress = utilityServerContext.ContextParams.TryGetValue(DataAccessConstants.ParamName_ControlEngineServerAddress)!;
                }     
                else
                {
                    ProcessModeling_EngineSessions.Remove(engineSessionId);
                }
            }
            catch
            {
            }
        }

        #endregion

        #region private fields

        /// <summary>
        ///     [AdditionalCentralServerAddress, AdditionalCentralServerInfo]
        /// </summary>
        private readonly CaseInsensitiveDictionary<AdditionalCentralServerInfo> _additionalCentralServerInfosCollection = new();

        /// <summary>
        ///     [ClientWorkstationName, EnginesHostInfo]
        /// </summary>
        private readonly CaseInsensitiveDictionary<EnginesHostInfo> _enginesHostInfosCollection = new();

        private readonly EnginesHostInfo _localEnginesHostInfo = new()
        {
            WorkstationName = @"localhost",
            ProcessModelNames = [@"*"]
        };

        #endregion

        public class AdditionalCentralServerInfo
        {
            /// <summary>
            ///     Obtained from AdditionalCentralServerAddress context param
            /// </summary>
            public string ServerAddress { get; set; } = null!;

            public HashSet<string> ClientWorkstationNames { get; } = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            public List<ServerContext> UtilityServerContexts { get; } = new();
        }

        public class EnginesHostInfo
        {
            /// <summary>
            ///     Obtained from ClientWorkstationName
            /// </summary>
            public string WorkstationName { get; set; } = null!;            
            
            public int EnginesCount
            { 
                get
                {
                    if (UtilityServerContexts.Count == 0)
                        return 0;

                    return new Any(UtilityServerContexts[0].ContextParams.TryGetValue(DataAccessConstants.ParamName_RunningControlEnginesCount)).ValueAsInt32(false);
                }                 
            }

            public string?[] ProcessModelNames { get; set; } = null!;

            /// <summary>
            ///     DataAccessConstants.CentralServer_ClientWindowsService_ClientApplicationName only
            /// </summary>
            public List<ServerContext> UtilityServerContexts { get; } = new();
        }        
    }
}