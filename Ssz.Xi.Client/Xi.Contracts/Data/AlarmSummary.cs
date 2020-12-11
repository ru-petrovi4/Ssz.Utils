/**********************************************************************
 * Copyright © 2009, 2010, 2011, 2012 OPC Foundation, Inc. 
 *
 * The source code and all binaries built with the OPC .NET 3.0 source
 * code are subject to the terms of the Express Interface Public
 * License (Xi-PL).  See http://www.opcfoundation.org/License/Xi-PL/
 *
 * The source code may be distributed from an OPC member company in
 * its original or modified form to its customers and to any others who
 * have software that needs to interoperate with the OPC member's OPC
* .NET 3.0 products. No other redistribution is permitted.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *********************************************************************/

using System;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace Xi.Contracts.Data
{
	/// <summary>
	/// <para>This class defines summary information of an alarm.</para>
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class AlarmSummary : IExtensibleDataObject
	{
		/// <summary>
		/// This member supports the addition of new members to a data contract 
		/// class by recording versioning information about it.  
		/// </summary>
		ExtensionDataObject? IExtensibleDataObject.ExtensionData { get; set; }

		/// <summary>
		/// The type of the alarm. Only Event Types associated with alarms 
		/// are permissable.
		/// </summary>
		[DataMember] public EventType EventType;

		/// <summary>
		/// For simple alarms, the name of the condition.
		/// For eclipsed and grouped alarms, the MultiplexedAlarmContainer name 
		/// (see the EventId and AlarmDescription classes).
		/// </summary>
		[DataMember] public string? Name;

		/// <summary>
		/// The current state of the alarm.
		/// </summary>
		[DataMember] public AlarmState State;

		/// <summary>
		/// The status code associated with the data object used to detect 
		/// an occurrence of the alarm (e.g. the status of the PV parameter).
		/// </summary>
		[DataMember] public ushort AlarmStateStatusCode;

		/// <summary>
		/// <para>The name of the most recent condition to become active.  
		/// Null if no conditions are active.</para>  
		/// <para>For Grouped and Eclipsed Alarms, the most recent condition 
		/// to become active may change while the alarm is active. </para>
		/// </summary>
		[DataMember] public string? MostRecentActiveCondition;

		/// <summary>
		/// <para>The time that the most recent condition became active.  
		/// Null if no conditions are active.</para>  
		/// <para>For Grouped and Eclipsed Alarms, the most recent condition 
		/// to become active may change while the alarm is active.  Since this 
		/// represents the most recent condition to become active, this is the 
		/// time that should be specified when acknowledging the alarm. </para>
		/// </summary>
		[DataMember] public Nullable<DateTime> TimeMostRecentConditionActive;

		/// <summary>
		/// The time that the alarm last transitioned to the active state.  
		/// This time is independent of the current state of the alarm.  
		/// Null if the alarm has never been active. 
		/// </summary>
		[DataMember] public Nullable<DateTime> TimeAlarmLastActive;

		/// <summary>
		/// The time that the alarm last transitioned to the inactive state.  
		/// This time is independent of the current state of the alarm.  Null 
		/// if the alarm has never been active and transitioned to inactive. 
		/// </summary>
		[DataMember] public Nullable<DateTime> TimeAlarmLastInactive;

		/// <summary>
		/// The time that the alarm last transitioned to the acknowledged state.  
		/// This time is independent of the current state of the alarm.  Null if 
		/// the alarm has never transitioned to the acknowledged state. 
		/// </summary>
		[DataMember] public Nullable<DateTime> TimeLastAck;

		/// <summary>
		/// The name or other system-specific identifier of the operator 
		/// that last acknowledged the alarm. Null if the alarm was never
		/// acknowledged.
		/// </summary>
		[DataMember] public string? AcknowledgingOperator;

		/// <summary>
		/// The operator comment that accompanied the last acknowledgement.  
		/// Null if the alarm was never acknowledged.
		/// </summary>
		[DataMember] public string? OperatorLastAckComment;

		/// <summary>
		/// The names of the active conditions of the alarm. 
		/// Null if no conditions are active.  
		/// </summary>
		[DataMember] public List<string>? ActiveConditions;

		/// <summary>
		/// The list of conditions defined for the alarm and 
		/// their active state.  
		/// </summary>
		[DataMember] public List<AlarmCondition>? Conditions;

		/// <summary>
		/// Server-specific information about the alarm.
		/// </summary>
		[DataMember] public DataValueArraysWithAlias? ServerData;
	}
}