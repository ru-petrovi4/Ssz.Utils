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
using System.Collections.Generic;
using System.ServiceModel;
using Xi.Contracts.Data;

namespace Xi.Contracts
{
	/// <summary>
	/// This interface is composed of methods used to retrieve 
	/// data, alarms, and events and their histories from the 
	/// server.
	/// </summary>
	[ServiceContract(Namespace = "urn:xi/contracts")]
	public interface IRead
	{
		/// <summary>
		/// <para>This method is used to read the values of one or more 
		/// data objects in a list.  It is also used as a keep-alive for the 
		/// read endpoint by setting the listId parameter to 0. In this case,
		/// null is returned immediately.  </para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listId">
		/// The  server identifier of the list that contains data objects to be read.
		/// Null if this is a keep-alive.
		/// </param>
		/// <param name="serverAliases">
		/// The server aliases of the data objects to read. When this value is null all elements 
		/// of the list are to be read.
		/// </param>
		/// <returns>
		/// <para>The list of requested values. Each value in this list is identified 
		/// by its client alias.  If the server alias for a data object to read 
		/// was not found, an ErrorInfo object will be returned that contains 
		/// the server alias instead of a value, status, and timestamp.  </para>
		/// <para>Returns null if this is a keep-alive.</para>
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		DataValueArraysWithAlias ReadData(string contextId, uint listId, List<uint> serverAliases);

		/// <summary>
		/// <para>This method is used to read the historical values that fall between 
		/// a start and end time for one or more data objects within a specific data 
		/// journal list.</para>
		/// </summary>
		/// <param name="contextId">
		/// The context id.
		/// </param>
		/// <param name="listId">
		/// The server identifier of the list that contains data objects whose 
		/// historical values are to be read.
		/// </param>
		/// <param name="firstTimeStamp">
		/// The filter that specifies the first or beginning (of returned list) 
		/// timestamp for values to be returned.  Valid operands include the 
		/// Timestamp (UTC) and OpcHdaTimestampStr constants defined by the 
		/// FilterOperand class.  The FilterOperand Operator is used to 
		/// determine if the returned data should include data values 
		/// the occur exactly at the first or second time stamp.  If the equals 
		/// operator is specified then values that occur at the first and second 
		/// time stamp will be included in the sample set.  Any other operator 
		/// will not include first or second time stamped values.
		/// </param>
		/// <param name="secondTimeStamp">
		/// The filter that specifies the second or ending (of returned list)
		/// timestamp for values to be returned.  Valid operands include the 
		/// Timestamp (UTC) and OpcHdaTimestampStr constants defined by the 
		/// FilterOperand class.  The FilterOperand Operator is not used.
		/// </param>
		/// <param name="numValuesPerAlias">
		/// The maximum number of JournalDataReturnValues to be returned per alias.
		/// </param>
		/// <param name="serverAliases">
		/// The list of server aliases for the data objects whose historical 
		/// values are to be read. When this value is null all elements of the list are to be read.
		/// </param>
		/// <returns>
		/// The list of requested historical values, or the reason they could not 
		/// be read.
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		JournalDataValues[] ReadJournalDataForTimeInterval(string contextId, uint listId,
			FilterCriterion firstTimeStamp, FilterCriterion secondTimeStamp,
			uint numValuesPerAlias, List<uint> serverAliases);

