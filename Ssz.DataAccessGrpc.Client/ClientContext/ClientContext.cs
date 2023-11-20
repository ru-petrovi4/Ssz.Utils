using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Ssz.Utils;
using Ssz.DataAccessGrpc.Client.Managers;
using Ssz.DataAccessGrpc.ServerBase;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Ssz.DataAccessGrpc.Client.ClientLists;
using Google.Protobuf.WellKnownTypes;
using Ssz.Utils.DataAccess;
using Grpc.Net.Client;

namespace Ssz.DataAccessGrpc.Client
{
    #region Context Management

    /// <summary>
    ///     This partial class defines the Context Management related aspects of the ClientContext class.  Two
    ///     static Initiate() methods are defined to create and establish a new context with the DataAccessGrpc server,
    ///     one that in which the calling client application supplies the user credentials, and one in which
    ///     the ClientBase calls into the DataAccessGrpc Client Credentials Project.Current for the user credentials when necessary.
    /// </summary>
    internal partial class ClientContext
    {
        #region construction and destruction
        
        public ClientContext(ILogger<GrpcDataAccessProvider> logger,
            IDispatcher workingDispatcher,
            GrpcChannel grpcChannel,
            DataAccess.DataAccessClient resourceManagementClient,            
            string clientApplicationName,
            string clientWorkstationName)
        {
            _logger = logger;
            _workingDispatcher = workingDispatcher;
            GrpcChannel = grpcChannel;
            _resourceManagementClient = resourceManagementClient;
            _applicationName = clientApplicationName;
            _workstationName = clientWorkstationName;
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
        ///     method of this object.  It is expected that the DataAccessGrpc client application
        ///     will perform a Dispose() on each active context to close the connection with the
        ///     DataAccessGrpc ServerBase.  Failure to perform the close will result in the DataAccessGrpc Context remaining
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
                _cancellationTokenSource.Cancel();

                if (_serverContextIsOperational)
                {
                    _serverContextIsOperational = false;

                    try
                    {
                        var t = _resourceManagementClient.ConcludeAsync(new ConcludeRequest
                        {
                            ContextId = _serverContextId
                        });
                    }
                    catch
                    {
                    }
                }

                ClientContextNotification = delegate { };
                ServerContextNotification = delegate { };

                GrpcChannel.Dispose();
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

        public GrpcChannel GrpcChannel { get; }

        public ContextStatus? ServerContextStatus { get; private set; }        

        public event EventHandler<ContextStatusChangedEventArgs> ServerContextNotification = delegate { };

        public event EventHandler<ClientContextNotificationEventArgs> ClientContextNotification = delegate { };
        
        public string ServerContextId
        {
            get { return _serverContextId; }
        }
        
        public uint ServerContextTimeoutMs
        {
            get { return _serverContextTimeoutMs; }
        }

        /// <summary>
        ///     This property is the Windows LocaleId (language/culture id) for the context.
        ///     Its default value is automatically set to the LocaleId of the calling client application.
        /// </summary>
        public string ServerCultureName
        {
            get { return _serverCultureName; }
        }
        
        public bool ServerContextIsOperational
        {
            get { return _serverContextIsOperational; }            
        }

        public async Task InitiateAsync(uint requestedServerContextTimeoutMs,
            string requestedCultureName,
            string systemNameToConnect,
            CaseInsensitiveDictionary<string?> contextParams)
        {            
            var initiateRequest = new InitiateRequest
            {
                ClientApplicationName = _applicationName,
                ClientWorkstationName = _workstationName,
                RequestedServerContextTimeoutMs = requestedServerContextTimeoutMs,
                RequestedCultureName = requestedCultureName,
            };
            initiateRequest.SystemNameToConnect = systemNameToConnect;
            foreach (var kvp in contextParams)
                initiateRequest.ContextParams.Add(kvp.Key,
                    kvp.Value is not null ? new NullableString { Data = kvp.Value } : new NullableString { Null = NullValue.NullValue });

            InitiateReply initiateReply = await _resourceManagementClient.InitiateAsync(initiateRequest);
            _serverContextId = initiateReply.ContextId;
            _serverContextTimeoutMs = initiateReply.ServerContextTimeoutMs;
            _serverCultureName = initiateReply.ServerCultureName;

            if (_serverContextId == @"") 
                throw new Exception("Server returns empty contextId.");

            SetResourceManagementLastCallUtc();

            _callbackMessageStream = _resourceManagementClient.SubscribeForCallback(new SubscribeForCallbackRequest
            {
                ContextId = _serverContextId
            });

            _serverContextIsOperational = true;

            var t = Task.Factory.StartNew(async () =>
            {
                await ReadCallbackMessagesAsync(_callbackMessageStream.ResponseStream, _cancellationTokenSource.Token);
            }, TaskCreationOptions.LongRunning);
        }

        public void KeepContextAliveIfNeeded(CancellationToken ct, DateTime nowUtc)
        {
            if (!_serverContextIsOperational) return;

            uint timeDiffInMs = (uint)(nowUtc - _resourceManagementLastCallUtc).TotalMilliseconds + 500;

            if (timeDiffInMs >= KeepAliveIntervalMs)
            {
                try
                {
                    _resourceManagementClient.ClientKeepAlive(new ClientKeepAliveRequest
                    {
                        ContextId = _serverContextId
                    }, cancellationToken: ct);

                    _resourceManagementLastCallUtc = nowUtc;
                }
                catch
                {
                    _serverContextIsOperational = false;
                    _pendingClientContextNotificationEventArgs = new ClientContextNotificationEventArgs(ClientContextNotificationType.ClientKeepAliveException, null);
                }
            }
        }
        
        public void ProcessPendingClientContextNotification()
        {
            var pendingClientContextNotificationEventArgs = _pendingClientContextNotificationEventArgs;
            _pendingClientContextNotificationEventArgs = null;
            if (pendingClientContextNotificationEventArgs is not null)
                ClientContextNotification(this, pendingClientContextNotificationEventArgs);
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
                if (!_serverContextIsOperational) return;

                _serverContextIsOperational = false;

                // if not a server shutdown, then throw the error message from the server
                //if (IsServerShutdownOrNoContextServerFault(ex as FaultException<DataAccessGrpcFault>)) return;
                _pendingClientContextNotificationEventArgs = new ClientContextNotificationEventArgs(ClientContextNotificationType.RemoteMethodCallException,
                        ex);

                _logger.LogDebug(ex, "RpcException when server method call. Client reconnecting..");
            }

            _logger.LogDebug(ex, "Exception when server method call.");
        }   

