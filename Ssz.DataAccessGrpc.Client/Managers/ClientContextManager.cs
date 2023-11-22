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
    internal class ClientContextManager : IDisposable
    {
        #region construction and destruction

        public ClientContextManager(ILogger<GrpcDataAccessProvider> logger, IDispatcher workingDispatcher)
        {
            _logger = logger;
            _workingDispatcher = workingDispatcher;
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
        ///     DataAccessGrpc Server.  Failure to perform the close will result in the DataAccessGrpc Context remaining
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
                CloseConnectionInternal();
            }

            _disposed = true;
        }

        /// <summary>
        ///     The standard destructor invoked by the .NET garbage collector during Finalize.
        /// </summary>
        ~ClientContextManager()
        {
            Dispose(false);
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

        public event EventHandler<ContextStatusChangedEventArgs> ServerContextStatusChanged = delegate { };
        
        public async Task InitiateConnectionAsync(string serverAddress,
            string clientApplicationName,
            string clientWorkstationName,
            string systemNameToConnect,
            CaseInsensitiveDictionary<string?> contextParams,
            IDispatcher? callbackDispatcher)
        {
            if (_disposed) throw new ObjectDisposedException(@"Cannot access a disposed DataAccessGrpcServerProxy.");

            if (_clientContext is not null) throw new Exception(@"DataAccessGrpc context already exists.");

#if DEBUG            
            TimeSpan requestedServerContextTimeoutMs = new TimeSpan(0, 30, 0);
#else
            TimeSpan requestedServerContextTimeoutMs = new TimeSpan(0, 0, 30);
#endif
            GrpcChannel? grpcChannel = null;            
            try
            {
                var handler = new GrpcWebHandler(
                        GrpcWebMode.GrpcWeb,
                        new HttpClientHandler());
                handler.HttpVersion = HttpVersion.Version11;
                grpcChannel = GrpcChannel.ForAddress(serverAddress,
                    new GrpcChannelOptions
                    {
                        HttpClient = new HttpClient(handler)
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
                            (uint)requestedServerContextTimeoutMs.TotalMilliseconds,
                            CultureInfo.CurrentUICulture.Name,
                            systemNameToConnect,
                            contextParams
                            );

                _clientContext = clientContext;
            }
            catch
            {                
                if (grpcChannel is not null)
                {                    
                    grpcChannel.Dispose();
                }
                throw;
            }

            _clientContext.ClientContextNotification += ClientContext_OnClientContextNotification;
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
        public void CloseConnection()
        {
            if (_disposed) return;

            CloseConnectionInternal();
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

        public async Task<IEnumerable<byte>> PassthroughAsync(string recipientId,
                                      string passthroughName, byte[] dataToSend)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataAccessGrpcServerProxy.");

            if (_clientContext is null) throw new ConnectionDoesNotExistException();
            
            return await _clientContext.PassthroughAsync(recipientId,
                                      passthroughName, dataToSend);
        }

        public async Task<Task<uint>> LongrunningPassthroughAsync(string recipientId, string passthroughName, byte[]? dataToSend,
            Action<Ssz.Utils.DataAccess.LongrunningPassthroughCallback>? callbackAction)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataAccessGrpcServerProxy.");

            if (_clientContext is null) throw new ConnectionDoesNotExistException();

            return await _clientContext.LongrunningPassthroughAsync(recipientId,
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
        public bool ConnectionExists
        {
            get { return _clientContext is not null; }
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
        
        public void DoWork(CancellationToken ct, DateTime nowUtc)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataAccessGrpcServerProxy.");

            if (_clientContext is null) throw new ConnectionDoesNotExistException();

            var t = _clientContext.KeepContextAliveIfNeededAsync(ct, nowUtc);
            _clientContext.ProcessPendingClientContextNotification();
        }

#endregion

#region private functions

        /// <summary>
        ///     This method is used to close a context with the server and disconnect the WCF connection.
        /// </summary>
        private void CloseConnectionInternal()
        {
            if (_clientContext is null) return;
            
            _clientContext.Dispose();
            _clientContext = null;
        }

        private void ClientContext_OnClientContextNotification(object? sender, ClientContext.ClientContextNotificationEventArgs args)
        { 
            if (_disposed) return;

            switch (args.ReasonForNotification)
            {
                case ClientContext.ClientContextNotificationType.RemoteMethodCallException:
                case ClientContext.ClientContextNotificationType.ClientKeepAliveException:                
                case ClientContext.ClientContextNotificationType.Shutdown:
                case ClientContext.ClientContextNotificationType.ReadCallbackMessagesException:
                    CloseConnectionInternal();
                    break;
            }
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