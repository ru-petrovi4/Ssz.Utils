using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Ssz.DataGrpc.Client.Data;
using Ssz.DataGrpc.Server;
using Ssz.Utils;

namespace Ssz.DataGrpc.Client
{
    public partial class ClientContext
    {
        #region public functions

        /// <summary>
        ///     This method is used to write data of the specified list to the server.  It is called
        ///     by the ClientBase after the client application has prepared and committed the data
        ///     values.
        /// </summary>
        /// <param name="listServerAlias"> The server identifier of the list containing the data objects to write. </param>
        /// <param name="elementValuesCollection"> The data values to write. </param>
        /// <returns>
        ///     The list server aliases and result codes for the data objects whose write failed. Returns empty if all writes
        ///     succeeded.
        /// </returns>
        public AliasResult[] WriteData(uint listServerAlias, ElementValuesCollection elementValuesCollection)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed ClientContext.");

            if (!ServerContextIsOperational) throw new InvalidOperationException();            

            try
            {
                var request = new WriteElementValuesRequest
                {
                    ContextId = _serverContextId,
                    ListServerAlias = listServerAlias,
                    ElementValuesCollection = elementValuesCollection
                };
                WriteElementValuesReply reply = _resourceManagementClient.WriteElementValues(request);
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
        public EventIdResult[] AckAlarms(uint listServerAlias,
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
                AckAlarmsReply reply = _resourceManagementClient.AckAlarms(request);
                SetResourceManagementLastCallUtc();
                return reply.Results.ToArray();
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
                throw;
            }
        }

        public void Passthrough(string recipientId, string passthroughName, byte[] dataToSend, out IEnumerable<byte> returnData)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed Context.");

            if (!ServerContextIsOperational) throw new InvalidOperationException();

            try
            {
                var passthroughDataToSendFull = new PassthroughData();
                passthroughDataToSendFull.Data = ByteString.CopyFrom(dataToSend);
                returnData = new byte[0];                            
                foreach (var passthroughDataToSend in passthroughDataToSendFull.SplitForCorrectGrpcMessageSize())
                {
                    var request = new PassthroughRequest
                    {
                        ContextId = _serverContextId,
                        RecipientId = recipientId,
                        PassthroughName = passthroughName,
                        DataToSend = passthroughDataToSend
                    };                    
                    while (true)
                    {
                        PassthroughReply reply = _resourceManagementClient.Passthrough(request);
                        request.DataToSend = new PassthroughData();
                        SetResourceManagementLastCallUtc();
                        IEnumerable<byte>? returnDataTemp = null;
                        if (reply.ReturnData.Guid != @"" && _incompletePassthroughRepliesCollection.Count > 0)
                        {
                            var beginPassthroughReply = _incompletePassthroughRepliesCollection.TryGetValue(reply.ReturnData.Guid);
                            if (beginPassthroughReply != null)
                            {
                                _incompletePassthroughRepliesCollection.Remove(reply.ReturnData.Guid);
                                returnDataTemp = beginPassthroughReply.Concat(reply.ReturnData.Data);
                            }
                        }
                        if (returnDataTemp == null)
                        {
                            returnDataTemp = reply.ReturnData.Data;
                        }

                        if (reply.ReturnData.NextGuid != @"")
                        {
                            _incompletePassthroughRepliesCollection[reply.ReturnData.NextGuid] = returnDataTemp;

                            request = new PassthroughRequest
                            {
                                ContextId = _serverContextId
                            };

                            continue;
                        }

                        returnData = returnDataTemp;                                           
                        break;
                    }
                }                                
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
                throw;
            }            
        }

        public async Task<StatusCode> LongrunningPassthroughAsync(string recipientId, string passthroughName, byte[]? dataToSend,
            Action<Ssz.Utils.DataAccess.LongrunningPassthroughCallback>? callbackAction)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed Context.");

            if (!ServerContextIsOperational) throw new InvalidOperationException();

            string invokeId = Guid.NewGuid().ToString();
            try
            {
                var incompleteLongrunningPassthroughRequest = new IncompleteLongrunningPassthroughRequest
                {
                    CallbackAction = callbackAction
                };
                lock (_incompleteLongrunningPassthroughRequestsCollection)
                {
                    _incompleteLongrunningPassthroughRequestsCollection.Add(invokeId, incompleteLongrunningPassthroughRequest);
                }

                var passthroughDataToSendFull = new PassthroughData();
                if (dataToSend != null)
                    passthroughDataToSendFull.Data = ByteString.CopyFrom(dataToSend);                
                foreach (var passthroughDataToSend in passthroughDataToSendFull.SplitForCorrectGrpcMessageSize())
                {
                    var request = new LongrunningPassthroughRequest
                    {
                        InvokeId = invokeId,
                        ContextId = _serverContextId,
                        RecipientId = recipientId,
                        PassthroughName = passthroughName,
                        DataToSend = passthroughDataToSend
                    };
                    
                    LongrunningPassthroughReply reply = await _resourceManagementClient.LongrunningPassthroughAsync(request);
                    SetResourceManagementLastCallUtc();
                }

                return await incompleteLongrunningPassthroughRequest.TaskCompletionSource.Task;
            }
            catch (Exception ex)
            {
                lock (_incompleteLongrunningPassthroughRequestsCollection)
                {
                    _incompleteLongrunningPassthroughRequestsCollection.Remove(invokeId);
                }
                ProcessRemoteMethodCallException(ex);
                throw;
            }            
        }

        #endregion

        #region private fields

        private readonly CaseInsensitiveDictionary<IEnumerable<byte>> _incompletePassthroughRepliesCollection = new();

        /// <summary>
        ///     [InvokeId, IncompleteLongrunningPassthroughRequest]
        /// </summary>
        private readonly Dictionary<string, IncompleteLongrunningPassthroughRequest> _incompleteLongrunningPassthroughRequestsCollection = new();

        #endregion        

        private class IncompleteLongrunningPassthroughRequest
        {
            public readonly TaskCompletionSource<StatusCode> TaskCompletionSource = new();

            public Action<Ssz.Utils.DataAccess.LongrunningPassthroughCallback>? CallbackAction;
        }
    }
}