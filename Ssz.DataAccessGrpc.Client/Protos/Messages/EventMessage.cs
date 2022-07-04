using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.ServerBase
{
    internal sealed partial class EventMessage
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
            if (Fields.Count > 0)
            {
                eventMessage.Fields = new Utils.CaseInsensitiveDictionary<string?>(Fields
                            .Select(cp => new KeyValuePair<string, string?>(cp.Key, cp.Value.KindCase == NullableString.KindOneofCase.Data ? cp.Value.Data : null)));

            }
            return eventMessage;
        }

#endregion
    }
}
