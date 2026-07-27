using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Ssz.Utils;
using Ssz.DataAccessGrpc.Client.Managers;
using Ssz.DataAccessGrpc.Common;
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
    internal partial class ClientContext: IAsyncDisposable
    {
        #region construction and destruction
        
        public ClientContext(ILogger<GrpcDataAccessProvider> logger,
            IDispatcher workingDispatcher,
            GrpcChannel? grpcChannel,
            IDataAccessService dataAccessService,            
            string clientApplicationName,
            string clientWorkstationName)
        {
            _logger = logger;
            _workingDispatcher = workingDispatcher;
            GrpcChannel = grpcChannel;
            _dataAccessService = dataAccessService;
            _applicationName = clientApplicationName;
            _workstationName = clientWorkstationName;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;

            _cancellationTokenSource.Cancel();

            if (_contextIsOperational)
            {
                _contextIsOperational = false;

                try
                {
                    await _dataAccessService.ConcludeAsync(new ConcludeRequest
                    {
                        ContextId = _serverContextId
                    });
                }
                catch
                {
                }
            }
            _dataAccessService.Dispose();

            ServerContextNotification = delegate { };

            GrpcChannel?.Dispose();

            _disposed = true;

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
            GC.SuppressFinalize(this);
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
        }

        #endregion

        #region public functions

        public GrpcChannel? GrpcChannel { get; }

        public ContextStatus? ServerContextStatus { get; private set; }        

        public event EventHandler<ContextStatusChangedEventArgs> ServerContextNotification = delegate { };
        
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
        
        public bool ContextIsOperational
        {
            get { return _contextIsOperational; }            
        }

        public async Task InitiateAsync(uint requestedServerContextTimeoutMs,
            string requestedCultureName,
            string systemNameToConnect,
            CaseInsensitiveOrderedDictionary<string?> contextParams)
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

            InitiateReply initiateReply = await _dataAccessService.InitiateAsync(initiateRequest);
            _serverContextId = initiateReply.ContextId;
            _serverContextTimeoutMs = initiateReply.ServerContextTimeoutMs;
            _serverCultureName = initiateReply.ServerCultureName;

            if (_serverContextId == @"") 
                throw new Exception("Server returns empty contextId.");

            SetResourceManagementLastCallUtc();

            _callbackStreamReader = _dataAccessService.SubscribeForCallback(new SubscribeForCallbackRequest
            {
                ContextId = _serverContextId
            });

            _contextIsOperational = true;
            LastServerContextCallbackMessage = DateTime.UtcNow;

            var cancellationToken = _cancellationTokenSource.Token;

            bool isBrowser = false;
#if NET5_0_OR_GREATER
            if (OperatingSystem.IsBrowser())
                isBrowser = true;
#endif
            if (isBrowser)
            {
                _readCallbackMessages_Task = Task.Run(async () =>
                    await ReadCallbackMessagesAsync(_callbackStreamReader, cancellationToken)
                );
                _keepAlive_Task = Task.Run(async () =>
                    await KeepAliveAsync(cancellationToken)
                );
            }
            else
            {
                var readCallbackMessages_TaskCompletionSource = new TaskCompletionSource<int>();
                var readCallbackMessagesThread = new Thread(async () =>
                {
                    await ReadCallbackMessagesAsync(_callbackStreamReader, cancellationToken);
                    readCallbackMessages_TaskCompletionSource.SetResult(0);
                });
                _readCallbackMessages_Task = readCallbackMessages_TaskCompletionSource.Task;
                readCallbackMessagesThread.Start();

                var keepAlive_TaskCompletionSource = new TaskCompletionSource<int>();
                var keepAliveThread = new Thread(async () =>
                {
                    await KeepAliveAsync(cancellationToken);
                    keepAlive_TaskCompletionSource.SetResult(0);
                });
                _keepAlive_Task = keepAlive_TaskCompletionSource.Task;
                keepAliveThread.Start();
            }            
        }

        #endregion

        #region private functions

        private DateTime LastServerContextCallbackMessage
        {
            get => new DateTime(Interlocked.Read(ref _lastServerContextCallbackMessage_Ticks), DateTimeKind.Utc);
            set => Interlocked.Exchange(ref _lastServerContextCallbackMessage_Ticks, value.Ticks);
        }

        public async Task KeepAliveAsync(CancellationToken cancellationToken)
        {
#if !DEBUG
            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(5000, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!_contextIsOperational)
                        throw new OperationCanceledException();

                    try
                    {
                        await _dataAccessService.ClientKeepAliveAsync(new ClientKeepAliveRequest
                        {
                            ContextId = _serverContextId
                        }, cancellationToken: cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        ProcessRemoteMethodCallException(ex);
                    }

                    uint timeDiffInMs = (uint)(DateTime.UtcNow - LastServerContextCallbackMessage).TotalMilliseconds;
                    if (timeDiffInMs >= _serverContextTimeoutMs)
                    {
                        ProcessRemoteMethodCallException(new RpcException(new Status(StatusCode.DeadlineExceeded, @"STATE_OPERATIONAL ContextMessage DeadlineExceeded")));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception)
            {                
            }
#endif
        }

        private void SetResourceManagementLastCallUtc()
        {
            // For future use, if we want to track the last time we called a resource management method on the server.
        }

        private void ProcessRemoteMethodCallException(Exception ex)
        {
            if (!_contextIsOperational)
                return;

            if (ex is RpcException rpcException)
            {
                if (rpcException.StatusCode != StatusCode.Cancelled)
                {
                    _contextIsOperational = false;

                    _logger.LogDebug(ex, "RpcException when server method call. ContextIsOperational = false");
                }
            }
            else
            {
                _logger.LogDebug(ex, "Exception when server method call.");
            }
        }   

        #endregion

        #region private fields

        private bool _disposed;

        private Task? _readCallbackMessages_Task;
        private Task? _keepAlive_Task;

        private ILogger<GrpcDataAccessProvider> _logger;

        private IDispatcher _workingDispatcher;

        private readonly CancellationTokenSource _cancellationTokenSource = new();

        private IDataAccessService _dataAccessService;
        
        private readonly string _applicationName;
        
        private readonly string _workstationName;
        
        private string _serverContextId = null!;
        
        private uint _serverContextTimeoutMs;
        
        private string _serverCultureName = null!;        

        private long _lastServerContextCallbackMessage_Ticks;

        private IAsyncStreamReader<CallbackMessage>? _callbackStreamReader;
        
        private volatile bool _contextIsOperational;

        /// <summary>
        ///     The time interval that controls when ClientKeepAlive messages are
        ///     sent to the Common.  If no IResourceManagement messages are sent to
        ///     the server for this period of time, a ClientKeepAlive message is
        ///     sent.  The value is expressed in milliseconds.  This value is the
        ///     same for all contexts.
        /// </summary>
        private const uint KeepAliveIntervalMs = 10000;

        #endregion
    }

#endregion // Context Management
}