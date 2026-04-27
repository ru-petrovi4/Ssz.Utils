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
        /// </summary>
        /// <param name="action"></param>
        void BeginInvoke(Action<CancellationToken> action);

        /// <summary>
        ///     asyncAction is NOT awaited internally.
        /// </summary>
        /// <param name="asyncAction"></param>
        void BeginInvoke(Func<CancellationToken, Task> asyncAction);

        /// <summary>
        ///     asyncAction is awaited internally.
        /// </summary>
        /// <param name="asyncAction"></param>
        void BeginInvokeEx(Func<CancellationToken, Task> asyncAction);
    }

    public static class IDispatcherExtensions
    {
        /// <summary>        
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
                    taskCompletionSource.TrySetException(ex);
                }
            });
            return taskCompletionSource.Task;
        }

        /// <summary>
        ///     asyncAction is NOT awaited internally.
        /// </summary>
        /// <param name="asyncAction"></param>
        public static Task<Task<T>> InvokeAsync<T>(this IDispatcher dispatcher, Func<CancellationToken, Task<T>> asyncAction)
        {
            var taskCompletionSource = new TaskCompletionSource<Task<T>>();
            dispatcher.BeginInvoke(ct =>
            {
                try
                {
                    var result = asyncAction(ct);
                    taskCompletionSource.SetResult(result);
                }
                catch (Exception ex)
                {
                    taskCompletionSource.TrySetException(ex);
                }
            });
            return taskCompletionSource.Task;
        }

        /// <summary>
        ///     asyncAction is awaited internally.
        /// </summary>
        /// <param name="asyncAction"></param>
        public static Task<T> InvokeExAsync<T>(this IDispatcher dispatcher, Func<CancellationToken, Task<T>> asyncAction)
        {
            var taskCompletionSource = new TaskCompletionSource<T>();
            dispatcher.BeginInvokeEx(async ct =>
            {
                try
                {
                    var result = await asyncAction(ct);
                    taskCompletionSource.SetResult(result);
                }
                catch (Exception ex)
                {
                    taskCompletionSource.TrySetException(ex);
                }
            });
            return taskCompletionSource.Task;
        }
    }
}
