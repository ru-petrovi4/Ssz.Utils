using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Ssz.Utils
{
    public class DispatcherSynchronizationContext : SynchronizationContext
    {        
        #region construction and destruction

        public DispatcherSynchronizationContext(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        #endregion

        #region public functions

        /// <summary>
        /// Sends a message and does not wait
        /// </summary>
        /// <param name="callback">The delegate to execute</param>
        /// <param name="state">The state associated with the message</param>
        public override void Post(SendOrPostCallback callback, object? state)
        {
            _dispatcher.BeginInvoke(ct => callback(state));
        }

        /// <summary>
        /// Sends a message and waits for completion
        /// </summary>
        /// <param name="callback">The delegate to execute</param>
        /// <param name="state">The state associated with the message</param>
        public override void Send(SendOrPostCallback callback, object? state)
        {
            var ev = new ManualResetEventSlim(false);
            try
            {
                _dispatcher.BeginInvoke(ct =>
                {
                    callback(state);
                    ev.Set();
                });
                ev.Wait();
            }
            finally
            {
                ev.Dispose();
            }
        }

        #endregion

        #region private fields

        private readonly IDispatcher _dispatcher;

        #endregion
    }
}
