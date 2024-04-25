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

using Xi.Common.Support;
using Xi.Contracts;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Xi.Server.Base
{
	/// <summary>
	/// This partial class implements the IPoll interface
	/// </summary>
	public abstract partial class ServerBase<TContext, TList> : ServerRoot
									, IPoll
		where TContext : ContextBase<TList>
		where TList : ListRoot
	{
		/// <summary>
		/// <para>This method is used to poll the endpoint for changes 
		/// to a specific data list.</para>    
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
		/// The identifier for the list whose changes are to be returned 
		/// (reported).
		/// </param>
		/// <returns>
		/// The list of changed values.
		/// </returns>
		DataValueArraysWithAlias IPoll.PollDataChanges(string contextId, uint listId)
		{
            //using (StaticLogger.Logger.EnterMethod(contextId, listId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId, false);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnPollDataChanges(listId);
				}				
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// This method is used to poll the endpoint for changes to a 
		/// specific event list.  Event messages are sent when there 
		/// has been a change to the specified event list. A new alarm 
		/// or event that has been added to the list, a change to an 
		/// alarm already in the list, or the deletion of an alarm from 
		/// the list constitutes a change to the list.
		/// <para>Once an event has been reported from the list, it 
		/// is automatically deleted from the list.  Alarms are only 
		/// deleted from the list when they transition to inactive and 
		/// acknowledged.  </para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listId">
		/// The identifier for the list whose changes are to be returned 
		/// (reported).
		/// </param>
		/// <param name="filterSet">
		/// Optional set of filters to further refine the selection from 
		/// the alarms and events in the list. The event list itself is 
		/// created using a filter.
		/// </param>
		/// <returns>
		/// The list of new alarm/event messages, changes to alarm messages 
		/// that are already in the list, and deletions of alarm messages 
		/// from the list.  
		/// </returns>
		EventMessage[] IPoll.PollEventChanges(string contextId, uint listId, FilterSet filterSet)
		{
            //using (StaticLogger.Logger.EnterMethod(contextId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId, false);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnPollEventChanges(listId, filterSet);
				}				
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

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
		List<PassthroughResult> IPoll.PollPassthroughResponses(string contextId)
		{
            //using (StaticLogger.Logger.EnterMethod(contextId))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId, false);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnPollPassthroughResponses();
				}				
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}
	}
}