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

        public void BeginInvoke(Action<CancellationToken> action)
        {
            Action<CancellationToken> actions2;
            Action<CancellationToken> actions = _actions;
            do
            {
                actions2 = actions;
                Action<CancellationToken> actions3 = (Action<CancellationToken>)Delegate.Combine(actions2, action);
                actions = Interlocked.CompareExchange(ref _actions, actions3, actions2);
            }
            while (actions != actions2);
        }

        /// <summary>
        ///     Returns Actions count
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public int InvokeActionsInQueue(CancellationToken cancellationToken)
        {
            var actions = Interlocked.Exchange(ref _actions, delegate { });
            actions.Invoke(cancellationToken);
            return actions.GetInvocationList().Length;
        }

        #endregion

        #region private fields

        private Action<CancellationToken> _actions = delegate { };

        #endregion
    }
}
