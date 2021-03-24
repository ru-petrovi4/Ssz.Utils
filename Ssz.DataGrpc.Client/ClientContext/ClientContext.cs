using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Ssz.Utils;
using Ssz.DataGrpc.Client.Managers;
using Ssz.DataGrpc.Server;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Ssz.DataGrpc.Client.Data;

namespace Ssz.DataGrpc.Client
{

    #region Context Management

    /// <summary>
    ///     This partial class defines the Context Management related aspects of the ClientContext class.  Two
    ///     static Initiate() methods are defined to create and establish a new context with the DataGrpc server,
    ///     one that in which the calling client application supplies the user credentials, and one in which
    ///     the ClientBase calls into the DataGrpc Client Credentials Project.Current for the user credentials when necessary.
    /// </summary>
    public partial class ClientContext
    {
        #region construction and destruction

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="resourceManagementClient"></param>
        /// <param name="applicationName"></param>
        /// <param name="workstationName"></param>
        /// <param name="serverContextTimeoutMs"></param>
        /// <param name="localeId"></param>
        /// <param name="systemNames"></param>
        /// <param name="contextParams"></param>
        public ClientContext(ILogger<DataGrpcProvider> logger,
            ResourceManagement.ResourceManagementClient resourceManagementClient,            
            string applicationName,
            string workstationName,            
            uint serverContextTimeoutMs,
            uint localeId,
            string[] systemNames,
            CaseInsensitiveDictionary<string> contextParams)
        {
            _logger = logger;
            _resourceManagementClient = resourceManagementClient;
            _applicationName = applicationName;
            _workstationName = workstationName;          

            var initiateRequest = new InitiateRequest
            {
                ApplicationName = applicationName,
                WorkstationName = workstationName,
                ContextTimeoutMs = serverContextTimeoutMs,
                LocaleId = localeId,
            };
            initiateRequest.SystemNames.Add(systemNames);
            initiateRequest.ContextParams.Add(contextParams);

            InitiateReply initiateReply = _resourceManagementClient.Initiate(initiateRequest);
            _serverContextId = initiateReply.ContextId;
            _serverContextTimeoutMs = initiateReply.ContextTimeoutMs;
            _localeId = initiateReply.LocaleId;

            if (_serverContextId == @"") throw new Exception("Server returns empty contextId.");

            SetResourceManagementLastCallUtc();

            _serverContextIsOperational = true;
        }

        /// <summary>
        ///     This method disposes of the object.  It is invoked by the client application, client base, or
        ///     the destructor of this object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     This method disposes of the object.  It is invoked by the parameterless Dispose()
        ///     method of this object.  It is expected that the DataGrpc client application
        ///     will perform a Dispose() on each active context to close the connection with the
        ///     DataGrpc Server.  Failure to perform the close will result in the DataGrpc Context remaining
        ///     active until the application edataGrpcts.
        /// </summary>
        /// <summary>
        /// </summary>
        /// <param name="disposing">
        ///     <para>
        ///         This parameter indicates, when TRUE, this Dispose() method was called directly or indirectly by a user's
        ///         code. When FALSE, this method was called by the runtime from inside the finalizer.
        ///     </para>
        ///     <para>
        ///         When called by user code, references within the class should be valid and should be disposed of properly.
        ///         When called by the finalizer, references within the class are not guaranteed to be valid and attempts to
        ///         dispose of them should not be made.
        ///     </para>
        /// </param>
        /// <returns> Returns TRUE to indicate that the object has been disposed. </returns>
        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                if (_serverContextIsOperational)
                {
                    _serverContextIsOperational = false;

                    try
                    {
                        ConcludeReply concludeReply = _resourceManagementClient.Conclude(new ConcludeRequest
                        {
                            ContextId = _serverContextId
                        });                        
                    }
                    catch
                    {
                    }
                }
            }        

