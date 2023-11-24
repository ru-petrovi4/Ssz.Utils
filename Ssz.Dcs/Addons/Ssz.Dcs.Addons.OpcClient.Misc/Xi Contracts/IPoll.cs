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

using System.Collections.Generic;
using System.ServiceModel;
using Xi.Contracts.Data;

namespace Xi.Contracts
{
	/// <summary>
	/// <para>This interface is composed of methods called by the client to 
	/// poll the server for data, alarms, events, and asynchronous passthrough 
	/// responses.</para>
	/// <para>The server returns poll responses from a queue or similar mechanism 
	/// that stores data, alarms, events, and asynchronous passthrough 
	/// responses that are returned via callbacks when the callback interface is 
	/// used by the client.  To protect against indeterminate queue sizes, the 
	/// server is permitted to define the maximum number of entries that can be 
	/// stored in the queue. This number should be large enough to hold entries 
	/// for two update rates for the lists assigned to the Poll endpoint.  </para>
	/// <para>When the queue becomes full, the server is permitted to discard 
	/// the oldest entry in the queue when a new entry is to be added.  </para>
	/// <para>When entries are discarded, a new entry is added to the head of the 
	/// queue that indicates how many entries have been discarded, including the 
	/// entry that was discarded to make room for this status entry. See each 
	/// Poll method below for the definition of this status entry.</para>
	/// </summary>
	[ServiceContract(Namespace = "urn:xi/contracts")]
	public interface IPoll
	{
		/// <summary>
		/// <para>This method is used to poll the endpoint for changes 
		/// to a specific data list.  It is also used as a keep-alive for the 
		/// poll endpoint by setting the listId parameter to 0. In this case,
		/// null is returned immediately.</para>    
		/// <para>Changes consists of:</para>
		/// <para>1) values for data objects that were added to the list,</para> 
		/// <para>2) values for data objects whose current values 
		/// have changed since the last time they were reported to the 
		/// client via this interface.  If a deadband filter has been 
		/// defined for the list, floating point values are not considered 
		/// to have changed unless they have changed by the deadband amount.</para>
		/// <para>3) historical values that meet the list filter criteria, 
		/// including the deadband.</para> 
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listId">
		/// The identifier for the list whose changes are to be returned (reported).
		/// Null if this is a keep-alive.
		/// </param>
		/// <returns>
		/// <para>The list of changed values or null if this is a keep-alive.  The 
		/// following two standard data objects can also be returned. </para>  
		/// <para>The first is identified by a ListId of 0 and a ClientAlias of 0.  It 
		/// contains a ServerStatus object value that indicates to the client that 
		/// the server or one of its wrapped servers is shutting down.  When present, 
		/// this will always be the first value in the returned OBJECT value array.</para>
		/// <para>The second is identified by its ListId and a ClientAlias of 0.  It contains 
		/// a UInt32 value that indicates to the client how many data changes have been 
		/// discarded for the specified list since the last poll response.  If this 
		/// condition persists, the client should increase its poll frequency. 
		/// When present, this will always be the first value in the returned 
		/// UINT value array.</para>
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		DataValueArraysWithAlias PollDataChanges(string contextId, uint listId);

		/// <summary>
		/// <para>This method is used to poll the endpoint for changes to 
		/// a specific event list.  Event messages are sent when there 
		/// has been a change to the specified event list. A new alarm 
		/// or event that has been added to the list, a change to an 
		/// alarm already in the list, or the deletion of an alarm from 
		/// the list constitutes a change to the list.</para>para>
		/// <para>Once an event has been reported from the list, it 
		/// is automatically deleted from the list.  Alarms are only 
		/// deleted from the list when they transition to inactive and 
		/// acknowledged.  </para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listId">
		/// The server identifier for the list whose changes are to be returned 
		/// (reported).
		/// </param>
		/// <param name="filterSet">
		/// Optional set of filters to further refine the selection from 
		/// the alarms and events in the list. The event list itself is 
		/// created using a filter.
		/// </param>
		/// <returns>
		/// <para>A list consisting of alarm/event messages for new alarms/events 
		/// in the Event List, and alarm/event messages that represent state changes 
		/// to alarms that are already in the list, including alarm/event messages 
		/// that identify state changes that caused alarms to tbe deleted from the list.</para> 
		/// <para>Null is returned as a keep-alive message when there have been no new 
		/// alarm/event messages since the last poll.</para>
		/// <para>In addition, a special event message is included as the first entry 
		/// in the list to indicate to the client that one or more event message have 
		/// been discarded due to queue size limitations.  All fields of this message 
		/// are set to null with the exception of the following:
		///<para>	OccurrenceTime = current time of the response</para>
		///<para>	EventType = EventType.DiscardedMessage</para>
		///<para>	TextMessage = the number of event/alarm messages discarded since 
		///last poll response was returned.</para>
		/// </para>
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		EventMessage[] PollEventChanges(string contextId, uint listId, FilterSet filterSet);

		/// <summary>
		/// This method returns the results of invoking one or more asynchronous passthrough 
		/// requests.
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <returns>
		/// The results of executing the passthroughs. Each result in the list consists of the 
		/// result code, the invokeId supplied in the request, and a byte array.  It is up to the 
		/// client application to interpret this byte array.  
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		List<PassthroughResult> PollPassthroughResponses(string contextId);

	}
}