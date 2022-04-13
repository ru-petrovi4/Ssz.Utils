using Microsoft.Extensions.Logging;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.DataGrpc.ServerBase
{
    public abstract partial class ServerWorkerBase : IDispatcher
    {
        #region construction and destruction

        protected ServerWorkerBase(ILogger logger)
        {
            Logger = logger;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     true - added, false - removed
        /// </summary>
        public event Action<ServerContext, bool> ServerContextAddedOrRemoved = delegate { };

        public event Action ShutdownRequested = delegate { };

        /// <summary>
        ///     Invokes Action in working thread with cancellation support.
        /// </summary>
        /// <param name="action"></param>
        public void BeginInvoke(Action<CancellationToken> action)
        {
            ThreadSafeDispatcher.BeginInvoke(action);
        }

        public abstract ServerListRoot NewServerList(ServerContext serverContext, uint listClientAlias, uint listType, CaseInsensitiveDictionary<string> listParams);

        public abstract void Passthrough(ServerContext serverContext, string recipientId, string passthroughName, byte[] dataToSend, out byte[] returnData);

        public abstract void LongrunningPassthrough(ServerContext serverContext, string invokeId, string recipientId, string passthroughName, byte[] dataToSend);

        public abstract void LongrunningPassthroughCancel(ServerContext serverContext, string invokeId);

        public virtual async Task DoWorkAsync(DateTime nowUtc, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;

            ThreadSafeDispatcher.InvokeActionsInQueue(cancellationToken);

            if (cancellationToken.IsCancellationRequested) return;
            
            foreach (ServerContext serverContext in _serverContextsDictionary.Values.ToArray())
            {
                if (cancellationToken.IsCancellationRequested) return;                
                if (nowUtc - serverContext.LastAccessDateTimeUtc > TimeSpan.FromMilliseconds(serverContext.ContextTimeoutMs))
                {
                    // Expired
                    RemoveServerContext(serverContext);
                    await serverContext.DisposeAsync();
                    Logger.LogWarning("Timeout out Context {0}", serverContext.ContextId);
                }
                else
                {
                    serverContext.DoWork(nowUtc, cancellationToken);
                }
            }
        }

        public virtual async Task ShutdownAsync()
        {
            var serverContexts = _serverContextsDictionary.Values.ToArray();
            _serverContextsDictionary.Clear();
            await ServerContextsAbortAsync(serverContexts);
        }

        #endregion        

        #region protected functions

        protected ILogger Logger { get; }

        protected ThreadSafeDispatcher ThreadSafeDispatcher = new();

        /// <summary>
        ///     Raises ShutdownRequested event.
        /// </summary>
        protected virtual void OnShutdownRequested()
        {
            ShutdownRequested();
        }

        /// <summary>
        ///     Force to send all data.
        /// </summary>
        protected void Reset()
        {
            foreach (ServerContext serverContext in _serverContextsDictionary.Values.ToArray())
            {
                serverContext.Reset();
            }
        }

        #endregion
    }
}
