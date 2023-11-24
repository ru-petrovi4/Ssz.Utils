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

using Xi.Common.Support;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;
using Xi.Contracts;

namespace Xi.Server.Base
{
	/// <summary>
	/// This partial class defines the methods to be overridden by the server implementation 
	/// to support the methods of the IRead interface.
	/// </summary>
	public abstract partial class ContextBase<TList>
		where TList : ListRoot
	{
		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// </summary>
		/// <param name="listId">
		/// The identifier of the list that contains data objects to be read.
		/// Null if this is a keep-alive.
		/// </param>
		/// <param name="serverAliases">
		/// The server aliases of the data objects to read.
		/// </param>
		/// <returns>
		/// <para>The list of requested values. Each value in this list is identified 
		/// by its client alias.  If the server alias for a data object to read 
		/// was not found, an ErrorInfo object will be returned that contains 
		/// the server alias instead of a value, status, and timestamp.  </para>
		/// <para>Returns null if this is a keep-alive.</para>
		/// </returns>
		internal DataValueArraysWithAlias OnReadData(uint listId, List<uint> serverAliases)
		{
			// If this is a keep-alive, return null
			if (listId == 0)
				return null;

			TList tList = null;
			lock (ContextLock)
			{
				bool bGetValue = _XiLists.TryGetValue(listId, out tList);
				if ((bGetValue) && (tList != null))
					tList.AuthorizeEndpointUse(typeof(IRead)); // throws an exception if validation fails
				else
					throw FaultHelpers.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Read Data.");
			}

			if (serverAliases == null) // null means to read all elements in the list
				serverAliases = tList.GetServerAliases();

			return tList.OnReadData(serverAliases);
		}

		/// <summary>
		/// <para>This method is used to read the historical values that fall between 
		/// a start and end time for one or more data objects within a specific data 
		/// journal list.</para>
		/// </summary>
		/// <param name="listId">
		/// The identifier of the list that contains data objects whose 
		/// historical values are to be read.
		/// </param>
		/// <param name="firstTimeStamp">
		/// The filter that specifies the inclusive beginning (of returned list) 
		/// timestamp for values to be returned.  Valid operands include the 
		/// Timestamp and OpcHdaTimestampStr constants defined by the 
		/// FilterOperand class.
		/// </param>
		/// <param name="secondTimeStamp">
		/// The filter that specifies the inclusive ending (of returned list)
		/// timestamp for values to be returned.  Valid operands include the 
		/// Timestamp and OpcHdaTimestampStr constants defined by the 
		/// FilterOperand class.
		/// </param>
		/// <param name="numValuesPerAlias">
		/// The maximum number of data sample value to be returned.
		/// </param>
		/// <param name="serverAliases">
		/// The list of server aliases for the data objects whose historical 
		/// values are to be read.  
		/// </param>
		/// <returns>
		/// The list of requested historical values, or the reason they could not 
		/// be read.
		/// </returns>
		internal JournalDataValues[] OnReadJournalDataForTimeInterval(uint listId,
			FilterCriterion firstTimeStamp, FilterCriterion secondTimeStamp,
			uint numValuesPerAlias, List<uint> serverAliases)
		{
			TList tList = null;
			lock (ContextLock)
			{
				bool bGetValue = _XiLists.TryGetValue(listId, out tList);
				if ((bGetValue) && (tList != null))
					tList.AuthorizeEndpointUse(typeof(IRead)); // throws an exception if validation fails
				else
					throw FaultHelpers.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Read Journal Data For Time Interval.");
			}

			if (serverAliases == null) // null means to read all elements in the list
				serverAliases = tList.GetServerAliases();

			return tList.OnReadJournalDataForTimeInterval(firstTimeStamp, secondTimeStamp, numValuesPerAlias, serverAliases);
		}

		internal JournalDataValues[] OnReadJournalDataNext(uint listId,
			uint numValuesPerAlias)
		{
			TList tList = null;
			lock (ContextLock)
			{
				bool bGetValue = _XiLists.TryGetValue(listId, out tList);
				if ((bGetValue) && (tList != null))
					tList.AuthorizeEndpointUse(typeof(IRead)); // throws an exception if validation fails
				else
					throw FaultHelpers.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Read Journal Data Next.");
			}
			return tList.OnReadJournalDataNext(numValuesPerAlias);
		}

		internal JournalDataValues[] OnReadJournalDataAtSpecificTimes(uint listId,
			List<DateTime> timestamps, List<uint> serverAliases)
		{
			TList tList = null;
			lock (ContextLock)
			{
				bool bGetValue = _XiLists.TryGetValue(listId, out tList);
				if ((bGetValue) && (tList != null))
					tList.AuthorizeEndpointUse(typeof(IRead)); // throws an exception if validation fails
				else
					throw FaultHelpers.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Read Journal Dat aAt Specific Times.");
			}

			if (serverAliases == null) // null means to read all elements in the list
				serverAliases = tList.GetServerAliases();

			return tList.OnReadJournalDataAtSpecificTimes(timestamps, serverAliases);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="listId"></param>
		/// <param name="firstTimeStamp"></param>
		/// <param name="secondTimeStamp"></param>
		/// <param name="serverAliases"></param>
		/// <returns></returns>
		internal JournalDataChangedValues[] OnReadJournalDataChanges(uint listId,
			FilterCriterion firstTimeStamp, FilterCriterion secondTimeStamp,
			uint numValuesPerAlias, List<uint> serverAliases)
		{
			TList tList = null;
			lock (ContextLock)
			{
				bool bGetValue = _XiLists.TryGetValue(listId, out tList);
				if ((bGetValue) && (tList != null))
					tList.AuthorizeEndpointUse(typeof(IRead)); // throws an exception if validation fails
				else
					throw FaultHelpers.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Read Journal Data Changes.");
			}

			if (serverAliases == null) // null means to read all elements in the list
				serverAliases = tList.GetServerAliases();

			return tList.OnReadJournalDataChanges(firstTimeStamp, secondTimeStamp, numValuesPerAlias, serverAliases);
		}

		internal JournalDataChangedValues[] OnReadJournalDataChangesNext(uint listId,
			uint numValuesPerAlias)
		{
			TList tList = null;
			lock (ContextLock)
			{
				bool bGetValue = _XiLists.TryGetValue(listId, out tList);
				if ((bGetValue) && (tList != null))
					tList.AuthorizeEndpointUse(typeof(IRead)); // throws an exception if validation fails
				else
					throw FaultHelpers.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Read Journal Data Changes Next.");
			}
			return tList.OnReadJournalDataChangesNext(numValuesPerAlias);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="listId"></param>
		/// <param name="firstTimeStamp"></param>
		/// <param name="secondTimeStamp"></param>
		/// <param name="calculationPeriod"></param>
		/// <param name="serverAliasesAndCalculations"></param>
		/// <returns></returns>
		internal JournalDataValues[] OnReadCalculatedJournalData(uint listId,
			FilterCriterion firstTimeStamp, FilterCriterion secondTimeStamp, TimeSpan calculationPeriod,
			List<AliasAndCalculation> serverAliasesAndCalculations)
		{
			TList tList = null;
			lock (ContextLock)
			{
				bool bGetValue = _XiLists.TryGetValue(listId, out tList);
				if ((bGetValue) && (tList != null))
					tList.AuthorizeEndpointUse(typeof(IRead)); // throws an exception if validation fails
				else
					throw FaultHelpers.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Read Journal Data.");
			}
			return tList.OnReadCalculatedJournalData(firstTimeStamp, secondTimeStamp, calculationPeriod,
				serverAliasesAndCalculations);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="listId"></param>
		/// <param name="firstTimeStamp"></param>
		/// <param name="secondTimeStamp"></param>
		/// <param name="serverAlias"></param>
		/// <param name="propertiesToRead"></param>
		/// <returns></returns>
		internal JournalDataPropertyValue[] OnReadJournalDataProperties(uint listId,
			FilterCriterion firstTimeStamp, FilterCriterion secondTimeStamp, uint serverAlias,
			List<TypeId> propertiesToRead)
		{
			TList tList = null;
			lock (ContextLock)
			{
				bool bGetValue = _XiLists.TryGetValue(listId, out tList);
				if ((bGetValue) && (tList != null))
					tList.AuthorizeEndpointUse(typeof(IRead)); // throws an exception if validation fails
				else
					throw FaultHelpers.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Read Journal Data.");
			}
			return tList.OnReadJournalDataProperties(firstTimeStamp, secondTimeStamp, serverAlias, propertiesToRead);
		}

		/// <summary>
		/// This method is to be overridden by the context implementation 
		/// in the Server Implementation project.
		/// </summary>
		/// <param name="listId">
		/// The identifier of the list that contains alarms and events 
		/// to be read.
		/// </param>
		/// <param name="filterSet">
		/// The set of filters used to select alarms and events to be read.
		/// </param>
		/// <returns>
		/// The list of selected alarms and events.
		/// Null if no alarms or events were selected.
		/// </returns>
		internal EventMessage[] OnReadEvents(uint listId, FilterSet filterSet)
		{
			TList tList = null;
			lock (ContextLock)
			{
				bool bGetValue = _XiLists.TryGetValue(listId, out tList);
				if ((bGetValue) && (tList != null))
					tList.AuthorizeEndpointUse(typeof(IRead)); // throws an exception if validation fails
				else
					throw FaultHelpers.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Read Events.");
			}
			return tList.OnReadEvents(filterSet);
		}

		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// </summary>
		/// <param name="listId">
		/// The identifier of the list that contains historical alarms and 
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
		internal EventMessage[] OnReadJournalEvents(uint listId,
			FilterCriterion firstTimeStamp, FilterCriterion secondTimeStamp,
			uint numEventMessages, FilterSet filterSet)
		{
			TList tList = null;
			lock (ContextLock)
			{
				bool bGetValue = _XiLists.TryGetValue(listId, out tList);
				if ((bGetValue) && (tList != null))
					tList.AuthorizeEndpointUse(typeof(IRead)); // throws an exception if validation fails
				else
					throw FaultHelpers.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Read Journal Events.");
			}
			return tList.OnReadJournalEvents(firstTimeStamp, secondTimeStamp, numEventMessages, filterSet);
		}

		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// </summary>
		/// <param name="listId">
		/// The identifier of the list that contains historical alarms and 
		/// events that are to be read.
		/// </param>
		/// <param name="numEventMessages">
		/// The maximum number of EventMessages to return.
		/// </param>
		/// <returns>
		/// The list of selected historical alarms and events.
		/// Null if no alarms or events were selected.
		/// </returns>
		internal EventMessage[] OnReadJournalEventsNext(uint listId, uint numEventMessages)
		{
			TList tList = null;
			lock (ContextLock)
			{
				bool bGetValue = _XiLists.TryGetValue(listId, out tList);
				if ((bGetValue) && (tList != null))
					tList.AuthorizeEndpointUse(typeof(IRead)); // throws an exception if validation fails
				else
					throw FaultHelpers.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Read Journal Events Next.");
			}
			return tList.OnReadJournalEventsNext(numEventMessages);
		}
	}
}
