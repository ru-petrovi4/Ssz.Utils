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
        /// 
        /// </summary>
        /// <param name="action"></param>
        void BeginInvoke(Action<CancellationToken> action);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AsyncResult<T> : IDisposable
    {
        #region construction and destruction

        /// <summary>
        ///     This is the implementation of the IDisposable.Dispose method.  The client
        ///     application should invoke this method when this instance is no longer needed.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     This method is invoked when the IDisposable.Dispose or Finalize actions are
        ///     requested.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _semaphoreSlim.Dispose();
            }
        }

        /// <summary>
        ///     Invoked by the .NET Framework while doing heap managment (Finalize).
        /// </summary>
        ~AsyncResult()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public void SetResult(T result)
        {
            _result = result;
            _semaphoreSlim.Release();
        }

        public void SetException(Exception exception)
        {
            _exception = exception;
            _semaphoreSlim.Release();
        }

        public async Task<T> GetResultAsync()
        {
            await _semaphoreSlim.WaitAsync();
            if (_exception != null) throw _exception;
            if (_result == null) throw new OperationCanceledException();
            return _result;
        }

        public async Task<T> GetResultAsync(int millisecondsTimeout)
        {
            await _semaphoreSlim.WaitAsync(millisecondsTimeout);
            if (_exception != null) throw _exception;
            if (_result == null) throw new OperationCanceledException();
            return _result;
        }

        public async Task<T> GetResultAsync(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            await _semaphoreSlim.WaitAsync(millisecondsTimeout, cancellationToken);
            if (_exception != null) throw _exception;
            if (_result == null) throw new OperationCanceledException();
            return _result;
        }

        public async Task<T> GetResultAsync(CancellationToken cancellationToken)
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);
            if (_exception != null) throw _exception;
            if (_result == null) throw new OperationCanceledException();
            return _result;
        }

        public async Task<T> GetResultAsync(TimeSpan timeout)
        {
            await _semaphoreSlim.WaitAsync(timeout);
            if (_exception != null) throw _exception;
            if (_result == null) throw new OperationCanceledException();
            return _result;
        }

        public async Task<T> GetResultAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            await _semaphoreSlim.WaitAsync(timeout, cancellationToken);
            if (_exception != null) throw _exception;
            if (_result == null) throw new OperationCanceledException();
            return _result;
        }

        #endregion        

        #region private fields

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(0, 1);

        private T? _result;

        private Exception? _exception;

        #endregion
    }
}
