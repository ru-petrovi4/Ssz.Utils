using System;
using System.Collections.Generic;
using System.Threading;
using Ssz.DataAccessGrpc.ServerBase;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Ssz.Utils.DataAccess;
using Ssz.Utils;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IO;
using Ssz.Dcs.CentralServer.Common;
using Ssz.Dcs.CentralServer.ServerListItems;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Ssz.Dcs.CentralServer.Properties;
using static Ssz.Dcs.CentralServer.ServerWorker;

namespace Ssz.Dcs.CentralServer
{
    public partial class ServerWorker : ServerWorkerBase
    {
        #region public functions        

        public void SetJobProgress(
            string jobId,
            uint progressPercent, 
            string progressLabelResourceName,
            string progressDetails,
            uint jobStatusCode)
        {
            JobProgress? jobProgress = _jobProgressesCollection.TryGetValue(jobId);
            if (jobProgress is null)
                return;
            
            jobProgress.ForTimeout_LastDateTimeUtc = DateTime.UtcNow;
            jobProgress.ProgressPercent = progressPercent;
            jobProgress.ProgressLabelResourceName = progressLabelResourceName;
            jobProgress.JobStatusCode = jobStatusCode;

            foreach (ServerContext serverContext in jobProgress.ProgressSubscribers)
            {   
                string? progressLabel = null;
                if (progressLabelResourceName != @"")
                {
                    try
                    {
                        progressLabel = Resources.ResourceManager.GetString(progressLabelResourceName, serverContext.CultureInfo);
                    }
                    catch
                    {
                    }
                }                             
                serverContext.AddCallbackMessage(new ServerContext.LongrunningPassthroughCallbackMessage
                {
                    JobId = jobProgress.JobId,
                    ProgressPercent = progressPercent,
                    ProgressLabel = progressLabel,
                    ProgressDetails = progressDetails,
                    JobStatusCode = jobStatusCode
                });
            }

            if (jobStatusCode != JobStatusCodes.OK || progressPercent == 100)
            {
                jobProgress.JobCompletedDateTimeUtc = DateTime.UtcNow;
                jobProgress.ProgressSubscribers.Clear();
            }
        }

        #endregion

        #region private functions

        private JobProgress SubscribeForNewJobProgress(string jobId, ServerContext serverContext)
        {
            var jobProgress = new JobProgress(jobId);
            _jobProgressesCollection.Add(jobId, jobProgress);
            jobProgress.ProgressSubscribers.Add(serverContext);
            return jobProgress;
        }

        private JobProgress? SubscribeForExistingJobProgress(string jobId, ServerContext serverContext)
        {
            JobProgress? jobProgress = _jobProgressesCollection.TryGetValue(jobId);
            if (jobProgress is null) // Completed long time ago, more than 1 day
            {
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
                jobProgress.ProgressSubscribers.Add(serverContext);
                serverContext.AddCallbackMessage(new ServerContext.LongrunningPassthroughCallbackMessage
                {
                    JobId = jobId,
                    ProgressPercent = jobProgress.ProgressPercent,
                    ProgressLabel = Resources.ResourceManager.GetString(jobProgress.ProgressLabelResourceName, serverContext.CultureInfo),
                    JobStatusCode = jobProgress.JobStatusCode
                });
            }
            return jobProgress;
        }

        #endregion

        #region private fields

        /// <summary>
        ///     [JobId, JobProgress]
        /// </summary>
        private CaseInsensitiveDictionary<JobProgress> _jobProgressesCollection = new();

        #endregion

        public class JobProgress
        {
            #region construction and destruction

            public JobProgress(string jobId)
            {                
                JobId = jobId;                
            }

            #endregion

            #region public functions

            /// <summary>
            ///     
            /// </summary>
            public List<ServerContext> ProgressSubscribers { get; } = new();

            public string JobId { get; }

            /// <summary>
            ///     Current value
            /// </summary>
            public uint ProgressPercent { get; set; }

            /// <summary>
            ///     Current value
            /// </summary>
            public string ProgressLabelResourceName { get; set; } = @"";

            /// <summary>
            ///     Current value
            /// </summary>
            public uint JobStatusCode { get; set; }

            /// <summary>
            ///     Time for inactivity time-out check.
            /// </summary>
            public DateTime? ForTimeout_LastDateTimeUtc { get; set; }

            /// <summary>            
            ///     JobProgress is not deleted for some time after completion for later clients progress requests.
            /// </summary>
            public DateTime? JobCompletedDateTimeUtc { get; set; }

            public string? JobTimeout_ProgressLabel { get; set; }

            #endregion
        }        
    }
}