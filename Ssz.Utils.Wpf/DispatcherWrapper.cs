using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Ssz.Utils.Wpf
{   
    public class DispatcherWrapper : IDispatcher, IDisposable
    {
        #region construction and destruction

        public DispatcherWrapper(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

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
                _cancellationTokenSource.Cancel();
            }

            Disposed = true;
        }

        /// <summary>
        ///     Invoked by the .NET Framework while doing heap managment (Finalize).
        /// </summary>
        ~DispatcherWrapper()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public bool Disposed { get; private set; }

        public void BeginInvoke(Action<CancellationToken> action)
        {
            if (Disposed) return;

            _dispatcher.BeginInvoke(action, _cancellationTokenSource.Token);
        }

        public void BeginAsyncInvoke(Func<CancellationToken, Task> action)
        {
            if (Disposed) return;

            _dispatcher.BeginInvoke(action, _cancellationTokenSource.Token);
        }

        #endregion

        #region private fields

        private Dispatcher _dispatcher;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        #endregion
    }
}
