using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    /// <summary>
    ///     If method uses <see cref="IJobProgress"/> it is supposed as no-throwing.
    /// </summary>
    public interface IJobProgress
    {
        string JobId { get; }

        /// <summary>
        ///     Процент выполнения задачи 0 - 100
        /// </summary>		
        uint ProgressPercent { get; }

        /// <summary>
        ///     Лейбл о статусе выполнения или сообщение об ошибке
        /// </summary>		
        string ProgressLabel { get; }

        /// <summary>
        ///     Детали о статусе исполнение или детали об ошибке
        /// </summary>		
        string ProgressDetails { get; }

        /// <summary>
        ///     See consts in <see cref="StatusCodes"/>.        
        /// </summary>
        public uint StatusCode { get; }

        /// <summary>
        ///     ContinuationSemaphoreSlim for job continue from pause or cancel.
        /// </summary>
        public SemaphoreSlim Job_ContinuationSemaphoreSlim { get; }

        /// <summary>
        ///     If parameter is null, the parameter does not change.
        /// </summary>
        /// <param name="progressPercent"></param>
        /// <param name="progressLabel"></param>progressDetails
        /// <param name="progressDetails"></param>
        /// <param name="statusCode">See consts in <see cref="StatusCodes"/></param>
        /// <returns></returns>
        Task SetJobProgressAsync(uint? progressPercent, string? progressLabel, string? progressDetails, uint statusCode);

        /// <summary>
        ///     failedIfChildFailed - If child JobProgress failed, then this JobProgress failed. Otherwise, this progress continues.
        /// </summary>
        /// <param name="minProgressPercent"></param>
        /// <param name="maxProgressPercent"></param>
        /// <param name="parentFailedIfFailed"></param>
        /// <returns></returns>
        IJobProgress GetChildJobProgress(uint minProgressPercent, uint maxProgressPercent, bool parentFailedIfFailed);
    }

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