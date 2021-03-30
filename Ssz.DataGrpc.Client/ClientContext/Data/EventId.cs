using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataGrpc.Server
{
    public sealed partial class EventId
    {
        #region construction and destruction

        public EventId(Ssz.Utils.DataSource.EventId eventId)
        {
            SourceElementId = eventId.SourceElementId;
            if (eventId.MultiplexedAlarmContainer != null)
            {
                MultiplexedAlarmContainer = new TypeId(eventId.MultiplexedAlarmContainer);
            }
            if (eventId.Conditions != null)
            {
                Conditions.Add(eventId.Conditions.Select(t => new TypeId(t)));
            }            
            OccurrenceId = eventId.OccurrenceId;
            if (eventId.TimeLastActive != null)
            {
                TimeLastActive = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(eventId.TimeLastActive.Value);
            }
        }

        #endregion

        #region public functions

        public Ssz.Utils.DataSource.EventId ToEventId()
        {
            var eventId = new Ssz.Utils.DataSource.EventId();
            eventId.SourceElementId = SourceElementId;
            if (OptionalMultiplexedAlarmContainerCase == OptionalMultiplexedAlarmContainerOneofCase.MultiplexedAlarmContainer)
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
                eventId.TimeLastActive = TimeLastActive.ToDateTime();
            }
            return eventId;
        }

        #endregion
    }
}
