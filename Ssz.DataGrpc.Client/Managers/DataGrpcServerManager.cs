using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Ssz.Utils;
using Ssz.DataGrpc.Client.Core;
using Ssz.DataGrpc.Client.Core.Context;
using Ssz.DataGrpc.Client.Core.Lists;
using Ssz.DataGrpc.Server;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Ssz.DataGrpc.Client.Data;
using System.Threading.Tasks;
using Grpc.Core;
using System.Linq;

namespace Ssz.DataGrpc.Client.Managers
{
    public class ConnectionDoesNotExistException : Exception
    {
        #region construction and destruction

        public ConnectionDoesNotExistException() :
            base("DataGrpc connection doesn't exist - need to connect to server first.")
        {
        }

        #endregion
    }

    /// <summary>
    ///     This class defines the DataGrpc Server entries in the DataGrpcClient DataGrpcServerList.
    ///     Each DataGrpcServer in the list represents an DataGrpc server for which the client application
    ///     can create an DataGrpcSubscription.
    /// </summary>
    public class DataGrpcServerManager : IDisposable
    {
        #region construction and destruction

        public DataGrpcServerManager(ILogger<DataGrpcProvider> logger)
        {
            _logger = logger;
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
                CloseConnectionInternal();
            }

            _disposed = true;
        }

        /// <summary>
        ///     The standard destructor invoked by the .NET garbage collector during Finalize.
        /// </summary>
        ~DataGrpcServerManager()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        /// <summary>
        ///     This method is used to connect to the server and establish a context with it.
        /// </summary>
        public void InitiateConnection(string serverAddress,
            string applicationName,
            string workstationName,
            string[] systemNames,
            CaseInsensitiveDictionary<string> contextParams)
        {
            if (_disposed) throw new ObjectDisposedException(@"Cannot access a disposed DataGrpcServerProxy.");

            if (_connectionInfo != null) throw new Exception(@"DataGrpc context already exists.");

#if DEBUG            
            TimeSpan contextTimeout = new TimeSpan(0, 30, 0);
#else
            TimeSpan contextTimeout = new TimeSpan(0, 0, 30);
#endif
            GrpcChannel? grpcChannel = null;
            AsyncServerStreamingCall<CallbackMessage>? callbackMessageStream = null;
            try
            {
                grpcChannel = GrpcChannel.ForAddress(serverAddress);

                var resourceManagementClient = new ResourceManagement.ResourceManagementClient(grpcChannel);

                callbackMessageStream = resourceManagementClient.SubscribeForCallback(new SubscribeForCallbackRequest());                

                var context = new DataGrpcContext(_logger,
                            resourceManagementClient,                            
                            applicationName, 
                            workstationName,
                            (uint)contextTimeout.TotalMilliseconds,
                            (uint)Thread.CurrentThread.CurrentCulture.LCID,
                            systemNames,
                            contextParams
                            );

                _connectionInfo = new ConnectionInfo(grpcChannel, resourceManagementClient, callbackMessageStream, context);
            }
            catch
            { 
                if (grpcChannel != null)
                {                    
                    grpcChannel.Dispose();
                }
                if (callbackMessageStream != null)
                {
                    callbackMessageStream.Dispose();
                }

                throw;
            }

            _connectionInfo.Context.ContextNotifyEvent += DataGrpcContextOnContextNotifyEvent;
            var task = ReadCallbackMessagesAsync(callbackMessageStream.ResponseStream);
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
        public DataGrpcElementValueList NewElementValueList(CaseInsensitiveDictionary<string>? listParams)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcServerProxy.");

            if (_connectionInfo == null) throw new ConnectionDoesNotExistException();

            return new DataGrpcElementValueList(_connectionInfo.Context, listParams);
        }

        /// <summary>
        ///     This method creates a new event list for the context.
        /// </summary>
        /// <param name="updateRate"> The update rate for the list. </param>
        /// <param name="bufferingRate"> The buffering rate for the list. 0 if not used. </param>
        /// <param name="filterSet"> The filter set for the list. Null if not used. </param>
        /// <returns> Returns the new data list. </returns>
        public DataGrpcEventList NewEventList(CaseInsensitiveDictionary<string>? listParams)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcServerProxy.");

