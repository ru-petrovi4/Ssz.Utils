using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Ssz.DataGrpc.Client.Data;
using Ssz.DataGrpc.Server;

namespace Ssz.DataGrpc.Client.Core.Context
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
        /// <param name="elementValueArrays"> The data values to write. </param>
        /// <returns>
        ///     The list server aliases and result codes for the data objects whose write failed. Returns empty if all writes
        ///     succeeded.
        /// </returns>
        public AliasResult[] WriteData(uint listServerAlias, ElementValueArrays elementValueArrays)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed ClientContext.");

            if (!ServerContextIsOperational) throw new InvalidOperationException();            

            try
            {
                var request = new WriteValuesRequest
                {
                    ContextId = _serverContextId,
                    ListServerAlias = listServerAlias,
                    ElementValueArrays = elementValueArrays
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
            string operatorName, string comment, EventId[] alarmsToAck)
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
                request.AlarmsToAck.Add(alarmsToAck);
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

        public PassthroughResult Passthrough(string recipientId, string passthroughName, byte[] dataToSend)
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
                PassthroughReply reply = _resourceManagementClient.Passthrough(request);
                SetResourceManagementLastCallUtc();
                return new PassthroughResult
                {
                    ResultCode = reply.ResultCode,
                    ReturnData = reply.ReturnData.ToByteArray()
                };
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
                throw;
            }            
        }

        #endregion
    }
}