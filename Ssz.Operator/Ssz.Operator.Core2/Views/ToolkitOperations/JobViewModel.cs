using Ssz.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Operator.Core
{
	/// <summary>
	///     Represents info about long-running operation
	/// </summary>
    public class JobViewModel : Ssz.Operator.Play.ViewModels.ViewModelBase, IJobProgress
    {
		#region construction and destruction

		public JobViewModel(string jobId, string jobTitle, string user)
        {            
            JobId = jobId;
			JobTitle = jobTitle;
			User = user;			
		}

        #endregion

        #region public functions

        public int ProgressMaxValue { get; set; }

        public int ProgressCurrentValue { get; set; }

        public string JobId { get; }

		/// <summary>
		///     Заголовок задачи
		/// </summary>		
		public string JobTitle { get; }

		/// <summary>
		///     Пользователь, запустивший задачу
		/// </summary>
		public string User { get; }

        /// <summary>
        ///     Процент выполнения задачи 0 - 100
        /// </summary>		
        public uint ProgressPercent
        {
            get => _progressPercent;
            set => SetProperty(ref _progressPercent, value);
        }

        /// <summary>
        ///     Лейбл о статусе выполнения или сообщение об ошибке
        /// </summary>		
        public string ProgressLabel
        {
            get => _progressLabel;
            set => SetProperty(ref _progressLabel, value);
        }

        /// <summary>
        ///     Детали о статусе исполнение или детали об ошибке
        /// </summary>		
        public string ProgressDetails
        {
            get => _progressDetails;
            set => SetProperty(ref _progressDetails, value);
        }

        /// <summary>
		///     Cancelled - BadRequestCancelledByClient = 0x802C0000
        ///     See consts in <see cref="StatusCodes"/>.        
        /// </summary>
        public uint StatusCode
        {
            get => _statusCode;
            set => SetProperty(ref _statusCode, value);
        }

        /// <summary>
        ///     CancellationTokenSource for job cancelling.
        /// </summary>
        public CancellationTokenSource Job_CancellationTokenSource { get; } = new();

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
        /// <param name="statusCode">See consts in StatusCodes</param>
        /// <returns></returns>
        public async Task SetJobProgressAsync(uint? progressPercent, string? progressLabel, string? progressDetails, uint statusCode)
        {
			//await _syncRoot.WaitAsync();
			try
			{
				if (progressPercent is not null)
					ProgressPercent = (uint)progressPercent;
                if (progressLabel is not null)
                    ProgressLabel = progressLabel;
                if (progressDetails is not null)
                    ProgressDetails = progressDetails;
                StatusCode = statusCode;

				if (!StatusCodes.IsGood(StatusCode) || ProgressPercent == 100) // If failed, cancelled or finished
				{
					_endTimeUtc = DateTime.UtcNow;                    
                }
			}
			finally
			{
				//_syncRoot.Release();
			}
            await Task.Delay(0);
        }

        public Task<IJobProgress> GetChildJobProgressAsync(uint minProgressPercent, uint maxProgressPercent, bool parentFailedIfFailed)
        {
            return Task.FromResult<IJobProgress>(new ChildJobProgress(this, 
				minProgressPercent, 
				maxProgressPercent,
                parentFailedIfFailed));
        }        

		#endregion

		#region private fields

		/// <summary>
		///     Время начала задачи.
		/// </summary>		
		private DateTime _beginTimeUtc = DateTime.UtcNow;

		/// <summary>
		///     Время окончания задачи, успешно или с ошибкой.
		/// </summary>		
		private DateTime? _endTimeUtc;

        /// <summary>
        ///     Процент выполнения задачи 0 - 100
        /// </summary>		
        private uint _progressPercent;

        /// <summary>
        ///     Лейбл о статусе выполнения или сообщение об ошибке
        /// </summary>		
        private string _progressLabel = @"";

        /// <summary>
        ///     Детали о статусе исполнение или детали об ошибке
        /// </summary>		
        private string _progressDetails = @"";

        /// <summary>
		///     Cancelled - BadRequestCancelledByClient = 0x802C0000
        ///     See consts in <see cref="StatusCodes"/>.        
        /// </summary>
        private uint _statusCode;

        #endregion
    }
}
