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
        }

        /// <summary>
        ///   This is the implementation of the IDisposable.Dispose method.  The client 
        ///   application should invoke this method when this instance is no longer needed.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            if (Disposed) return;

            await DisposeAsyncCore();

            Dispose(disposing: false);
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
            GC.SuppressFinalize(this);
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
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
                if (!IsConcluded)
                {
                    AddCallbackMessage(
                        new ContextInfoMessage
                        {
                            State = State.Aborting
                        });                    
                }
                else
                {
                    _callbackWorkingTask_CancellationTokenSource.Cancel();
                }

                // Dispose of the lists.
                foreach (ServerListRoot list in _listsManager)
                {
                    list.Dispose();
                }

                _listsManager.Clear();
            }

            // Release unmanaged resources.
            // Set large fields to null.			
            Disposed = true;
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (!IsConcluded)
            {
                AddCallbackMessage(
                    new ContextInfoMessage
                    {
                        State = State.Aborting
                    });
            }
            else
            {
                _callbackWorkingTask_CancellationTokenSource.Cancel();
            }                       

            if (_callbackWorkingTask is not null)
            {
                await _callbackWorkingTask;
                _callbackWorkingTask = null;
            }

            try
            {
                // Dispose of the lists.
                foreach (ServerListRoot list in _listsManager.ToArray()) // .ToArray() due to thread issues
                {
                    list?.Dispose(); // Unknown issue
                }
            }
            catch
            {
            }            

            _listsManager.Clear();
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
        ///   User's culture, negotiated when context was created.
        /// </summary>
        public CultureInfo CultureInfo { get; }

        public string SystemNameToConnect { get; }

        public CaseInsensitiveDictionary<string?> ContextParams { get; }

        /// <summary>
        ///    Context identifier != ""
        /// </summary>
        public string ContextId { get; }        

        public ServerListRoot[] Lists
        {
            get
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                return _listsManager.ToArray();
            }
        }

        /// <summary>
        ///   The last time the context was accessed.
        /// </summary>
        public DateTime LastAccessDateTimeUtc { get; set; }

        public bool IsConcluded { get; set; }

        public void DoWork(DateTime nowUtc, CancellationToken token)
        {
            foreach (var list in _listsManager)
            {
                list.DoWork(nowUtc, token);
            }
        }

        /// <summary>
        ///     Force to send all data.
        /// </summary>
        public void Reset()
        {
            foreach (var list in _listsManager)
            {
                list.Reset();
            }
        }

        #endregion

        #region internal functions

        internal async Task<List<AliasResult>> WriteElementValuesAsync(uint listServerAlias, ElementValuesCollection elementValuesCollection)
        {
            ServerListRoot? serverList;

            if (!_listsManager.TryGetValue(listServerAlias, out serverList))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Incorrect listServerAlias."));
            }

            return await serverList.WriteElementValuesAsync(elementValuesCollection);
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

        internal async Task<PassthroughReply> PassthroughAsync(string recipientId, string passthroughName, PassthroughData dataToSend)
        {
            var reply = new PassthroughReply();
            var returnData = new PassthroughData();
            reply.ReturnData = returnData;

            if (_pendingPassthroughReplyData is not null)
            {                
                int length = Math.Min(_pendingPassthroughReplyData.Length - _pendingPassthroughReplyDataIndex, MaxLength);                
                returnData.Data = UnsafeByteOperations.UnsafeWrap(new ReadOnlyMemory<byte>(_pendingPassthroughReplyData, _pendingPassthroughReplyDataIndex, length));
                returnData.Guid = _pendingPassthroughReplyDataGuid;
                _pendingPassthroughReplyDataIndex += length;
                if (_pendingPassthroughReplyDataIndex >= _pendingPassthroughReplyData.Length)
                {
                    _pendingPassthroughReplyData = null;                    
                }
                else
                {
                    var nextGuid = Guid.NewGuid().ToString();
                    returnData.NextGuid = nextGuid;
                    _pendingPassthroughReplyDataGuid = nextGuid;
                }                
                return reply;
            }

            IEnumerable<byte>? dataToSendTemp = null;
            if (!String.IsNullOrEmpty(dataToSend.Guid) && _incompletePassthroughRequestsCollection.Count > 0)
            {
                var beginPassthroughRequest = _incompletePassthroughRequestsCollection.TryGetValue(dataToSend.Guid);
                if (beginPassthroughRequest is not null)
                {
                    _incompletePassthroughRequestsCollection.Remove(dataToSend.Guid);
                    dataToSendTemp = beginPassthroughRequest.Concat(dataToSend.Data);
                }
            }
            if (dataToSendTemp is null)
            {
                dataToSendTemp = dataToSend.Data;
            }

            if (!String.IsNullOrEmpty(dataToSend.NextGuid))
            {
                _incompletePassthroughRequestsCollection[dataToSend.NextGuid] = dataToSendTemp;
                return reply;
            }

            byte[] returnDataArray = await ServerWorker.PassthroughAsync(this, recipientId, passthroughName, dataToSendTemp.ToArray());            
            if (returnDataArray.Length > MaxLength)
            {
                _pendingPassthroughReplyData = returnDataArray;
                _pendingPassthroughReplyDataIndex = MaxLength;                
                returnData.Data = UnsafeByteOperations.UnsafeWrap(new ReadOnlyMemory<byte>(_pendingPassthroughReplyData, 0, MaxLength));
                var nextGuid = Guid.NewGuid().ToString();
                returnData.NextGuid = nextGuid;
                _pendingPassthroughReplyDataGuid = nextGuid;                
            }
            else
            {                
                returnData.Data = UnsafeByteOperations.UnsafeWrap(returnDataArray);                
            }
            
            return reply;
        }

        internal LongrunningPassthroughReply LongrunningPassthrough(string recipientId, string passthroughName, PassthroughData dataToSend)
        {
            var reply = new LongrunningPassthroughReply();

            IEnumerable<byte>? dataToSendTemp = null;
            if (dataToSend.Guid != @"" && _incompletePassthroughRequestsCollection.Count > 0)
            {
                var beginPassthroughRequest = _incompletePassthroughRequestsCollection.TryGetValue(dataToSend.Guid);
                if (beginPassthroughRequest is not null)
                {
                    _incompletePassthroughRequestsCollection.Remove(dataToSend.Guid);
                    dataToSendTemp = beginPassthroughRequest.Concat(dataToSend.Data);
                }
            }
            if (dataToSendTemp is null)
            {
                dataToSendTemp = dataToSend.Data;
            }

            if (dataToSend.NextGuid != @"")
            {
                _incompletePassthroughRequestsCollection[dataToSend.NextGuid] = dataToSendTemp;
                return reply;
            }            
            
            reply.JobId = ServerWorker.LongrunningPassthrough(this, recipientId, passthroughName, dataToSendTemp.ToArray());            

            return reply;
        }

        internal LongrunningPassthroughCancelReply LongrunningPassthroughCancel(string jobId)
        {
            var reply = new LongrunningPassthroughCancelReply();

            ServerWorker.LongrunningPassthroughCancel(this, jobId);

            return reply;
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
            if (requestedServerContextTimeoutMs < 9 * 1000) requestedServerContextTimeoutMs = 9 * 1000; // The minimum timeout is nine seconds.
            if (requestedServerContextTimeoutMs > 30 * 60 * 1000) requestedServerContextTimeoutMs = 30 * 60 * 1000; // The maximum timeout is 30 minutes.
            return requestedServerContextTimeoutMs;
        }        

        #endregion

        #region private fields                

        /// <summary>
        ///   The collection of lists for this context.
        /// </summary>
        private ObjectManager<ServerListRoot> _listsManager = new ObjectManager<ServerListRoot>(20);

        private byte[]? _pendingPassthroughReplyData;

        private int _pendingPassthroughReplyDataIndex;

        private string _pendingPassthroughReplyDataGuid = @"";

        private const int MaxLength = 1 * 1024 * 1024;

        private CaseInsensitiveDictionary<IEnumerable<byte>> _incompletePassthroughRequestsCollection = new CaseInsensitiveDictionary<IEnumerable<byte>>();

        #endregion
    }
}