		/// <summary>
		/// <para>This method is used to return an in-sequence subset of the 
		/// historical values selected by the last IRead_ReadJournalDataForTimeInterval() 
		/// call issued by the client on this client context.  This method is used 
		/// when the number of values to be returned for one or more aliases 
		/// exceeds the number specified by the numValuesPerAlias parameter of the 
		/// IRead_ReadJournalDataForTimeInterval() method.  </para>
		/// <para>The client may have to reissue this call multiple times to 
		/// receive all historical values for all aliases.  The client may specify 
		/// a new numValuesPerAlias with each call to this method to better optimize 
		/// its performance.  </para>
		/// <para>The server is responsible for maintaining the list of requested 
		/// aliases for which values remain, and the timestamp of the last value 
		/// sent to the client for each alias. </para>
		/// </summary>
		/// <param name="contextId">
		/// The context id.
		/// </param>
		/// <param name="listId">
		/// The server identifier of the list that contains data objects whose 
		/// historical values are to be returned.
		/// </param>
		/// <param name="numValuesPerAlias">
		/// The maximum number of data sample values to be returned per alias.
		/// </param>
		/// <returns>
		/// The next set of remaining values for each alias.  If the number of values 
		/// returned for any one alias is less than numValuesPerAlias, then there are 
		/// no additional values to return to the client for that alias. If, however, 
		/// the number returned for any alias is equal to numValuesPerAlias, then the 
		/// client should issue a ReadJournalDataNext() to retrieve any remaining values.
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		JournalDataValues[] ReadJournalDataNext(string contextId, uint listId, uint numValuesPerAlias);

		/// <summary>
		/// <para>This method is used to read the historical values at specific times for 
		/// one or more data objects within a specific data journal list.  If no entry exists 
		/// at the specified time in the data journal for an object, the server creates an 
		/// interpolated value for that time and includes it in the response as though it 
		/// actually existed in the journal.</para>
		/// </summary>
		/// <param name="contextId">
		/// The context id.
		/// </param>
		/// <param name="listId">
		/// The server identifier of the list that contains data objects whose historical 
		/// values are to be read.
		/// </param>
		/// <param name="timestamps">
		/// Identifies the timestamps of historical values to be returned for each 
		/// of the requested data objects. 
		/// </param>
		/// <param name="serverAliases">
		/// The list of server aliases for the data objects whose historical 
		/// values are to be read. When this value is null all elements of the list are to be read.
		/// </param>
		/// <returns>
		/// The list of requested historical values, or the reason they could not 
		/// be read.
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		JournalDataValues[] ReadJournalDataAtSpecificTimes(string contextId, uint listId, 
			List<DateTime> timestamps, List<uint> serverAliases);

		/// <summary>
		/// <para>This method is used to read changed historical values for one 
		/// or more data objects within a specific data journal list.  Changed historical 
		/// values are those that were entered into the journal and then changed (corrected) 
		/// by an operator or other user.</para>
		/// </summary>
		/// <param name="contextId">
		/// The context id.
		/// </param>
		/// <param name="listId">
		/// The server identifier of the list that contains data objects whose historical 
		/// values are to be read.
		/// </param>
		/// <param name="firstTimeStamp">
		/// The filter that specifies the inclusive earliest (oldest) timestamp 
		/// for values to be returned.  Valid operands include the Timestamp and 
		/// OpcHdaTimestampStr constants defined by the FilterOperand class.
		/// </param>
		/// <param name="secondTimeStamp">
		/// The filter that specifies the inclusive newest (most recent) timestamp 
		/// for values to be returned.  Valid operands include the Timestamp and 
		/// OpcHdaTimestampStr constants defined by the FilterOperand class.
		/// </param>
		/// <param name="serverAliases">
		/// The list of server aliases for the data objects whose historical 
		/// values are to be read. When this value is null all elements of the 
		/// list are to be read.
		/// </param>
		/// <param name="numValuesPerAlias">
		/// The maximum number of JournalDataChangedValues to be returned per alias.  
		/// </param>
		/// <returns>
		/// The list of requested historical values, or the reason they could not 
		/// be read.  If, however, the number returned for any alias is equal to 
		/// numValuesPerAlias, then the client should issue a ReadJournalDataChangesNext() 
		/// to retrieve any remaining values.
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		JournalDataChangedValues[] ReadJournalDataChanges(string contextId, uint listId,
			FilterCriterion firstTimeStamp, FilterCriterion secondTimeStamp,
			uint numValuesPerAlias, List<uint> serverAliases);

