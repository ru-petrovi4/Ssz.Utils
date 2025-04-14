using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ssz.Utils;
using Ssz.Dcs.CentralServer.Common;
using Ssz.Dcs.CentralServer.Properties;
using Ssz.Utils.DataAccess;
using Ssz.DataAccessGrpc.ServerBase;
using System.Threading;
using Grpc.Core;

namespace Ssz.Dcs.CentralServer
{
    public partial class ServerWorker : DataAccessServerWorkerBase
    {
        #region internal functions

        internal void OnChangeElementValueAction(
            ProcessModelingSession? processModelingSession,
            string operatorRoleId,
            string operatorRoleName,
            string operatorUserName,
            string elementId,
            ValueStatusTimestamp oldValueStatusTimestamp,
            ValueStatusTimestamp newValueStatusTimestamp)
        {
            if (processModelingSession is null || !StatusCodes.IsGood(newValueStatusTimestamp.StatusCode))
                return;

            if (elementId == @"" || StringHelper.StartsWithIgnoreCase(elementId, @"SYSTEM."))
                return;

            //StreamWriter(string path, bool append)            

            Action<ServerContext, Ssz.Utils.DataAccess.EventMessage>? processEventMessageNotification = ProcessEventMessageNotification;
            if (processEventMessageNotification is null) 
                return;

            var eventMessage = new Ssz.Utils.DataAccess.EventMessage(null);

            eventMessage.EventType = EventType.OperatorActionEvent;
            eventMessage.OccurrenceTimeUtc = newValueStatusTimestamp.TimestampUtc;
            eventMessage.TextMessage = CsvHelper.FormatForCsv(",", new object?[] {
                ProcessModelingSessionConstants.EventSubType_ChangeElementValue,
                processModelingSession.ProcessTimeSeconds,
                operatorRoleId,
                operatorRoleName,
                operatorUserName,
                elementId,
                oldValueStatusTimestamp.Value,
                newValueStatusTimestamp.Value });

            foreach (var processServerContext in processModelingSession.ProcessServerContextsCollection)
            {
                processEventMessageNotification(processServerContext, eventMessage);
            }
        }

        internal void NotifyJournalEvent(string processModelingSessionId, EventType eventType, DateTime occurrenceTimeUtc, string textMessage)
        {
            ProcessModelingSession processModelingSession = GetProcessModelingSession(processModelingSessionId);

            Action<ServerContext, Ssz.Utils.DataAccess.EventMessage>? processEventMessageNotification = ProcessEventMessageNotification;
            if (processEventMessageNotification is null) 
                return;

            var eventMessage = new Ssz.Utils.DataAccess.EventMessage(null);

            eventMessage.EventType = eventType;
            eventMessage.OccurrenceTimeUtc = occurrenceTimeUtc;
            eventMessage.TextMessage = textMessage;

            foreach (var processServerContext in processModelingSession.ProcessServerContextsCollection)
            {
                processEventMessageNotification(processServerContext, eventMessage);
            }            
        }

        #endregion        
    }
}
