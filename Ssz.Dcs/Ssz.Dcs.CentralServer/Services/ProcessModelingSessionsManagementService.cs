using Grpc.Core;
using Microsoft.Extensions.Logging;
using Ssz.Dcs.CentralServer.Common;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer
{
    public class ProcessModelingSessionsManagementService : ProcessModelingSessionsManagement.ProcessModelingSessionsManagementBase
    {
        #region construction and destruction

        public ProcessModelingSessionsManagementService(ILogger<ProcessModelingSessionsManagementService> logger, ServerWorkerBase serverWorker)
        {
            _logger = logger;            
            _serverWorker = (ServerWorker)serverWorker;
        }

        #endregion

        #region public functions

        public override async Task<InitiateProcessModelingSessionReply> InitiateProcessModelingSession(InitiateProcessModelingSessionRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
                {
                    string processModelingSessionId = _serverWorker.InitiateProcessModelingSession(
                        request.ClientApplicationName ?? @"",
                        request.ClientWorkstationName ?? @"",
                        request.ProcessModelName ?? @"",
                        request.InstructorUserName ?? @"",
                        (InstructorAccessFlags)request.InstructorAccessFlags,
                        request.Mode ?? @"");
                    var reply = new InitiateProcessModelingSessionReply
                    {
                        ProcessModelingSessionId = processModelingSessionId
                    };
                    return reply;
                },
                context);
        }

        public override async Task<GetProcessModelingSessionPropsReply> GetProcessModelingSessionProps(GetProcessModelingSessionPropsRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
            {
                ServerWorker.ProcessModelingSession processModelingSession = _serverWorker.GetProcessModelingSession(request.ProcessModelingSessionId ?? @"");
                var reply = new GetProcessModelingSessionPropsReply
                {
                    ProcessModelName = processModelingSession.ProcessModelName,
                    ProcessModelNameToDisplay = processModelingSession.ProcessModelNameToDisplay,
                    InstructorUserName = processModelingSession.InstructorUserName,
                    InstructorAccessFlags = (uint)processModelingSession.InstructorAccessFlags,
                    Mode = processModelingSession.Mode
                };
                return reply;
            },
                context);
        }

        public override async Task<ConcludeProcessModelingSessionReply> ConcludeProcessModelingSession(ConcludeProcessModelingSessionRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
                {
                    _serverWorker.ConcludeProcessModelingSession(request.ProcessModelingSessionId ?? @"");
                    var reply = new ConcludeProcessModelingSessionReply
                    {
                    };
                    return reply;
                }, 
                context);
        }

        public override async Task<SetOperatorSessionPropsReply> SetOperatorSessionProps(SetOperatorSessionPropsRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
            {
                _serverWorker.SetOperatorSessionProps(request.OperatorSessionId ?? @"", request.OperatorUserName ?? @"");
                var reply = new SetOperatorSessionPropsReply
                {
                };
                return reply;
            },
                context);
        }

        public override async Task<ConcludeOperatorSessionReply> ConcludeOperatorSession(ConcludeOperatorSessionRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
                {
                    _serverWorker.ConcludeOperatorSession(request.OperatorSessionId ?? @"");
                    var reply = new ConcludeOperatorSessionReply
                    {
                    };
                    return reply;
                },
                context);
        }

        public override async Task<NotifyJobProgressReply> NotifyJobProgress(NotifyJobProgressRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
                {
                    _serverWorker.SetJobProgress(
                        request.JobId ?? @"",
                        request.ProgressPercent,
                        request.ProgressLabelResourceName ?? @"",
                        request.ProgressDetails ?? @"",
                        request.StatusCode);
                    var reply = new NotifyJobProgressReply
                    {
                    };
                    return reply;
                },
                context);
        }

        public override async Task<NotifyJournalEventReply> NotifyJournalEvent(NotifyJournalEventRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
                {
                    _serverWorker.NotifyJournalEvent(request.ProcessModelingSessionId ?? @"", (EventType)request.EventType, request.OccurrenceTime.ToDateTime(), request.TextMessage ?? @"");
                    var reply = new NotifyJournalEventReply
                    {
                    };
                    return reply;
                },
                context);
        }

        public override async Task<InsertUserReply> InsertUser(InsertUserRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
                {
                    _serverWorker.InsertUser(
                        request.UserName ?? @"",
                        request.PersonnelNumber ?? @"",
                        request.DomainUserName ?? @"",
                        request.ProcessModelNames ?? @"");
                    var reply = new InsertUserReply();
                    return reply;
                },
                context);
        }

        public override async Task<UpdateUserReply> UpdateUser(UpdateUserRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
                {
                    _serverWorker.UpdateUser(
                        request.UserName ?? @"", 
                        request.NewUserName ?? @"", 
                        request.NewPersonnelNumber ?? @"", 
                        request.NewDomainUserName ?? @"",
                        request.NewProcessModelNames ?? @"");
                    var reply = new UpdateUserReply();
                    return reply;
                },
                context);
        }

        public override async Task<DeleteUserReply> DeleteUser(DeleteUserRequest request, ServerCallContext context)
        {
            return await GetReplyAsync(() =>
                {
                    _serverWorker.DeleteUser(request.UserName ?? @"");
                    var reply = new DeleteUserReply();
                    return reply;
                },
                context);
        }

        #endregion

        #region private functions        

        private async Task<TReply> GetReplyAsync<TReply>(Func<TReply> func, ServerCallContext context)
        {
            var taskCompletionSource = new TaskCompletionSource<TReply>();            
            //context.CancellationToken.Register(() => taskCompletionSource.TrySetCanceled(), useSynchronizationContext: false);
            _serverWorker.ThreadSafeDispatcher.BeginInvoke(ct =>
            {
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
            catch (OperationCanceledException ex)
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
            catch (Exception ex)
            {
                string message = @"General Exception";
                _logger.LogWarning(ex, message);
                throw;
            }
        }        

        #endregion

        #region private fields

        private readonly ILogger<ProcessModelingSessionsManagementService> _logger;
        
        private readonly ServerWorker _serverWorker;

        #endregion
    }
}
