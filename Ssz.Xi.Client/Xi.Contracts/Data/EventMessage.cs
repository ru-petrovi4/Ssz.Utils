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
	/// <para>This class defines the Event Messages that are used to 
	/// report the occurrence of an event or alarm.</para> 
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
    [KnownType(typeof(DBNull))]
    // one dimensional arrays of basic types
    [KnownType(typeof(float[]))]
	[KnownType(typeof(double[]))]
	[KnownType(typeof(byte[]))]
	[KnownType(typeof(sbyte[]))]
	[KnownType(typeof(short[]))]
	[KnownType(typeof(ushort[]))]
	[KnownType(typeof(int[]))]
	[KnownType(typeof(uint[]))]
	[KnownType(typeof(long[]))]
	[KnownType(typeof(ulong[]))]
	[KnownType(typeof(bool[]))]
	//[KnownType(typeof(System.Collections.Generic.List<bool>))]  //?????
	[KnownType(typeof(string[]))]
	[KnownType(typeof(object[]))]
	[KnownType(typeof(System.DateTime[]))]
	[KnownType(typeof(System.TimeSpan[]))]
	[KnownType(typeof(System.Decimal[]))]
	// two dimensional arrays of basic types
	[KnownType(typeof(float[][]))]
	[KnownType(typeof(double[][]))]
	[KnownType(typeof(byte[][]))]
	[KnownType(typeof(sbyte[][]))]
	[KnownType(typeof(short[][]))]
	[KnownType(typeof(ushort[][]))]
	[KnownType(typeof(int[][]))]
	[KnownType(typeof(uint[][]))]
	[KnownType(typeof(long[][]))]
	[KnownType(typeof(ulong[][]))]
	[KnownType(typeof(bool[][]))]
	[KnownType(typeof(string[][]))]
	[KnownType(typeof(object[][]))]
	[KnownType(typeof(System.DateTime[][]))]
	[KnownType(typeof(System.TimeSpan[][]))]
	[KnownType(typeof(System.Decimal[][]))]
	// Xi types
	[KnownType(typeof(TypeId))]
	[KnownType(typeof(ServerStatus))]
	[KnownType(typeof(StringTableEntry))]
	[KnownType(typeof(StringTableEntry[]))]
	public class EventMessage : IExtensibleDataObject
	{
		/// <summary>
		/// This member supports the addition of new members to a data contract 
		/// class by recording versioning information about it.  
		/// </summary>
		ExtensionDataObject? IExtensibleDataObject.ExtensionData { get; set; }

		#region Data Members
		/// <summary>
		/// The time of the event/alarm occurrence.  
		/// </summary>
		[DataMember] public DateTime OccurrenceTime;

		/// <summary>
		/// The type of the event/alarm that is being reported by this 
		/// event message.  
		/// </summary>
		[DataMember] public EventType EventType;

		/// <summary>
		/// The identifier for the event/alarm occurrence.  
		/// </summary>
		[DataMember] public EventId? EventId;

		/// <summary>
		/// Text that describes the event occurrence.
		/// </summary>
		[DataMember] public string? TextMessage;

		/// <summary>
		/// The category to which the event is assigned.
		/// </summary>
		[DataMember] public uint CategoryId;

		/// <summary>
		/// The priority of the event.
		/// </summary>
		[DataMember] public uint Priority;

		/// <summary>
		/// <para>For event messages that report operator action events, 
		/// the name of the operator who caused an operator action event 
		/// to be generated.</para>
		/// <para>For event messages that report the acknowledgement of 
		/// an alarm, the name of the operator who acknowledged the 
		/// alarm.</para>
		/// <para>Null for all other event messages.</para>  
		/// </summary>
		[DataMember] public string? OperatorName;

		/// <summary>
		/// Data to be included in the event message for alarms.  Null 
		/// if the event message is not reporting an alarm.  
		/// </summary>
		[DataMember] public AlarmMessageData? AlarmData;

		/// <summary>
		/// The fields selected by the client to be included in Event Messages 
		/// for the indicated Event Category.  The fields that can be selected 
		/// by the client to be returned in Event Messages for a given Category 
		/// are specified in the EventCategories member of the Event Capabilities 
		/// MIB Element.
		/// </summary>
		[DataMember] public List<object>? ClientRequestedFields;

		public Ssz.Utils.DataSource.EventMessage ToEventMessage()
		{
			var eventInfo = new Ssz.Utils.DataSource.EventMessage(EventId != null ? EventId.ToEventId() : new Ssz.Utils.DataSource.EventId());
			eventInfo.OccurrenceTime = OccurrenceTime;
			eventInfo.EventType = (Ssz.Utils.DataSource.EventType)EventType;
			eventInfo.TextMessage = TextMessage ?? @"";
			eventInfo.CategoryId = CategoryId;
			eventInfo.Priority = Priority;
			eventInfo.OperatorName = OperatorName ?? @"";
			if (AlarmData != null)
			{
				eventInfo.AlarmMessageData = AlarmData.ToAlarmMessageData();
			}
			if (ClientRequestedFields != null)
			{
				// TODO
				//eventInfo.ClientRequestedFields = new CaseInsensitiveDictionary<string>(ClientRequestedFields);
			}
			return eventInfo;
		}

		#endregion
	}
}