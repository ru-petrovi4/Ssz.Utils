using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.ServerBase
{
    public partial class DataAccessService : DataAccess.DataAccessBase
    {
        #region construction and destruction

        public DataAccessService(ILogger<DataAccessService> logger, IConfiguration configuration, ServerWorkerBase serverWorker)
        {
            _logger = logger;
            _configuration = configuration;
            _serverWorker = serverWorker;            
        }

        #endregion

        #region public functions

        public override async Task<InitiateReply> Initiate(InitiateRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
                {
                    CaseInsensitiveDictionary<string?> contextParams = new CaseInsensitiveDictionary<string?>(request.ContextParams
                            .Select(cp => KeyValuePair.Create(cp.Key, cp.Value.KindCase == NullableString.KindOneofCase.Data ? cp.Value.Data : null)));
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
                        byte[] clientPasswordInRequestHashBytes = SHA512.HashData(new UTF8Encoding(false).GetBytes(clientPasswordInRequest));
                        if (!clientPasswordInRequestHashBytes.SequenceEqual(dataAccessClientPasswordHashBytes))
                            throw new RpcException(new Status(StatusCode.PermissionDenied, "Invalid client password (ClientPassword context params)."));
                    }
                    var serverContext = new ServerContext(
                        _logger,
                        _serverWorker,
                        request.ClientApplicationName ?? @"", 
                        request.ClientWorkstationName ?? @"",
                        request.RequestedServerContextTimeoutMs,
                        request.RequestedCultureName ?? @"",
                        request.SystemNameToConnect ?? @"",
                        contextParams
                        );
                    serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                    _serverWorker.AddServerContext(serverContext);                    
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

        public override async Task SubscribeForCallback(SubscribeForCallbackRequest request, IServerStreamWriter<CallbackMessage> responseStream, ServerCallContext context)
        {
            ServerContext serverContext = await GetReplyAsync(() =>
                {
                    ServerContext serverContext = _serverWorker.LookupServerContext(request.ContextId ?? @"");
                    serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                    serverContext.SetResponseStream(responseStream);
                    return Task.FromResult(serverContext);
                },
                context);

            var token = serverContext.CallbackWorkingTask_CancellationTokenSource.Token;
            await Task.Run(() => { token.WaitHandle.WaitOne(); });
        }

        public override async Task<UpdateContextParamsReply> UpdateContextParams(UpdateContextParamsRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
            {
                CaseInsensitiveDictionary<string?> contextParams = new CaseInsensitiveDictionary<string?>(request.ContextParams
                        .Select(cp => KeyValuePair.Create(cp.Key, cp.Value.KindCase == NullableString.KindOneofCase.Data ? cp.Value.Data : null)));

                ServerContext serverContext = _serverWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                serverContext.UpdateContextParams(contextParams);                

                var reply = new UpdateContextParamsReply();
                return Task.FromResult(reply);
            },
                context);
        }

        public override async Task<ConcludeReply> Conclude(ConcludeRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(async () =>
                {
                    ServerContext? serverContext = _serverWorker.TryLookupServerContext(request.ContextId ?? @"");
                    if (serverContext is not null)
                    {
                        serverContext.IsConcludeCalled = true;
                        _serverWorker.RemoveServerContext(serverContext);
                        await serverContext.DisposeAsync();
                    }
                    return new ConcludeReply();
                },
                context);
        }

        public override async Task<ClientKeepAliveReply> ClientKeepAlive(ClientKeepAliveRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
                {
                    ServerContext? serverContext = _serverWorker.TryLookupServerContext(request.ContextId ?? @"");
                    if (serverContext is not null)
                    {
                        serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                    }
                    return Task.FromResult(new ClientKeepAliveReply());
                },
                context);
        }

        public override async Task<DefineListReply> DefineList(DefineListRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
                {
                    ServerContext serverContext = _serverWorker.LookupServerContext(request.ContextId ?? @"");
                    serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;                    
                    var reply = new DefineListReply();
                    reply.Result = serverContext.DefineList(request.ListClientAlias, request.ListType,
                        new Utils.CaseInsensitiveDictionary<string?>(request.ListParams
                            .Select(cp => new KeyValuePair<string, string?>(cp.Key, cp.Value.KindCase == NullableString.KindOneofCase.Data ? cp.Value.Data : null))));
                    return Task.FromResult(reply);
                },
                context);
        }

        public override async Task<DeleteListsReply> DeleteLists(DeleteListsRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
                {
                    ServerContext serverContext = _serverWorker.LookupServerContext(request.ContextId ?? @"");
                    serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                    var reply = new DeleteListsReply();
                    reply.Results.Add(serverContext.DeleteLists(request.ListServerAliases.ToList()));
                    return Task.FromResult(reply);
                },
                context);
        }

        public override async Task<AddItemsToListReply> AddItemsToList(AddItemsToListRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(async () =>
                {
                    ServerContext serverContext = _serverWorker.LookupServerContext(request.ContextId ?? @"");
                    serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                    var reply = new AddItemsToListReply();
                    reply.Results.Add(await serverContext.AddItemsToListAsync(request.ListServerAlias, request.ItemsToAdd.ToList()));
                    return reply;
                },
                context);
        }

        public override async Task<RemoveItemsFromListReply> RemoveItemsFromList(RemoveItemsFromListRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(async () =>
                {
                    ServerContext serverContext = _serverWorker.LookupServerContext(request.ContextId ?? @"");
                    serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                    var reply = new RemoveItemsFromListReply();
                    reply.Results.Add(await serverContext.RemoveItemsFromListAsync(request.ListServerAlias, request.ServerAliasesToRemove.ToList()));
                    return reply;
                },
                context);
        }

        public override async Task<EnableListCallbackReply> EnableListCallback(EnableListCallbackRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
                {
                    ServerContext serverContext = _serverWorker.LookupServerContext(request.ContextId ?? @"");
                    serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                    bool isEnabled = request.Enable;
                    var reply = new EnableListCallbackReply();                    
                    serverContext.EnableListCallback(request.ListServerAlias, ref isEnabled);
                    reply.Enabled = isEnabled;
                    return Task.FromResult(reply);
                },
                context);
        }

        public override async Task<TouchListReply> TouchList(TouchListRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
                { 
                    ServerContext serverContext = _serverWorker.LookupServerContext(request.ContextId ?? @"");
                    serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                    var reply = new TouchListReply();
                    serverContext.TouchList(request.ListServerAlias);
                    return Task.FromResult(reply);
                },
                context);
        }

        public override async Task PollElementValuesChanges(PollElementValuesChangesRequest request, IServerStreamWriter<ElementValuesCallback> responseStream, ServerCallContext context)
        {
            await GetReplyAsync(async () =>
                {
                    ServerContext serverContext = _serverWorker.LookupServerContext(request.ContextId ?? @"");
                    serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                    await serverContext.PollElementValuesChangesAsync(request.ListServerAlias, responseStream);
                    return 0;
                },
                context);
        }

        public override async Task PollEventsChanges(PollEventsChangesRequest request, IServerStreamWriter<EventMessagesCollection> responseStream, ServerCallContext context)
        {
            await GetReplyAsync(async () =>
                {
                    ServerContext serverContext = _serverWorker.LookupServerContext(request.ContextId ?? @"");
                    serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;                    
                    await serverContext.PollEventsChangesAsync(request.ListServerAlias, responseStream);
                    return 0;
                },
                context);
        }

        public override async Task ReadElementValuesJournals(ReadElementValuesJournalsRequest request, IServerStreamWriter<DataChunk> responseStream, ServerCallContext context)
        {            
            await GetReplyAsync(async () =>
                {
                    ServerContext serverContext = _serverWorker.LookupServerContext(request.ContextId ?? @"");
                    serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;                    
                    await serverContext.ReadElementValuesJournalsAsync(
                            request.ListServerAlias,
                            request.FirstTimestamp.ToDateTime(),
                            request.SecondTimestamp.ToDateTime(),
                            request.NumValuesPerAlias,
                            request.Calculation,
                            new CaseInsensitiveDictionary<string?>(request.Params
                                .Select(cp => KeyValuePair.Create(cp.Key, cp.Value.KindCase == NullableString.KindOneofCase.Data ? cp.Value.Data : null))),                        
                            request.ServerAliases.ToList(),
                            responseStream
                        );
                    return 0;
                }, 
                context);
        }

        public override async Task ReadEventMessagesJournal(ReadEventMessagesJournalRequest request, IServerStreamWriter<EventMessagesCollection> responseStream, ServerCallContext context)
        {
            await GetReplyAsync(async () =>
                {
                    ServerContext serverContext = _serverWorker.LookupServerContext(request.ContextId ?? @"");
                    serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;                    
                    await serverContext.ReadEventMessagesJournalAsync(
                            request.ListServerAlias,
                            request.FirstTimestamp.ToDateTime(),
                            request.SecondTimestamp.ToDateTime(),
                            new Utils.CaseInsensitiveDictionary<string?>(request.Params
                                .Select(cp => new KeyValuePair<string, string?>(cp.Key, cp.Value.KindCase == NullableString.KindOneofCase.Data ? cp.Value.Data : null))),
                                responseStream);
                    return 0;
                }, 
                context);
        }

        public override async Task<WriteElementValuesReply> WriteElementValues(IAsyncStreamReader<WriteElementValuesRequest> requestStream, ServerCallContext context)
        {
            List<ByteString> requestByteStrings = new();
            WriteElementValuesRequest? request = null;
            await foreach (WriteElementValuesRequest writeElementValuesRequest in requestStream.ReadAllAsync())
            {
                request = writeElementValuesRequest;
                requestByteStrings.Add(writeElementValuesRequest.ElementValuesCollection);
            }
            if (request is null)
                throw new RpcException(new Status(StatusCode.Internal, "Invalid request."));

            ReadOnlyMemory<byte> elementValuesCollectionBytes = ProtobufHelper.Combine(requestByteStrings);

            return await GetReplyAsync(async () =>
                {
                    var reply = new WriteElementValuesReply();
                    ServerContext serverContext = _serverWorker.LookupServerContext(request.ContextId ?? @"");
                    serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                    var aliasResults = await serverContext.WriteElementValuesAsync(request.ListServerAlias, elementValuesCollectionBytes);
                    if (aliasResults is not null)
                        reply.Results.Add(aliasResults);
                    return reply;
                },
                context);                        
        }

        public override async Task<AckAlarmsReply> AckAlarms(AckAlarmsRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
                {
                    ServerContext serverContext = _serverWorker.LookupServerContext(request.ContextId ?? @"");
                    serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                    var reply = new AckAlarmsReply();
                    reply.Results.Add(serverContext.AckAlarms(request.ListServerAlias, 
                        request.OperatorName ?? @"", request.Comment ?? @"", request.EventIdsToAck));
                    return Task.FromResult(reply);
                },
                context);
        }

        public override async Task Passthrough(IAsyncStreamReader<PassthroughRequest> requestStream, IServerStreamWriter<DataChunk> responseStream, ServerCallContext context)
        {
            List<ByteString> requestByteStrings = new();
            PassthroughRequest? request = null;
            await foreach (PassthroughRequest passthroughRequest in requestStream.ReadAllAsync())
            {
                request = passthroughRequest;
                requestByteStrings.Add(passthroughRequest.DataToSend);
            }
            if (request is null)
                throw new RpcException(new Status(StatusCode.Internal, "Invalid request."));

            ReadOnlyMemory<byte> dataToSend = ProtobufHelper.Combine(requestByteStrings);

            await GetReplyAsync(async () =>
                {
                    ServerContext serverContext = _serverWorker.LookupServerContext(request.ContextId ?? @"");
                    serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                    await serverContext.PassthroughAsync(request.RecipientPath ?? @"", request.PassthroughName ?? @"", dataToSend, responseStream);
                    return 0;
                },
                context);
        }

        public override async Task<LongrunningPassthroughReply> LongrunningPassthrough(IAsyncStreamReader<LongrunningPassthroughRequest> requestStream, ServerCallContext context)
        {
            List<ByteString> requestByteStrings = new();
            LongrunningPassthroughRequest? request = null;
            await foreach (LongrunningPassthroughRequest longrunningPassthroughRequest in requestStream.ReadAllAsync())
            {
                request = longrunningPassthroughRequest;
                requestByteStrings.Add(longrunningPassthroughRequest.DataToSend);
            }
            if (request is null)
                throw new RpcException(new Status(StatusCode.Internal, "Invalid request."));

            ReadOnlyMemory<byte> dataToSend = ProtobufHelper.Combine(requestByteStrings);

            return await GetReplyAsync(() =>
                {
                    var reply = new LongrunningPassthroughReply();
                    ServerContext serverContext = _serverWorker.LookupServerContext(request.ContextId ?? @"");
                    serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                    reply.JobId = serverContext.LongrunningPassthrough(request.RecipientPath ?? @"", request.PassthroughName ?? @"", dataToSend);
                    return Task.FromResult(reply);
                },
                context);
        }

        public override async Task<LongrunningPassthroughCancelReply> LongrunningPassthroughCancel(LongrunningPassthroughCancelRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
                {
                    var reply = new LongrunningPassthroughCancelReply();
                    ServerContext serverContext = _serverWorker.LookupServerContext(request.ContextId ?? @"");
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
            _serverWorker.ThreadSafeDispatcher.BeginInvoke(async ct =>
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
        private readonly ServerWorkerBase _serverWorker;        

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
