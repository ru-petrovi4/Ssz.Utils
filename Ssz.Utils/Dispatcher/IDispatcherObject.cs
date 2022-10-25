using System;
using System.Collections.Generic;
using System.Text;

namespace Ssz.Utils.Dispatcher
{
    /// <summary>
    ///     Only the thread that the Dispatcher was created on may access the IDispatcherObject directly. 
    ///     To access a IDispatcherObject from a thread other than the thread the IDispatcherObject was created on,
    ///     call BeginInvoke or BeginAsyncInvoke on the Dispatcher the IDispatcherObject is associated with.
    /// </summary>
    public interface IDispatcherObject
    {
        IDispatcher? Dispatcher { get; }
    }
}
