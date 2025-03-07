using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.DataAccessGrpc.Client.ClientLists;
using Ssz.Utils.DataAccess;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils;
using Google.Protobuf.WellKnownTypes;
using System.Threading.Tasks;
using Grpc.Core;
using CommunityToolkit.HighPerformance;
using Ssz.Utils.Serialization;

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
        /// <param name="elementValuesJournalList"></param>
        /// <param name="firstTimestampUtc"></param>
        /// <param name="secondTimestampUtc"></param>
        /// <param name="numValuesPerAlias"></param>
        /// <param name="calculation"></param>
        /// <param name="params_"></param>
        /// <param name="serverAliases"></param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<ElementValuesJournal[]> ReadElementValuesJournalsAsync(
            ClientElementValuesJournalList elementValuesJournalList, 
            DateTime firstTimestampUtc,
            DateTime secondTimestampUtc,
            uint numValuesPerAlias, 
            Ssz.Utils.DataAccess.TypeId? calculation, 
            CaseInsensitiveDictionary<string?>? params_, 
            uint[] serverAliases)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed ClientContext.");

            if (!ContextIsOperational) throw new InvalidOperationException();

            try
            {
                var request = new ReadElementValuesJournalsRequest
                {
                    ContextId = _serverContextId,
                    ListServerAlias = elementValuesJournalList.ListServerAlias,
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
                var reply = await _dataAccessService.ReadElementValuesJournalsAsync(request);
                SetResourceManagementLastCallUtc();

                ElementValuesJournal[]? result = null;

                using (var stream = reply.AsStream())
                using (var reader = new SerializationReader(stream))
                {
                    using (Block block = reader.EnterBlock())
                    {
                        switch (block.Version)
                        {
                            case 1:
                                result = reader.ReadArrayOfOwnedDataSerializable<ElementValuesJournal>(() => new ElementValuesJournal(), null);
                                break;
                            default:
                                throw new BlockUnsupportedVersionException();
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
                throw;
            }
        }

        public async Task<List<Utils.DataAccess.EventMessagesCollection>> ReadEventMessagesJournalAsync(ClientEventList eventList, DateTime firstTimestampUtc, DateTime secondTimestampUtc, CaseInsensitiveDictionary<string?>? params_)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed ClientContext.");

            if (!ContextIsOperational) throw new InvalidOperationException();

            try
            {
                var request = new ReadEventMessagesJournalRequest
                {
                    ContextId = _serverContextId,
                    ListServerAlias = eventList.ListServerAlias,
                    FirstTimestamp = ProtobufHelper.ConvertToTimestamp(firstTimestampUtc),
                    SecondTimestamp = ProtobufHelper.ConvertToTimestamp(secondTimestampUtc),
                };
                if (params_ is not null)
                    foreach (var kvp in params_)
                        request.Params.Add(kvp.Key,
                            kvp.Value is not null ? new NullableString { Data = kvp.Value } : new NullableString { Null = NullValue.NullValue });
                var reply = await _dataAccessService.ReadEventMessagesJournalAsync(request);
                SetResourceManagementLastCallUtc();
                return reply;
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