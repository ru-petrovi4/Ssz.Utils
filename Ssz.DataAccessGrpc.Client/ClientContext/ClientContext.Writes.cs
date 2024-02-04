using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils;

namespace Ssz.DataAccessGrpc.Client
{
    internal partial class ClientContext
    {
        #region public functions

        /// <summary>
        ///     This method is used to write data of the specified list to the ServerBase.  It is called
        ///     by the ClientBase after the client application has prepared and committed the data
        ///     values.
        /// </summary>
        /// <param name="listServerAlias"> The server identifier of the list containing the data objects to write. </param>
        /// <param name="fullElementValuesCollection"> The data values to write. </param>
        /// <returns>
        ///     The list server aliases and result codes for the data objects whose write failed. Returns empty if all writes
        ///     succeeded.
        /// </returns>
        public async Task<AliasResult[]> WriteElementValuesAsync(uint listServerAlias, byte[] fullElementValuesCollection)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed ClientContext.");

            if (!ServerContextIsOperational) throw new InvalidOperationException();            

            try
            {
                var call = _resourceManagementClient.WriteElementValues();

                foreach (ByteString elementValuesCollection in ProtobufHelper.SplitForCorrectGrpcMessageSize(fullElementValuesCollection))
                {
                    await call.RequestStream.WriteAsync(new WriteElementValuesRequest
                    {
                        ContextId = _serverContextId,
                        ListServerAlias = listServerAlias,
                        ElementValuesCollection = elementValuesCollection
                    });
                }
                await call.RequestStream.CompleteAsync();

                WriteElementValuesReply reply = await call.ResponseAsync;
                SetResourceManagementLastCallUtc();
                return reply.Results.ToArray();                
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
                throw;
            }
        }

        /// <summary>
        ///     Returns the array EventIds and result codes for the alarms whose
        ///     acknowledgement failed.
        /// </summary>
        /// <param name="listServerAlias"></param>
        /// <param name="operatorName"></param>
        /// <param name="comment"></param>
        /// <param name="eventIdsToAck"></param>
        /// <returns></returns>
        public async Task<EventIdResult[]> AckAlarmsAsync(uint listServerAlias,
            string operatorName, string comment, Ssz.Utils.DataAccess.EventId[] eventIdsToAck)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed ClientContext.");

            if (!ServerContextIsOperational) throw new InvalidOperationException();

            try
            {
                var request = new AckAlarmsRequest
                {
                    ContextId = _serverContextId,
                    ListServerAlias = listServerAlias,
                    OperatorName = operatorName,
                    Comment = comment,                   
                };
                request.EventIdsToAck.Add(eventIdsToAck.Select(e => new EventId(e)));
                AckAlarmsReply reply = await _resourceManagementClient.AckAlarmsAsync(request);
                SetResourceManagementLastCallUtc();
                return reply.Results.ToArray();
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
                throw;
            }
        }

        public async Task<IEnumerable<byte>> PassthroughAsync(string recipientPath, string passthroughName, byte[] dataToSend)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed Context.");

            if (!ServerContextIsOperational) throw new InvalidOperationException();

            try
            {
                var call = _resourceManagementClient.Passthrough();
                SetResourceManagementLastCallUtc();

                foreach (var dataToSendByteString in ProtobufHelper.SplitForCorrectGrpcMessageSize(dataToSend))
                {
                    await call.RequestStream.WriteAsync(new PassthroughRequest
                    {
                        ContextId = _serverContextId,
                        RecipientPath = recipientPath,
                        PassthroughName = passthroughName,
                        DataToSend = dataToSendByteString
                    });                    
                }
                await call.RequestStream.CompleteAsync();                

                DataChunk? dataChunk = null;
                while (await call.ResponseStream.MoveNext())
                {
                    dataChunk = call.ResponseStream.Current;                    
                    if (dataChunk.Guid != @"" && _incompletePassthroughRepliesCollection.Count > 0)
                    {
                        var beginPassthroughReply = _incompletePassthroughRepliesCollection.TryGetValue(dataChunk.Guid);
                        if (beginPassthroughReply is not null)
                        {
                            _incompletePassthroughRepliesCollection.Remove(dataChunk.Guid);
                            beginPassthroughReply.CombineWith(dataChunk);
                            dataChunk = beginPassthroughReply;
                        }
                    }

                    if (dataChunk.NextDataChunkGuid != @"")
                        _incompletePassthroughRepliesCollection[dataChunk.NextDataChunkGuid] = dataChunk;
                }

                return dataChunk!.Bytes;
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
                throw;
            }            
        }

        /// <summary>
        ///     Returns StatusCode
        /// </summary>
        /// <param name="recipientPath"></param>
        /// <param name="passthroughName"></param>
        /// <param name="dataToSend"></param>
        /// <param name="callbackAction"></param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<Task<uint>> LongrunningPassthroughAsync(string recipientPath, string passthroughName, byte[]? dataToSend,
            Action<Ssz.Utils.DataAccess.LongrunningPassthroughCallback>? callbackAction)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed Context.");

            if (!ServerContextIsOperational) throw new InvalidOperationException();
            
            try
            {
                var call = _resourceManagementClient.LongrunningPassthrough();
                SetResourceManagementLastCallUtc();

                foreach (var dataToSendByteString in ProtobufHelper.SplitForCorrectGrpcMessageSize(dataToSend))
                {
                    await call.RequestStream.WriteAsync(new ServerBase.LongrunningPassthroughRequest
                    {
                        ContextId = _serverContextId,
                        RecipientPath = recipientPath,
                        PassthroughName = passthroughName,
                        DataToSend = dataToSendByteString
                    });
                }
                await call.RequestStream.CompleteAsync();

                LongrunningPassthroughReply reply = await call.ResponseAsync;
                string jobId = reply.JobId;

                var longrunningPassthroughRequest = new LongrunningPassthroughRequest
                {
                    CallbackAction = callbackAction
                };
                if (!_longrunningPassthroughRequestsCollection.TryGetValue(jobId, out List<LongrunningPassthroughRequest>? longrunningPassthroughRequestsList))
                {
                    longrunningPassthroughRequestsList = new List<LongrunningPassthroughRequest>();
                    _longrunningPassthroughRequestsCollection.Add(jobId, longrunningPassthroughRequestsList);
                }
                longrunningPassthroughRequestsList.Add(longrunningPassthroughRequest);

                return longrunningPassthroughRequest.TaskCompletionSource.Task;
            }
            catch (Exception ex)
            {                
                ProcessRemoteMethodCallException(ex);
                throw;
            }            
        }

        #endregion

        #region private fields

        private readonly CaseInsensitiveDictionary<DataChunk> _incompletePassthroughRepliesCollection = new();

        /// <summary>
        ///     [JobId, IncompleteLongrunningPassthroughRequest]
        /// </summary>
        private readonly Dictionary<string, List<LongrunningPassthroughRequest>> _longrunningPassthroughRequestsCollection = new();

        #endregion        

        private class LongrunningPassthroughRequest
        {
            public readonly TaskCompletionSource<uint> TaskCompletionSource = new();

            public Action<Ssz.Utils.DataAccess.LongrunningPassthroughCallback>? CallbackAction;
        }
    }
}