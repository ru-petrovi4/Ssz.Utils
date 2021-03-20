using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.ServiceModel;
using Ssz.Utils;
using Ssz.DataGrpc.Server.Core.Lists;
using Xi.Common.Support.Extensions;
using Xi.Contracts.Data;

namespace Ssz.DataGrpc.Server.Core.Context
{
    /// <summary>
    ///   This class is intended to be used as the base class for the server-side context of a client
    ///   connection.  An instance of this class is instantiated for each client context established 
    ///   by IResourceManagement.Initiate(...). <see cref = "ResourceManagement.Initiate" />
    /// </summary>
    /// <typeparam name = "TListRoot">
    ///   The concrete type used for the Xi Lists managed by this Context.  
    ///   This is commonly specified as "ListRoot" as a context will generally 
    ///   manage lists of multiple types.
    /// </typeparam>
    public partial class ServerContext : IDisposable       
    {
        #region construction and destruction

        /// <summary>
        ///   Constructs a new instance of the <see cref = "ServerContext" /> class.
        /// </summary>
        protected ServerContext()
        {
            LastAccessUtc = DateTime.UtcNow;
        }

        /// <summary>
        ///   This is the implementation of the IDisposable.Dispose method.  The client 
        ///   application should invoke this method when this instance is no longer needed.
        /// </summary>
        public void Dispose()
        {
            using (SyncRoot.Enter())
            {
                Dispose(true);
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///   This method is invoked when the IDisposable.Dispose or Finalize actions are 
        ///   requested.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                OnReleaseResources();

                // Dispose of the lists.
                foreach (TListRoot list in _lists)
                {
                    list.Dispose();
                }

                _listManager.Clear();
                _lists.Clear();

                CloseEndpointConnections();
                // Release and Dispose managed resources.			
            }

            Identity = null;
            _localeIds = null;
            _listManager = null;
            _lists = null;
            _connectedResourceManagementEndpoint = null;
            _endpoints = null;

            // Release unmanaged resources.
            // Set large fields to null.			
            _disposed = true;
        }

        /// <summary>
        ///   Invoked by the .NET Framework while doing heap managment (Finalize).
        /// </summary>
        ~ServerContext()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        /// <summary>
        ///   Gets the remote address for the current operations context.
        /// </summary>
        public static string GetRemoteIpAddress()
        {
            string clientIpAddress = null;
            int clientPort = -1;
            OperationContext.Current.GetRemoteAddress(out clientIpAddress, out clientPort);
            return clientIpAddress;
        }

        /// <summary>
        ///   The endpoint item for the connected ResourceManagement endpoint.
        ///   Preconditions: Context must be NOT added to _activeContexts.
        /// </summary>
        public void CreateConnectedResourceManagementEndpoint(EndpointDefinition epDefinition)
        {
            using (SyncRoot.Enter())
            {
                _connectedResourceManagementEndpoint = new EndpointEntry<TListRoot>(epDefinition);
                _connectedResourceManagementEndpoint.IsOpen = true;
                _connectedResourceManagementEndpoint.WcfChannel = OperationContext.Current.Channel;
                _connectedResourceManagementEndpoint.SessionId = OperationContext.Current.SessionId;
                _connectedResourceManagementEndpoint.ClientIpAddress = GetRemoteIpAddress();
            }
        }

        /// <summary>
        ///   This method is used to set the list of valid or supported 
        ///   LocalId’s for the server.  This list is then used in the 
        ///   validation of the LocalId.
        ///   Preconditions: Context must be NOT added to _activeContexts.
        /// </summary>
        /// <param name = "localIds"></param>
        public void SetSupportedLocaleIds(List<uint> localeIds)
        {
            using (SyncRoot.Enter())
            {
                // Do not allow an empty list.
                if ((localeIds != null) && (localeIds.Count > 0)) _localeIds = localeIds;
            }
        }

