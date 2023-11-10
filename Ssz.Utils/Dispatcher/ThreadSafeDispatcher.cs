using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    public class ThreadSafeDispatcher : IDispatcher
    {
        #region construction and destruction

        public ThreadSafeDispatcher(ILogger? logger = null)
        {
            _logger = logger;
        }

        #endregion

        #region public functions       

        public void BeginInvoke(Action<CancellationToken> action)
        {
            Action<CancellationToken>? actions2;
            Action<CancellationToken>? actions = _actions;
            do
            {
                actions2 = actions;
                Action<CancellationToken>? actions3 = (Action<CancellationToken>?)Delegate.Combine(actions2, action);
                actions = Interlocked.CompareExchange(ref _actions, actions3, actions2);
            }
            while (actions != actions2);
        }

        public void BeginAsyncInvoke(Func<CancellationToken, Task> asyncAction)
        {
            Func<CancellationToken, Task>? asyncActions2;
            Func<CancellationToken, Task>? asyncActions = _asyncActions;
            do
            {
                asyncActions2 = asyncActions;
                Func<CancellationToken, Task>? asyncActions3 = (Func<CancellationToken, Task>?)Delegate.Combine(asyncActions2, asyncAction);
                asyncActions = Interlocked.CompareExchange(ref _asyncActions, asyncActions3, asyncActions2);
            }
            while (asyncActions != asyncActions2);
        }

        /// <summary>
        ///     Returns Actions count
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<int> InvokeActionsInQueueAsync(CancellationToken cancellationToken)
        {
            int result = 0;
            var actions = Interlocked.Exchange(ref _actions, null);
            if (actions is not null)
            {
                var actionsInvocationList = actions.GetInvocationList();
                foreach (Action<CancellationToken> action in actionsInvocationList)
                {
                    if (cancellationToken.IsCancellationRequested) 
                        return result;
                    try
                    {
                        action.Invoke(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, @"action.Invoke(cancellationToken) Error.");
                    }                    
                    result += 1;
                }                
            }
            var asyncActions = Interlocked.Exchange(ref _asyncActions, null);
            if (asyncActions is not null)
            {
                var asyncActionsInvocationList = asyncActions.GetInvocationList();
                foreach (Func<CancellationToken, Task> asyncAction in asyncActionsInvocationList)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return result;
                    try
                    {
                        await asyncAction.Invoke(cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, @"await asyncAction.Invoke(cancellationToken) Error.");
                    }                    
                    result += 1;
                }                
            }                    
            return result;
        }

        #endregion

        #region private fields

        private readonly ILogger? _logger;

        private Action<CancellationToken>? _actions;

        private Func<CancellationToken, Task>? _asyncActions;

        #endregion
    }
}
