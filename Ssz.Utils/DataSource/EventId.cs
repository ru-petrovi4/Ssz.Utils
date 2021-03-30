using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils.DataSource
{
    public class EventId
    {
		#region public functions

		public string SourceElementId = @"";

		/// <summary>
		/// The TypeId of the container for alarms with multiple conditions, 
		/// such as grouped or eclipsed alarms. The EventType enumeration defines 
		/// these types of alarms. 
		/// Null if the event is not a grouped or eclipsed alarm. 
		/// </summary>
		public TypeId? MultiplexedAlarmContainer;

		/// <summary>
		/// <para>For system events, operator action events, simple alarms, 
		/// and complex alarms, the TypeId of the condition 
		/// that is being reported in the event message.</para>
		/// <para>For grouped or eclipsed alarms, the name of 
		/// one or more conditions that are active.</para>
		/// </summary>
		public List<TypeId>? Conditions;

		/// <summary>
		/// A server-specific id that identifies an individual occurrence of the 
		/// alarm/event.  This identifier can be constructed by the server to meet 
		/// the server's needs for identifying alarms.  For example, if the server 
		/// wraps an OPC AE server, the OccurrenceId may be constructed from the 
		/// ActiveTime and Cookie parameters of the IOPCEventServer::AckCondition() 
		/// method.
		/// </summary>
		public string OccurrenceId = @"";

		/// <summary>
		/// This element is mandatory when acknowledging an alarm using the AcknowledgeAlarms() method. 
		/// It is set to null in all other uses.  Its value is copied from the AlarmMessageData object 
		/// contained in the EventMessage used to report the alarm being acknowledged.
		/// </summary>
		public DateTime? TimeLastActive;

		#endregion
	}
}