		/// <summary>
		/// <para>This method is used to return an in-sequence subset of the 
		/// historical values selected by the last IRead_ReadJournalDataChanges() 
		/// call issued by the client on this client context.  This method is used 
		/// when the number of values to be returned for one or more aliases 
		/// exceeds the number specified by the numValuesPerAlias parameter of the 
		/// IRead_ReadJournalDataChanges() method.  </para>
		/// <para>The client may have to reissue this call multiple times to 
		/// receive all historical values for all aliases.  The client may specify 
		/// a new numValuesPerAlias with each call to this method to better optimize 
		/// its performance.  </para>
		/// <para>The server is responsible for maintaining the list of requested 
		/// aliases for which values remain, and the timestamp of the last value 
		/// sent to the client for each alias. </para>
		/// </summary>
		/// <param name="contextId">
		/// The context id.
		/// </param>
		/// <param name="listId">
		/// The server identifier of the list that contains data objects whose 
		/// historical values are to be returned.
		/// </param>
		/// <param name="numValuesPerAlias">
		/// The maximum number of JournalDataChangedValues to be returned per alias.
		/// </param>
		/// <returns>
		/// The next set of remaining values for each alias.  If the number of values 
		/// returned for any one alias is less than numValuesPerAlias, then there are 
		/// no additional values to return to the client for that alias. If, however, 
		/// the number returned for any alias is equal to numValuesPerAlias, then the 
		/// client should issue a ReadJournalDataChangesNext() to retrieve any remaining 
		/// values.
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		JournalDataChangedValues[] ReadJournalDataChangesNext(string contextId, uint listId, 
			uint numValuesPerAlias);

		/// <summary>
		/// <para>This method is used to read calculated historical values (e.g. averages or 
		/// interpolations) for one or more data objects within a specific data journal list.  
		/// The time-range used to select the historical values is specified by the client. 
		/// Additionally, the client specifies a calculation period that divides that time 
		/// range into periods. The server calculates a return value for each of these periods.</para>
		/// </summary>
		/// <param name="contextId">
		/// The context id.
		/// </param>
		/// <param name="listId">
		/// The server identifier of the list that contains data objects whose historical 
		/// values are to be read.
		/// </param>
		/// <param name="firstTimeStamp">
		/// The filter that specifies the inclusive earliest (oldest) timestamp 
		/// for values to be returned.  Valid operands include the Timestamp and 
		/// OpcHdaTimestampStr constants defined by the FilterOperand class.
		/// </param>
		/// <param name="secondTimeStamp">
		/// The filter that specifies the inclusive newest (most recent) timestamp 
		/// for values to be returned.  Valid operands include the Timestamp and 
		/// OpcHdaTimestampStr constants defined by the FilterOperand class.
		/// </param>
		/// <param name="calculationPeriod">
		/// The time span used to divide the specified time range into individual periods for 
		/// which return values are calculated.  The specified calculation is performed on the 
		/// set of historical values of a data object that fall within each period. 
		/// </param>
		/// <param name="serverAliasesAndCalculations">
		/// The list of server aliases for the data objects whose historical 
		/// values are to be calculated, and the calculation to perform for each.  
		/// </param>
		/// <returns>
		/// The set of calculated values. There is one value for each calculation period within 
		/// the specified time range for each specific data object.
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		JournalDataValues[] ReadCalculatedJournalData(string contextId, uint listId,
			FilterCriterion firstTimeStamp, FilterCriterion secondTimeStamp, TimeSpan calculationPeriod,
			List<AliasAndCalculation> serverAliasesAndCalculations);

		/// <summary>
		/// This method reads the properties associated with a historized data object.
		/// </summary>
		/// <param name="contextId">
		/// The context id.
		/// </param>
		/// <param name="listId">
		/// The server identifier of the list that contains data objects whose property 
		/// values are to be read.
		/// </param>
		/// <param name="firstTimeStamp">
		/// The filter that specifies the inclusive earliest (oldest) timestamp 
		/// for values to be returned.  Valid operands include the Timestamp and 
		/// OpcHdaTimestampStr constants defined by the FilterOperand class.
		/// </param>
		/// <param name="secondTimeStamp">
		/// The filter that specifies the inclusive newest (most recent) timestamp 
		/// for values to be returned.  Valid operands include the Timestamp and 
		/// OpcHdaTimestampStr constants defined by the FilterOperand class.
		/// </param>
		/// <param name="serverAlias">
		/// The server alias of the data object whose property values are to be read.  
		/// </param>
		/// <param name="propertiesToRead">
		/// The TypeIds of the properties to read. Each property is identified by 
		/// its property type.
		/// </param>
		/// <returns>
		/// The array of requested property values.
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		JournalDataPropertyValue[] ReadJournalDataProperties(string contextId, uint listId,
			FilterCriterion firstTimeStamp, FilterCriterion secondTimeStamp, uint serverAlias,
			List<TypeId> propertiesToRead);

