using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataGrpc.ServerBase
{
    public sealed partial class EventMessage
    {
        #region public functions

        public Utils.DataAccess.EventMessage ToEventMessage()
        {
            var eventMessage = new Utils.DataAccess.EventMessage(EventId?.ToEventId());
            eventMessage.OccurrenceTimeUtc = OccurrenceTime.ToDateTime();
            eventMessage.EventType = (EventType)EventType;
            eventMessage.TextMessage = TextMessage;
            eventMessage.CategoryId = CategoryId;
            eventMessage.Priority = Priority;
            eventMessage.OperatorName = OperatorName;
            if (AlarmMessageData is not null)
            {
                eventMessage.AlarmMessageData = AlarmMessageData.ToAlarmMessageData();
            }
            if (ClientRequestedFields.Count > 0)
            {
                eventMessage.ClientRequestedFields = new Utils.CaseInsensitiveDictionary<string>(ClientRequestedFields);
            }
            return eventMessage;
        }

        #endregion
    }
}