        /// <summary>
        ///   Invoke this method to set the valid endpoint for this context.
        ///   Preconditions: Context must be NOT added to _activeContexts.
        /// </summary>
        /// <param name = "listEndpointDefinitions"></param>
        public virtual void OnInitiate(List<EndpointDefinition> listEndpointDefinitions)
        {
            using (SyncRoot.Enter())
            {
                foreach (EndpointDefinition ed in listEndpointDefinitions)
                {
                    _endpoints.Add(ed.EndpointId, new EndpointEntry<TListRoot>(ed));
                }
            }
        }

        /// <summary>
        ///   This method is used to close a context.  It deletes all lists and closes 
        ///   supporting resources held by the context implementation.
        /// </summary>
        public void OnConclude()
        {
            _concluded = true;
        }

        /// <summary>
        ///   This validates the security credentials of the user each time the
        ///   context is retrieved.  It should ensure the Paged credentials match
        ///   the current transport security credentials.
        /// </summary>
        /// <param name = "ctx">WCF operation context currently active</param>
        /// <returns>true/false</returns>
        public bool ValidateSecurity(OperationContext ctx)
        {
            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                // Validate context ID
                if (ctx == null) return false;

                // Not the same transport connection?
                if (ctx.SessionId != TransportSessionId) return false;

                // No security?
                if (ctx.ServiceSecurityContext == null) return false;

                // Different user name?
                if (ctx.ServiceSecurityContext.PrimaryIdentity.Name != Identity.Name) return false;

                return OnValidateSecurity(ctx);
            }
        }

        /// <summary>
        ///   User's locale, negotiated when context was created, zero for default.
        /// </summary>
        public uint LocaleId
        {
            get { return _localeId; }
            protected set { _localeId = value; }
        }

        /// <summary>
        ///   Indicates when TRUE that the Data Access capabilities of the server are enabled
        /// </summary>
        public abstract bool IsAccessibleDataAccess { get; }

        /// <summary>
        ///   Indicates when TRUE that the Alarms and Events capabilities of the server are enabled
        /// </summary>
        public abstract bool IsAccessibleAlarmsAndEvents { get; }

        /// <summary>
        ///   Indicates when TRUE that the Journal Data Access capabilities of the server are enabled
        /// </summary>
        public abstract bool IsAccessibleJournalDataAccess { get; }

        /// <summary>
        ///   Indicates when TRUE that the Journal Alarms and Events capabilities of the server are enabled
        /// </summary>
        public abstract bool IsAccessibleJournalAlarmsAndEvents { get; }

        /// <summary>
        ///   The negotiated timeout in milliseconds from the Resource Discover Initiate
        /// </summary>
        public TimeSpan ContextTimeout
        {
            get { return _contextTimeout; }
            protected set { _contextTimeout = value; }
        }

        /// <summary>
        ///   Indicates, when TRUE, that OnConclude has been called on this context
        /// </summary>
        public bool Concluded
        {
            get { return _concluded; }
        }

        /// <summary>
        ///   Context identifier (must be unique).
        /// </summary>
        public string Id { get; protected set; }

        /// <summary>
        ///   The key to be used when re-initiating the context.
        /// </summary>
        public string ReInitiateKey { get; protected set; }

        /// <summary>
        ///   Transport session identifier (may be null or not present).
        /// </summary>
        public string TransportSessionId { get; set; }

        /// <summary>
        ///   Application name handed to server when context was created.
        /// </summary>
        public string ApplicationName { get; protected set; }

        /// <summary>
        ///   Workstation name handed to server when context was created.
        /// </summary>
        public string WorkstationName { get; protected set; }

        /// <summary>
        ///   User identity (may be null).
        /// </summary>
        public IIdentity Identity { get; protected set; }

        /// <summary>
        ///   The context options set for the context by the Initiate() method.
        /// </summary>
        public uint NegotiatedContextOptions { get; protected set; }

        /// <summary>
        ///   The number of accessible wrapped servers as defined by the context options .
        /// </summary>
        public uint NumberOfWrappedServersForThisContext { get; protected set; }

