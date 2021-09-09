using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    /// <summary>
    /// 
    /// </summary>
    public class DisposableViewModelBase : ViewModelBase, IDisposable, IAsyncDisposable
    {
        #region construction and destruction

        /// <summary>
        ///     This is the implementation of the IDisposable.Dispose method.  The client
        ///     application should invoke this method when this instance is no longer needed.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        
        public async ValueTask DisposeAsync()
        {
            if (Disposed) return;

            await DisposeAsyncCore();

            Dispose(disposing: false);
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
            GC.SuppressFinalize(this);
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
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
                ClearPropertyChangedEvent();
            }            

            Disposed = true;
        }

        protected virtual ValueTask DisposeAsyncCore()
        {
            ClearPropertyChangedEvent();

            return ValueTask.CompletedTask;
        }

        /// <summary>
        ///     Invoked by the .NET Framework while doing heap managment (Finalize).
        /// </summary>
        ~DisposableViewModelBase()
        {
            Dispose(disposing: false);
        }

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        public bool Disposed { get; private set; }

        #endregion
    }
}