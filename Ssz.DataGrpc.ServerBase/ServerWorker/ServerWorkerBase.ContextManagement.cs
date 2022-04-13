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

namespace Ssz.DataGrpc.ServerBase
{
    public abstract partial class ServerWorkerBase : IDispatcher
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

        #endregion

        #region internal functions

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serverContext"></param>
        internal void AddServerContext(ServerContext serverContext)
        {
            _serverContextsDictionary.Add(serverContext.ContextId, serverContext);

            ServerContextAddedOrRemoved(serverContext, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serverContext"></param>
        internal void RemoveServerContext(ServerContext serverContext)
        {
            _serverContextsDictionary.Remove(serverContext.ContextId);

            ServerContextAddedOrRemoved(serverContext, false);
        }

        #endregion

        #region protected functions        

        protected async Task ServerContextsAbortAsync(ServerContext[] serverContexts)
        {
            var tasks = new List<Task>(serverContexts.Length);
            foreach (ServerContext serverContext in serverContexts)
            {
                ServerContextAddedOrRemoved(serverContext, false);

                ValueTask valueTask = serverContext.DisposeAsync();
                if (!valueTask.IsCompletedSuccessfully) // 'if' here only for optimizations purposes.
                    tasks.Add(valueTask.AsTask());
            }
            await Task.WhenAll(tasks);
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
