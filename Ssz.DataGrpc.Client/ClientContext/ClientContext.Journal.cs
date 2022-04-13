using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.DataGrpc.Client.ClientLists;
using Ssz.Utils.DataAccess;
using Ssz.DataGrpc.ServerBase;
using Ssz.Utils;

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
        /// <param name="clientElementValuesJournalList"></param>
        /// <param name="firstTimestampUtc"></param>
        /// <param name="secondTimestampUtc"></param>
        /// <param name="numValuesPerAlias"></param>
        /// <param name="calculation"></param>
        /// <param name="params_"></param>
        /// <param name="serverAliases"></param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public ValueStatusTimestamp[][] ReadElementValuesJournals(ClientElementValuesJournalList clientElementValuesJournalList, DateTime firstTimestampUtc,
            DateTime secondTimestampUtc,
            uint numValuesPerAlias, Ssz.Utils.DataAccess.TypeId? calculation, CaseInsensitiveDictionary<string>? params_, uint[] serverAliases)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed ClientContext.");

            if (!ServerContextIsOperational) throw new InvalidOperationException();

            try
            {
                while (true)
                {
                    var request = new ReadElementValuesJournalsRequest
                    {
                        ContextId = _serverContextId,
                        ListServerAlias = clientElementValuesJournalList.ListServerAlias,
                        FirstTimestamp = DateTimeHelper.ConvertToTimestamp(firstTimestampUtc),
                        SecondTimestamp = DateTimeHelper.ConvertToTimestamp(secondTimestampUtc),
                        NumValuesPerAlias = numValuesPerAlias,
                        Calculation = new Server.TypeId(calculation),                        
                    };
                    if (params_ is not null)
                        request.Params.Add(params_);
                    request.ServerAliases.Add(serverAliases);
                    ReadElementValuesJournalsReply reply = _resourceManagementClient.ReadElementValuesJournals(request);
                    SetResourceManagementLastCallUtc();

                    var result = clientElementValuesJournalList.OnReadElementValuesJournals(reply.ElementValuesJournalsCollection);
                    if (result is not null) return result;
                }
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
                throw;
            }
        }

        public Server.EventMessage[] ReadEventMessagesJournal(ClientEventList clientEventList, DateTime firstTimestampUtc, DateTime secondTimestampUtc, CaseInsensitiveDictionary<string>? params_)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed ClientContext.");

            if (!ServerContextIsOperational) throw new InvalidOperationException();

            try
            {
                while (true)
                {
                    var request = new ReadEventMessagesJournalRequest
                    {
                        ContextId = _serverContextId,
                        ListServerAlias = clientEventList.ListServerAlias,
                        FirstTimestamp = DateTimeHelper.ConvertToTimestamp(firstTimestampUtc),
                        SecondTimestamp = DateTimeHelper.ConvertToTimestamp(secondTimestampUtc),                        
                    };
                    if (params_ is not null)
                        request.Params.Add(params_);                    
                    ReadEventMessagesJournalReply reply = _resourceManagementClient.ReadEventMessagesJournal(request);
                    SetResourceManagementLastCallUtc();

                    var result = clientEventList.EventMessagesCallback(reply.EventMessagesCollection);
                    if (result is not null) return result;
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