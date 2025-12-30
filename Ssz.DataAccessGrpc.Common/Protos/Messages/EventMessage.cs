using Google.Protobuf.WellKnownTypes;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.Common
{
    public sealed partial class EventMessage
    {
        #region construction and destruction

        public EventMessage(Ssz.Utils.DataAccess.EventMessage eventMessage)
        {
            if (eventMessage.EventId is not null)
                EventId = new EventId(eventMessage.EventId);
            OccurrenceTime = ProtobufHelper.ConvertToTimestamp(eventMessage.OccurrenceTimeUtc);
            EventType = (uint)eventMessage.EventType;
            TextMessage = eventMessage.TextMessage;
            CategoryId = eventMessage.CategoryId;
            Priority = eventMessage.Priority;
            OperatorName = eventMessage.OperatorName;
            if (eventMessage.AlarmMessageData is not null)
            {
                AlarmMessageData = new AlarmMessageData(eventMessage.AlarmMessageData);
            }
            if (eventMessage.Fields is not null)
            {
                foreach (var kvp in eventMessage.Fields)
                {
                    FieldsOrdered.Add(new Field
                    {
                        Name = kvp.Key,
                        Value = kvp.Value is not null ? new NullableString { Data = kvp.Value } : new NullableString { Null = NullValue.NullValue }
                    });                    
                }
            }
        }

        #endregion

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
            if (FieldsOrdered.Count > 0)
            {
                eventMessage.Fields = new Utils.CaseInsensitiveOrderedDictionary<string?>(FieldsOrdered
                            .Select(f => new KeyValuePair<string, string?>(f.Name, f.Value.KindCase == NullableString.KindOneofCase.Data ? f.Value.Data : null)));

            }            
            return eventMessage;
        }

        #endregion
    }
}
