using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

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
    }
}