            if (_connectionInfo == null) throw new ConnectionDoesNotExistException();

            return new DataGrpcEventList(_connectionInfo.Context, listParams);
        }

        /// <summary>
        ///     This method creates a new data journal (historical data) list for the context.
        /// </summary>
        /// <param name="updateRate"> The update rate for the list. </param>
        /// <param name="bufferingRate"> The buffering rate for the list. 0 if not used. </param>
        /// <param name="filterSet"> The filter set for the list. Null if not used. </param>
        /// <returns> Returns the new data list. </returns>
        public DataGrpcElementValueJournalList NewElementValueJournalList(CaseInsensitiveDictionary<string>? listParams)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcServerProxy.");

            if (_connectionInfo == null) throw new ConnectionDoesNotExistException();

            return new DataGrpcElementValueJournalList(_connectionInfo.Context, listParams);
        }

        /*
        /// <summary>
        ///   This method creates a new event journal (historical events) list for the context.
        /// </summary>
        /// <param name = "updateRate">The update rate for the list.</param>
        /// <param name = "bufferingRate">The buffering rate for the list. 0 if not used.</param>
        /// <param name = "filterSet">The filter set for the list. Null if not used.</param>
        /// <returns>Returns the new data list.</returns>
        public IDataGrpcEventJournalList NewEventJournalList(CaseInsensitiveDictionary<string>? listParams)
        {
        }*/

        public PassthroughResult Passthrough(string recipientId,
                                      string passthroughName, byte[] dataToSend)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcServerProxy.");

            if (_connectionInfo == null) throw new ConnectionDoesNotExistException();
            
            return _connectionInfo.Context.Passthrough(recipientId,
                                      passthroughName, dataToSend);
        }

        /// <summary>
        ///     This property specifies how long the context will stay alive in the server after a WCF
        ///     connection failure. The ClientBase will attempt reconnection during this period.
        /// </summary>
        public ServerInfo? ServerInfo
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcServerProxy.");

                if (_connectionInfo == null) throw new ConnectionDoesNotExistException();

                return _serverInfo;
            }
        }

        /// <summary>
        ///     This property specifies how long the context will stay alive in the server after a WCF
        ///     connection failure. The ClientBase will attempt reconnection during this period.
        /// </summary>
        public TimeSpan ServerContextTimeout
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcServerProxy.");

                if (_connectionInfo == null) throw new ConnectionDoesNotExistException();

                return TimeSpan.FromMilliseconds(_connectionInfo.Context.ServerContextTimeout);
            }
        }

        /// <summary>
        ///     This property is the Windows LocaleId (language/culture id) for the context.
        ///     Its default value is automatically set to the LocaleId of the calling client application.
        /// </summary>
        public uint LocaleId
        {
            get 
            {
                if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcServerProxy.");

                if (_connectionInfo == null) throw new ConnectionDoesNotExistException();

                return _connectionInfo.Context.LocaleId; 
            }            
        }

        /// <summary>
        ///     This property indicates, when TRUE, that the client has an open context (session) with
        ///     the server.
        /// </summary>
        public bool ConnectionExists
        {
            get { return _connectionInfo != null; }
        }

        public string ContextId
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcServerProxy.");

                if (_connectionInfo == null) throw new ConnectionDoesNotExistException();

                return _connectionInfo.Context.ContextId;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nowUtc"></param>
        public void Process(DateTime nowUtc)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcServerProxy.");

            if (_connectionInfo == null) throw new ConnectionDoesNotExistException();

            _connectionInfo.Context.KeepContextAlive(nowUtc);
            _connectionInfo.Context.ProcessPendingContextNotificationData();
        }

        #endregion

        #region private functions

        /// <summary>
        ///     This method is used to close a context with the server and disconnect the WCF connection.
        /// </summary>
        private void CloseConnectionInternal()
        {
            if (_connectionInfo == null) return;

            _connectionInfo.Context.ContextNotifyEvent -= DataGrpcContextOnContextNotifyEvent;
            _connectionInfo.Context.Dispose();
            _connectionInfo.CallbackMessageStream.Dispose();
            _connectionInfo.GrpcChannel.Dispose();
            _connectionInfo = null;
        }

        private void DataGrpcContextOnContextNotifyEvent(object sender, DataGrpcContextNotificationData notificationData)
        {
            if (_disposed) return;

            switch (notificationData.ReasonForNotification)
            {
                case DataGrpcContextNotificationType.ResourceManagementFail:
                case DataGrpcContextNotificationType.ClientKeepAliveException:
                case DataGrpcContextNotificationType.Shutdown:                
                case DataGrpcContextNotificationType.ServerKeepAliveError:                
                case DataGrpcContextNotificationType.GeneralException:
                case DataGrpcContextNotificationType.PollException:                
                case DataGrpcContextNotificationType.ResourceManagementDisconnected:                
                    CloseConnectionInternal();
                    break;
            }
        }

        private async Task ReadCallbackMessagesAsync(IAsyncStreamReader<CallbackMessage> reader)
        {
            while (true)
            {
                if (_connectionInfo == null) return;

                try
                {
                    if (!await reader.MoveNext()) return;
                }
                //catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
                //{
                //    break;
                //}
                //catch (OperationCanceledException)
                //{
                //    break;
                //}
                catch
                {
                    return;
                }

                try
                {
                    DataGrpcContext? context;
                    switch (reader.Current.OptionalMessageCase)
                    {
                        case CallbackMessage.OptionalMessageOneofCase.ServerInfo:
                            _serverInfo = reader.Current.ServerInfo;
                            break;
                        case CallbackMessage.OptionalMessageOneofCase.ContextInfo:
                            ContextInfo contextInfo = reader.Current.ContextInfo;
                            context = DataGrpcContext.LookUpContext(contextInfo.ContextId);
                            if (context != null)
                            {
                                context.ContextInfo = contextInfo;
                            }
                            break;
                        case CallbackMessage.OptionalMessageOneofCase.InformationReport:
                            InformationReport informationReport = reader.Current.InformationReport;
                            context = DataGrpcContext.LookUpContext(informationReport.ContextId);
                            if (context != null)
                            {
                                context.InformationReport(informationReport.ListClientAlias, informationReport.ElementValueArrays);
                            }
                            break;
                        case CallbackMessage.OptionalMessageOneofCase.EventNotification:
                            EventNotification eventNotification = reader.Current.EventNotification;
                            context = DataGrpcContext.LookUpContext(eventNotification.ContextId);
                            if (context != null)
                            {
                                context.EventNotification(eventNotification.ListClientAlias, eventNotification.EventMessageArrays);
                            }
                            break;
                    }
                }                
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Callback message exception.");
                }
            }
        }

        #endregion

        #region private fields

        private bool _disposed;

        private ILogger<DataGrpcProvider> _logger;

        private ConnectionInfo? _connectionInfo;        

        private ServerInfo? _serverInfo;

        #endregion

        private class ConnectionInfo
        {
            public ConnectionInfo(GrpcChannel grpcChannel,
                ResourceManagement.ResourceManagementClient resourceManagementClient,
                AsyncServerStreamingCall<CallbackMessage> callbackMessageStream,
                DataGrpcContext context)
            {
                GrpcChannel = grpcChannel;
                ResourceManagementClient = resourceManagementClient;
                CallbackMessageStream = callbackMessageStream;
                Context = context;
            }

            public readonly GrpcChannel GrpcChannel;

            public readonly ResourceManagement.ResourceManagementClient ResourceManagementClient;

            public readonly AsyncServerStreamingCall<CallbackMessage> CallbackMessageStream;
            
            public readonly DataGrpcContext Context;
        }        
    }
}