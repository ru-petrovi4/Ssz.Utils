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
        ///     If action is async, it is NOT awaited internally.
        /// </summary>
        /// <param name="action"></param>
        void BeginInvoke(Action<CancellationToken> action);

        /// <summary>
        ///     asyncAction is awaited internally.
        /// </summary>
        /// <param name="action"></param>
        void BeginInvokeEx(Func<CancellationToken, Task> action);
    }
}