            _disposed = true;
        }

        /// <summary>
        ///     The standard destructor invoked by the .NET garbage collector during Finalize.
        /// </summary>
        ~ClientContext()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public ContextInfo? ServerContextInfo
        {
            get
            {
                return _serverContextInfo;
            }
            set
            {
                _serverContextInfo = value;
                if (_serverContextInfo != null && _serverContextInfo.State == State.Aborting)
                {
                    _serverContextIsOperational = false;
                    _pendingContextNotificationData = new ClientContextNotificationData(ClientContextNotificationType.Shutdown,
                        null);
                }
            }            
        }

        /// <summary>
        ///     This method is used to
        ///     keep the context alive.
        /// </summary>
        public void KeepContextAliveIfNeeded(DateTime nowUtc)
        {
            if (!_serverContextIsOperational) return;
                
            uint timeDiffInMs = (uint) (nowUtc - _resourceManagementLastCallUtc).TotalMilliseconds + 500;

            if (timeDiffInMs >= KeepAliveIntervalMs)
            {
                try
                {
                    _resourceManagementClient.ClientKeepAlive(new ClientKeepAliveRequest
                    {
                        ContextId = _serverContextId
                    });
                    
                    _resourceManagementLastCallUtc = nowUtc;
                }
                catch
                {
                    _serverContextIsOperational = false;
                    _pendingContextNotificationData = new ClientContextNotificationData(ClientContextNotificationType.ClientKeepAliveException, null);                    
                }
            }
        }

        /// <summary>        
        /// </summary>
        public void ProcessPendingContextNotificationData()
        {
            if (_pendingContextNotificationData != null)
            {
                ContextNotifyEvent(this, _pendingContextNotificationData);
                _pendingContextNotificationData = null;
            }
        }

        /// <summary>
        ///     This event is used to notify the ClientBase user of events that occur within the ClientBase.
        ///     Caution: Be sure to disconnect the event handler prior to returning.
        /// </summary>
        public event ClientContextNotification ContextNotifyEvent = delegate { };

        /// <summary>
        ///     This property is the server-unique identifier of the context. It is returned by the server
        ///     when the client application creates the context.
        /// </summary>
        public string ServerContextId
        {
            get { return _serverContextId; }
        }        

        /// <summary>
        ///     The publically visible context timeout provided to the server (in msecs). If the server fails to
        ///     receive a call from the client for this period, it will close the context.
        ///     Within this time period, if there was a communications failure, the client can
        ///     attempt to ReInitiate the connection with the server for this context.
        /// </summary>
        public uint ServerContextTimeoutMs
        {
            get { return _serverContextTimeoutMs; }
        }

        /// <summary>
        ///     This property is the Windows LocaleId (language/culture id) for the context.
        ///     Its default value is automatically set to the LocaleId of the calling client application.
        /// </summary>
        public uint LocaleId
        {
            get { return _localeId; }
        }

        /// <summary>
        ///     Inidicates, when TRUE, that the context is closing or has completed closing
        ///     and will not accept any more requests on the context.
        /// </summary>
        public bool ServerContextIsOperational
        {
            get { return _serverContextIsOperational; }            
        }        

        #endregion

        #region private functions

        private void SetResourceManagementLastCallUtc()
        {
            _resourceManagementLastCallUtc = DateTime.UtcNow;
        }

        /// <summary>
        ///     <para> Re throws. </para>
        ///     <para>
        ///         This method processes an exception thrown when the client application calls one of the methods on the
        ///         IResourceManagment interface.
        ///     </para>
        ///     <para>
        ///         If the exception is a FaultException, the exception is from the server and is rethrown unless the exception
        ///         indicates that the server has shutdown. In this case the Abort callback is called to notify the client of the
        ///         shutdown.
        ///     </para>
        ///     <para>
        ///         If the exception is a CommunicationException, then the ThrowOnDisconnectedEndpoint() method is called on the
        ///         ResourceManagment endpoint to throw the exception back to the calling client application to notify it of the
        ///         failed endpoint.
        ///     </para>
        ///     <para> For all other exceptions, the exception is rethrown. </para>
        /// </summary>
        /// <param name="ex"> The exception that was thrown. </param>
        private void ProcessRemoteMethodCallException(Exception ex)
        {   
            if (ex is RpcException)
            {
                _serverContextIsOperational = false;

                // if not a server shutdown, then throw the error message from the server
                //if (IsServerShutdownOrNoContextServerFault(ex as FaultException<DataGrpcFault>)) return;
                _pendingContextNotificationData = new ClientContextNotificationData(ClientContextNotificationType.ResourceManagementFail,
                        ex);

                _logger.LogDebug(ex, "RpcException");
            }            
            else
            {
                _logger.LogDebug(ex, "Exception");
            }           
        }

        #endregion

        #region private fields

        /// <summary>
        ///     This member indicates, when TRUE, that the object has been disposed by the Dispose(bool isDisposing) method.
        /// </summary>
        private bool _disposed;

        private ILogger<DataGrpcProvider> _logger;

        /// <summary>
        ///     This data member is the Endpoint Discovery object used to access the server for its
        ///     connection information. The Endpoint Discovery object retrieves the endpoints of the
        ///     server that are used for browsing, reading, writing, and subscribing.  It also sorts
        ///     them into the preferred order of use. For example, if the client and server are on the
        ///     same machine, the netPipe endpoints will sort to the top.
        /// </summary>
        private ResourceManagement.ResourceManagementClient _resourceManagementClient;

        /// <summary>
        ///     This data member represents the ApplicationName public property
        /// </summary>
        private readonly string _applicationName;        

        /// <summary>
        ///     This data member represents the WorkstationName public property
        /// </summary>
        private readonly string _workstationName;

        /// <summary>
        ///     This data member is the private representation of the ContextId interface property.
        /// </summary>
        private readonly string _serverContextId;

        /// <summary>
        ///     This data member is the private representation of the public ContextIimeout property
        /// </summary>
        private readonly uint _serverContextTimeoutMs;

        /// <summary>
        ///     This data member is the private representation of the LocaleId interface property.
        /// </summary>
        private readonly uint _localeId;

        /// <summary>
        ///     The time of receipt of the response to the last successful IResourceManagement call.
        /// </summary>
        private DateTime _resourceManagementLastCallUtc;
        
        /// <summary>
        ///     This data member contains the Server Description for this DataGrpc Context.  Is set by
        ///     the GetContextInfo() method during context establishment.
        /// </summary>
        private ContextInfo? _serverContextInfo;        

        /// <summary>
        ///     Inidicates, when TRUE, that the context is closing or has completed closing
        ///     and will not accept any more requests on the context.
        /// </summary>
        private volatile bool _serverContextIsOperational; 
        

        private ClientContextNotificationData? _pendingContextNotificationData;

        /// <summary>
        ///     The time interval that controls when ClientKeepAlive messages are
        ///     sent to the server.  If no IResourceManagement messages are sent to
        ///     the server for this period of time, a ClientKeepAlive message is
        ///     sent.  The value is expressed in milliseconds.  This value is the
        ///     same for all contexts.
        /// </summary>
        private const uint KeepAliveIntervalMs = 10000;        

        #endregion        
    }

    #endregion // Context Management
}