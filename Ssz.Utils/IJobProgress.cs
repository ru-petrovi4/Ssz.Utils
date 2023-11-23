using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    public interface IJobProgress
    {
        string JobId { get; }

        /// <summary>
        ///     ������� ���������� ������ 0 - 100
        /// </summary>		
        uint ProgressPercent { get; }

        /// <summary>
        ///     ����� � ������� ���������� ��� ��������� �� ������
        /// </summary>		
        string ProgressLabel { get; }

        /// <summary>
        ///     ������ � ������� ���������� ��� ������ �� ������
        /// </summary>		
        string ProgressDetails { get; }

        /// <summary>
        ///     See consts in <see cref="StatusCodes"/>.        
        /// </summary>
        public uint StatusCode { get; }

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
        /// <param name="failedIfChildFailed"></param>
        /// <returns></returns>
        IJobProgress GetChildJobProgress(uint minProgressPercent, uint maxProgressPercent, bool failedIfChildFailed);
    }

    public class NullJobProgress : IJobProgress
    {
        public static readonly NullJobProgress Instance = new();

        public string JobId => @"";

        /// <summary>
        ///     ������� ���������� ������ 0 - 100
        /// </summary>		
        public uint ProgressPercent { get; private set; }

        /// <summary>
        ///     ����� � ������� ���������� ��� ��������� �� ������
        /// </summary>		
        public string ProgressLabel { get; private set; } = @"";

        /// <summary>
        ///     ������ � ������� ���������� ��� ������ �� ������
        /// </summary>		
        public string ProgressDetails { get; private set; } = @"";

        /// <summary>
        ///     See consts in <see cref="StatusCodes"/>.        
        /// </summary>
        public uint StatusCode { get; private set; }

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

        public IJobProgress GetChildJobProgress(uint minProgressPercent, uint maxProgressPercent, bool failedIfChildFailed)
        {
            return this;
        }
    }
}