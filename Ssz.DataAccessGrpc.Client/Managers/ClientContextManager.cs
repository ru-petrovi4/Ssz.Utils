using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Ssz.Utils;
using Ssz.DataAccessGrpc.Client.ClientLists;
using Ssz.DataAccessGrpc.ServerBase;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Grpc.Core;
using System.Linq;
using System.Globalization;
using Ssz.Utils.DataAccess;
using Grpc.Net.Client.Web;
using System.Net.Http;
using System.Net;
using System.Runtime.InteropServices;
using System.Diagnostics.Eventing.Reader;

namespace Ssz.DataAccessGrpc.Client.Managers
{
    internal class ConnectionDoesNotExistException : Exception
    {
        #region construction and destruction

        public ConnectionDoesNotExistException() :
            base("DataAccessGrpc connection doesn't exist - need to connect to server first.")
        {
        }

        #endregion
    }

    /// <summary>
    ///     This class defines the DataAccessGrpc Server entries in the DataAccessGrpcClient DataAccessGrpcServerList.
    ///     Each DataAccessGrpcServer in the list represents an DataAccessGrpc server for which the client application
    ///     can create an DataAccessGrpcSubscription.
    /// </summary>
    internal class ClientContextManager
    {
        #region construction and destruction

        public ClientContextManager(ILogger<GrpcDataAccessProvider> logger, IDispatcher workingDispatcher)
        {
            _logger = logger;
            _workingDispatcher = workingDispatcher;
        }

        #endregion

        #region public functions

        public GrpcChannel? GrpcChannel
        {
            get
            {
                if (_clientContext is null) return null;

                return _clientContext.GrpcChannel; 
            }
        }

        public DateTime LastFailedConnectionDateTimeUtc { get; protected set; }

        public DateTime LastSuccessfulConnectionDateTimeUtc { get; protected set; }

        public event EventHandler<ContextStatusChangedEventArgs> ServerContextStatusChanged = delegate { };
        
        public async Task InitiateConnectionAsync(string serverAddress,
            string clientApplicationName,
            string clientWorkstationName,
            string systemNameToConnect,
            CaseInsensitiveDictionary<string?> contextParams,
            bool dangerousAcceptAnyServerCertificate,
            IDispatcher? callbackDispatcher)
        {
            if (_disposed) throw new ObjectDisposedException(@"Cannot access a disposed DataAccessGrpcServerProxy.");

            if (_clientContext is not null) throw new Exception(@"DataAccessGrpc context already exists.");

#if DEBUG            
            uint requestedServerContextTimeoutMs = 7 * 24 * 60 * 60 * 1000;
#else
            uint requestedServerContextTimeoutMs = 30 * 1000;
#endif
            GrpcChannel? grpcChannel = null;            
            try
            {
                var httpClientHandler = new HttpClientHandler();
#if NET5_0_OR_GREATER
                if (dangerousAcceptAnyServerCertificate)
                {
                    if (OperatingSystem.IsBrowser())
                        throw new InvalidOperationException("In WebAssembly dangerousAcceptAnyServerCertificate MUST be False");
                    else
                        httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }
#else
                if (dangerousAcceptAnyServerCertificate)
                    httpClientHandler.ServerCertificateCustomValidationCallback = (httpRequestMessage, x509Certificate2, x509Chain, sslPolicyErrors) => true;
#endif
                var grpcWebHandler = new GrpcWebHandler(
                        GrpcWebMode.GrpcWeb,
                        httpClientHandler);                
                grpcChannel = GrpcChannel.ForAddress(serverAddress,
                    new GrpcChannelOptions
                    {
                        HttpVersion = HttpVersion.Version11,
                        HttpHandler = grpcWebHandler
                    });                

                var resourceManagementClient = new DataAccess.DataAccessClient(grpcChannel);

                var clientContext = new ClientContext(_logger,
                            _workingDispatcher,
                            grpcChannel,
                            resourceManagementClient,
                            clientApplicationName,
                            clientWorkstationName
                            );

                await clientContext.InitiateAsync(
                            requestedServerContextTimeoutMs,
                            CultureInfo.CurrentUICulture.Name,
                            systemNameToConnect,
                            contextParams
                            );

                _clientContext = clientContext;
            }
            catch
            { 
                LastFailedConnectionDateTimeUtc = DateTime.UtcNow;
                if (grpcChannel is not null)
                {                    
                    grpcChannel.Dispose();
                }
                throw;
            }
            
            if (callbackDispatcher is not null)
                _clientContext.ServerContextNotification += (s, a) =>
                {                    
                    callbackDispatcher.BeginInvoke(ct =>
                    {
                        ServerContextStatusChanged(this, a);
                    });
                };
        }

        /// <summary>
        ///     This method is used to close a context with the server and disconnect the WCF connection.
        /// </summary>
        public async Task CloseConnectionAsync()
        {
            if (_disposed) return;

            if (_clientContext is not null)
            {
                await _clientContext.DisposeAsync();
                _clientContext = null;
            }
        }

