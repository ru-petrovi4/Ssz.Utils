using Microsoft.Extensions.Logging;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils.DataAccess;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Grpc.Core;
using EventIdResult = Ssz.Utils.DataAccess.EventIdResult;
using EventId = Ssz.Utils.DataAccess.EventId;

namespace Ssz.Dcs.ControlEngine
{
    /// <summary>
    /// </summary>
    public class ProcessEventList : EventListBase
    {
        #region construction and destruction
        
        public ProcessEventList(DataAccessServerWorkerBase serverWorker, ILogger logger, ServerContext serverContext, uint listClientAlias, CaseInsensitiveOrderedDictionary<string?> listParams)
            : base(serverWorker, serverContext, listClientAlias, listParams)
        {
            _logger = logger;            
        }

        //protected override void Dispose(bool disposing)
        //{
        //    if (Disposed) return;

        //    if (disposing)
        //    {

        //    }

        //    base.Dispose(disposing);
        //}

        #endregion

        #region public functions

        public override List<EventIdResult> AckAlarms(string operatorName, string comment,
                                                               IEnumerable<EventId> eventIdsToAck)
        {
            return eventIdsToAck.Select(e => new EventIdResult { StatusCode = (uint)StatusCode.OK, EventId = e }).ToList();
        }

        #endregion

        //#region private functions

        //private void DataAccessProviderOnEventMessagesCallback(Ssz.Utils.DataAccess.EventMessage[] eventMessages)
        //{
        //    if (Disposed) return;

        //    _logger.LogDebug("ModelDataEventList::DataAccessProviderOnEventMessagesCallback eventMessages.Length=" + eventMessages.Length);

        //    EventMessagesCollection.AddRange(eventMessages.Select(em => new ServerBase.EventMessage(em)));
        //}

        //#endregion

        #region private fields

        private readonly ILogger _logger;

        #endregion
    }
}