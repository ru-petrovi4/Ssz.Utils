using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    public interface IJobProgress
    {
        string JobId { get; }

        /// <summary>
        ///     
        /// </summary>
        /// <param name="progressPercent"></param>
        /// <param name="progressLabel"></param>
        /// <param name="progressDetail"></param>
        /// <param name="jobStatusCode">See consts in JobStatusCodes</param>
        /// <returns></returns>
        Task SetJobProgressAsync(uint progressPercent, string? progressLabel, string? progressDetail, uint jobStatusCode);
    }

    public class DummyJobProgress : IJobProgress
    {
        public static readonly DummyJobProgress Default = new();

        public string JobId => @"";

        /// <summary>
        ///     
        /// </summary>
        /// <param name="progressPercent"></param>
        /// <param name="progressLabel"></param>
        /// <param name="progressDetail"></param>
        /// <param name="jobStatusCode">See consts in JobStatusCodes</param>
        /// <returns></returns>
        public Task SetJobProgressAsync(uint progressPercent, string? progressLabel, string? progressDetail, uint jobStatusCode)
        {
            return Task.CompletedTask;
        }
    }

    public static class JobStatusCodes
    {
        public const uint OK = 0;

        public const uint Cancelled = 1;

        public const uint UnknownError = 2;

        /// <summary>
        /// Client specified an invalid argument.  Note that this differs
        /// from FAILED_PRECONDITION.  INVALID_ARGUMENT indicates arguments
        /// that are problematic regardless of the state of the system
        /// (e.g., a malformed file name).
        /// </summary>
        public const uint InvalidArgument = 3;

        /// <summary>
        /// Deadline expired before operation could complete.  For operations
        /// that change the state of the system, this error may be returned
        /// even if the operation has completed successfully.  For example, a
        /// successful response from a server could have been delayed long
        /// enough for the deadline to expire.
        /// </summary>
        public const uint DeadlineExceeded = 4;

        /// <summary>Some requested entity (e.g., file or directory) was not found.</summary>
        public const uint NotFound = 5;

        /// <summary>Some entity that we attempted to create (e.g., file or directory) already exists.</summary>
        public const uint AlreadyExists = 6;

        /// <summary>
        /// The caller does not have permission to execute the specified
        /// operation.  PERMISSION_DENIED must not be used for rejections
        /// caused by exhausting some resource (use RESOURCE_EXHAUSTED
        /// instead for those errors).  PERMISSION_DENIED must not be
        /// used if the caller can not be identified (use UNAUTHENTICATED
        /// instead for those errors).
        /// </summary>
        public const uint PermissionDenied = 7;

        /// <summary>The request does not have valid authentication credentials for the operation.</summary>
        public const uint Unauthenticated = 16;

        /// <summary>
        /// Some resource has been exhausted, perhaps a per-user quota, or
        /// perhaps the entire file system is out of space.
        /// </summary>
        public const uint ResourceExhausted = 8;

        /// <summary>
        /// Operation was rejected because the system is not in a state
        /// required for the operation's execution.  For example, directory
        /// to be deleted may be non-empty, an rmdir operation is applied to
        /// a non-directory, etc.
        /// </summary>
        public const uint FailedPrecondition = 9;

        /// <summary>
        /// The operation was aborted, typically due to a concurrency issue
        /// like sequencer check failures, transaction aborts, etc.
        /// </summary>
        public const uint Aborted = 10;

        /// <summary>
        /// Operation was attempted past the valid range.  E.g., seeking or
        /// reading past end of file.
        /// </summary>
        public const uint OutOfRange = 11;

        /// <summary>Operation is not implemented or not supported/enabled in this service.</summary>
        public const uint Unimplemented = 12;

        /// <summary>
        /// Internal errors.  Means some invariants expected by underlying
        /// system has been broken.  If you see one of these errors,
        /// something is very broken.
        /// </summary>
        public const uint Internal = 13;

        /// <summary>
        /// The service is currently unavailable.  This is a most likely a
        /// transient condition and may be corrected by retrying with
        /// a backoff. Note that it is not always safe to retry
        /// non-idempotent operations.
        /// </summary>
        public const uint Unavailable = 14;

        /// <summary>Unrecoverable data loss or corruption.</summary>
        public const uint DataLoss = 15;

        public static bool IsOK(uint jobStatusCode) => jobStatusCode == OK;
    }
}