        /// <summary>
        ///     This method creates a new data list for the context.
        /// </summary>
        /// <param name="updateRate"> The update rate for the list. </param>
        /// <param name="bufferingRate"> The buffering rate for the list. 0 if not used. </param>
        /// <param name="filterSet"> The filter set for the list. Null if not used. </param>
        /// <returns> Returns the new data list. </returns>
        public async Task<ClientElementValueList> NewElementValueListAsync(CaseInsensitiveDictionary<string>? listParams)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataAccessGrpcServerProxy.");

            if (_clientContext is null) throw new ConnectionDoesNotExistException();

            var list = new ClientElementValueList(_clientContext);
            await list.DefineListAsync(listParams);
            return list;
        }

        /// <summary>
        ///     This method creates a new event list for the context.
        /// </summary>
        /// <param name="updateRate"> The update rate for the list. </param>
        /// <param name="bufferingRate"> The buffering rate for the list. 0 if not used. </param>
        /// <param name="filterSet"> The filter set for the list. Null if not used. </param>
        /// <returns> Returns the new data list. </returns>
        public async Task<ClientEventList> NewEventListAsync(CaseInsensitiveDictionary<string>? listParams)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataAccessGrpcServerProxy.");

            if (_clientContext is null) throw new ConnectionDoesNotExistException();
            
            var list = new ClientEventList(_clientContext);
            await list.DefineListAsync(listParams);
            return list;
        }

        /// <summary>
        ///     This method creates a new data journal (historical data) list for the context.
        /// </summary>
        /// <param name="updateRate"> The update rate for the list. </param>
        /// <param name="bufferingRate"> The buffering rate for the list. 0 if not used. </param>
        /// <param name="filterSet"> The filter set for the list. Null if not used. </param>
        /// <returns> Returns the new data list. </returns>
        public async Task<ClientElementValuesJournalList> NewElementValuesJournalListAsync(CaseInsensitiveDictionary<string>? listParams)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataAccessGrpcServerProxy.");

            if (_clientContext is null) throw new ConnectionDoesNotExistException();
            
            var list = new ClientElementValuesJournalList(_clientContext);
            await list.DefineListAsync(listParams);
            return list;
        }

        public async Task UpdateContextParamsAsync(CaseInsensitiveDictionary<string?> contextParams)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataAccessGrpcServerProxy.");

            if (_clientContext is null) throw new ConnectionDoesNotExistException();

            await _clientContext.UpdateContextParamsAsync(contextParams);
        }

        public async Task<ReadOnlyMemory<byte>> PassthroughAsync(string recipientPath,
                                      string passthroughName, ReadOnlyMemory<byte> dataToSend)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataAccessGrpcServerProxy.");

            if (_clientContext is null) throw new ConnectionDoesNotExistException();
            
            return await _clientContext.PassthroughAsync(recipientPath,
                                      passthroughName, dataToSend);
        }

        public async Task<Task<uint>> LongrunningPassthroughAsync(string recipientPath, string passthroughName, ReadOnlyMemory<byte> dataToSend,
            Action<Ssz.Utils.DataAccess.LongrunningPassthroughCallback>? callbackAction)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataAccessGrpcServerProxy.");

            if (_clientContext is null) throw new ConnectionDoesNotExistException();

            return await _clientContext.LongrunningPassthroughAsync(recipientPath,
                                      passthroughName, dataToSend, callbackAction);
        }

        /// <summary>
        ///     This property specifies how long the context will stay alive in the server after a WCF
        ///     connection failure. The ClientBase will attempt reconnection during this period.
        /// </summary>
        public TimeSpan ServerContextTimeout
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataAccessGrpcServerProxy.");

                if (_clientContext is null) throw new ConnectionDoesNotExistException();

                return TimeSpan.FromMilliseconds(_clientContext.ServerContextTimeoutMs);
            }
        }

        /// <summary>        
        ///     Its default value is automatically set to the LocaleId of the calling client application.
        /// </summary>
        public string ServerCultureName
        {
            get 
            {
                if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataAccessGrpcServerProxy.");

                if (_clientContext is null) throw new ConnectionDoesNotExistException();

                return _clientContext.ServerCultureName; 
            }            
        }

        /// <summary>
        ///     This property indicates, when TRUE, that the client has an open context (session) with
        ///     the server.
        /// </summary>
        public bool ContextIsOperational
        {
            get { return _clientContext is not null && _clientContext.ContextIsOperational; }
        }

        public string ServerContextId
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataAccessGrpcServerProxy.");

                if (_clientContext is null) throw new ConnectionDoesNotExistException();

                return _clientContext.ServerContextId;
            }
        }
        
        public async Task DoWorkAsync(CancellationToken ct, DateTime nowUtc)
        {
            if (_disposed) 
                throw new ObjectDisposedException("Cannot access a disposed DataAccessGrpcServerProxy.");

            if (_clientContext is null) 
                throw new ConnectionDoesNotExistException();

            LastSuccessfulConnectionDateTimeUtc = nowUtc;

            await _clientContext.KeepContextAliveIfNeededAsync(ct, nowUtc);                      
        }

        #endregion

        #region private fields

        private bool _disposed;

        private ILogger<GrpcDataAccessProvider> _logger;

        private IDispatcher _workingDispatcher;

        private ClientContext? _clientContext;

        #endregion  
    }
}