        public List<TListRoot> Lists
        {
            get
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                return _lists;
            }
        }

        #endregion

        #region internal functions

        /// <summary>
        ///   This method is invoked to determine if this context 
        ///   instance has timed out.  And thus should be disposed.
        ///   Preconditions: _activeContexts must be locked.
        /// </summary>
        /// <param name = "nowUtc"></param>
        /// <returns></returns>
        internal bool CheckTimeout(DateTime nowUtc)
        {
            return (nowUtc - LastAccessUtc) > ContextTimeout;
        }

        /// <summary>
        ///   The last time the context was accessed.
        /// </summary>
        internal DateTime LastAccessUtc { get; set; }

        #endregion

        #region protected functions

        /// <summary>
        ///   This method is used to validate the selected LocalId.  
        ///   It will default to 0x409 (US English) if not in the 
        ///   supported list.  This method may be overridden if 
        ///   an alternative validation is desired.
        /// </summary>
        /// <param name = "localeId"></param>
        /// <returns></returns>
        protected uint ValidateLocaleId(uint localeId)
        {
            if (_localeIds.Contains(localeId)) return localeId;
            return 0x0409u;
        }

        /// <summary>
        ///   The implementation class may override this method to 
        ///   validate an acceptable timeout for the server instance.
        /// </summary>
        /// <param name = "value"></param>
        /// <returns></returns>
        protected TimeSpan ValidateContextTimeout(TimeSpan value)
        {
            if (value == TimeSpan.Zero) return new TimeSpan(0, 10, 0); // Default is 10 Minutes (a long time)
            if (value < new TimeSpan(0, 0, 9)) return new TimeSpan(0, 0, 9); // The minimum timeout is nine seconds.
            if (value > new TimeSpan(0, 30, 0)) return new TimeSpan(0, 30, 0); // The maximum timeout is 30 minutes.
            return value;
        }

        /// <summary>
        ///   This method is invoked to add a list to the specified context in the 
        ///   server implementation
        /// </summary>
        /// <param name = "list">The Xi List to add to the context.</param>
        /// <returns></returns>
        protected ListAttributes AddList(TListRoot list)
        {
            list.ServerId = _listManager.Add(list);

            _lists = _listManager.ToList();

            return list.ListAttributes;
        }

        /// <summary>
        ///   Override this method in an implementation subclass to release resources 
        ///   held by the server, such as connections to wrapped servers.
        /// </summary>
        protected abstract void OnReleaseResources();

        /// <summary>
        ///   This abstract method is to be overridden to provide additional context
        ///   security validation.
        /// </summary>
        /// <param name = "ctx">WCF operation context currently active</param>
        /// <returns>true/false</returns>
        protected virtual bool OnValidateSecurity(OperationContext ctx)
        {
            return true;
        }

        protected bool Disposed
        {
            get { return _disposed; }
        }

        /// <summary>
        ///   <para>The sync root of the state of this instance, except: TListRoot.</para>
        /// </summary>
        protected readonly LeveledLock SyncRoot = new LeveledLock(1200, false, "ServerContext.SyncRoot");

        #endregion

        #region private fields

        private volatile bool _disposed;

        private uint _localeId = 0x0409u;

        private List<uint> _localeIds = new List<uint>
            {
                0x0409u
            };

        /// <summary>
        ///   The collection of lists for this context.
        /// </summary>
        private ObjectManager<TListRoot> _listManager = new ObjectManager<TListRoot>(20);

        /// <summary>
        ///   The endpoint item for the connected ResourceManagement endpoint.
        /// </summary>
        private EndpointEntry<TListRoot> _connectedResourceManagementEndpoint;

        private List<TListRoot> _lists = new List<TListRoot>();

        /// <summary>
        ///   The collection of Endpoints for this context.
        ///   The key for this dictionary is the Endpoint LocalId a GUID created by the Xi Server.
        /// </summary>
        private Dictionary<string, EndpointEntry<TListRoot>> _endpoints =
            new Dictionary<string, EndpointEntry<TListRoot>>();

        private TimeSpan _contextTimeout = new TimeSpan(0, 10, 0);

        private volatile bool _concluded;

        #endregion
    }
}