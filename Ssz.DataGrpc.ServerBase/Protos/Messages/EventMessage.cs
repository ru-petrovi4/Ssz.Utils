using Google.Protobuf.WellKnownTypes;
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
        #region construction and destruction

        public EventMessage(Ssz.Utils.DataAccess.EventMessage eventMessage)
        {
            if (eventMessage.EventId is not null)
                EventId = new EventId(eventMessage.EventId);
            OccurrenceTime = DateTimeHelper.ConvertToTimestamp(eventMessage.OccurrenceTimeUtc);
            EventType = (uint)eventMessage.EventType;
            TextMessage = eventMessage.TextMessage;
            CategoryId = eventMessage.CategoryId;
            Priority = eventMessage.Priority;
            OperatorName = eventMessage.OperatorName;
            if (eventMessage.AlarmMessageData is not null)
            {
                AlarmMessageData = new AlarmMessageData(eventMessage.AlarmMessageData);
            }
            if (eventMessage.ClientRequestedFields is not null)
            {
                ClientRequestedFields.Add(eventMessage.ClientRequestedFields);
            }
        }

        #endregion
    }
}
