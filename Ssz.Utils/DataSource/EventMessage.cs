﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils.DataSource
{
    public class EventMessage
    {
		#region construction and destruction

		public EventMessage(EventId eventId)
        {
			EventId = eventId;
		}

		#endregion

		#region public functions

		/// <summary>
		/// The time of the event/alarm occurrence.  
		/// </summary>
		public DateTime OccurrenceTime;

		/// <summary>
		/// The type of the event/alarm that is being reported by this 
		/// event message.  
		/// </summary>
		public EventType EventType;

		/// <summary>
		/// The identifier for the event/alarm occurrence.  
		/// </summary>
		public EventId EventId;

		/// <summary>
		/// Text that describes the event occurrence.
		/// </summary>
		public string TextMessage = @"";

		/// <summary>
		/// The category to which the event is assigned.
		/// </summary>
		public uint CategoryId;

		/// <summary>
		/// The priority of the event.
		/// </summary>
		public uint Priority;

		/// <summary>
		/// <para>For event messages that report operator action events, 
		/// the name of the operator who caused an operator action event 
		/// to be generated.</para>
		/// <para>For event messages that report the acknowledgement of 
		/// an alarm, the name of the operator who acknowledged the 
		/// alarm.</para>
		/// <para>Null for all other event messages.</para>  
		/// </summary>
		public string OperatorName = @"";

		/// <summary>
		/// Data to be included in the event message for alarms.  Null 
		/// if the event message is not reporting an alarm.  
		/// </summary>
		public AlarmMessageData? AlarmMessageData;

		/// <summary>
		/// The fields selected by the client to be included in Event Messages 
		/// for the indicated Event Category.  The fields that can be selected 
		/// by the client to be returned in Event Messages for a given Category 
		/// are specified in the EventCategories member of the Event Capabilities 
		/// MIB Element.
		/// </summary>
		public CaseInsensitiveDictionary<string>? ClientRequestedFields;

        #endregion
    }

	public class AlarmMessageData
	{
		/// <summary>
		/// The current state of the alarm.
		/// </summary>
		public AlarmState AlarmState;

		/// <summary>
		/// The state change(s) that caused the alarm message to sent.
		/// The AlarmStateChangeCodes class defines 
		/// the values for this member.
		/// </summary>
		public uint AlarmStateChange;

		/// <summary>
		/// The time that the alarm last transitioned to the active state.  
		/// This time is independent of the current state of the alarm.  
		/// Null if the alarm has never been active. 
		/// </summary>
		public DateTime? TimeLastActive;
	}

	public enum EventType
	{
		/// <summary>
		/// An event generated by a condition within the system 
		/// that does not require operator attention.
		/// See EEMUA Publication 191, 2.4.1.
		/// </summary>
		SystemEvent = 1,

		/// <summary>
		/// An event generated as the result of an operator action. 
		/// See EEMUA Publication 191, 2.4.1.
		/// </summary>
		OperatorActionEvent = 2,

		/// <summary>
		/// The general case of an alarm as defined by See EEMUA Publication 191.  
		/// A simple alarm is represented by a single condition.
		/// </summary>
		SimpleAlarm = 3,

		/// <summary>
		/// An alarm that is composed of a set of conditions that are 
		/// all related to the same monitored data object, but where 
		/// only the one with the highest operational significance 
		/// can be active at a time. See EEMUA Publication 191, A8.3.2. 
		/// </summary>
		EclipsedAlarm = 4,

		/// <summary>
		/// An alarm that is represented by multiple conditions, 
		/// any number of which can be active at the same time.  
		/// For alarms of this type, the alarm is active when at 
		/// least one of its conditions is active, and is inactive 
		/// when none of its conditions are active. See EEMUA 
		/// Publication 191, A8.2.
		/// </summary>
		GroupedAlarm = 5,

		/// <summary>
		/// An event similar to an alarm, but that is a lower 
		/// priority and has no significant consequences if missed. 
		/// Alerts are often referred to as warnings or prompts.  
		/// See EEMUA Publication 191, Appendix 7. 
		/// </summary>
		Alert = 6,

		/// <summary>
		/// An event that indicates that the server has discarded 
		/// one or more queued messages from its poll queue. This 
		/// event type is only used when event polling is in use. 
		/// </summary>
		DiscardedMessage = 7
	}

	[Flags]
	public enum AlarmState
	{
		/// <summary>
		/// The default value of 0 is the starting state, 
		/// which is inactive (cleared), acknowledged, enabled, 
		/// and unsuppressed.
		/// </summary>
		Initial = 0x00,

		/// <summary>
		/// The generation/detection of the alarm is disabled 
		/// even though the base condition may be active.
		/// </summary>
		Disabled = 0x01,

		/// <summary>
		/// The Alarm has been detected and its condition 
		/// continues to persist. This state is also referred 
		/// to as "raised" or "standing".  The inactive state 
		/// is indicated by not setting the Alarm to active.
		/// </summary>
		Active = 0x02,

		/// <summary>
		/// The Alarm has not been acknowledged.
		/// </summary>
		Unacked = 0x04,

		/// <summary>
		/// Automatic generation/detection of the alarm is 
		/// disabled, even though the base condition may be active.  
		/// </summary>
		Suppressed = 0x08
	}

	public class AlarmStateChangeCodes
	{
		/// <summary>
		/// The Active State has changed
		/// </summary>
		public const uint Active = 0x01;
		/// <summary>
		/// The Acknowledge State has changed. 
		/// </summary>
		public const uint Acknowledge = 0x02;
		/// <summary>
		/// The Disable State has changed.
		/// </summary>
		public const uint Disable = 0x04;
		/// <summary>
		/// The Priority has changed.
		/// </summary>
		public const uint Priority = 0x10;
		/// <summary>
		/// The Subcondition has changed.
		/// </summary>
		public const uint Subcondition = 0x20;
		/// <summary>
		/// The Message has changed.
		/// </summary>
		public const uint Message = 0x40;
		/// <summary>
		/// One or more of the Requested Fields has changed.
		/// </summary>
		public const uint RequestedField = 0x80;
	}
}
