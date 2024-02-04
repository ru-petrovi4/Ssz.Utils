using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.DataAccessGrpc.Client.ClientLists;
using Ssz.Utils.DataAccess;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils;
using Google.Protobuf.WellKnownTypes;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.Client
{
    /// <summary>
    ///     This partial class defines the IRead related aspects of the ClientContext class.
    /// </summary>
    internal partial class ClientContext
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
        public async Task<ValueStatusTimestamp[][]> ReadElementValuesJournalsAsync(ClientElementValuesJournalList clientElementValuesJournalList, DateTime firstTimestampUtc,
            DateTime secondTimestampUtc,
            uint numValuesPerAlias, Ssz.Utils.DataAccess.TypeId? calculation, CaseInsensitiveDictionary<string?>? params_, uint[] serverAliases)
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
                        FirstTimestamp = ProtobufHelper.ConvertToTimestamp(firstTimestampUtc),
                        SecondTimestamp = ProtobufHelper.ConvertToTimestamp(secondTimestampUtc),
                        NumValuesPerAlias = numValuesPerAlias,
                        Calculation = new ServerBase.TypeId(calculation),                        
                    };
                    if (params_ is not null)
                        foreach (var kvp in params_)
                            request.Params.Add(kvp.Key,
                                kvp.Value is not null ? new NullableString { Data = kvp.Value } : new NullableString { Null = NullValue.NullValue });
                    request.ServerAliases.Add(serverAliases);
                    ReadElementValuesJournalsReply reply = await _resourceManagementClient.ReadElementValuesJournalsAsync(request);
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

        public async Task<Utils.DataAccess.EventMessagesCollection> ReadEventMessagesJournalAsync(ClientEventList clientEventList, DateTime firstTimestampUtc, DateTime secondTimestampUtc, CaseInsensitiveDictionary<string?>? params_)
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
                        FirstTimestamp = ProtobufHelper.ConvertToTimestamp(firstTimestampUtc),
                        SecondTimestamp = ProtobufHelper.ConvertToTimestamp(secondTimestampUtc),                        
                    };
                    if (params_ is not null)
                        foreach (var kvp in params_)
                            request.Params.Add(kvp.Key,
                                kvp.Value is not null ? new NullableString { Data = kvp.Value } : new NullableString { Null = NullValue.NullValue });
                    ReadEventMessagesJournalReply reply = await _resourceManagementClient.ReadEventMessagesJournalAsync(request);
                    SetResourceManagementLastCallUtc();

                    var result = clientEventList.GetEventMessagesCollection(reply.EventMessagesCollection);
                    if (result is not null) 
                        return result;
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