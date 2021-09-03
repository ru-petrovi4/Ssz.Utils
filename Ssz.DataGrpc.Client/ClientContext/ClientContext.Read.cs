using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.DataGrpc.Client.ClientLists;
using Ssz.Utils.DataAccess;
using Ssz.DataGrpc.Server;

namespace Ssz.DataGrpc.Client
{
    /// <summary>
    ///     This partial class defines the IRead related aspects of the ClientContext class.
    /// </summary>
    public partial class ClientContext
    {
        #region public functions

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataGrpcElementValueJournalList"></param>
        /// <param name="firstTimestampUtc"></param>
        /// <param name="secondTimestampUtc"></param>
        /// <param name="numValuesPerAlias"></param>
        /// <param name="calculation"></param>
        /// <param name="serverAliases"></param>
        /// <returns></returns>
        public ValueStatusTimestamp[][] ReadElementValueJournals(ClientElementValueJournalList dataGrpcElementValueJournalList, DateTime firstTimestampUtc,
            DateTime secondTimestampUtc,
            uint numValuesPerAlias, Ssz.Utils.DataAccess.TypeId calculation, uint[] serverAliases)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed ClientContext.");

            if (!ServerContextIsOperational) throw new InvalidOperationException();

            try
            {
                while (true)
                {
                    var request = new ReadElementValueJournalsRequest
                    {
                        ContextId = _serverContextId,
                        ListServerAlias = dataGrpcElementValueJournalList.ListServerAlias,
                        FirstTimestamp = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(firstTimestampUtc),
                        SecondTimestamp = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(secondTimestampUtc),
                        NumValuesPerAlias = numValuesPerAlias,
                        Calculation = new Server.TypeId(calculation)
                    };
                    request.ServerAliases.Add(serverAliases);
                    ReadElementValueJournalsReply reply = _resourceManagementClient.ReadElementValueJournals(request);
                    SetResourceManagementLastCallUtc();

                    var result = dataGrpcElementValueJournalList.OnReadElementValueJournal(reply.ElementValueJournalsCollection);
                    if (result != null) return result;
                }
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

///// <summary>
/////     <para>
/////         This method is used to read the values of one or more data objects in a list. It is also used as a
/////         keep-alive for the read endpoint by setting the listId parameter to 0. In this case, null is returned
/////         immediately.
/////     </para>
///// </summary>
///// <param name="listServerAlias">
/////     The server identifier of the list that contains data objects to be read. Null if this is a
/////     keep-alive.
///// </param>
///// <param name="serverAliases"> The server aliases of the data objects to read. </param>
///// <returns>
/////     <para>
/////         The list of requested values. Each value in this list is identified by its client alias. If the server alias
/////         for a data object to read was not found, an ErrorInfo object will be returned that contains the server alias
/////         instead of a value, status, and timestamp.
/////     </para>
/////     <para> Returns null if this is a keep-alive. </para>
///// </returns>
//public ElementValuesCollection? ReadData(uint listServerAlias, List<uint> serverAliases)
//{
//    if (_disposed) throw new ObjectDisposedException("Cannot access a disposed ClientContext.");

//    if (!ServerContextIsOperational) throw new InvalidOperationException();

//    try
//    {
//        var request = new PassthroughRequest
//        {
//            ContextId = _serverContextId,
//            RecipientId = recipientId,
//            PassthroughName = passthroughName,
//            DataToSend = ByteString.CopyFrom(dataToSend)
//        };
//        PassthroughReply reply = _resourceManagementClient.Read;
//        SetResourceManagementLastCallUtc();
//        return new PassthroughResult
//        {
//            ResultCode = reply.ResultCode,
//            ReturnData = reply.ReturnData.ToByteArray()
//        };
//    }
//    catch (Exception ex)
//    {
//        ProcessRemoteMethodCallException(ex);
//        throw;
//    }

//    ElementValuesCollection? readValueList = null;
//    if (DataGrpcEndpointRoot.CreateChannelIfNotCreated(_readEndpoint))
//    {
//        try
//        {
//            readValueList = _readEndpoint.Proxy.ReadData(ContextId, listServerAlias,
//                serverAliases);


//            _readEndpoint.LastCallUtc = DateTime.UtcNow;
//        }
//        catch (Exception ex)
//        {
//            ProcessRemoteMethodCallException(ex);
//        }
//    }
//    return readValueList;
//}