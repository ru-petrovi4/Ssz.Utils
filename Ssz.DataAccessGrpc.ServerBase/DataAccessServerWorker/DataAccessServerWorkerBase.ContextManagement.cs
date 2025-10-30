using Grpc.Core;
using Microsoft.Extensions.Logging;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.ServerBase
{
    public abstract partial class DataAccessServerWorkerBase
    {
        #region public functions
        
        public IDataAccessServerContext AddServerContext(ILogger logger,            
            string clientApplicationName,
            string clientWorkstationName,
            uint requestedServerContextTimeoutMs,
            string requestedCultureName,
            string systemNameToConnect,
            CaseInsensitiveOrderedDictionary<string?> contextParams)
        {
            var serverContext = new ServerContext(
                        logger,
                        this,
                        clientApplicationName,
                        clientWorkstationName,
                        requestedServerContextTimeoutMs,
                        requestedCultureName,
                        systemNameToConnect,
                        contextParams
                        );            

            _serverContextsDictionary.Add(serverContext.ContextId, serverContext);

            ServerContextAddedOrRemoved(this, new ServerContextAddedOrRemovedEventArgs { ServerContext = serverContext, Added = true });

            return serverContext;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contextId"></param>
        /// <returns></returns>
        public IDataAccessServerContext LookupServerContext(string contextId)
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

        public IDataAccessServerContext? TryLookupServerContext(string contextId)
        {
            ServerContext? serverContext;
            _serverContextsDictionary.TryGetValue(contextId, out serverContext);            
            return serverContext;
        }        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serverContext"></param>
        public void RemoveServerContext(IDataAccessServerContext serverContext)
        {
            _serverContextsDictionary.Remove(serverContext.ContextId);

            ServerContextAddedOrRemoved(this, new ServerContextAddedOrRemovedEventArgs { ServerContext = serverContext, Added = false });
        }

        #endregion

        #region protected functions  

        protected IReadOnlyDictionary<string, ServerContext> ServerContextsDictionary => _serverContextsDictionary;

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
