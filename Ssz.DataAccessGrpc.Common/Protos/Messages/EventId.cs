using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.Common
{
    public sealed partial class EventId
    {
        #region construction and destruction

        public EventId(Ssz.Utils.DataAccess.EventId eventId)
        {
            SourceElementId = eventId.SourceElementId;
            if (eventId.MultiplexedAlarmContainer is not null)
            {
                MultiplexedAlarmContainer = new TypeId(eventId.MultiplexedAlarmContainer);
            }
            if (eventId.Conditions is not null)
            {
                Conditions.Add(eventId.Conditions.Select(t => new TypeId(t)));
            }            
            OccurrenceId = eventId.OccurrenceId;
            if (eventId.TimeLastActiveUtc is not null)
            {
                TimeLastActive = ProtobufHelper.ConvertToTimestamp(eventId.TimeLastActiveUtc.Value);
            }
        }

        #endregion

        #region public functions

        public Ssz.Utils.DataAccess.EventId ToEventId()
        {
            var eventId = new Ssz.Utils.DataAccess.EventId();
            eventId.SourceElementId = SourceElementId;
            if (MultiplexedAlarmContainer is not null)
            {
                eventId.MultiplexedAlarmContainer = MultiplexedAlarmContainer.ToTypeId();
            }
            if (Conditions.Count > 0)
            {
                eventId.Conditions = Conditions.Select(t => t.ToTypeId()).ToList();
            }
            eventId.OccurrenceId = OccurrenceId;
            if (OptionalTimeLastActiveCase == OptionalTimeLastActiveOneofCase.TimeLastActive)
            {
                eventId.TimeLastActiveUtc = TimeLastActive.ToDateTime();
            }
            return eventId;
        }

        #endregion
    }
}
