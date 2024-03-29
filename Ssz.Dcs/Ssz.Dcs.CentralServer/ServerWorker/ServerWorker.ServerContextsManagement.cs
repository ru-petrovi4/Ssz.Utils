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
                if (!serverContext.ContextParams.ContainsKey(DataAccessConstants.ParamName_HostType))
                {
                    if (serverContext.ClientApplicationName == DataAccessConstants.CentralServer_ClientWindowsService_ClientApplicationName)
                    {
                        On_CentralServer_ClientWindowsService_UtilityServerContext_AddedOrRemoved(serverContext, args.Added);
                    }
                    else if (serverContext.ClientApplicationName == DataAccessConstants.Launcher_ClientApplicationName)
                    {
                        On_Launcher_UtilityServerContext_AddedOrRemoved(serverContext, args.Added);
                    }
                }
            }
            else // Process context  
            {
                OnProcessServerContext_AddedOrRemoved(serverContext, args.Added, systemNameToConnect);
            }
        }

        private void On_CentralServer_ClientWindowsService_UtilityServerContext_AddedOrRemoved(ServerContext utilityServerContext, bool added)
        {
            if (added)
            {
                _operatorWorkstationNamesCollection.Add(utilityServerContext.ClientWorkstationName);
            }
            else
            {
                _operatorWorkstationNamesCollection.Remove(utilityServerContext.ClientWorkstationName);
            }

            _utilityItemsDoWorkNeeded = true;
        }

        private void On_Launcher_UtilityServerContext_AddedOrRemoved(ServerContext utilityServerContext, bool added)
        {
            string? operatorSessionId = utilityServerContext.ContextParams.TryGetValue(@"OperatorSessionId");
            if (String.IsNullOrEmpty(operatorSessionId))
                return;

            if (added)
            {
                OperatorSession? operatorSession = OperatorSessionsCollection.TryGetValue(operatorSessionId);
                if (operatorSession is null)
                {
                    operatorSession = new OperatorSession(operatorSessionId, utilityServerContext.ClientWorkstationName);

                    SetOperatorSessionStatus(operatorSession, OperatorSessionConstants.ReadyToLaunchOperator);

                    OperatorSessionsCollection.Add(operatorSession.OperatorSessionId, operatorSession);
                }
            }
            else
            {
                OperatorSession? operatorSession = OperatorSessionsCollection.TryGetValue(operatorSessionId);
                if (operatorSession is not null && operatorSession.OperatorSessionStatus == OperatorSessionConstants.ReadyToLaunchOperator)
                {
                    OperatorSessionsCollection.Remove(operatorSession.OperatorSessionId);
                }
            }

            _utilityItemsDoWorkNeeded = true;
        }

        #endregion
    }
}