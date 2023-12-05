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
using Ssz.Utils;
using Ssz.Utils.Net4;
using Xi.Common.Support;
using Xi.Contracts;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Xi.Server.Base
{
	/// <summary>
	/// This partial class implements the IRead interface
	/// </summary>
	public abstract partial class ServerBase<TContext, TList> : ServerRoot
									, IRead
									where TContext : ContextBase<TList>
									where TList : ListRoot
	{
		/// <summary>
		/// <para>This method is used to read the values of one or more 
		/// data objects in a list.</para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listId">
		/// The identifier of the list that contains data objects to be read.
		/// </param>
		/// <param name="serverAliases">
		/// The server aliases of the data objects to read.
		/// </param>
		/// <returns>
		/// The list of requested values. The size and order of this list 
		/// matches the size and order of serverAliases parameter.
		/// </returns>
		DataValueArraysWithAlias IRead.ReadData(string contextId, uint listId, List<uint> serverAliases)
		{
            using (Logger.EnterMethod(contextId, listId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId, false);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					DataValueArraysWithAlias readValueList = context.OnReadData(listId, serverAliases);
					return readValueList;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// <para>This method is used to read the historical values that fall between 
		/// a start and end time for one or more data objects within a specific data 
		/// journal list.</para>
		/// </summary>
		/// <param name="contextId">
		/// The context id.
		/// </param>
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
		JournalDataValues[] IRead.ReadJournalDataForTimeInterval(string contextId, uint listId,
			FilterCriterion firstTimeStamp, FilterCriterion secondTimeStamp,
			uint numValuesPerAlias, List<uint> serverAliases)
		{
            using (Logger.EnterMethod(contextId, listId))
			{
                try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId, false);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnReadJournalDataForTimeInterval(listId,
						firstTimeStamp, secondTimeStamp, numValuesPerAlias, serverAliases);
                }                
                catch (Exception ex)
                {
                    throw FaultHelpers.Create(ex);
                }
			}
		}

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
		/// The identifier of the list that contains data objects whose 
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
		JournalDataValues[] IRead.ReadJournalDataNext(string contextId, uint listId,
			uint numValuesPerAlias)
		{
            using (Logger.EnterMethod(contextId, listId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId, false);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnReadJournalDataNext(listId, numValuesPerAlias);
				}				
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

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
		/// The identifier of the list that contains data objects whose historical 
		/// values are to be read.
		/// </param>
		/// <param name="timestamps">
		/// Identifies the timestamps of historical values to be returned for each 
		/// of the requested data objects. 
		/// </param>
		/// <param name="serverAliases">
		/// The list of server aliases for the data objects whose historical 
		/// values are to be read.  
		/// </param>
		/// <returns>
		/// The list of requested historical values, or the reason they could not 
		/// be read.
		/// </returns>
		JournalDataValues[] IRead.ReadJournalDataAtSpecificTimes(string contextId, uint listId,
									  List<DateTime> timestamps, List<uint> serverAliases)
		{
            using (Logger.EnterMethod(contextId, listId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId, false);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnReadJournalDataAtSpecificTimes(listId,
						timestamps, serverAliases);
				}				
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// <para>This method is used to read changed historical values that for one 
		/// or more data objects within a specific data journal list.  Changed historical 
		/// values are those that were entered into the journal and then changed (corrected) 
		/// by an operator or other user.</para>
		/// </summary>
		/// <param name="contextId">
		/// The context id.
		/// </param>
		/// <param name="listId">
		/// The identifier of the list that contains data objects whose historical 
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
		/// values are to be read.  
		/// </param>
		/// <returns>
		/// The list of requested historical values, or the reason they could not 
		/// be read.
		/// </returns>
		JournalDataChangedValues[] IRead.ReadJournalDataChanges(string contextId, uint listId,
			FilterCriterion firstTimeStamp, FilterCriterion secondTimeStamp,
			uint numValuesPerAlias, List<uint> serverAliases)
		{
            using (Logger.EnterMethod(contextId, listId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId, false);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnReadJournalDataChanges(listId,
						firstTimeStamp, secondTimeStamp, numValuesPerAlias, serverAliases);
				}				
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// <para>This method is used to return an in-sequence subset of the 
		/// historical values selected by the last OnReadJournalDataChanges() 
		/// call issued by the client on this client context.  This method is used 
		/// when the number of values to be returned for one or more aliases 
		/// exceeds the number specified by the numValuesPerAlias parameter of the 
		/// OnReadJournalDataChanges() method.  </para>
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
		/// The identifier of the list that contains data objects whose 
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
		JournalDataChangedValues[] IRead.ReadJournalDataChangesNext(string contextId, uint listId,
			uint numValuesPerAlias)
		{
            using (Logger.EnterMethod(contextId, listId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId, false);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnReadJournalDataChangesNext(listId, numValuesPerAlias);
				}				
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		JournalDataValues[] IRead.ReadCalculatedJournalData(string contextId, uint listId,
			FilterCriterion firstTimeStamp, FilterCriterion secondTimeStamp, TimeSpan calculationPeriod,
			List<AliasAndCalculation> serverAliasesAndCalculations)
		{
            using (Logger.EnterMethod(contextId, listId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId, false);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnReadCalculatedJournalData(listId, firstTimeStamp, secondTimeStamp,
						calculationPeriod, serverAliasesAndCalculations);
				}				
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		JournalDataPropertyValue[] IRead.ReadJournalDataProperties(string contextId, uint listId,
			FilterCriterion firstTimeStamp, FilterCriterion secondTimeStamp, uint serverAlias,
			List<TypeId> propertiesToRead)
		{
            using (Logger.EnterMethod(contextId, listId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId, false);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnReadJournalDataProperties(listId, firstTimeStamp, secondTimeStamp,
						serverAlias, propertiesToRead);
				}				
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// This method is used to read an event list or a subset of it 
		/// using a filter.
		/// </summary>
		/// <param name="contextId">
		/// The context id.
		/// </param>
		/// <param name="listId">
		/// The identifier of the list that contains alarms and events 
		/// to be read.
		/// </param>
		/// <param name="filterSet">
		/// The FilterSet used to select the alarms and events to read.
		/// </param>
		/// <returns>
		/// The list of selected alarms and events.
		/// Null if no alarms or events were selected.
		/// </returns>
		EventMessage[] IRead.ReadEvents(string contextId, uint listId, FilterSet filterSet)
		{
            using (Logger.EnterMethod(contextId, listId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId, false);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnReadEvents(listId, filterSet);
				}				
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

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
		/// The FilterSet used to select the historical alarms and events to read.
		/// </param>
		/// <returns>
		/// The list of selected historical alarms and events.
		/// Or null if no alarms or events were selected.
		/// </returns>
		EventMessage[] IRead.ReadJournalEvents(string contextId, uint listId,
			FilterCriterion firstTimeStamp, FilterCriterion secondTimeStamp, uint numEventMessages, FilterSet filterSet)
		{
            using (Logger.EnterMethod(contextId, listId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId, false);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnReadJournalEvents(listId, firstTimeStamp, secondTimeStamp, numEventMessages, filterSet);
				}				
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

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
		/// The identifier of the list that contains data objects whose 
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
		EventMessage[] IRead.ReadJournalEventsNext(string contextId, uint listId, uint numEventMessages)
		{
            using (Logger.EnterMethod(contextId, listId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId, false);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnReadJournalEventsNext(listId, numEventMessages);
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

	}
}