		/// <summary>
		/// This method is used to read an event list or a subset of it 
		/// using a filter.
		/// </summary>
		/// <param name="contextId">
		/// The context id.
		/// </param>
		/// <param name="listId">
		/// The server identifier of the list that contains alarms and events 
		/// to be read.
		/// </param>
		/// <param name="filterSet">
		/// The set of filters used to select alarms and events 
		/// to be read.
		/// </param>
		/// <returns>
		/// The list of selected alarms and events.
		/// Null if no alarms or events were selected.
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		EventMessage[] ReadEvents(string contextId, uint listId, FilterSet filterSet);

		/// <summary>
		/// <para>This method is used to read a list of historical alarms or 
		/// events.  This method only accesses historical events rather 
		/// than also accessing historical data as does the MMS ReadJournal 
		/// service.  This is because the return value is strongly typed 
		/// to historical alarms and event messages and not to historical  
		/// data.</para>
		/// <para>To simplify implementation, clients must first define a   
		/// historical alarm/event list that the server will prepare to access. </para>
		/// </summary>
		/// <param name="contextId">
		/// The context id.
		/// </param>
		/// <param name="listId">
		/// The server identifier of the list that contains historical alarms and 
		/// events that are to be read.
		/// </param>
		/// <param name="firstTimeStamp">
		/// The filter that specifies the first or beginning (of returned list) 
		/// timestamp for event messages to be returned.  Valid operands include 
		/// the Timestamp (UTC) constant defined by the FilterOperand class.
		/// </param>
		/// <param name="secondTimeStamp">
		/// The filter that specifies the second or ending (of returned list)
		/// timestamp for event messages to be returned.  Valid operands include 
		/// the Timestamp (UTC) constant defined by the FilterOperand class.
		/// </param>
		/// <param name="numEventMessages">
		/// The maximum number of EventMessages to be returned.
		/// </param>
		/// <param name="filterSet">
		/// The set of filters used to select historical alarms and events 
		/// to be read.
		/// </param>
		/// <returns>
		/// The list of selected historical alarms and events.
		/// Or null if no alarms or events were selected.
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		EventMessage[] ReadJournalEvents(string contextId, uint listId, 
			FilterCriterion firstTimeStamp, FilterCriterion secondTimeStamp, uint numEventMessages,
			FilterSet filterSet);

		/// <summary>
		/// <para>This method is used to return an in-sequence subset of the 
		/// historical events selected by the last ReadJournalEvents() 
		/// call issued by the client on this client context.  This method is used 
		/// when the number of EventMessages to be returned exceeds the number specified 
		/// by the numEventMessages parameter of the ReadJournalEvents() method.  </para>
		/// <para>The client may have to reissue this call multiple times to 
		/// receive all historical EventMessages selected by the initial call to 
		/// ReadJournalEvents().  The client may specify a new numEventMessages with each 
		/// call to this method to better optimize its performance.  </para>
		/// </summary>
		/// <param name="contextId">
		/// The context id.
		/// </param>
		/// <param name="listId">
		/// The server identifier of the list that contains data objects whose 
		/// historical events are to be returned.
		/// </param>
		/// <param name="numEventMessages">
		/// The maximum number of EventMessages to be returned.
		/// </param>
		/// <returns>
		/// The selected EventMessages. If, however, the number returned is equal to 
		/// numEventMessages, then the client should issue a ReadJournalEventsNext() 
		/// to retrieve any remaining EventMessages.
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		EventMessage[] ReadJournalEventsNext(string contextId, uint listId, uint numEventMessages);

	}
}