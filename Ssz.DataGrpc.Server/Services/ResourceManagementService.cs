using Grpc.Core;
using Microsoft.Extensions.Logging;
using Ssz.DataGrpc.Server.Core.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ssz.DataGrpc.Server
{
    public class ResourceManagementService : ResourceManagement.ResourceManagementBase
    {
        #region construction and destruction

        public ResourceManagementService(ILogger<ResourceManagementService> logger)
        {
            _logger = logger;

            _serverContextsManager = new ServerContextsManager(_logger);
        }

        #endregion

        #region public functions

        public override Task SubscribeForCallback(SubscribeForCallbackRequest request, IServerStreamWriter<CallbackMessage> responseStream, ServerCallContext context)
        {
            return base.SubscribeForCallback(request, responseStream, context);
        }

        public override Task<InitiateReply> Initiate(InitiateRequest request, ServerCallContext context)
        {
            return base.Initiate(request, context);
        }

        public override Task<ConcludeReply> Conclude(ConcludeRequest request, ServerCallContext context)
        {
            ServerContext? serverContext = _serverContextsManager.LookupServerContext(request.ContextId);
            if (serverContext != null)
            {
                serverContext.OnConclude();
            }
            return Task.FromResult(new ConcludeReply());
        }

        public override Task<ClientKeepAliveReply> ClientKeepAlive(ClientKeepAliveRequest request, ServerCallContext context)
        {
            return base.ClientKeepAlive(request, context);
        }

        public override Task<DefineListReply> DefineList(DefineListRequest request, ServerCallContext context)
        {
            return base.DefineList(request, context);
        }

        public override Task<DeleteListsReply> DeleteLists(DeleteListsRequest request, ServerCallContext context)
        {
            return base.DeleteLists(request, context);
        }

        public override Task<AddDataObjectsToListReply> AddDataObjectsToList(AddDataObjectsToListRequest request, ServerCallContext context)
        {
            return base.AddDataObjectsToList(request, context);
        }

        public override Task<RemoveDataObjectsFromListReply> RemoveDataObjectsFromList(RemoveDataObjectsFromListRequest request, ServerCallContext context)
        {
            return base.RemoveDataObjectsFromList(request, context);
        }

        public override Task<EnableListCallbackReply> EnableListCallback(EnableListCallbackRequest request, ServerCallContext context)
        {
            return base.EnableListCallback(request, context);
        }

        public override Task<TouchListReply> TouchList(TouchListRequest request, ServerCallContext context)
        {
            return base.TouchList(request, context);
        }

        public override Task<PollDataChangesReply> PollDataChanges(PollDataChangesRequest request, ServerCallContext context)
        {
            return base.PollDataChanges(request, context);
        }

        public override Task<PollEventChangesReply> PollEventChanges(PollEventChangesRequest request, ServerCallContext context)
        {
            return base.PollEventChanges(request, context);
        }

        public override Task<ReadElementValueJournalForTimeIntervalReply> ReadElementValueJournalForTimeInterval(ReadElementValueJournalForTimeIntervalRequest request, ServerCallContext context)
        {
            return base.ReadElementValueJournalForTimeInterval(request, context);
        }

        public override Task<WriteValuesReply> WriteValues(WriteValuesRequest request, ServerCallContext context)
        {
            return base.WriteValues(request, context);
        }

        public override Task<AcknowledgeAlarmsReply> AcknowledgeAlarms(AcknowledgeAlarmsRequest request, ServerCallContext context)
        {
            return base.AcknowledgeAlarms(request, context);
        }

        public override Task<PassthroughReply> Passthrough(PassthroughRequest request, ServerCallContext context)
        {
            return base.Passthrough(request, context);
        }

        #endregion       

        #region private fields

        private readonly ILogger<ResourceManagementService> _logger;

        private readonly ServerContextsManager _serverContextsManager;

        #endregion
    }
}
