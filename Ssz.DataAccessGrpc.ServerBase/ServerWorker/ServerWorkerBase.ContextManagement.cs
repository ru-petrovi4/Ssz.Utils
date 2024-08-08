using Grpc.Core;
using Microsoft.Extensions.Logging;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.ServerBase
{
    public abstract partial class ServerWorkerBase
    {
        #region public functions

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contextId"></param>
        /// <returns></returns>
        public ServerContext LookupServerContext(string contextId)
        {
            ServerContext? serverContext;
            _serverContextsDictionary.TryGetValue(contextId, out serverContext);
            if (serverContext is null)
            {
                if (Logger.IsEnabled(LogLevel.Warning))
                {
                    string callStack = "";
                    var st = new StackTrace();
                    foreach (var f in st.GetFrames())
                    {
                        callStack += "->" + f.GetMethod()?.Name;
                    }
                    //var sf = st.GetFrame(7);
                    //if (sf is not null)
                    //{
                    //    parentMethodName = sf.GetMethod()?.Name ?? "";
                    //}
                    Logger.LogWarning("Invalid contextId: '{0}'; Call Stack: {1}", contextId, callStack);
                }

                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid contextId."));
            }
            return serverContext;
        }

        public ServerContext? TryLookupServerContext(string contextId)
        {
            ServerContext? serverContext;
            _serverContextsDictionary.TryGetValue(contextId, out serverContext);            
            return serverContext;
        }

        #endregion

        #region internal functions

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serverContext"></param>
        internal void AddServerContext(ServerContext serverContext)
        {
            _serverContextsDictionary.Add(serverContext.ContextId, serverContext);

            ServerContextAddedOrRemoved(this, new ServerContextAddedOrRemovedEventArgs { ServerContext = serverContext, Added = true });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serverContext"></param>
        internal void RemoveServerContext(ServerContext serverContext)
        {
            _serverContextsDictionary.Remove(serverContext.ContextId);

            ServerContextAddedOrRemoved(this, new ServerContextAddedOrRemovedEventArgs { ServerContext = serverContext, Added = false });
        }

        #endregion

        #region protected functions  

        protected void ServerContextsAbort(ServerContext[] serverContexts)
        {
            foreach (ServerContext serverContext in serverContexts)
            {
                ServerContextAddedOrRemoved(this, new ServerContextAddedOrRemovedEventArgs { ServerContext = serverContext, Added = false });
                
                serverContext.Dispose();
            }
        }

        protected async Task ServerContextsAbortAsync(ServerContext[] serverContexts)
        {
            foreach (ServerContext serverContext in serverContexts)
            {
                ServerContextAddedOrRemoved(this, new ServerContextAddedOrRemovedEventArgs { ServerContext = serverContext, Added = false });

                await serverContext.DisposeAsync();
            }
        }

        #endregion

        #region private fields

        /// <summary>
        ///  Contexts.
        /// </summary>
        private readonly Dictionary<string, ServerContext> _serverContextsDictionary = new Dictionary<string, ServerContext>(20);

        #endregion
    }
}
