using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
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
                var request = new WriteValuesRequest
                {
                    ContextId = _serverContextId,
                    ListServerAlias = listServerAlias,
                    ElementValuesCollection = elementValuesCollection
                };
                WriteValuesReply reply = _resourceManagementClient.WriteValues(request);
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

        public uint Passthrough(string recipientId, string passthroughName, byte[] dataToSend, out IEnumerable<byte> returnData)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed Context.");

            if (!ServerContextIsOperational) throw new InvalidOperationException();

            try
            {
                var passthroughDataToSendFull = new PassthroughData();
                passthroughDataToSendFull.Data = ByteString.CopyFrom(dataToSend);
                returnData = new byte[0];
                uint resultCode = 0;                
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
                        resultCode = reply.ResultCode;                        
                        break;
                    }
                }                
                return resultCode;
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
                throw;
            }            
        }

        #endregion

        #region private fields
        
        private CaseInsensitiveDictionary<IEnumerable<byte>> _incompletePassthroughRepliesCollection = new CaseInsensitiveDictionary<IEnumerable<byte>>();

        #endregion        
    }
}