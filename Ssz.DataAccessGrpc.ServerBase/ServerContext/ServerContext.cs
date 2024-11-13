using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace Ssz.DataAccessGrpc.ServerBase
{
    /// <summary>
    /// 
    /// </summary>
    public partial class ServerContext : IDisposable, IAsyncDisposable
    {
        #region construction and destruction
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="serverWorker"></param>
        /// <param name="clientApplicationName"></param>
        /// <param name="clientWorkstationName"></param>
        /// <param name="requestedServerContextTimeoutMs"></param>
        /// <param name="requestedCultureName"></param>
        /// <param name="systemNameToConnect"></param>
        /// <param name="contextParams"></param>
        public ServerContext(
            ILogger<DataAccessService> logger,
            ServerWorkerBase serverWorker,
            string clientApplicationName, 
            string clientWorkstationName, 
            uint requestedServerContextTimeoutMs, 
            string requestedCultureName, 
            string systemNameToConnect, 
            CaseInsensitiveDictionary<string?> contextParams)
        {
            Logger = logger;
            ServerWorker = serverWorker;
            ClientApplicationName = clientApplicationName;
            ClientWorkstationName = clientWorkstationName;
            ContextTimeoutMs = ValidateContextTimeout(requestedServerContextTimeoutMs);
            CultureInfo = ValidateCultureName(requestedCultureName);
            SystemNameToConnect = systemNameToConnect;
            ContextParams = contextParams;

            ContextId = Guid.NewGuid().ToString();
            
            _callbackWorkingTask = CallbackWorkingTaskMainAsync(CallbackWorkingTask_CancellationTokenSource.Token);
        }

        /// <summary>
        ///   Should not be called. Use DisposeAsync() when possible.
        /// </summary>
        public void Dispose()
        {
            if (Disposed) 
                return;
            Disposed = true;

            Dispose(disposing: true);
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
            GC.SuppressFinalize(this);
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
        }

        public async ValueTask DisposeAsync()
        {
            if (Disposed) 
                return;
            Disposed = true;

            await DisposeAsyncCore();

            Dispose(disposing: false);
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
            GC.SuppressFinalize(this);
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
        }

        /// <summary>
        ///     Should not be called. Use DisposeAsync() when possible.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!IsConcludeCalled)
                {
                    lock (_messagesSyncRoot)
                    {
                        _contextStatusMessagesCollection.Add(new ContextStatusMessage
                        {
                            StateCode = ContextStateCodes.STATE_ABORTING
                        });
                    }
                }
                else
                {
                    CallbackWorkingTask_CancellationTokenSource.Cancel();
                }                

                // Dispose of the lists.
                foreach (ServerListRoot list in _listsManager)
                {
                    list.Dispose();
                }

                _listsManager.Clear();
            }
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (!IsConcludeCalled)
            {
                lock (_messagesSyncRoot)
                {
                    _contextStatusMessagesCollection.Add(new ContextStatusMessage
                    {
                        StateCode = ContextStateCodes.STATE_ABORTING
                    });
                }
            }
            else
            {
                CallbackWorkingTask_CancellationTokenSource.Cancel();
            }

            // Dispose of the lists.
            foreach (ServerListRoot list in _listsManager)
            {
                list.Dispose();
            }

            _listsManager.Clear();

            await _callbackWorkingTask;
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

        public CancellationTokenSource CallbackWorkingTask_CancellationTokenSource { get; } = new CancellationTokenSource();

        public bool Disposed { get; private set; }

        public ILogger<DataAccessService> Logger { get; }

        public ServerWorkerBase ServerWorker { get; }

        /// <summary>
        ///   Application name handed to server when context was created.
        /// </summary>
        public string ClientApplicationName { get; }

        /// <summary>
        ///   Workstation name handed to server when context was created.
        /// </summary>
        public string ClientWorkstationName { get; }        

        /// <summary>
        ///   The negotiated timeout in milliseconds.
        /// </summary>
        public uint ContextTimeoutMs { get; }

        /// <summary>
        ///   The negotiated timeout in milliseconds.
        /// </summary>
        public uint ContextStatusCallbackPeriodMs { get; } = 5000;

        /// <summary>
        ///   User's culture, negotiated when context was created.
        /// </summary>
        public CultureInfo CultureInfo { get; }

        public string SystemNameToConnect { get; }

        public CaseInsensitiveDictionary<string?> ContextParams { get; private set; }

        public event EventHandler ContextParamsChanged = delegate { };

        /// <summary>
        ///    Context identifier != ""
        /// </summary>
        public string ContextId { get; }        

        public ServerListRoot[] Lists
        {
            get
            {
                if (Disposed)
                    return new ServerListRoot[0];

                return _listsManager.ToArray();
            }
        }

        /// <summary>
        ///   The last time the context was accessed.
        /// </summary>
        public DateTime LastAccessDateTimeUtc { get; set; }

        public DateTime? LastContextStatusCallbackDateTimeUtc { get; set; }

        /// <summary>
        ///     Did the client call Conclude(...)
        /// </summary>
        public bool IsConcludeCalled { get; set; }        

        public void UpdateContextParams(CaseInsensitiveDictionary<string?> contextParams)
        {
            ContextParams = contextParams;
            ContextParamsChanged(this, EventArgs.Empty);
        }

        public void DoWork(DateTime nowUtc, CancellationToken token)
        {
            if (Disposed)
                return;

            foreach (var list in _listsManager)
            {
                try
                {
                    list.DoWork(nowUtc, token);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "list.DoWork() Error.");
                }
            }

            if (LastContextStatusCallbackDateTimeUtc is null ||
                nowUtc - LastContextStatusCallbackDateTimeUtc.Value >= TimeSpan.FromMilliseconds(ContextStatusCallbackPeriodMs))
            {
                if (!IsConcludeCalled)
                {
                    AddCallbackMessage(
                            new ContextStatusMessage
                            {
                                StateCode = ContextStateCodes.STATE_OPERATIONAL
                            });
                    LastContextStatusCallbackDateTimeUtc = nowUtc;
                }
            }
        }        

        #endregion

        #region internal functions

        internal async Task<List<AliasResult>?> WriteElementValuesAsync(uint listServerAlias, ReadOnlyMemory<byte> elementValuesCollectionBytes)
        {
            ServerListRoot? serverList;

            if (!_listsManager.TryGetValue(listServerAlias, out serverList))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Incorrect listServerAlias."));
            }

            return await serverList.WriteElementValuesAsync(elementValuesCollectionBytes);
        }

        internal List<EventIdResult> AckAlarms(uint listServerAlias, string operatorName, string comment,
            IEnumerable<EventId> eventIdsToAck)
        {
            ServerListRoot? serverList;

            if (!_listsManager.TryGetValue(listServerAlias, out serverList))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Incorrect listServerAlias."));
            }

            return serverList.AckAlarms(operatorName, comment, eventIdsToAck);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="listServerAlias"></param>
        /// <returns></returns>
        internal void TouchList(uint listServerAlias)
        {
            ServerListRoot? serverList;

            if (!_listsManager.TryGetValue(listServerAlias, out serverList))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Incorrect listServerAlias."));
            }

            serverList.TouchList();
        }

        internal async Task PassthroughAsync(string recipientPath, string passthroughName, ReadOnlyMemory<byte> dataToSend, IServerStreamWriter<DataChunk> responseStream)
        {
            ReadOnlyMemory<byte> returnData = await ServerWorker.PassthroughAsync(this, recipientPath, passthroughName, dataToSend);

            foreach (var dataChunk in ProtobufHelper.SplitForCorrectGrpcMessageSize(returnData))
            {
                await responseStream.WriteAsync(dataChunk);
            };
        }

        internal string LongrunningPassthrough(string recipientPath, string passthroughName, ReadOnlyMemory<byte> dataToSend)
        {
            return ServerWorker.LongrunningPassthrough(this, recipientPath, passthroughName, dataToSend);
        }

        internal void LongrunningPassthroughCancel(string jobId)
        {
            ServerWorker.LongrunningPassthroughCancel(this, jobId);
        }

        #endregion

        #region protected functions

        /// <summary>
        ///   This method is used to validate the selected LocalId.  
        ///   It will default to 0x409 (US English) if not in the 
        ///   supported list.  This method may be overridden if 
        ///   an alternative validation is desired.
        /// </summary>
        /// <param name = "requestedServerCultureName"></param>
        /// <returns></returns>
        protected CultureInfo ValidateCultureName(string requestedServerCultureName)
        {            
            return CultureHelper.GetCultureInfo(requestedServerCultureName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestedServerContextTimeoutMs"></param>
        /// <returns></returns>
        protected uint ValidateContextTimeout(uint requestedServerContextTimeoutMs)
        {            
            if (requestedServerContextTimeoutMs < 9 * 1000) 
                requestedServerContextTimeoutMs = 9 * 1000; // The minimum timeout is nine seconds.
            if (requestedServerContextTimeoutMs > 7 * 24 * 60 * 60 * 1000) 
                requestedServerContextTimeoutMs = 7 * 24 * 60 * 60 * 1000; // The maximum timeout is 1 week.
            return requestedServerContextTimeoutMs;
        }        

        #endregion

        #region private fields                

        /// <summary>
        ///   The collection of lists for this context.
        /// </summary>
        private ObjectManager<ServerListRoot> _listsManager = new ObjectManager<ServerListRoot>(20);                

        #endregion
    }
}