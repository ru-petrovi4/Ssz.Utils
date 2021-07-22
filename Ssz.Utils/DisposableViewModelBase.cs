using System;
using System.ComponentModel;

namespace Ssz.Utils
{
    /// <summary>
    /// 
    /// </summary>
    public class DisposableViewModelBase : ViewModelBase, IDisposable
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
                ClearPropertyChangedEvent();
            }            

            Disposed = true;
        }

        /// <summary>
        ///     Invoked by the .NET Framework while doing heap managment (Finalize).
        /// </summary>
        ~DisposableViewModelBase()
        {
            Dispose(false);
        }

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        public bool Disposed { get; private set; }

        #endregion
    }
}