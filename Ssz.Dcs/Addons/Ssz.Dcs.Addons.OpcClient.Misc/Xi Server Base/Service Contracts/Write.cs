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
	/// This partial class implements the IWrite interface
	/// </summary>
	public abstract partial class ServerBase<TContext, TList> : ServerRoot
									, IWrite
									where TContext : ContextBase<TList>
									where TList : ListRoot
	{
		/// <summary>
		/// <para>This method is used to write the values of one or more 
		/// data objects in a list.</para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listId">
		/// The identifier of the list that contains data objects to be read.
		/// </param>
		/// <param name="dataObjectsToWrite">
		/// The server aliases and values of the data objects to write.
		/// </param>
		/// <returns>
		/// The list server aliases and result codes for the data objects whose 
		/// write failed. Returns null if all writes succeeded.  
		/// </returns>
		List<AliasResult> IWrite.WriteValues(string contextId, uint listId,
									 WriteValueArrays writeValueArrays)
		{
            using (Logger.EnterMethod(contextId, listId, writeValueArrays))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId, false);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnWriteValues(listId, writeValueArrays);
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		List<AliasResult> IWrite.WriteVST(string contextId, uint listId,
							 DataValueArraysWithAlias writeValueArrays)
		{
            using (Logger.EnterMethod(contextId, listId, writeValueArrays))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId, false);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnWriteVST(listId, writeValueArrays);
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// <para>This method is used to modify historical data values.  
		/// The modification type parameter indicates the type of 
		/// modification to perform.  </para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listId">
		/// The identifier of the list that contains the data objects 
		/// to be written.
		/// </param>
		/// <param name="modificationType">
		/// Indicates the type of modification to perform.  
		/// </param>
		/// <param name="valuesToWrite">
		/// The list of historical values to write.  Each is identified 
		/// by its list id, its server alias, and its timestamp.
		/// </param>
		/// <returns>
		/// The list of identifiers and error codes for each data object 
		/// whose write failed. Returns null if all writes succeeded.  
		/// </returns>
		List<DataJournalWriteResult> IWrite.WriteJournalData(string contextId, uint listId,
			ModificationType modificationType, WriteJournalValues[] valuesToWrite)
		{
            using (Logger.EnterMethod(contextId, listId, modificationType, valuesToWrite))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId, false);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnWriteJournalData(listId, modificationType, valuesToWrite);
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// <para>This method is used to modify historical alarms and/or 
		/// events. The modification type parameter indicates the type of 
		/// modification to perform.  </para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listId">
		/// The identifier of the list that contains the alarms and/or 
		/// events to be written.
		/// </param>
		/// <param name="modificationType">
		/// Indicates the type of modification to perform.  
		/// </param>
		/// <param name="eventsToWrite">
		/// The list of historical alarms and/or events to write.  Each 
		/// is identified by its EventId contained in the EventMessage.
		/// </param>
		/// <returns>
		/// The list server aliases and result codes for the alarms and/or 
		/// events whose write failed. Returns null if all writes succeeded.  
		/// </returns>
		List<EventIdResult> IWrite.WriteJournalEvents(string contextId, uint listId,
			ModificationType modificationType, EventMessage[] eventsToWrite)
		{
            using (Logger.EnterMethod(contextId, listId, modificationType, eventsToWrite))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId, false);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnWriteJournalEvents(listId, modificationType, eventsToWrite);
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// <para>This method is used to acknowledge one or more alarms.</para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listId">
		/// The identifier for the list that contains the alarms to be 
		/// acknowledged.
		/// </param>
		/// <param name="operatorName">
		/// The name or other identifier of the operator who is acknowledging 
		/// the alarm.
		/// </param>
		/// <param name="comment">
		/// An optional comment submitted by the operator to accompany the 
		/// acknowledgement.
		/// </param>
		/// <param name="alarmsToAck">
		/// The list of alarms to acknowledge.
		/// </param>
		/// <returns>
		/// The list EventIds and result codes for the alarms whose 
		/// acknowledgement failed. Returns null if all acknowledgements 
		/// succeeded.  
		/// </returns>
		List<EventIdResult> IWrite.AcknowledgeAlarms(string contextId, uint listId,
			string operatorName, string comment, List<EventId> alarmsToAck)
		{
            using (Logger.EnterMethod(contextId, listId, operatorName, comment, alarmsToAck))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId, false);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnAcknowledgeAlarms(listId, operatorName, comment, alarmsToAck);
				}
                catch (FaultException<XiFault> fe)
                {
                    throw fe;
                }
                catch (Exception ex)
                {
                    throw FaultHelpers.Create(ex);
                }
			}
		}

		/// <summary>
		/// This method allows the client to send a message to the server that 
		/// the server delivers unmodified to the intended recipient.
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="recipientId">
		/// The recipient identifier. The list of recipients is contained in 
		/// the RecipientPassthroughs MIB element.   
		/// </param>
		/// <param name="invokeId">
		/// A client-defined integer identifier for this invocation of the passthrough.  When
		/// used with asynchronous passthroughs, the server returns the invokeId with the response.  
		/// </param>
		/// <param name="passthroughName">
		/// The name of the passthrough message. The list of passthroughs for a recipient 
		/// is contained in the RecipientPassthroughs MIB element.   
		/// </param>
		/// <param name="DataToSend">
		/// The Data To Send is just an array of bytes.  No interpretation of the data 
		/// is made by the Xi server.  This byte array is forwarded unaltered to the 
		/// underlying system.  It is up to the client application to format this byte 
		/// array in a valid format for the underlying system.
		/// </param>
		/// <returns>
		/// The Passthrough Result simply returns a Result value and a byte array as 
		/// returned from the underlying system.  Again it is up to the client 
		/// application to interpret this byte array.
		/// </returns>
		PassthroughResult IWrite.Passthrough(string contextId, string recipientId, int invokeId,
									  string passthroughName, byte[] DataToSend)
		{
            using (Logger.EnterMethod(contextId, recipientId, passthroughName, DataToSend))
			{
				try
				{
					TContext context = ContextManager<TContext, TList>.LookupContext(contextId, false);
					if (context == null)
						throw FaultHelpers.Create(XiFaultCodes.E_NOCONTEXT);

					return context.OnPassthrough(recipientId, invokeId, passthroughName, DataToSend);
				}
				catch (FaultException<XiFault> fe)
				{
					throw fe;
				}
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

	}
}