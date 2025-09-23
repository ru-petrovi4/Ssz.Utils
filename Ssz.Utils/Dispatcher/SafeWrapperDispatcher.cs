using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Utils
{    
    //public class SafeWrapperDispatcher : IDispatcher
    //{
    //    #region construction and destruction

    //    public SafeWrapperDispatcher(IDispatcher innerDispatcher, ILogger logger)
    //    {
    //        _innerDispatcher = innerDispatcher;
    //        _logger = logger;
    //    }

    //    #endregion

    //    #region public functions

    //    public void BeginInvoke(Action<CancellationToken> action)
    //    {
    //        try
    //        {
    //            innerDispatcher. action(CancellationToken.None);
    //        }
    //        catch
    //        {
    //        }
    //    }

    //    public async void BeginInvokeEx(Func<CancellationToken, Task> action)
    //    {
    //        try
    //        {
    //            await action(CancellationToken.None);
    //        }
    //        catch
    //        {
    //        }
    //    }

    //    #endregion

    //    #region private fields

    //    private readonly IDispatcher _innerDispatcher;
    //    private readonly ILogger _logger;

    //    #endregion
    //}
}