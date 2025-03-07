using Grpc.Core;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.ServerBase
{
    internal interface IDataAccessService
    {
        Task<InitiateReply> InitiateAsync(InitiateRequest request);

        IAsyncStreamReader<CallbackMessage> SubscribeForCallback(SubscribeForCallbackRequest request);

        Task<UpdateContextParamsReply> UpdateContextParamsAsync(UpdateContextParamsRequest request);

        Task<ConcludeReply> ConcludeAsync(ConcludeRequest request);

        Task<ClientKeepAliveReply> ClientKeepAliveAsync(ClientKeepAliveRequest request, CancellationToken cancellationToken);

        Task<DefineListReply> DefineListAsync(DefineListRequest request);

        Task<DeleteListsReply> DeleteListsAsync(DeleteListsRequest request);

        Task<AddItemsToListReply> AddItemsToListAsync(AddItemsToListRequest request);

        Task<RemoveItemsFromListReply> RemoveItemsFromListAsync(RemoveItemsFromListRequest request);

        Task<EnableListCallbackReply> EnableListCallbackAsync(EnableListCallbackRequest request);

        Task<TouchListReply> TouchListAsync(TouchListRequest request);

        Task<List<(uint, ValueStatusTimestamp)>?> PollElementValuesChangesAsync(PollElementValuesChangesRequest request);

        Task<List<Utils.DataAccess.EventMessagesCollection>?> PollEventsChangesAsync(PollEventsChangesRequest request);

        Task<ReadOnlyMemory<byte>> ReadElementValuesJournalsAsync(ReadElementValuesJournalsRequest request);

        Task<List<Utils.DataAccess.EventMessagesCollection>> ReadEventMessagesJournalAsync(ReadEventMessagesJournalRequest request);

        Task<WriteElementValuesReply> WriteElementValuesAsync(uint listServerAlias, byte[] fullElementValuesCollection, string serverContextId);

        Task<AckAlarmsReply> AckAlarmsAsync(AckAlarmsRequest request);

        Task<ReadOnlyMemory<byte>> PassthroughAsync(string recipientPath, string passthroughName, ReadOnlyMemory<byte> dataToSend, string serverContextId);

        Task<LongrunningPassthroughReply> LongrunningPassthroughAsync(
            string recipientPath,
            string passthroughName,
            ReadOnlyMemory<byte> dataToSend,
            string serverContextId
            );

        Task<LongrunningPassthroughCancelReply> LongrunningPassthroughCancelAsync(LongrunningPassthroughCancelRequest request);
    } 
}
