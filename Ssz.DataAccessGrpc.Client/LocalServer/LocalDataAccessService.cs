using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ssz.DataAccessGrpc.Common;
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
    internal class LocalDataAccessService : IDataAccessService
    {
        #region construction and destruction

        public LocalDataAccessService(ILogger logger, IDataAccessServerWorker localDataAccessServerWorker)
        {
            _logger = logger;            
            _localDataAccessServerWorker = localDataAccessServerWorker;            
        }

        #endregion

        #region public functions

        public async Task<InitiateReply> InitiateAsync(InitiateRequest request)
        {
            return await GetReplyAsync(() =>
                {
                    CaseInsensitiveDictionary<string?> contextParams = new CaseInsensitiveDictionary<string?>(request.ContextParams
                            .Select(cp => new KeyValuePair<string, string?>(cp.Key, cp.Value.KindCase == NullableString.KindOneofCase.Data ? cp.Value.Data : null)));
                    //string dataAccessClientUserName = ConfigurationHelper.GetValue(_configuration, @"DataAccessClientUserName", @"");
                    //if (dataAccessClientUserName != @"")
                    //{
                    //    string dataAccessClientPasswordHash = ConfigurationHelper.GetValue(_configuration, @"DataAccessClientPasswordHash", @"");
                    //    if (String.IsNullOrEmpty(dataAccessClientPasswordHash))
                    //        throw new RpcException(new Status(StatusCode.PermissionDenied, "Client password hash must be specified (DataAccessClientPasswordHash in appsettings.json)."));
                    //    byte[] dataAccessClientPasswordHashBytes = Convert.FromBase64String(dataAccessClientPasswordHash);
                    //    var clientUserNameInRequest = contextParams.TryGetValue(@"ClientUserName");
                    //    if (String.IsNullOrEmpty(clientUserNameInRequest))
                    //        throw new RpcException(new Status(StatusCode.PermissionDenied, "Client user name must be specified (ClientUserName context param)."));
                    //    if (!String.Equals(clientUserNameInRequest, dataAccessClientUserName, StringComparison.InvariantCultureIgnoreCase))
                    //        throw new RpcException(new Status(StatusCode.PermissionDenied, "Invalid client user name (ClientUserName context params).")); // Invalid client user name or password (ClientUserName or ClientPassword context params)
                    //    var clientPasswordInRequest = contextParams.TryGetValue(@"ClientPassword");
                    //    if (String.IsNullOrEmpty(clientPasswordInRequest))
                    //        throw new RpcException(new Status(StatusCode.PermissionDenied, "Client password must be specified (ClientPassword context param)."));
                    //    byte[] clientPasswordInRequestHashBytes;
                    //    using (SHA512 sha512 = SHA512.Create())
                    //    {
                    //        clientPasswordInRequestHashBytes = sha512.ComputeHash(new UTF8Encoding(false).GetBytes(clientPasswordInRequest));
                    //    }                        
                    //    if (!clientPasswordInRequestHashBytes.SequenceEqual(dataAccessClientPasswordHashBytes))
                    //        throw new RpcException(new Status(StatusCode.PermissionDenied, "Invalid client password (ClientPassword context params)."));
                    //}
                    IDataAccessServerContext serverContext = _localDataAccessServerWorker.AddServerContext(
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
                });
        }

        public IAsyncStreamReader<CallbackMessage> SubscribeForCallback(SubscribeForCallbackRequest request)
        {
            DuplexStream<CallbackMessage> duplexStream = new();

            IDataAccessServerContext serverContext = _localDataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
            serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
            serverContext.SetResponseStream(duplexStream);

            return duplexStream;
        }

        public async Task<UpdateContextParamsReply> UpdateContextParamsAsync(UpdateContextParamsRequest request)
        {
            return await GetReplyAsync(() =>
            {
                CaseInsensitiveDictionary<string?> contextParams = new CaseInsensitiveDictionary<string?>(request.ContextParams
                        .Select(cp => new KeyValuePair<string, string?>(cp.Key, cp.Value.KindCase == NullableString.KindOneofCase.Data ? cp.Value.Data : null)));

                IDataAccessServerContext serverContext = _localDataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                serverContext.UpdateContextParams(contextParams);

                var reply = new UpdateContextParamsReply();
                return Task.FromResult(reply);
            });
        }

        public async Task<ConcludeReply> ConcludeAsync(ConcludeRequest request)
        {
            return await GetReplyAsync(() =>
            {
                IDataAccessServerContext? serverContext = _localDataAccessServerWorker.TryLookupServerContext(request.ContextId ?? @"");
                if (serverContext is not null)
                {
                    serverContext.IsConcludeCalled = true;
                    _localDataAccessServerWorker.RemoveServerContext(serverContext);
                    serverContext.Dispose();
                }
                return Task.FromResult(new ConcludeReply());
            });
        }

        public async Task<ClientKeepAliveReply> ClientKeepAliveAsync(ClientKeepAliveRequest request, CancellationToken cancellationToken)
        {
            return await GetReplyAsync(() =>
            {
                IDataAccessServerContext? serverContext = _localDataAccessServerWorker.TryLookupServerContext(request.ContextId ?? @"");
                if (serverContext is not null)
                {
                    serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                }
                return Task.FromResult(new ClientKeepAliveReply());
            });
        }

        public async Task<DefineListReply> DefineListAsync(DefineListRequest request)
        {
            return await GetReplyAsync(() =>
            {
                IDataAccessServerContext serverContext = _localDataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                var reply = new DefineListReply();
                reply.Result = new Common.AliasResult(serverContext.DefineList(request.ListClientAlias, request.ListType,
                    new Utils.CaseInsensitiveDictionary<string?>(request.ListParams
                        .Select(cp => new KeyValuePair<string, string?>(cp.Key, cp.Value.KindCase == NullableString.KindOneofCase.Data ? cp.Value.Data : null)))));
                return Task.FromResult(reply);
            });
        }

        public async Task<DeleteListsReply> DeleteListsAsync(DeleteListsRequest request)
        {
            return await GetReplyAsync(() =>
            {
                IDataAccessServerContext serverContext = _localDataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                var reply = new DeleteListsReply();
                reply.Results.Add(serverContext.DeleteLists(request.ListServerAliases.ToList()).Select(ar => new Common.AliasResult(ar)));
                return Task.FromResult(reply);
            });
        }

        public async Task<AddItemsToListReply> AddItemsToListAsync(AddItemsToListRequest request)
        {
            return await GetReplyAsync(async () =>
            {
                IDataAccessServerContext serverContext = _localDataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                var reply = new AddItemsToListReply();
                reply.Results.Add((await serverContext.AddItemsToListAsync(request.ListServerAlias, request.ItemsToAdd.Select(i => i.ToListItemInfoMessage()).ToList()))
                    .Select(ar => new Common.AliasResult(ar)).ToList());
                return reply;
            });
        }

        public async Task<RemoveItemsFromListReply> RemoveItemsFromListAsync(RemoveItemsFromListRequest request)
        {
            return await GetReplyAsync(async () =>
            {
                IDataAccessServerContext serverContext = _localDataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                var reply = new RemoveItemsFromListReply();
                reply.Results.Add((await serverContext.RemoveItemsFromListAsync(request.ListServerAlias, request.ServerAliasesToRemove.ToList()))
                    .Select(ar => new Common.AliasResult(ar)).ToList());
                return reply;
            });
        }

        public async Task<EnableListCallbackReply> EnableListCallbackAsync(EnableListCallbackRequest request)
        {
            return await GetReplyAsync(() =>
            {
                IDataAccessServerContext serverContext = _localDataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                bool isEnabled = request.Enable;
                var reply = new EnableListCallbackReply();
                serverContext.EnableListCallback(request.ListServerAlias, ref isEnabled);
                reply.Enabled = isEnabled;
                return Task.FromResult(reply);
            });
        }

        public async Task<TouchListReply> TouchListAsync(TouchListRequest request)
        {
            return await GetReplyAsync(() =>
            {
                IDataAccessServerContext serverContext = _localDataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                var reply = new TouchListReply();
                serverContext.TouchList(request.ListServerAlias);
                return Task.FromResult(reply);
            });
        }

        public async Task<List<(uint, ValueStatusTimestamp)>?> PollElementValuesChangesAsync(PollElementValuesChangesRequest request)
        {
            return await GetReplyAsync(async () =>
            {
                IDataAccessServerContext serverContext = _localDataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                ElementValuesCallbackMessage? elementValuesCallbackMessage = await serverContext.PollElementValuesChangesAsync(request.ListServerAlias);                
                return elementValuesCallbackMessage?.ElementValues;
            });
        }

        public async Task<List<Utils.DataAccess.EventMessagesCollection>?> PollEventsChangesAsync(PollEventsChangesRequest request)
        {
            return await GetReplyAsync(async () =>
            {
                IDataAccessServerContext serverContext = _localDataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                List<EventMessagesCallbackMessage>? eventMessagesCallbackMessages = await serverContext.PollEventsChangesAsync(request.ListServerAlias);                
                return eventMessagesCallbackMessages
                    ?.Select(m => new Utils.DataAccess.EventMessagesCollection
                    {
                        CommonFields = m.CommonFields,
                        EventMessages = m.EventMessages
                    })
                    .ToList();
            });
        }

        public async Task<ReadOnlyMemory<byte>> ReadElementValuesJournalsAsync(ReadElementValuesJournalsRequest request)
        {
            return await GetReplyAsync(async () =>
            {
                IDataAccessServerContext serverContext = _localDataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
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
                return bytes;
            });
        }

        public async Task<List<Utils.DataAccess.EventMessagesCollection>> ReadEventMessagesJournalAsync(ReadEventMessagesJournalRequest request)
        {
            return await GetReplyAsync(async () =>
            {
                IDataAccessServerContext serverContext = _localDataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                EventMessagesCallbackMessage? fullEventMessagesCallbackMessage = await serverContext.ReadEventMessagesJournalAsync(
                        request.ListServerAlias,
                        request.FirstTimestamp.ToDateTime(),
                        request.SecondTimestamp.ToDateTime(),
                        new Utils.CaseInsensitiveDictionary<string?>(request.Params
                            .Select(cp => new KeyValuePair<string, string?>(cp.Key, cp.Value.KindCase == NullableString.KindOneofCase.Data ? cp.Value.Data : null))));
                Utils.DataAccess.EventMessagesCollection result = new();
                if (fullEventMessagesCallbackMessage is not null)
                {
                    result.EventMessages = fullEventMessagesCallbackMessage.EventMessages;
                    result.CommonFields = fullEventMessagesCallbackMessage.CommonFields;
                }
                return new List<Utils.DataAccess.EventMessagesCollection> { result };
            });
        }

        public async Task<WriteElementValuesReply> WriteElementValuesAsync(uint listServerAlias, byte[] fullElementValuesCollection, string serverContextId)
        {
            return await GetReplyAsync(async () =>
            {
                var reply = new WriteElementValuesReply();
                IDataAccessServerContext serverContext = _localDataAccessServerWorker.LookupServerContext(serverContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                var aliasResults = await serverContext.WriteElementValuesAsync(listServerAlias, fullElementValuesCollection);
                if (aliasResults is not null)
                    reply.Results.Add(aliasResults.Select(ar => new Common.AliasResult(ar)));
                return reply;
            });
        }

        public async Task<AckAlarmsReply> AckAlarmsAsync(AckAlarmsRequest request)
        {
            return await GetReplyAsync(() =>
            {
                IDataAccessServerContext serverContext = _localDataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                var reply = new AckAlarmsReply();
                reply.Results.Add(serverContext.AckAlarms(request.ListServerAlias,
                    request.OperatorName ?? @"", request.Comment ?? @"", request.EventIdsToAck.Select(e => e.ToEventId()))
                    .Select(e => new Common.EventIdResult(e)));
                return Task.FromResult(reply);
            });
        }

        public async Task<ReadOnlyMemory<byte>> PassthroughAsync(string recipientPath, string passthroughName, ReadOnlyMemory<byte> dataToSend, string serverContextId)
        {            
            return await GetReplyAsync(async () =>
            {
                IDataAccessServerContext serverContext = _localDataAccessServerWorker.LookupServerContext(serverContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                ReadOnlyMemory<byte> returnData = await serverContext.PassthroughAsync(recipientPath ?? @"", passthroughName ?? @"", dataToSend);                
                return returnData;
            });
        }

        public async Task<LongrunningPassthroughReply> LongrunningPassthroughAsync(
            string recipientPath,
            string passthroughName,
            ReadOnlyMemory<byte> dataToSend,
            string serverContextId
            )
        {
            return await GetReplyAsync(() =>
            {
                var reply = new LongrunningPassthroughReply();
                IDataAccessServerContext serverContext = _localDataAccessServerWorker.LookupServerContext(serverContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                reply.JobId = serverContext.LongrunningPassthrough(recipientPath ?? @"", passthroughName ?? @"", dataToSend);
                return Task.FromResult(reply);
            });
        }

        public async Task<LongrunningPassthroughCancelReply> LongrunningPassthroughCancelAsync(LongrunningPassthroughCancelRequest request)
        {
            return await GetReplyAsync(() =>
            {
                var reply = new LongrunningPassthroughCancelReply();
                IDataAccessServerContext serverContext = _localDataAccessServerWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                serverContext.LongrunningPassthroughCancel(request.JobId ?? @"");
                return Task.FromResult(reply);
            });
        }

        #endregion

        #region private functions

        private Task<T> GetReplyAsync<T>(Func<Task<T>> func)
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
            _localDataAccessServerWorker.ThreadSafeDispatcher.BeginInvoke(async ct =>
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

        private readonly ILogger _logger;        
        private readonly IDataAccessServerWorker _localDataAccessServerWorker;

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