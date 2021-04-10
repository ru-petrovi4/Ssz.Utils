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
        /// <param name="alarmsToAck"></param>
        /// <returns></returns>
        public EventIdResult[] AcknowledgeAlarms(uint listServerAlias,
            string operatorName, string comment, Ssz.Utils.DataAccess.EventId[] alarmsToAck)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed ClientContext.");

            if (!ServerContextIsOperational) throw new InvalidOperationException();

            try
            {
                var request = new AcknowledgeAlarmsRequest
                {
                    ContextId = _serverContextId,
                    ListServerAlias = listServerAlias,
                    OperatorName = operatorName,
                    Comment = comment,                   
                };
                request.AlarmsToAck.Add(alarmsToAck.Select(e => new EventId(e)));
                AcknowledgeAlarmsReply reply = _resourceManagementClient.AcknowledgeAlarms(request);
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
                var request = new PassthroughRequest
                {
                    ContextId = _serverContextId,
                    RecipientId = recipientId,
                    PassthroughName = passthroughName,
                    DataToSend = ByteString.CopyFrom(dataToSend)
                };
                while (true)
                {                    
                    PassthroughReply reply = _resourceManagementClient.Passthrough(request);
                    SetResourceManagementLastCallUtc();
                    IEnumerable<byte>? returnDataTemp = null;
                    if (reply.Guid != @"" && _incompletePassthroughRepliesCollection.Count > 0)
                    {
                        var beginPassthroughReply = _incompletePassthroughRepliesCollection.TryGetValue(reply.Guid);
                        if (beginPassthroughReply != null)
                        {
                            _incompletePassthroughRepliesCollection.Remove(reply.Guid);
                            returnDataTemp = beginPassthroughReply.Concat(reply.ReturnData);
                        }
                    }
                    if (returnDataTemp == null)
                    {
                        returnDataTemp = reply.ReturnData;
                    }

                    if (reply.NextGuid != @"")
                    {
                        _incompletePassthroughRepliesCollection[reply.NextGuid] = returnDataTemp;

                        request = new PassthroughRequest
                        {
                            ContextId = _serverContextId                            
                        };

                        continue;
                    }

                    returnData = returnDataTemp;
                    return reply.ResultCode;
                }                
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
                throw;
            }            
        }

        #endregion

        #region private fields

        /// <summary>
        ///     This data member holds the last exception message encountered by the
        ///     InformationReport callback when calling valuesUpdateEvent().
        /// </summary>
        private CaseInsensitiveDictionary<IEnumerable<byte>> _incompletePassthroughRepliesCollection = new CaseInsensitiveDictionary<IEnumerable<byte>>();

        #endregion        
    }
}