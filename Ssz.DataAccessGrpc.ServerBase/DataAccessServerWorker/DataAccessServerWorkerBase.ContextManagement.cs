using Grpc.Core;
using Microsoft.Extensions.Logging;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.ServerBase
{
    public abstract partial class DataAccessServerWorkerBase
    {
        #region public functions
        
        public ServerContext AddServerContext(
            ILogger logger,            
            string clientApplicationName,
            string clientWorkstationName,
            uint requestedServerContextTimeoutMs,
            string requestedCultureName,
            string systemNameToConnect,
            CaseInsensitiveOrderedDictionary<string?> contextParams,
            ClaimsPrincipal? user)
        {
            var serverContext = new ServerContext(
                        logger,
                        this,
                        clientApplicationName,
                        clientWorkstationName,
                        requestedServerContextTimeoutMs,
                        requestedCultureName,
                        systemNameToConnect,
                        contextParams,
                        user
                        );

            _serverContexts_ImmutableDictionary = _serverContexts_ImmutableDictionary.Add(serverContext.ContextId, serverContext);

            ServerContextAddedOrRemoved(this, new ServerContextAddedOrRemovedEventArgs { ServerContext = serverContext, Added = true });

            return serverContext;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contextId"></param>
        /// <returns></returns>
        public ServerContext LookupServerContext(string contextId, ServerCallContext? callContext)
        {
            ServerContext? serverContext;
            _serverContexts_ImmutableDictionary.TryGetValue(contextId, out serverContext);
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

                throw new RpcException(new Status(StatusCode.PermissionDenied, "Context belongs to another user."));
            }
            
            if (!serverContext.UserIsValid(callContext?.GetHttpContext()?.User))
                throw new RpcException(new Status(StatusCode.PermissionDenied, @"Context belongs to another user."));

            return serverContext;
        }

        public ServerContext? TryLookupServerContext_ThreadSafe(string contextId, ServerCallContext? callContext)
        {
            ServerContext? serverContext;
            _serverContexts_ImmutableDictionary.TryGetValue(contextId, out serverContext);

            if (serverContext is not null && !serverContext.UserIsValid(callContext?.GetHttpContext()?.User))
                serverContext = null;

            return serverContext;
        }        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serverContext"></param>
        public void RemoveServerContext(IDataAccessServerContext serverContext)
        {
            _serverContexts_ImmutableDictionary = _serverContexts_ImmutableDictionary.Remove(serverContext.ContextId);

            ServerContextAddedOrRemoved(this, new ServerContextAddedOrRemovedEventArgs { ServerContext = serverContext, Added = false });
        }

        IDataAccessServerContext IDataAccessServerWorker.AddServerContext(ILogger logger, string clientApplicationName, string clientWorkstationName, uint requestedServerContextTimeoutMs, string requestedCultureName, string systemNameToConnect, CaseInsensitiveOrderedDictionary<string?> contextParams)
        {
            return AddServerContext(logger, clientApplicationName, clientWorkstationName, requestedServerContextTimeoutMs, requestedCultureName, systemNameToConnect, contextParams, null);
        }

        IDataAccessServerContext IDataAccessServerWorker.LookupServerContext(string contextId)
        {
            return LookupServerContext(contextId, null);
        }

        IDataAccessServerContext? IDataAccessServerWorker.TryLookupServerContext_ThreadSafe(string contextId)
        {
            return TryLookupServerContext_ThreadSafe(contextId, null);
        }        

        #endregion

        #region protected functions  

        protected ImmutableDictionary<string, ServerContext> ServerContexts_ImmutableDictionary => _serverContexts_ImmutableDictionary;

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
        private ImmutableDictionary<string, ServerContext> _serverContexts_ImmutableDictionary = ImmutableDictionary.Create<string, ServerContext>();

        #endregion
    }
}
