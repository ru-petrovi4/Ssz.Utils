using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.DataGrpc.Client.ClientLists;
using Ssz.Utils.DataAccess;
using Ssz.DataGrpc.Server;
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
        /// <param name="dataGrpcElementValueJournalList"></param>
        /// <param name="firstTimestampUtc"></param>
        /// <param name="secondTimestampUtc"></param>
        /// <param name="numValuesPerAlias"></param>
        /// <param name="calculation"></param>
        /// <param name="_params"></param>
        /// <param name="serverAliases"></param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public ValueStatusTimestamp[][] ReadElementValueJournals(ClientElementValueJournalList dataGrpcElementValueJournalList, DateTime firstTimestampUtc,
            DateTime secondTimestampUtc,
            uint numValuesPerAlias, Ssz.Utils.DataAccess.TypeId calculation, CaseInsensitiveDictionary<string>? _params, uint[] serverAliases)
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
                        FirstTimestamp = DateTimeHelper.ConvertToTimestamp(firstTimestampUtc),
                        SecondTimestamp = DateTimeHelper.ConvertToTimestamp(secondTimestampUtc),
                        NumValuesPerAlias = numValuesPerAlias,
                        Calculation = new Server.TypeId(calculation),                        
                    };
                    if (_params is not null)
                        request.Params.Add(_params);
                    request.ServerAliases.Add(serverAliases);
                    ReadElementValueJournalsReply reply = _resourceManagementClient.ReadElementValueJournals(request);
                    SetResourceManagementLastCallUtc();

                    var result = dataGrpcElementValueJournalList.OnReadElementValueJournal(reply.ElementValueJournalsCollection);
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