        #endregion

        #region private fields

        private bool _disposed;

        private ILogger<GrpcDataAccessProvider> _logger;

        private IDispatcher _workingDispatcher;

        private readonly CancellationTokenSource _cancellationTokenSource = new();

        private DataAccess.DataAccessClient _resourceManagementClient;
        
        private readonly string _applicationName;
        
        private readonly string _workstationName;
        
        private string _serverContextId = null!;
        
        private uint _serverContextTimeoutMs;
        
        private string _serverCultureName = null!;

        private DateTime _resourceManagementLastCallUtc;

        private AsyncServerStreamingCall<CallbackMessage>? _callbackMessageStream;
        
        private volatile bool _serverContextIsOperational;

        private ClientContextNotificationEventArgs? _pendingClientContextNotificationEventArgs;

        /// <summary>
        ///     The time interval that controls when ClientKeepAlive messages are
        ///     sent to the ServerBase.  If no IResourceManagement messages are sent to
        ///     the server for this period of time, a ClientKeepAlive message is
        ///     sent.  The value is expressed in milliseconds.  This value is the
        ///     same for all contexts.
        /// </summary>
        private const uint KeepAliveIntervalMs = 10000;

        #endregion

        public class ClientContextNotificationEventArgs : EventArgs
        {
            public ClientContextNotificationEventArgs(ClientContextNotificationType reasonForNotification, object? data)
            {
                ReasonForNotification = reasonForNotification;
                Data = data;
            }

            /// <summary>
            ///     This property specifies the reason for the notification.
            /// </summary>
            public ClientContextNotificationType ReasonForNotification { get; }

            /// <summary>
            ///     This property contains the details about the notification.
            /// </summary>
            public object? Data { get; }
        }

        /// <summary>
        ///     This enumeration indicates why the notification is being sent.
        /// </summary>
        public enum ClientContextNotificationType
        {
            /// <summary>
            ///     The server shutting down.            
            /// </summary>
            Shutdown,

            /// <summary>
            ///     Remote Method Call Exception.            
            /// </summary>
            RemoteMethodCallException,

            /// <summary>            
            /// </summary>
            ClientKeepAliveException,

            /// <summary>            
            /// </summary>
            ReadCallbackMessagesException
        }
    }

    #endregion // Context Management
}