using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using Ssz.Utils;
using Ssz.DataGrpc.Server.Core.Lists;
using Xi.Common.Support;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;
using Ssz.DataGrpc.Server.Data;
using Microsoft.Extensions.Logging;

namespace Ssz.DataGrpc.Server.Core.Context
{
    /// <summary>
    ///   This is the Context Manager for the reference implementation of an Express Interface (Xi) Server.
    ///   The reference implantation provides some base classes that allow for the implantation of 
    ///   a Xi Server with some common or standardized behavior.
    ///   This class manages the active contexts (sessions) and provides lookup, timeout and caching support.
    /// </summary>
    public class ServerContextsManager
    {
        #region construction and destruction

        public ServerContextsManager(ILogger<ResourceManagementService> logger)
        {
            _logger = logger;
        }

        #endregion

        #region public functions

        /// <summary>
        /// </summary>
        public void Start()
        {
            lock (_syncRoot)
            {
                if (_started) return;

                _started = true;
            }
        }

        /// <summary>
        /// </summary>
        public void Stop(ServerStatus serverStatus, string reason)
        {
            lock (_syncRoot)
            {
                if (!_started) return;

                foreach (ServerContext context in ActiveContexts)
                {
                    context.OnAbort(serverStatus, reason);
                    context.OnConclude();
                }

                _concludedServerContexts.ClearAndSetCapacity(ActiveContexts.Count);

                foreach (ServerContext context in ActiveContexts)
                {
                    _concludedServerContexts.Add(context);
                    RaiseContextChanged(new ContextCollectionChangedEventArgs<ServerContext>(null, context));
                }

                _serverContextsDictionary.Clear();                

                _started = false;
            }

            foreach (ServerContext context in _concludedServerContexts)
            {
                context.Dispose();
            }
            _concludedServerContexts.Clear();
        }

        /// <summary>
        ///   This method locates a context object using the context ID.
        ///   It allows security checks to be disabled
        /// </summary>
        /// <param name = "contextId">Context ID</param>
        /// <param name = "validate">Whether to validate the context credentials</param>
        /// <returns>ServerContext object</returns>
        public ServerContext? LookupServerContext(string contextId, bool validate = false)
        {
            OperationContext ctx = OperationContext.Current;
            if (ctx == null) return null;

            ServerContext context = null;

            if (validate)
            {
                if (ctx.ServiceSecurityContext != null && ctx.ServiceSecurityContext.PrimaryIdentity != null)
                {
                    lock (_syncRoot)
                    {
                        if (_started) _serverContextsDictionary.TryGetValue(contextId, out context);
                    }
                    if (context != null)
                    {
                        if (context.ValidateSecurity(ctx))
                        {
                            if (context.Concluded) throw RpcExceptionHelper.Create("Context is concluded.");
                            context.LastAccessUtc = DateTime.UtcNow;
                        }
                    }
                }
            }
            else // validate == false
            {
                lock (_syncRoot)
                {
                    if (_started) _serverContextsDictionary.TryGetValue(contextId, out context);
                }
                if (context != null)
                {
                    if (context.Concluded) throw RpcExceptionHelper.Create("Context is concluded.");
                    context.LastAccessUtc = DateTime.UtcNow;
                }
            }

            if (context == null) ChannelCloser.Close(ctx.Channel);

            return context;
        }

        public void Cleanup(CancellationToken token)
        {
            if (token.IsCancellationRequested) return;

            lock (_syncRoot)
            {
                DateTime nowUtc = DateTime.UtcNow;

                _concludedServerContexts.ClearAndSetCapacity(ActiveContexts.Count);
                _callbackServerContexts.ClearAndSetCapacity(ActiveContexts.Count);

                foreach (ServerContext context in ActiveContexts)
                {
                    if (context.Concluded)
                    {
                        _concludedServerContexts.Add(context);
                        RaiseContextChanged(new ContextCollectionChangedEventArgs<ServerContext>(null, context));
                        continue;
                    }

                    if (context.CheckTimeout(nowUtc))
                    {
                        context.OnConclude();
                        _concludedServerContexts.Add(context);
                        RaiseContextChanged(new ContextCollectionChangedEventArgs<ServerContext>(null, context));
                        Logger.Warning("Timeout out Context {0}", context.Id);
                        continue;
                    }

                    if (context.CallbackEndpointOpen &&
                        ((nowUtc - context.LastCallbackTimeUtc) > context.CallbackRate))
                        _callbackServerContexts.Add(context);
                }

                if (_concludedServerContexts.Count > 0)
                {
                    if (_concludedServerContexts.Count == ActiveContexts.Count)
                        _serverContextsDictionary.Clear();
                    else
                        _concludedServerContexts.ForEach(context => _serverContextsDictionary.Remove(context.Id));                    
                }
            }

            foreach (ServerContext context in _concludedServerContexts)
            {
                context.Dispose();
            }
            _concludedServerContexts.Clear();

            foreach (ServerContext context in _callbackServerContexts)
            {
                context.OnInformationReport(0, null); // the keep-alive callback                            
            }
            _callbackServerContexts.Clear();
        }

        public event EventHandler<ServerContextsCollectionChangedEventArgs> ServerContextsCollectionChanged;

        //public bool IsServerShutdown { get; private set; }
        public string ShutdownReason { get; private set; }

        /// <summary>
        ///  Active Contexts.
        /// </summary>
        public ServerContext[] ActiveContexts
        {
            get
            {
                lock (_syncRoot)
                {
                    return _serverContextsDictionary.Values.ToArray();
                }
            }
        }

        #endregion

        #region internal functions

        /// <summary>
        ///   This method adds a new context to the manager's collection.  The assigned
        ///   Context.LocalId is used to store the context object.
        /// </summary>
        /// <param name = "context">Context to add</param>
        internal void AddContext(ServerContext context)
        {
            lock (_syncRoot)
            {
                if (!_started) throw RpcExceptionHelper.Create(XiFaultCodes.E_SERVER_SHUTDOWN);

                _serverContextsDictionary.Add(context.Id, context);                

                RaiseContextChanged(new ContextCollectionChangedEventArgs<ServerContext>(context, null));
            }
        }

        #endregion

        #region private functions

        private void RaiseContextChanged(ContextCollectionChangedEventArgs<ServerContext> e)
        {
            EventHandler<ContextCollectionChangedEventArgs<ServerContext>> changed = ServerContextsCollectionChanged;
            if (changed != null)
            {
                try
                {
                    changed(null, e);
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        #endregion

        #region private fields

        private readonly ILogger<ResourceManagementService> _logger;

        private bool _started;

        /// <summary>
        ///   <para>The sync root of the state of this instance, except: ServerContext.</para>        
        /// </summary>
        private readonly object _syncRoot = new object();

        /// <summary>
        ///  Contexts.
        /// </summary>
        private readonly Dictionary<string, ServerContext> _serverContextsDictionary = new Dictionary<string, ServerContext>(20);

        /// <summary>
        ///   Contexts that have been concluded.
        /// </summary>
        private readonly List<ServerContext> _concludedServerContexts = new List<ServerContext>(20);

        /// <summary>
        ///   Contexts that need to have a keep-alive callback sent.
        /// </summary>
        private readonly List<ServerContext> _callbackServerContexts = new List<ServerContext>(20);

        #endregion
    }
}