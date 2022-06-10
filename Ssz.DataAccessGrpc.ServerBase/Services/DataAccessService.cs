using Grpc.Core;
using Microsoft.Extensions.Logging;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.ServerBase
{
    public partial class DataAccessService : DataAccess.DataAccessBase
    {
        #region construction and destruction

        public DataAccessService(ILogger<DataAccessService> logger, ServerWorkerBase serverWorker)
        {
            _logger = logger;
            _serverWorker = serverWorker;            
        }

        #endregion

        #region public functions

        public override async Task<InitiateReply> Initiate(InitiateRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
                {
                    var serverContext = new ServerContext(
                        _logger,
                        _serverWorker,
                        request.ClientApplicationName ?? @"", 
                        request.ClientWorkstationName ?? @"",
                        request.RequestedServerContextTimeoutMs,
                        request.RequestedServerCultureName ?? @"",
                        request.SystemNameToConnect ?? @"",
                        new CaseInsensitiveDictionary<string>(request.ContextParams)
                        );
                    serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                    _serverWorker.AddServerContext(serverContext);                    
                    var reply = new InitiateReply
                    {
                        ContextId = serverContext.ContextId,
                        ServerContextTimeoutMs = serverContext.ContextTimeoutMs,
                        ServerCultureName = serverContext.CultureInfo.Name
                    };
                    return reply;
                },
                context);
        }

        public override async Task SubscribeForCallback(SubscribeForCallbackRequest request, IServerStreamWriter<CallbackMessage> responseStream, ServerCallContext context)
        {
            await GetReplyAsync(() =>
                {
                    ServerContext serverContext = _serverWorker.LookupServerContext(request.ContextId ?? @"");
                    serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                    serverContext.SetResponseStream(responseStream);                    
                    return new ConcludeReply();
                },
                context);

            var taskCompletionSource = new TaskCompletionSource<object?>();
            context.CancellationToken.Register(() => taskCompletionSource.SetResult(null));
            await taskCompletionSource.Task;            
        }

        public override async Task<ConcludeReply> Conclude(ConcludeRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
                {
                    try
                    {
                        ServerContext serverContext = _serverWorker.LookupServerContext(request.ContextId ?? @"");
                        serverContext.IsConcluded = true;
                        _serverWorker.RemoveServerContext(serverContext);                        
                        var t = serverContext.DisposeAsync();
                    }
                    catch
                    {
                    }
                    return new ConcludeReply();
                },
                context);
        }

        public override async Task<ClientKeepAliveReply> ClientKeepAlive(ClientKeepAliveRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
                {
                    ServerContext serverContext = _serverWorker.LookupServerContext(request.ContextId ?? @"");
                    serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                    return new ClientKeepAliveReply();
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
                    reply.Result = serverContext.DefineList(request.ListClientAlias, request.ListType, new CaseInsensitiveDictionary<string>(request.ListParams));
                    return reply;
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
                    return reply;
                },
                context);
        }

        public override async Task<AddItemsToListReply> AddItemsToList(AddItemsToListRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
                {
                    ServerContext serverContext = _serverWorker.LookupServerContext(request.ContextId ?? @"");
                    serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                    var reply = new AddItemsToListReply();
                    reply.Results.Add(serverContext.AddItemsToList(request.ListServerAlias, request.ItemsToAdd.ToList()));
                    return reply;
                },
                context);
        }

        public override async Task<RemoveItemsFromListReply> RemoveItemsFromList(RemoveItemsFromListRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
                {
                    ServerContext serverContext = _serverWorker.LookupServerContext(request.ContextId ?? @"");
                    serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                    var reply = new RemoveItemsFromListReply();
                    reply.Results.Add(serverContext.RemoveItemsFromList(request.ListServerAlias, request.ServerAliasesToRemove.ToList()));
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
                    return reply;
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
                    return reply;
                },
                context);
        }

        public override async Task<PollElementValuesChangesReply> PollElementValuesChanges(PollElementValuesChangesRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
                {
                    ServerContext serverContext = _serverWorker.LookupServerContext(request.ContextId ?? @"");
                    serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                    var reply = new PollElementValuesChangesReply();
                    reply.ElementValuesCollection  = serverContext.PollElementValuesChanges(request.ListServerAlias);
                    return reply;
                },
                context);
        }

        public override async Task<PollEventsChangesReply> PollEventsChanges(PollEventsChangesRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
                {
                    ServerContext serverContext = _serverWorker.LookupServerContext(request.ContextId ?? @"");
                    serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                    var reply = new PollEventsChangesReply();
                    reply.EventMessagesCollection = serverContext.PollEventsChanges(request.ListServerAlias);
                    return reply;
                },
                context);
        }

        public override async Task<ReadElementValuesJournalsReply> ReadElementValuesJournals(ReadElementValuesJournalsRequest request, ServerCallContext context)
        {            
            return await GetAsyncReplyAsync(async () =>
            {
                ServerContext serverContext = _serverWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                var reply = new ReadElementValuesJournalsReply();
                reply.ElementValuesJournalsCollection = await serverContext.ReadElementValuesJournalsAsync(
                        request.ListServerAlias,
                        request.FirstTimestamp.ToDateTime(),
                        request.SecondTimestamp.ToDateTime(),
                        request.NumValuesPerAlias,
                        request.Calculation,
                        new CaseInsensitiveDictionary<string>(request.Params),
                        request.ServerAliases.ToList()
                    );
                return reply;
            }, context);
        }

        public override async Task<ReadEventMessagesJournalReply> ReadEventMessagesJournal(ReadEventMessagesJournalRequest request, ServerCallContext context)
        {
            return await GetAsyncReplyAsync(async () =>
            {
                ServerContext serverContext = _serverWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                var reply = new ReadEventMessagesJournalReply();
                reply.EventMessagesCollection = await serverContext.ReadEventMessagesJournalAsync(
                        request.ListServerAlias,
                        request.FirstTimestamp.ToDateTime(),
                        request.SecondTimestamp.ToDateTime(),                        
                        new CaseInsensitiveDictionary<string>(request.Params)
                    );
                return reply;
            }, context);
        }

        public override async Task<WriteElementValuesReply> WriteElementValues(WriteElementValuesRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
                {
                    ServerContext serverContext = _serverWorker.LookupServerContext(request.ContextId ?? @"");
                    serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                    var reply = new WriteElementValuesReply();
                    reply.Results.Add(serverContext.WriteElementValues(request.ListServerAlias, request.ElementValuesCollection));
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
                    return reply;
                },
                context);
        }

        public override async Task<PassthroughReply> Passthrough(PassthroughRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
                {
                    ServerContext serverContext = _serverWorker.LookupServerContext(request.ContextId ?? @"");
                    serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;                    
                    return serverContext.Passthrough(request.RecipientId ?? @"", request.PassthroughName ?? @"", request.DataToSend);
                },
                context);
        }

        public override async Task<LongrunningPassthroughReply> LongrunningPassthrough(LongrunningPassthroughRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
                {
                    ServerContext serverContext = _serverWorker.LookupServerContext(request.ContextId ?? @"");
                    serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                    return serverContext.LongrunningPassthrough(request.InvokeId ?? @"", request.RecipientId ?? @"", request.PassthroughName ?? @"", request.DataToSend);
                },
                context);
        }

        public override async Task<LongrunningPassthroughCancelReply> LongrunningPassthroughCancel(LongrunningPassthroughCancelRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
            {
                ServerContext serverContext = _serverWorker.LookupServerContext(request.ContextId ?? @"");
                serverContext.LastAccessDateTimeUtc = DateTime.UtcNow;
                return serverContext.LongrunningPassthroughCancel(request.InvokeId ?? @"");
            },
                context);
        }

        #endregion

        #region private functions        

        private async Task<TReply> GetReplyAsync<TReply>(Func<TReply> func, ServerCallContext context)
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

            var taskCompletionSource = new TaskCompletionSource<TReply>();            
            context.CancellationToken.Register(() => taskCompletionSource.TrySetCanceled(), useSynchronizationContext: false);
            _serverWorker.ThreadSafeDispatcher.BeginInvoke(ct =>
            {
                _logger.LogTrace("Processing client call in worker thread: " + parentMethodName);
                try
                {
                    taskCompletionSource.TrySetResult(func());
                }
                catch (Exception ex)
                {
                    taskCompletionSource.TrySetException(ex);
                }
            });
            try
            {
                return await taskCompletionSource.Task;
            }
            catch (TaskCanceledException ex)
            {
                string message = @"Operation cancelled.";
                _logger.LogWarning(ex, message);
                throw new RpcException(new Status(StatusCode.Cancelled, message));
            }
            catch (RpcException ex)
            {
                string message = @"RPC Exception";
                _logger.LogWarning(ex, message);
                throw;
            }
            //catch (LocalizedMessageException ex)
            //{
            //    string message = @"LocalizedMessageException";
            //    _logger.LogWarning(ex, message);
            //    throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            //}
            catch (Exception ex)
            {
                string message = @"General Exception";
                _logger.LogWarning(ex, message);
                throw;
            }            
        }

        private async Task<TReply> GetAsyncReplyAsync<TReply>(Func<Task<TReply>> func, ServerCallContext context)
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

            var taskCompletionSource = new TaskCompletionSource<TReply>();
            context.CancellationToken.Register(() => taskCompletionSource.TrySetCanceled(), useSynchronizationContext: false);
            _serverWorker.ThreadSafeDispatcher.BeginInvoke(async ct =>
            {
                _logger.LogTrace("Processing client call in worker thread: " + parentMethodName);
                try
                {
                    var resultTask = func();
                    taskCompletionSource.TrySetResult(await resultTask);
                }
                catch (Exception ex)
                {
                    taskCompletionSource.TrySetException(ex);
                }
            });
            try
            {
                return await taskCompletionSource.Task;
            }
            catch (TaskCanceledException ex)
            {
                string message = @"Operation cancelled.";
                _logger.LogWarning(ex, message);
                throw new RpcException(new Status(StatusCode.Cancelled, message));
            }
            catch (RpcException ex)
            {
                string message = @"RPC Exception";
                _logger.LogWarning(ex, message);
                throw;
            }
            //catch (LocalizedMessageException ex)
            //{
            //    string message = @"LocalizedMessageException";
            //    _logger.LogWarning(ex, message);
            //    throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            //}
            catch (Exception ex)
            {
                string message = @"General Exception";
                _logger.LogWarning(ex, message);
                throw;
            }
        }

        #endregion

        #region private fields

        private readonly ILogger<DataAccessService> _logger;
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