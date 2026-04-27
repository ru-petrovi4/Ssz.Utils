using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Utils.Avalonia;

public class WrapperDispatcher : IDispatcher, IDisposable
{
    #region construction and destruction

    public WrapperDispatcher(global::Avalonia.Threading.Dispatcher dispatcher)
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
    ~WrapperDispatcher()
    {
        Dispose(false);
    }

    #endregion

    #region public functions

    public bool Disposed { get; private set; }        

    public void BeginInvoke(Action<CancellationToken> action)
    {
        if (Disposed) return;

        _dispatcher.Post(() => action(CancellationToken.None));
    }

    public void BeginInvoke(Func<CancellationToken, Task> asyncAction)
    {
        if (Disposed) return;

        _dispatcher.Post(() => asyncAction(CancellationToken.None));
    }

    public void BeginInvokeEx(Func<CancellationToken, Task> asyncAction)
    {
        if (Disposed) return;

        _dispatcher.Post(() => asyncAction(CancellationToken.None));
    }

    #endregion

    #region private fields

    private global::Avalonia.Threading.Dispatcher _dispatcher;

    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    #endregion
}
