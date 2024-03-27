using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Utils
{    
    /// <summary>
    /// 
    /// </summary>
    public interface IDispatcher
    {
        /// <summary>
        ///     If action is async, it is NOT awaited internally.
        /// </summary>
        /// <param name="action"></param>
        void BeginInvoke(Action<CancellationToken> action);

        /// <summary>
        ///     asyncAction is awaited internally.
        /// </summary>
        /// <param name="action"></param>
        void BeginInvokeEx(Func<CancellationToken, Task> action);
    }

    public static class IDispatcherExtensions
    {
        /// <summary>
        ///     If action is async, it is NOT awaited internally.
        /// </summary>
        /// <param name="action"></param>
        public static Task<T> InvokeAsync<T>(this IDispatcher dispatcher, Func<CancellationToken, T> action)
        {
            var taskCompletionSource = new TaskCompletionSource<T>();
            dispatcher.BeginInvoke(ct =>
            {
                try
                {
                    var result = action(ct);
                    taskCompletionSource.SetResult(result);
                }
                catch (Exception ex)
                {
                    taskCompletionSource.SetException(ex);
                }
            });
            return taskCompletionSource.Task;
        }

        /// <summary>
        ///     asyncAction is awaited internally.
        /// </summary>
        /// <param name="action"></param>
        public static Task<T> InvokeExAsync<T>(this IDispatcher dispatcher, Func<CancellationToken, Task<T>> action)
        {
            var taskCompletionSource = new TaskCompletionSource<T>();
            dispatcher.BeginInvokeEx(async ct =>
            {
                try
                {
                    var result = await action(ct);
                    taskCompletionSource.SetResult(result);
                }
                catch (Exception ex)
                {
                    taskCompletionSource.SetException(ex);
                }
            });
            return taskCompletionSource.Task;
        }
    }
}
