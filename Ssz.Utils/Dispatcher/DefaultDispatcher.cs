using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    /// <summary>
    ///     Uses SynchronizationContext for doing work.
    /// </summary>
    public class DefaultDispatcher : IDispatcher
    {   
        #region construction and destruction

        public DefaultDispatcher()
        {
            _synchronizationContext = SynchronizationContext.Current;
        }

        #endregion

        #region public functions

        public async Task<T> InvokeAsync<T>(Func<CancellationToken, T> action)
        {
            var taskCompletionSource = new TaskCompletionSource<T>();
            _synchronizationContext?.Post(state =>
            {
                try
                {                    
                    var result = action(CancellationToken.None);
                    taskCompletionSource.SetResult(result);
                }
                catch (Exception ex)
                {
                    taskCompletionSource.SetException(ex);
                }
            }, null);
            return await taskCompletionSource.Task;
        }

        public async Task<T> InvokeExAsync<T>(Func<CancellationToken, Task<T>> action)
        {
            var taskCompletionSource = new TaskCompletionSource<T>();
            _synchronizationContext?.Post(async state =>
            {
                try
                {
                    var result = await action(CancellationToken.None);
                    taskCompletionSource.SetResult(result);
                }
                catch (Exception ex)
                {
                    taskCompletionSource.SetException(ex);
                }
            }, null);
            return await taskCompletionSource.Task;
        }

        public void BeginInvoke(Action<CancellationToken> action)
        {
            _synchronizationContext?.Post(state =>
            {
                try
                {
                    action(CancellationToken.None);                    
                }
                catch
                {                    
                }
            }, null);
        }

        public void BeginInvokeEx(Func<CancellationToken, Task> action)
        {
            _synchronizationContext?.Post(async state =>
            {
                try
                {
                    await action(CancellationToken.None);                    
                }
                catch
                {                    
                }
            }, null);
        }        

        #endregion        

        #region private fields

        private readonly SynchronizationContext? _synchronizationContext;

        #endregion
    }
}
