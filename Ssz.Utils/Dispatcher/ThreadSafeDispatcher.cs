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
        #region public functions        

        public void BeginInvoke(Func<CancellationToken, Task> action)
        {
            Func<CancellationToken, Task>? actions2;
            Func<CancellationToken, Task>? actions = _actions;
            do
            {
                actions2 = actions;
                Func<CancellationToken, Task>? actions3 = (Func<CancellationToken, Task>?)Delegate.Combine(actions2, action);
                actions = Interlocked.CompareExchange(ref _actions, actions3, actions2);
            }
            while (actions != actions2);
        }

        /// <summary>
        ///     Returns Actions count
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<int> InvokeActionsInQueue(CancellationToken cancellationToken)
        {
            var actions = Interlocked.Exchange(ref _actions, null);
            if (actions is null)
                return 0;
            var actionsInvocationList = actions.GetInvocationList();            
            foreach (Func<CancellationToken, Task> action in actionsInvocationList)
            {                
                await action.Invoke(cancellationToken);
            }            
            return actionsInvocationList.Length;
        }

        #endregion

        #region private fields

        private Func<CancellationToken, Task>? _actions;

        #endregion
    }
}
