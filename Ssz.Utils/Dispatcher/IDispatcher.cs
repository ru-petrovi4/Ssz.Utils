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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        void BeginInvoke(Func<CancellationToken, Task> action);
    }
}
