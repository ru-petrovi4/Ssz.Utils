using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    public class NullJobProgress : IJobProgress
    {
        public static readonly NullJobProgress Instance = new();

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

        /// <summary>
        ///     
        /// </summary>
        /// <param name="progressPercent"></param>
        /// <param name="progressLabel"></param>
        /// <param name="progressDetails"></param>
        /// <param name="statusCode">See consts in <see cref="StatusCodes"/></param>
        /// <returns></returns>
        public Task SetJobProgressAsync(uint? progressPercent, string? progressLabel, string? progressDetails, uint statusCode)
        {
            return Task.CompletedTask;
        }

        public IJobProgress GetChildJobProgress(uint minProgressPercent, uint maxProgressPercent, bool parentFailedIfFailed)
        {
            return this;
        }
    }
}