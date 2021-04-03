using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataGrpc.Server
{
    public sealed partial class EventMessage
    {
        #region public functions

        public Utils.DataAccess.EventMessage ToEventMessage()
        {
            var eventInfo = new Utils.DataAccess.EventMessage(EventId.ToEventId());
            eventInfo.OccurrenceTime = OccurrenceTime.ToDateTime();
            eventInfo.EventType = (EventType)EventType;
            eventInfo.TextMessage = TextMessage;
            eventInfo.CategoryId = CategoryId;
            eventInfo.Priority = Priority;
            eventInfo.OperatorName = OperatorName;
            if (OptionalAlarmDataCase == OptionalAlarmDataOneofCase.AlarmMessageData)
            {
                eventInfo.AlarmMessageData = AlarmMessageData.ToAlarmMessageData();
            }
            if (ClientRequestedFields.Count > 0)
            {
                eventInfo.ClientRequestedFields = new Utils.CaseInsensitiveDictionary<string>(ClientRequestedFields);
            }
            return eventInfo;
        }

        #endregion
    }
}
