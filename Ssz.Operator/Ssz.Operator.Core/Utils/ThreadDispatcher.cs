#nullable disable

using System;
using System.Security.Permissions;
using System.Threading;
using System.Windows.Threading;

namespace Ssz.Operator.Core.Utils
{
    public static class DispatcherHelper
    {
        #region public functions
        
        public static void CurrentDispatcherDoEvents()
        {
            Dispatcher currentDispatcher = Dispatcher.CurrentDispatcher;
            if (currentDispatcher == null) return;
            var frame = new DispatcherFrame();
            currentDispatcher.BeginInvoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(ExitFrame), frame);
            Dispatcher.PushFrame(frame);
        }

        #endregion

        #region private functions

        private static object ExitFrame(object frame)
        {
            ((DispatcherFrame)frame).Continue = false;
            return null;
        }

        #endregion
    }

    public class ThreadDispatcher
    {
        #region construction and destruction

        public ThreadDispatcher(ApartmentState apartmentState)
        {
            var thread = new Thread(ThreadFunc);
            // TODO verify
            //thread.SetApartmentState(apartmentState);
            thread.Start();
            _dispatcherReadyEvent.WaitOne();
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
            if (Disposed) return;
            if (disposing)
            {
                // Release and Dispose managed resources.
                try
                {
                    if (_dispatcher != null) _dispatcher.InvokeShutdown();
                }
                catch (Exception)
                {
                }
            }
            // Release unmanaged resources.
            // Set large fields to null.            
            Disposed = true;
        }

        /// <summary>
        ///     Invoked by the .NET Framework while doing heap managment (Finalize).
        /// </summary>
        ~ThreadDispatcher()
        {
            Dispose(false);
        }

        #endregion

        #region public functions        

        public Dispatcher Dispatcher
        {
            get
            {
                if (Disposed) throw new ObjectDisposedException("Object Disposed");
                return _dispatcher;
            }
        }

        #endregion

        #region protected functions

        protected bool Disposed { get; private set; }

        #endregion

        #region private functions        

        private void ThreadFunc()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _dispatcherReadyEvent.Set();
            Dispatcher.Run();
        }

        #endregion

        #region private fields

        private readonly ManualResetEvent _dispatcherReadyEvent = new ManualResetEvent(false);
        private volatile Dispatcher _dispatcher;

        #endregion
    }
}