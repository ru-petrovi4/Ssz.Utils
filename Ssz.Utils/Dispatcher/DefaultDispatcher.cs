using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    /// <summary>
    ///     Uses SynchronizationContext for doing work.
    /// </summary>
    public class DefaultDispatcher : IDispatcher
    {   
        #region construction and destruction

        public DefaultDispatcher()
        {
            _synchronizationContext = SynchronizationContext.Current;
        }

        #endregion

        #region public functions

        public void BeginExclusiveInvoke(Func<CancellationToken, Task> action)
        {
            _synchronizationContext?.Post(OnBeginAsyncInvoke_Dispatched, action);
        }

        public void BeginInvoke(Action<CancellationToken> action)
        {
            _synchronizationContext?.Post(OnBeginInvoke_Dispatched, action);
        }

        #endregion

        #region private functions

        private async void OnBeginAsyncInvoke_Dispatched(object? state)
        {
            Func<CancellationToken, Task> action = (Func<CancellationToken, Task>)state!;
            await action.Invoke(CancellationToken.None);
        }

        private void OnBeginInvoke_Dispatched(object? state)
        {
            Action<CancellationToken> action = (Action<CancellationToken>)state!;
            action.Invoke(CancellationToken.None);
        }

        #endregion

        #region private fields

        private readonly SynchronizationContext? _synchronizationContext;

        #endregion
    }
}
