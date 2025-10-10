using Google.Protobuf;
using Grpc.Core;
using Ssz.DataAccessGrpc.Client.ClientLists;
using Ssz.DataAccessGrpc.Common;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.Client
{
    internal class RemoteDataAccessService : IDataAccessService
    {
        #region construction and destruction

        public RemoteDataAccessService(DataAccess.DataAccessClient resourceManagementClient)
        {
            _resourceManagementClient = resourceManagementClient;
        }

        #endregion

        #region public functions

        public async Task<InitiateReply> InitiateAsync(InitiateRequest request)
        {
            return await _resourceManagementClient.InitiateAsync(request);
        }

        public IAsyncStreamReader<CallbackMessage> SubscribeForCallback(SubscribeForCallbackRequest request)
        {
            return _resourceManagementClient.SubscribeForCallback(request).ResponseStream;
        }

        public async Task<UpdateContextParamsReply> UpdateContextParamsAsync(UpdateContextParamsRequest request)
        {
            return await _resourceManagementClient.UpdateContextParamsAsync(request);
        }

        public async Task<ConcludeReply> ConcludeAsync(ConcludeRequest request)
        {
            return await _resourceManagementClient.ConcludeAsync(request);
        }

        public async Task<ClientKeepAliveReply> ClientKeepAliveAsync(ClientKeepAliveRequest request, CancellationToken cancellationToken)
        {
            return await _resourceManagementClient.ClientKeepAliveAsync(request, cancellationToken: cancellationToken);
        }

        public async Task<DefineListReply> DefineListAsync(DefineListRequest request)
        {
            return await _resourceManagementClient.DefineListAsync(request);
        }

        public async Task<DeleteListsReply> DeleteListsAsync(DeleteListsRequest request)
        {
            return await _resourceManagementClient.DeleteListsAsync(request);
        }

        public async Task<AddItemsToListReply> AddItemsToListAsync(AddItemsToListRequest request)
        {
            return await _resourceManagementClient.AddItemsToListAsync(request);
        }

        public async Task<RemoveItemsFromListReply> RemoveItemsFromListAsync(RemoveItemsFromListRequest request)
        {
            return await _resourceManagementClient.RemoveItemsFromListAsync(request);
        }

        public async Task<EnableListCallbackReply> EnableListCallbackAsync(EnableListCallbackRequest request)
        {
            return await _resourceManagementClient.EnableListCallbackAsync(request);
        }

        public async Task<TouchListReply> TouchListAsync(TouchListRequest request)
        {
            return await _resourceManagementClient.TouchListAsync(request);
        }

        public async Task<List<(uint, ValueStatusTimestamp)>?> PollElementValuesChangesAsync(PollElementValuesChangesRequest request)
        {
            var call = _resourceManagementClient.PollElementValuesChanges(request);            

            List<DataChunk> responseDataChunks = new();
#if NET5_0_OR_GREATER
            await foreach (var it in call.ResponseStream.ReadAllAsync())
            {                    
                responseDataChunks.Add(it.ElementValuesCollection);
            }      
#else
            while (await call.ResponseStream.MoveNext())
            {
                responseDataChunks.Add(call.ResponseStream.Current.ElementValuesCollection);
            }
#endif
            return responseDataChunks.Count > 0 ?
                ClientElementValueList.GetElementValues(ProtobufHelper.Combine(responseDataChunks)) :
                null;
        }

        public async Task<List<Utils.DataAccess.EventMessagesCollection>?> PollEventsChangesAsync(PollEventsChangesRequest request)
        {
            var call = _resourceManagementClient.PollEventsChanges(request);

            List<Utils.DataAccess.EventMessagesCollection> eventMessagesCollectionsList = new();

            List<DataChunk> responseByteStrings = new();
#if NET5_0_OR_GREATER
            await foreach (var it in call.ResponseStream.ReadAllAsync())
            {                    
                eventMessagesCollectionsList.Add(
                    ClientEventList.GetEventMessagesCollection(it)
                    );
            }      
#else
            while (await call.ResponseStream.MoveNext())
            {
                eventMessagesCollectionsList.Add(
                    ClientEventList.GetEventMessagesCollection(call.ResponseStream.Current)
                    );
            }
#endif

            return eventMessagesCollectionsList.Count > 0 ?
                eventMessagesCollectionsList :
                null;
        }

        public async Task<ReadOnlyMemory<byte>> ReadElementValuesJournalsAsync(ReadElementValuesJournalsRequest request)
        {
            var call = _resourceManagementClient.ReadElementValuesJournals(request);

            List<DataChunk> responseDataChunks = new();
#if NET5_0_OR_GREATER
                await foreach (DataChunk dataChunk in call.ResponseStream.ReadAllAsync())
                {                    
                    responseDataChunks.Add(dataChunk);
                }      
#else
            while (await call.ResponseStream.MoveNext())
            {
                responseDataChunks.Add(call.ResponseStream.Current);
            }
#endif
            return ProtobufHelper.Combine(responseDataChunks);
        }

        public async Task<List<Utils.DataAccess.EventMessagesCollection>> ReadEventMessagesJournalAsync(ReadEventMessagesJournalRequest request)
        {
            var call = _resourceManagementClient.ReadEventMessagesJournal(request);            

            List<Utils.DataAccess.EventMessagesCollection> result = new();
#if NET5_0_OR_GREATER
                await foreach (var eventMessagesCollection in call.ResponseStream.ReadAllAsync())
                {
                    CaseInsensitiveDictionary<string?>? commonFields = null;
                    if (eventMessagesCollection.CommonFieldsOrdered.Count > 0)
                    {
                        commonFields = new CaseInsensitiveDictionary<string?>(eventMessagesCollection.CommonFieldsOrdered
                                        .Select(f => new KeyValuePair<string, string?>(f.Name, f.Value.KindCase == NullableString.KindOneofCase.Data ? f.Value.Data : null)));
                    }
                    else if (eventMessagesCollection.CommonFields.Count > 0) // Obsolete for compatibility only
                    {
                        commonFields = new CaseInsensitiveDictionary<string?>(eventMessagesCollection.CommonFields
                                        .Select(cp => new KeyValuePair<string, string?>(cp.Key, cp.Value.KindCase == NullableString.KindOneofCase.Data ? cp.Value.Data : null)));
                    }

                    result.Add(
                        new Utils.DataAccess.EventMessagesCollection
                        {
                            EventMessages = eventMessagesCollection.EventMessages.Select(em => em.ToEventMessage()).ToList(),
                            CommonFields = commonFields
                        });
                }
#else
            while (await call.ResponseStream.MoveNext())
            {
                var eventMessagesCollection = call.ResponseStream.Current;

                CaseInsensitiveDictionary<string?>? commonFields = null;
                if (eventMessagesCollection.CommonFieldsOrdered.Count > 0)
                {
                    commonFields = new CaseInsensitiveDictionary<string?>(eventMessagesCollection.CommonFieldsOrdered
                                    .Select(f => new KeyValuePair<string, string?>(f.Name, f.Value.KindCase == NullableString.KindOneofCase.Data ? f.Value.Data : null)));
                }
                else if (eventMessagesCollection.CommonFields.Count > 0) // Obsolete for compatibility only
                {
                    commonFields = new CaseInsensitiveDictionary<string?>(eventMessagesCollection.CommonFields
                                    .Select(cp => new KeyValuePair<string, string?>(cp.Key, cp.Value.KindCase == NullableString.KindOneofCase.Data ? cp.Value.Data : null)));
                }

                result.Add(
                    new Utils.DataAccess.EventMessagesCollection
                    {
                        EventMessages = eventMessagesCollection.EventMessages.Select(em => em.ToEventMessage()).ToList(),
                        CommonFields = commonFields
                    });
            }
#endif
            return result;
        }

        public async Task<WriteElementValuesReply> WriteElementValuesAsync(uint listServerAlias, byte[] fullElementValuesCollection, string serverContextId)
        {
            var call = _resourceManagementClient.WriteElementValues();

            foreach (DataChunk dataChunk in ProtobufHelper.SplitForCorrectGrpcMessageSize(fullElementValuesCollection))
            {
                await call.RequestStream.WriteAsync(new WriteElementValuesRequest
                {
                    ContextId = serverContextId,
                    ListServerAlias = listServerAlias,
                    ElementValuesCollection = dataChunk
                });
            }
            await call.RequestStream.CompleteAsync();

            return await call.ResponseAsync;            
        }

        public async Task<AckAlarmsReply> AckAlarmsAsync(AckAlarmsRequest request)
        {
            return await _resourceManagementClient.AckAlarmsAsync(request);
        }

        public async Task<ReadOnlyMemory<byte>> PassthroughAsync(string recipientPath, string passthroughName, ReadOnlyMemory<byte> dataToSend, string serverContextId)
        {
            var call = _resourceManagementClient.Passthrough();            

            foreach (DataChunk dataChunk in ProtobufHelper.SplitForCorrectGrpcMessageSize(dataToSend))
            {
                await call.RequestStream.WriteAsync(new PassthroughRequest
                {
                    ContextId = serverContextId,
                    RecipientPath = recipientPath,
                    PassthroughName = passthroughName,
                    DataToSend = dataChunk
                });
            }
            await call.RequestStream.CompleteAsync();

            List<DataChunk> responseDataChunks = new();
#if NET5_0_OR_GREATER
                await foreach (DataChunk dataChunk in call.ResponseStream.ReadAllAsync())
                {                    
                    responseDataChunks.Add(dataChunk);
                }      
#else
            while (await call.ResponseStream.MoveNext())
            {
                responseDataChunks.Add(call.ResponseStream.Current);
            }
#endif
            return ProtobufHelper.Combine(responseDataChunks);
        }

        public async Task<LongrunningPassthroughReply> LongrunningPassthroughAsync(
            string recipientPath, 
            string passthroughName, 
            ReadOnlyMemory<byte> dataToSend,
            string serverContextId
            )
        {
            var call = _resourceManagementClient.LongrunningPassthrough();            

            foreach (DataChunk dataChunk in ProtobufHelper.SplitForCorrectGrpcMessageSize(dataToSend))
            {
                await call.RequestStream.WriteAsync(new Common.LongrunningPassthroughRequest
                {
                    ContextId = serverContextId,
                    RecipientPath = recipientPath,
                    PassthroughName = passthroughName,
                    DataToSend = dataChunk
                });
            }
            await call.RequestStream.CompleteAsync();

            return await call.ResponseAsync;            
        }

        public async Task<LongrunningPassthroughCancelReply> LongrunningPassthroughCancelAsync(LongrunningPassthroughCancelRequest request)
        {
            return await _resourceManagementClient.LongrunningPassthroughCancelAsync(request);
        }

        #endregion

        #region private fields

        private readonly DataAccess.DataAccessClient _resourceManagementClient;

        #endregion
    }
}
