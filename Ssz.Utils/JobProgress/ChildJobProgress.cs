using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    public class ChildJobProgress : IJobProgress
    {
        public ChildJobProgress(IJobProgress parentJobProgress, uint minProgressPercent, uint maxProgressPercent, bool parentFailedIfFailed)
        {
            ParentJobProgress = parentJobProgress;
            MinProgressPercent = minProgressPercent;
            MaxProgressPercent = maxProgressPercent;
            _parentFailedIfFailed = parentFailedIfFailed;
        }

        public IJobProgress ParentJobProgress { get; }

        public uint MinProgressPercent { get; }

        public uint MaxProgressPercent { get; }

        public string JobId => ParentJobProgress.JobId;

        /// <summary>
        ///     Процент выполнения задачи 0 - 100
        /// </summary>		
        public uint ProgressPercent { get; private set; }

        /// <summary>
        ///     Лейбл о статусе выполнения или сообщение об ошибке
        /// </summary>		
        public string ProgressLabel { get; private set; } = @"";

        /// <summary>
        ///     Детали о статусе исполнение или детали об ошибке
        /// </summary>		
        public string ProgressDetails { get; private set; } = @"";

        /// <summary>
        ///     See consts in <see cref="StatusCodes"/>.        
        /// </summary>
        public uint StatusCode { get; private set; }

        /// <summary>
        ///     ContinuationSemaphoreSlim for job continue from pause or cancel.
        /// </summary>
        public SemaphoreSlim Job_ContinuationSemaphoreSlim { get; } = new SemaphoreSlim(0);

        public IJobProgress GetChildJobProgress(uint minProgressPercent, uint maxProgressPercent, bool parentFailedIfFailed)
        {
            return new ChildJobProgress(ParentJobProgress,
                MinProgressPercent + (MaxProgressPercent - MinProgressPercent) * minProgressPercent / 100,
                MinProgressPercent + (MaxProgressPercent - MinProgressPercent) * maxProgressPercent / 100,
                parentFailedIfFailed);
        }

        public Task SetJobProgressAsync(uint? progressPercent, string? progressLabel, string? progressDetails, uint statusCode)
        {
            if (progressPercent is not null)
                ProgressPercent = (uint)progressPercent;
            if (progressLabel is not null)
                ProgressLabel = progressLabel;
            if (progressDetails is not null)
                ProgressDetails = progressDetails;
            StatusCode = statusCode;

            if (!_parentFailedIfFailed && !StatusCodes.IsGood(statusCode))
            {
                statusCode = StatusCodes.Good;
                progressPercent = 100;
            }

            if (progressPercent is not null)
                return ParentJobProgress.SetJobProgressAsync(MinProgressPercent + (MaxProgressPercent - MinProgressPercent) * (uint)progressPercent / 100, progressLabel, progressDetails, statusCode);
            else
                return ParentJobProgress.SetJobProgressAsync(null, progressLabel, progressDetails, statusCode);
        }

        private bool _parentFailedIfFailed;
    }
}
