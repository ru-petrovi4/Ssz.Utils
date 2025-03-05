using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.Client.LocalServer
{
    internal class DataAccessService
    {
        #region construction and destruction

        public DataAccessService(ILogger<DataAccessService> logger, IConfiguration configuration, IDataAccessServerWorker dataAccessServerWorker)
        {
            _logger = logger;
            _configuration = configuration;
            _dataAccessServerWorker = dataAccessServerWorker;            
        }

        #endregion

        #region public functions

        public async Task<InitiateReply> Initiate(InitiateRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
                {
                    CaseInsensitiveDictionary<string?> contextParams = new CaseInsensitiveDictionary<string?>(request.ContextParams
                            .Select(cp => new KeyValuePair<string, string?>(cp.Key, cp.Value.KindCase == NullableString.KindOneofCase.Data ? cp.Value.Data : null)));
                    string dataAccessClientUserName = ConfigurationHelper.GetValue(_configuration, @"DataAccessClientUserName", @"");
                    if (dataAccessClientUserName != @"")
                    {
                        string dataAccessClientPasswordHash = ConfigurationHelper.GetValue(_configuration, @"DataAccessClientPasswordHash", @"");
                        if (String.IsNullOrEmpty(dataAccessClientPasswordHash))
                            throw new RpcException(new Status(StatusCode.PermissionDenied, "Client password hash must be specified (DataAccessClientPasswordHash in appsettings.json)."));
                        byte[] dataAccessClientPasswordHashBytes = Convert.FromBase64String(dataAccessClientPasswordHash);
                        var clientUserNameInRequest = contextParams.TryGetValue(@"ClientUserName");
                        if (String.IsNullOrEmpty(clientUserNameInRequest))
                            throw new RpcException(new Status(StatusCode.PermissionDenied, "Client user name must be specified (ClientUserName context param)."));
                        if (!String.Equals(clientUserNameInRequest, dataAccessClientUserName, StringComparison.InvariantCultureIgnoreCase))
                            throw new RpcException(new Status(StatusCode.PermissionDenied, "Invalid client user name (ClientUserName context params).")); // Invalid client user name or password (ClientUserName or ClientPassword context params)
                        var clientPasswordInRequest = contextParams.TryGetValue(@"ClientPassword");
                        if (String.IsNullOrEmpty(clientPasswordInRequest))
                            throw new RpcException(new Status(StatusCode.PermissionDenied, "Client password must be specified (ClientPassword context param)."));
                        byte[] clientPasswordInRequestHashBytes;
                        using (SHA512 sha512 = SHA512.Create())
                        {
                            clientPasswordInRequestHashBytes = sha512.ComputeHash(new UTF8Encoding(false).GetBytes(clientPasswordInRequest));
                        }                        
                        if (!clientPasswordInRequestHashBytes.SequenceEqual(dataAccessClientPasswordHashBytes))
                            throw new RpcException(new Status(StatusCode.PermissionDenied, "Invalid client password (ClientPassword context params)."));
                    }
                    IDataAccessServerContext serverContext = _dataAccessServerWorker.AddServerContext(
                        _logger,
                        request.ClientApplicationName ?? @"",
                        request.ClientWorkstationName ?? @"",
                        request.RequestedServerContextTimeoutMs,
                        request.RequestedCultureName ?? @"",
                        request.SystemNameToConnect ?? @"",
                        contextParams);
                    serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                    var reply = new InitiateReply
                    {
                        ContextId = serverContext.ContextId,
                        ServerContextTimeoutMs = serverContext.ContextTimeoutMs,
                        ServerCultureName = serverContext.CultureInfo.Name
                    };
                    return Task.FromResult(reply);
                },
                context);
        }

        public async Task SubscribeForCallback(SubscribeForCallbackRequest request, IServerStreamWriter<CallbackMessage> responseStream, ServerCallContext context)
        {
            IDataAccessServerContext serverContext = await GetReplyAsync(() =>
            {
                IDataAccessServerContext serverContext = _dataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                // TODO
                //((ServerContext)serverContext).SetResponseStream(responseStream);
                return Task.FromResult(serverContext);
            },
                context);

            var token = serverContext.CallbackWorkingTask_CancellationTokenSource.Token;
            await Task.Run(() => { token.WaitHandle.WaitOne(); });
        }

        public async Task<UpdateContextParamsReply> UpdateContextParams(UpdateContextParamsRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
            {
                CaseInsensitiveDictionary<string?> contextParams = new CaseInsensitiveDictionary<string?>(request.ContextParams
                        .Select(cp => new KeyValuePair<string, string?>(cp.Key, cp.Value.KindCase == NullableString.KindOneofCase.Data ? cp.Value.Data : null)));

                IDataAccessServerContext serverContext = _dataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                serverContext.UpdateContextParams(contextParams);

                var reply = new UpdateContextParamsReply();
                return Task.FromResult(reply);
            },
                context);
        }

        public async Task<ConcludeReply> ConcludeAsync(ConcludeRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
            {
                IDataAccessServerContext? serverContext = _dataAccessServerWorker.TryLookupServerContext(request.ContextId ?? @"");
                if (serverContext is not null)
                {
                    serverContext.IsConcludeCalled = true;
                    _dataAccessServerWorker.RemoveServerContext(serverContext);
                    serverContext.Dispose();
                }
                return Task.FromResult(new ConcludeReply());
            },
                context);
        }

        public async Task<ClientKeepAliveReply> ClientKeepAlive(ClientKeepAliveRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
            {
                IDataAccessServerContext? serverContext = _dataAccessServerWorker.TryLookupServerContext(request.ContextId ?? @"");
                if (serverContext is not null)
                {
                    serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                }
                return Task.FromResult(new ClientKeepAliveReply());
            },
                context);
        }

        public async Task<DefineListReply> DefineList(DefineListRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
            {
                IDataAccessServerContext serverContext = _dataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                var reply = new DefineListReply();
                reply.Result = new ServerBase.AliasResult(serverContext.DefineList(request.ListClientAlias, request.ListType,
                    new Utils.CaseInsensitiveDictionary<string?>(request.ListParams
                        .Select(cp => new KeyValuePair<string, string?>(cp.Key, cp.Value.KindCase == NullableString.KindOneofCase.Data ? cp.Value.Data : null)))));
                return Task.FromResult(reply);
            },
                context);
        }

        public async Task<DeleteListsReply> DeleteLists(DeleteListsRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
            {
                IDataAccessServerContext serverContext = _dataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                var reply = new DeleteListsReply();
                reply.Results.Add(serverContext.DeleteLists(request.ListServerAliases.ToList()).Select(ar => new ServerBase.AliasResult(ar)));
                return Task.FromResult(reply);
            },
                context);
        }

        public async Task<AddItemsToListReply> AddItemsToList(AddItemsToListRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(async () =>
            {
                IDataAccessServerContext serverContext = _dataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                var reply = new AddItemsToListReply();
                reply.Results.Add((await serverContext.AddItemsToListAsync(request.ListServerAlias, request.ItemsToAdd.Select(i => i.ToListItemInfoMessage()).ToList()))
                    .Select(ar => new ServerBase.AliasResult(ar)).ToList());
                return reply;
            },
                context);
        }

        public async Task<RemoveItemsFromListReply> RemoveItemsFromList(RemoveItemsFromListRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(async () =>
            {
                IDataAccessServerContext serverContext = _dataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                var reply = new RemoveItemsFromListReply();
                reply.Results.Add((await serverContext.RemoveItemsFromListAsync(request.ListServerAlias, request.ServerAliasesToRemove.ToList()))
                    .Select(ar => new ServerBase.AliasResult(ar)).ToList());
                return reply;
            },
                context);
        }

        public async Task<EnableListCallbackReply> EnableListCallback(EnableListCallbackRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
            {
                IDataAccessServerContext serverContext = _dataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                bool isEnabled = request.Enable;
                var reply = new EnableListCallbackReply();
                serverContext.EnableListCallback(request.ListServerAlias, ref isEnabled);
                reply.Enabled = isEnabled;
                return Task.FromResult(reply);
            },
                context);
        }

        public async Task<TouchListReply> TouchList(TouchListRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
            {
                IDataAccessServerContext serverContext = _dataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                var reply = new TouchListReply();
                serverContext.TouchList(request.ListServerAlias);
                return Task.FromResult(reply);
            },
                context);
        }

        public async Task PollElementValuesChanges(PollElementValuesChangesRequest request, IServerStreamWriter<ElementValuesCallback> responseStream, ServerCallContext context)
        {
            await GetReplyAsync(async () =>
            {
                IDataAccessServerContext serverContext = _dataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                ElementValuesCallbackMessage? elementValuesCallbackMessage = await serverContext.PollElementValuesChangesAsync(request.ListServerAlias);
                if (elementValuesCallbackMessage is not null)
                {
                    foreach (var elementValuesCallback in elementValuesCallbackMessage.SplitForCorrectGrpcMessageSize())
                    {
                        await responseStream.WriteAsync(elementValuesCallback);
                    }
                }
                return 0;
            },
                context);
        }

        public async Task PollEventsChanges(PollEventsChangesRequest request, IServerStreamWriter<ServerBase.EventMessagesCollection> responseStream, ServerCallContext context)
        {
            await GetReplyAsync(async () =>
            {
                IDataAccessServerContext serverContext = _dataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                List<EventMessagesCallbackMessage>? eventMessagesCallbackMessages = await serverContext.PollEventsChangesAsync(request.ListServerAlias);
                if (eventMessagesCallbackMessages is not null)
                {
                    foreach (var fullEventMessagesCallbackMessage in eventMessagesCallbackMessages)
                    {
                        foreach (var eventMessagesCallbackMessage in fullEventMessagesCallbackMessage.SplitForCorrectGrpcMessageSize())
                        {
                            await responseStream.WriteAsync(eventMessagesCallbackMessage.EventMessagesCollection);
                        }
                    }
                }
                return 0;
            },
                context);
        }

        public async Task ReadElementValuesJournals(ReadElementValuesJournalsRequest request, IServerStreamWriter<DataChunk> responseStream, ServerCallContext context)
        {
            await GetReplyAsync(async () =>
            {
                IDataAccessServerContext serverContext = _dataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                byte[] bytes = await serverContext.ReadElementValuesJournalsAsync(
                        request.ListServerAlias,
                        request.FirstTimestamp.ToDateTime(),
                        request.SecondTimestamp.ToDateTime(),
                        request.NumValuesPerAlias,
                        request.Calculation.ToTypeId(),
                        new CaseInsensitiveDictionary<string?>(request.Params
                            .Select(cp => new KeyValuePair<string, string?>(cp.Key, cp.Value.KindCase == NullableString.KindOneofCase.Data ? cp.Value.Data : null))),
                        request.ServerAliases.ToList()
                    );
                foreach (DataChunk dataChunk in ProtobufHelper.SplitForCorrectGrpcMessageSize(bytes))
                {
                    await responseStream.WriteAsync(dataChunk);
                }
                    ;
                return 0;
            },
                context);
        }

        public async Task ReadEventMessagesJournal(ReadEventMessagesJournalRequest request, IServerStreamWriter<ServerBase.EventMessagesCollection> responseStream, ServerCallContext context)
        {
            await GetReplyAsync(async () =>
            {
                IDataAccessServerContext serverContext = _dataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                EventMessagesCallbackMessage? fullEventMessagesCallbackMessage = await serverContext.ReadEventMessagesJournalAsync(
                        request.ListServerAlias,
                        request.FirstTimestamp.ToDateTime(),
                        request.SecondTimestamp.ToDateTime(),
                        new Utils.CaseInsensitiveDictionary<string?>(request.Params
                            .Select(cp => new KeyValuePair<string, string?>(cp.Key, cp.Value.KindCase == NullableString.KindOneofCase.Data ? cp.Value.Data : null))));
                if (fullEventMessagesCallbackMessage is not null)
                {
                    foreach (var eventMessagesCallbackMessage in fullEventMessagesCallbackMessage.SplitForCorrectGrpcMessageSize())
                    {
                        await responseStream.WriteAsync(eventMessagesCallbackMessage.EventMessagesCollection);
                    }
                }
                return 0;
            },
                context);
        }

        public async Task<WriteElementValuesReply> WriteElementValues(IAsyncStreamReader<WriteElementValuesRequest> requestStream, ServerCallContext context)
        {
            List<ByteString> requestByteStrings = new();
            WriteElementValuesRequest? request = null;
            await foreach (WriteElementValuesRequest writeElementValuesRequest in requestStream.ReadAllAsync())
            {
                request = writeElementValuesRequest;
                requestByteStrings.Add(writeElementValuesRequest.ElementValuesCollection.Bytes);
            }
            if (request is null)
                throw new RpcException(new Status(StatusCode.Internal, "Invalid request."));

            ReadOnlyMemory<byte> elementValuesCollectionBytes = ProtobufHelper.Combine(requestByteStrings);

            return await GetReplyAsync(async () =>
            {
                var reply = new WriteElementValuesReply();
                IDataAccessServerContext serverContext = _dataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                var aliasResults = await serverContext.WriteElementValuesAsync(request.ListServerAlias, elementValuesCollectionBytes);
                if (aliasResults is not null)
                    reply.Results.Add(aliasResults.Select(ar => new ServerBase.AliasResult(ar)));
                return reply;
            },
                context);
        }

        public async Task<AckAlarmsReply> AckAlarms(AckAlarmsRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
            {
                IDataAccessServerContext serverContext = _dataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                var reply = new AckAlarmsReply();
                reply.Results.Add(serverContext.AckAlarms(request.ListServerAlias,
                    request.OperatorName ?? @"", request.Comment ?? @"", request.EventIdsToAck.Select(e => e.ToEventId()))
                    .Select(e => new ServerBase.EventIdResult(e)));
                return Task.FromResult(reply);
            },
                context);
        }

        public async Task Passthrough(IAsyncStreamReader<PassthroughRequest> requestStream, IServerStreamWriter<DataChunk> responseStream, ServerCallContext context)
        {
            List<ByteString> requestByteStrings = new();
            PassthroughRequest? request = null;
            await foreach (PassthroughRequest passthroughRequest in requestStream.ReadAllAsync())
            {
                request = passthroughRequest;
                requestByteStrings.Add(passthroughRequest.DataToSend.Bytes);
            }
            if (request is null)
                throw new RpcException(new Status(StatusCode.Internal, "Invalid request."));

            ReadOnlyMemory<byte> dataToSend = ProtobufHelper.Combine(requestByteStrings);

            await GetReplyAsync(async () =>
            {
                IDataAccessServerContext serverContext = _dataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                ReadOnlyMemory<byte> returnData = await serverContext.PassthroughAsync(request.RecipientPath ?? @"", request.PassthroughName ?? @"", dataToSend);
                foreach (var dataChunk in ProtobufHelper.SplitForCorrectGrpcMessageSize(returnData))
                {
                    await responseStream.WriteAsync(dataChunk);
                }
                return 0;
            },
                context);
        }

        public async Task<LongrunningPassthroughReply> LongrunningPassthrough(IAsyncStreamReader<LongrunningPassthroughRequest> requestStream, ServerCallContext context)
        {
            List<ByteString> requestByteStrings = new();
            LongrunningPassthroughRequest? request = null;
            await foreach (LongrunningPassthroughRequest longrunningPassthroughRequest in requestStream.ReadAllAsync())
            {
                request = longrunningPassthroughRequest;
                requestByteStrings.Add(longrunningPassthroughRequest.DataToSend.Bytes);
            }
            if (request is null)
                throw new RpcException(new Status(StatusCode.Internal, "Invalid request."));

            ReadOnlyMemory<byte> dataToSend = ProtobufHelper.Combine(requestByteStrings);

            return await GetReplyAsync(() =>
            {
                var reply = new LongrunningPassthroughReply();
                IDataAccessServerContext serverContext = _dataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                reply.JobId = serverContext.LongrunningPassthrough(request.RecipientPath ?? @"", request.PassthroughName ?? @"", dataToSend);
                return Task.FromResult(reply);
            },
                context);
        }

        public async Task<LongrunningPassthroughCancelReply> LongrunningPassthroughCancel(LongrunningPassthroughCancelRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
            {
                var reply = new LongrunningPassthroughCancelReply();
                IDataAccessServerContext serverContext = _dataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                serverContext.LongrunningPassthroughCancel(request.JobId ?? @"");
                return Task.FromResult(reply);
            },
                context);
        }

        #endregion

        #region private functions

        private Task<T> GetReplyAsync<T>(Func<Task<T>> func, ServerCallContext context)
        {
            string parentMethodName = "";
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                var st = new StackTrace();
                //foreach (var f in st.GetFrames())
                //{
                //    parentMethodName += "->" + f.GetMethod()?.Name;
                //}
                var sf = st.GetFrame(7);
                if (sf is not null)
                {
                    parentMethodName = sf.GetMethod()?.Name ?? "";
                }
            }

            var taskCompletionSource = new TaskCompletionSource<T>();
            //context.CancellationToken.Register(() => taskCompletionSource.TrySetCanceled(), useSynchronizationContext: false);
            _dataAccessServerWorker.ThreadSafeDispatcher.BeginInvoke(async ct =>
            {
                _logger.LogTrace("Processing client call in worker thread: " + parentMethodName);
                try
                {
                    var result = await func();
                    taskCompletionSource.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    taskCompletionSource.TrySetException(ex);
                }
            });
            try
            {
                return taskCompletionSource.Task;
            }
            catch (OperationCanceledException ex)
            {
                string message = @"Operation cancelled.";
                _logger.LogDebug(ex, message);
                throw new RpcException(new Status(StatusCode.Cancelled, message));
            }
            catch (RpcException ex)
            {
                string message = @"RPC Exception";
                _logger.LogError(ex, message);
                throw;
            }
            catch (Exception ex)
            {
                string message = @"General Exception";
                _logger.LogCritical(ex, message);
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }
        }

        #endregion

        #region private fields

        private readonly ILogger<DataAccessService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IDataAccessServerWorker _dataAccessServerWorker;

        #endregion
    }

    //public class LocalizedMessageException : Exception
    //{
    //    #region construction and destruction

    //    public LocalizedMessageException(string message, Exception? innerException) :
    //        base(message, innerException)
    //    {
    //    }

    //    #endregion
    //}
}