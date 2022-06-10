using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Ssz.Utils;
using Ssz.DataAccessGrpc.Client.ClientLists;
using Ssz.DataAccessGrpc.ServerBase;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Ssz.DataAccessGrpc.Client.Data;
using System.Threading.Tasks;
using Grpc.Core;
using System.Linq;
using System.Globalization;

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
    internal class ClientConnectionManager : IDisposable
    {
        #region construction and destruction

        public ClientConnectionManager(ILogger<GrpcDataAccessProvider> logger, IDispatcher callbackDispatcher)
        {
            _logger = logger;
            _callbackDispatcher = callbackDispatcher;
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
        ~ClientConnectionManager()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public GrpcChannel? GrpcChannel
        {
            get
            {
                if (_connectionInfo is null) return null;

                return _connectionInfo.GrpcChannel; 
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serverAddress"></param>
        /// <param name="clientApplicationName"></param>
        /// <param name="clientWorkstationName"></param>
        /// <param name="systemNameToConnect"></param>
        /// <param name="contextParams"></param>
        public void InitiateConnection(string serverAddress,
            string clientApplicationName,
            string clientWorkstationName,
            string systemNameToConnect,
            CaseInsensitiveDictionary<string> contextParams)
        {
            if (_disposed) throw new ObjectDisposedException(@"Cannot access a disposed DataAccessGrpcServerProxy.");

            if (_connectionInfo is not null) throw new Exception(@"DataAccessGrpc context already exists.");

#if DEBUG            
            TimeSpan requestedServerContextTimeoutMs = new TimeSpan(0, 30, 0);
#else
            TimeSpan requestedServerContextTimeoutMs = new TimeSpan(0, 0, 30);
#endif
            GrpcChannel? grpcChannel = null;            
            try
            {
                grpcChannel = GrpcChannel.ForAddress(serverAddress);

                var resourceManagementClient = new DataAccess.DataAccessClient(grpcChannel);                               

                var clientContext = new ClientContext(_logger,
                            _callbackDispatcher,
                            resourceManagementClient,                            
                            clientApplicationName, 
                            clientWorkstationName,
                            (uint)requestedServerContextTimeoutMs.TotalMilliseconds,
                            CultureInfo.CurrentUICulture.Name,
                            systemNameToConnect,
                            contextParams
                            );

                _connectionInfo = new ConnectionInfo(grpcChannel, resourceManagementClient, clientContext);
            }
            catch
            { 
                if (grpcChannel is not null)
                {                    
                    grpcChannel.Dispose();
                }
                throw;
            }

            _connectionInfo.ClientContext.ContextNotifyEvent += ClientContextOnContextNotifyEvent;            
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
        public ClientElementValueList NewElementValueList(CaseInsensitiveDictionary<string>? listParams)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataAccessGrpcServerProxy.");

            if (_connectionInfo is null) throw new ConnectionDoesNotExistException();

            return new ClientElementValueList(_connectionInfo.ClientContext, listParams);
        }

        /// <summary>
        ///     This method creates a new event list for the context.
        /// </summary>
        /// <param name="updateRate"> The update rate for the list. </param>
        /// <param name="bufferingRate"> The buffering rate for the list. 0 if not used. </param>
        /// <param name="filterSet"> The filter set for the list. Null if not used. </param>
        /// <returns> Returns the new data list. </returns>
        public ClientEventList NewEventList(CaseInsensitiveDictionary<string>? listParams)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataAccessGrpcServerProxy.");

            if (_connectionInfo is null) throw new ConnectionDoesNotExistException();

            return new ClientEventList(_connectionInfo.ClientContext, listParams);
        }

        /// <summary>
        ///     This method creates a new data journal (historical data) list for the context.
        /// </summary>
        /// <param name="updateRate"> The update rate for the list. </param>
        /// <param name="bufferingRate"> The buffering rate for the list. 0 if not used. </param>
        /// <param name="filterSet"> The filter set for the list. Null if not used. </param>
        /// <returns> Returns the new data list. </returns>
        public ClientElementValuesJournalList NewElementValuesJournalList(CaseInsensitiveDictionary<string>? listParams)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataAccessGrpcServerProxy.");

            if (_connectionInfo is null) throw new ConnectionDoesNotExistException();

            return new ClientElementValuesJournalList(_connectionInfo.ClientContext, listParams);
        }

        public void Passthrough(string recipientId,
                                      string passthroughName, byte[] dataToSend, out IEnumerable<byte> returnData)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataAccessGrpcServerProxy.");

            if (_connectionInfo is null) throw new ConnectionDoesNotExistException();
            
            _connectionInfo.ClientContext.Passthrough(recipientId,
                                      passthroughName, dataToSend, out returnData);
        }

        public async Task<StatusCode> LongrunningPassthroughAsync(string recipientId, string passthroughName, byte[]? dataToSend,
            Action<Ssz.Utils.DataAccess.LongrunningPassthroughCallback>? callbackAction)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataAccessGrpcServerProxy.");

            if (_connectionInfo is null) throw new ConnectionDoesNotExistException();

            return await _connectionInfo.ClientContext.LongrunningPassthroughAsync(recipientId,
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

                if (_connectionInfo is null) throw new ConnectionDoesNotExistException();

                return TimeSpan.FromMilliseconds(_connectionInfo.ClientContext.ServerContextTimeoutMs);
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

                if (_connectionInfo is null) throw new ConnectionDoesNotExistException();

                return _connectionInfo.ClientContext.ServerCultureName; 
            }            
        }

        /// <summary>
        ///     This property indicates, when TRUE, that the client has an open context (session) with
        ///     the server.
        /// </summary>
        public bool ConnectionExists
        {
            get { return _connectionInfo is not null; }
        }

        public string ServerContextId
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataAccessGrpcServerProxy.");

                if (_connectionInfo is null) throw new ConnectionDoesNotExistException();

                return _connectionInfo.ClientContext.ServerContextId;
            }
        }
        
        public void DoWork(CancellationToken ct, DateTime nowUtc)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataAccessGrpcServerProxy.");

            if (_connectionInfo is null) throw new ConnectionDoesNotExistException();

            _connectionInfo.ClientContext.KeepContextAliveIfNeeded(ct, nowUtc);
            _connectionInfo.ClientContext.ProcessPendingContextNotificationData();
        }

        #endregion

        #region private functions

        /// <summary>
        ///     This method is used to close a context with the server and disconnect the WCF connection.
        /// </summary>
        private void CloseConnectionInternal()
        {
            if (_connectionInfo is null) return;

            _connectionInfo.ClientContext.ContextNotifyEvent -= ClientContextOnContextNotifyEvent;
            _connectionInfo.ClientContext.Dispose();            
            _connectionInfo.GrpcChannel.Dispose();
            _connectionInfo = null;
        }

        private void ClientContextOnContextNotifyEvent(object sender, ClientContextNotificationData notificationData)
        {
            if (_disposed) return;

            switch (notificationData.ReasonForNotification)
            {
                case ClientContextNotificationType.ResourceManagementFail:
                case ClientContextNotificationType.ClientKeepAliveException:
                case ClientContextNotificationType.Shutdown:                
                case ClientContextNotificationType.ServerKeepAliveError:                
                case ClientContextNotificationType.GeneralException:
                case ClientContextNotificationType.PollException:                
                case ClientContextNotificationType.ResourceManagementDisconnected:                
                    CloseConnectionInternal();
                    break;
            }
        }

        #endregion

        #region private fields

        private bool _disposed;

        private ILogger<GrpcDataAccessProvider> _logger;

        private IDispatcher _callbackDispatcher;

        private ConnectionInfo? _connectionInfo;

        #endregion

        private class ConnectionInfo
        {
            public ConnectionInfo(GrpcChannel grpcChannel,
                DataAccess.DataAccessClient resourceManagementClient,                
                ClientContext clientContext)
            {
                GrpcChannel = grpcChannel;
                ResourceManagementClient = resourceManagementClient;                
                ClientContext = clientContext;
            }

            public readonly GrpcChannel GrpcChannel;

            public readonly DataAccess.DataAccessClient ResourceManagementClient;
            
            public readonly ClientContext ClientContext;
        }        
    }
}