using Ssz.Utils;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Operator.Core
{
    /// <summary>
    ///     Forwards the startup job progress to the HTML loading overlay, so its bar also covers
    ///     downloading the project pages and other files.
    ///     <para>Files already cached in IndexedDB are counted too: they are not re-downloaded,
    ///     so the bar simply runs through them.</para>
    /// </summary>
    public class AppLoadingJobProgress : IJobProgress
    {
        #region public functions

        public static readonly AppLoadingJobProgress Instance = new();

        public string JobId => @"";

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

        public Task SetJobProgressAsync(uint? progressPercent, string? progressLabel, string? progressDetails, uint statusCode)
        {
            if (progressPercent is not null)
                ProgressPercent = progressPercent.Value;
            if (progressLabel is not null)
                ProgressLabel = progressLabel;
            if (progressDetails is not null)
                ProgressDetails = progressDetails;
            StatusCode = statusCode;

            if (ProgressLabel != @"")
                AppLoadingInterop.SetStatusSafe(ProgressLabel);
            AppLoadingInterop.SetProjectProgressSafe(ProgressPercent, ProgressDetails);

            return Task.CompletedTask;
        }

        public Task<IJobProgress> GetChildJobProgressAsync(uint minProgressPercent, uint maxProgressPercent, bool parentFailedIfFailed)
        {
            return Task.FromResult<IJobProgress>(new ChildJobProgress(this,
                minProgressPercent,
                maxProgressPercent,
                parentFailedIfFailed));
        }

        #endregion
    }
}
