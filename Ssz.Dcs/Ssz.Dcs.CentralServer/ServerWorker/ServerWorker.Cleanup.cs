using Grpc.Core;
using Ssz.Dcs.CentralServer.Common;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ssz.Utils;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;

namespace Ssz.Dcs.CentralServer
{
    public partial class ServerWorker : ServerWorkerBase
    {
        #region private functions        

        /// <summary>
        ///     WindowsService main thread.
        /// </summary>
        private void Cleanup(DateTime nowUtc, CancellationToken cancellationToken)
        {
            DateTime unrecoverableDateTimeUtc = nowUtc - DataAccessConstants.UnrecoverableTimeout;
            DateTime operationCleanupDateTimeUtc = nowUtc - DataAccessConstants.OperationCleanupTimeout;

            foreach (ProcessModelingSession processModelingSession in _processModelingSessionsCollection.Values.ToArray())
            {
                if (processModelingSession.ForTimeout_LastDateTimeUtc.HasValue && processModelingSession.ForTimeout_LastDateTimeUtc.Value < unrecoverableDateTimeUtc)
                {
                    ConcludeProcessModelingSession(processModelingSession.ProcessModelingSessionId);
                }
            }

            bool utilityItemsDoWorkNeeded = false;
            foreach (OperatorSession operatorSession in OperatorSessionsCollection.Values.ToArray())
            {
                if (operatorSession.ForTimeout_LastDateTimeUtc.HasValue && operatorSession.ForTimeout_LastDateTimeUtc.Value < unrecoverableDateTimeUtc)
                {
                    operatorSession.OperatorInterfaceConnected = false;
                    OperatorSessionsCollection.Remove(operatorSession.OperatorSessionId);
                    SetOperatorSessionStatus(operatorSession, OperatorSessionConstants.ShutdownedOperator);
                    utilityItemsDoWorkNeeded = true;
                }
            }

            foreach (JobProgress jobProgress in _jobProgressesCollection.Values.ToArray())
            {
                if (jobProgress.ForTimeout_LastDateTimeUtc.HasValue && jobProgress.ForTimeout_LastDateTimeUtc.Value < unrecoverableDateTimeUtc)
                {
                    jobProgress.JobCompletedDateTimeUtc = nowUtc;
                    foreach (ServerContext serverContext in jobProgress.ProgressSubscribers.ToArray())
                    {
                        if (serverContext.Disposed)
                        {
                            jobProgress.ProgressSubscribers.Remove(serverContext);
                            continue;
                        }

                        serverContext.AddCallbackMessage(new ServerContext.LongrunningPassthroughCallbackMessage
                        {
                            JobId = jobProgress.JobId,
                            ProgressPercent = 100,
                            ProgressLabel = jobProgress.JobTimeout_ProgressLabel,
                            StatusCode = StatusCodes.BadInvalidState
                        });
                    }                  
                }
                if (jobProgress.JobCompletedDateTimeUtc.HasValue && jobProgress.JobCompletedDateTimeUtc.Value < operationCleanupDateTimeUtc)
                {
                    _jobProgressesCollection.Remove(jobProgress.JobId);                    
                }
            }

            if (utilityItemsDoWorkNeeded)
                _utilityItemsDoWorkNeeded = true;

            if (nowUtc - _last_GC_CleanUpDateTimeUtc > TimeSpan.FromMinutes(5))
            {
                ResourceUtilization resourceUtilization = ResourceMonitor.GetUtilization(TimeSpan.FromSeconds(1));
                if (resourceUtilization.MemoryUsedPercentage > 90)
                    throw new OperationCanceledException();
                
                _last_GC_CleanUpDateTimeUtc = nowUtc; // Because long-running operation.                
            }
        }

        #endregion

        #region private fields

        private DateTime _last_GC_CleanUpDateTimeUtc = DateTime.UtcNow;

        #endregion
    }
}