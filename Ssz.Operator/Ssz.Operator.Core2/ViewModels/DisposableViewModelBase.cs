﻿using CommunityToolkit.Mvvm.ComponentModel;
using Ssz.Operator.Play.ViewModels;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Ssz.Operator.Play.ViewModels
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
            if (IsDisposed) return;

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
            if (IsDisposed) return;

            if (disposing)
            {
                ClearPropertyChangedEvent();
            }            

            IsDisposed = true;
        }

        protected virtual ValueTask DisposeAsyncCore()
        {
            ClearPropertyChangedEvent();

#if NET5_0_OR_GREATER
            return ValueTask.CompletedTask;
#else
            return new ValueTask();
#endif            

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
        public bool IsDisposed { get; private set; }

#endregion
    }
}