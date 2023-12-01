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
        /// <param name="elementValuesCollection"> The data values to write. </param>
        /// <returns>
        ///     The list server aliases and result codes for the data objects whose write failed. Returns empty if all writes
        ///     succeeded.
        /// </returns>
        public async Task<AliasResult[]> WriteElementValuesAsync(uint listServerAlias, ElementValuesCollection elementValuesCollection)
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
                WriteElementValuesReply reply = await _resourceManagementClient.WriteElementValuesAsync(request);
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
                var passthroughDataToSendFull = new PassthroughData();
                passthroughDataToSendFull.Data = ByteString.CopyFrom(dataToSend);
                IEnumerable<byte> returnData = new byte[0];                            
                foreach (var passthroughDataToSend in passthroughDataToSendFull.SplitForCorrectGrpcMessageSize())
                {
                    var request = new PassthroughRequest
                    {
                        ContextId = _serverContextId,
                        RecipientPath = recipientPath,
                        PassthroughName = passthroughName,
                        DataToSend = passthroughDataToSend
                    };                    
                    while (true)
                    {
                        PassthroughReply reply = await _resourceManagementClient.PassthroughAsync(request);
                        request.DataToSend = new PassthroughData();
                        SetResourceManagementLastCallUtc();
                        IEnumerable<byte>? returnDataTemp = null;
                        if (!String.IsNullOrEmpty(reply.ReturnData.Guid) && _incompletePassthroughRepliesCollection.Count > 0)
                        {
                            var beginPassthroughReply = _incompletePassthroughRepliesCollection.TryGetValue(reply.ReturnData.Guid);
                            if (beginPassthroughReply is not null)
                            {
                                _incompletePassthroughRepliesCollection.Remove(reply.ReturnData.Guid);
                                returnDataTemp = beginPassthroughReply.Concat(reply.ReturnData.Data);
                            }
                        }
                        if (returnDataTemp is null)
                        {
                            returnDataTemp = reply.ReturnData.Data;
                        }

                        if (!String.IsNullOrEmpty(reply.ReturnData.NextGuid))
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

                return returnData;
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
                var longrunningPassthroughRequest = new LongrunningPassthroughRequest
                {
                    CallbackAction = callbackAction
                };
                
                string jobId = @"";

                var passthroughDataToSendFull = new PassthroughData();
                if (dataToSend is not null)
                    passthroughDataToSendFull.Data = ByteString.CopyFrom(dataToSend);                
                foreach (var passthroughDataToSend in passthroughDataToSendFull.SplitForCorrectGrpcMessageSize())
                {
                    var request = new ServerBase.LongrunningPassthroughRequest
                    {                        
                        ContextId = _serverContextId,
                        RecipientPath = recipientPath,
                        PassthroughName = passthroughName,
                        DataToSend = passthroughDataToSend
                    };
                    
                    LongrunningPassthroughReply reply = await _resourceManagementClient.LongrunningPassthroughAsync(request);
                    jobId = reply.JobId;
                    SetResourceManagementLastCallUtc();
                }

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

        private readonly CaseInsensitiveDictionary<IEnumerable<byte>> _incompletePassthroughRepliesCollection = new();

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