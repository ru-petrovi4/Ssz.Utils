using Microsoft.Extensions.Logging;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Ssz.Utils.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.ServerBase
{
    public abstract partial class DataAccessServerWorkerBase : IDataAccessServerWorker
    {
        #region construction and destruction

        protected DataAccessServerWorkerBase(            
            ILogger logger)
        {            
            Logger = logger;           
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Dispacther for worker.
        /// </summary>
        public ThreadSafeDispatcher ThreadSafeDispatcher { get; } = new();

        /// <summary>
        ///     true - added, false - removed
        /// </summary>
        public event EventHandler<ServerContextAddedOrRemovedEventArgs> ServerContextAddedOrRemoved = delegate { };

        public event EventHandler ShutdownRequested = delegate { };        

        public abstract ServerListRoot NewServerList(ServerContext serverContext, uint listClientAlias, uint listType, CaseInsensitiveOrderedDictionary<string?> listParams);

        public abstract Task<ReadOnlyMemory<byte>> PassthroughAsync(ServerContext serverContext, string recipientPath, string passthroughName, ReadOnlyMemory<byte> dataToSend);

        /// <summary>
        ///     Returns JobId
        /// </summary>
        /// <param name="serverContext"></param>
        /// <param name="recipientPath"></param>
        /// <param name="passthroughName"></param>
        /// <param name="dataToSend"></param>
        /// <returns></returns>
        public abstract string LongrunningPassthrough(ServerContext serverContext, string recipientPath, string passthroughName, ReadOnlyMemory<byte> dataToSend);

        public abstract void LongrunningPassthroughCancel(ServerContext serverContext, string jobId);

        public virtual Task InitializeAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     
        /// </summary>
        /// <param name="nowUtc"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<int> DoWorkAsync(DateTime nowUtc, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) 
                return 0;

            int result = await ThreadSafeDispatcher.InvokeActionsInQueueAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested) 
                return 0;
            
            foreach (ServerContext serverContext in _serverContextsDictionary.Values.ToArray())
            {
                if (cancellationToken.IsCancellationRequested) 
                    return result; 
                
                if (nowUtc - serverContext.LastAccessDateTimeUtc > TimeSpan.FromMilliseconds(serverContext.ContextTimeoutMs))
                {
                    // Expired
                    serverContext.IsConcludeCalled = true; // Context is not operational
                    RemoveServerContext(serverContext);
                    serverContext.Dispose();
                    Logger.LogWarning("Timeout out Context {0}", serverContext.ContextId);
                }
                else
                {
                    serverContext.DoWork(nowUtc, cancellationToken);
                }
            }

            return result;
        }

        public virtual async Task CloseAsync()
        {
            var serverContexts = _serverContextsDictionary.Values.ToArray();
            _serverContextsDictionary.Clear();
            await ServerContextsAbortAsync(serverContexts);
        }        

        #endregion        

        #region protected functions

        protected ILogger Logger { get; }        

        /// <summary>
        ///     Raises ShutdownRequested event.
        /// </summary>
        protected virtual void OnShutdownRequested()
        {
            ShutdownRequested(this, EventArgs.Empty);
        }        

        #endregion

        public class ServerContextAddedOrRemovedEventArgs : EventArgs
        {
            public IDataAccessServerContext ServerContext { get; set; } = null!;

            public bool Added { get; set; }
        }

        public class ProcessShutdownRequestException : Exception
        {
        }
    }
}
