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
        Task<T> InvokeAsync<T>(Func<CancellationToken, T> action);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        Task<T> InvokeExAsync<T>(Func<CancellationToken, Task<T>> action);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        void BeginInvoke(Action<CancellationToken> action);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        void BeginInvokeEx(Func<CancellationToken, Task> action);
    }
}
