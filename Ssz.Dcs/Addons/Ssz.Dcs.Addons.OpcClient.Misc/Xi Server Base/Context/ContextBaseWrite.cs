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

using Xi.Common.Support;
using Xi.Contracts.Constants;
using Xi.Contracts;
using Xi.Contracts.Data;
using System;

namespace Xi.Server.Base
{
	/// <summary>
	/// This partial class defines the methods to be overridden by the server implementation 
	/// to support the methods of the IWrite interface.
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
		/// <param name="writeValueList">
		/// The server aliases and values of the data objects to write.
		/// </param>
		/// <returns>
		/// The list server aliases and result codes for the data objects whose 
		/// write failed. Returns null if all writes succeeded or null if this 
		/// is a keep-alive.  
		/// </returns>
		internal List<AliasResult> OnWriteValues(uint listId, WriteValueArrays writeValueList)
		{
			// If this is a keep-alive, return null
			if (listId == 0)
				return null;

			TList tList = null;
			lock (ContextLock)
			{
				bool bGetValue = _XiLists.TryGetValue(listId, out tList);
				if ((bGetValue) && (tList != null))
					tList.AuthorizeEndpointUse(typeof(IWrite)); // throws an exception if validation fails
				else
					throw FaultHelpers.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Write Values.");
			}
			return tList.OnWriteValues(writeValueList);
		}

		/// <summary>
		/// This method is used to allow an Xi client to write the value along with a 
		/// corresponding time stamp and status.
		/// </summary>
		/// <param name="listId"></param>
		/// <param name="readValueArrays"></param>
		/// <returns></returns>
		internal List<AliasResult> OnWriteVST(uint listId, DataValueArraysWithAlias readValueArrays)
		{
			TList tList = null;
			lock (ContextLock)
			{
				bool bGetValue = _XiLists.TryGetValue(listId, out tList);
				if ((bGetValue) && (tList != null))
					tList.AuthorizeEndpointUse(typeof(IWrite)); // throws an exception if validation fails
				else
					throw FaultHelpers.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Write VST.");
			} // end lock
			return tList.OnWriteVST(readValueArrays);
		}

		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// </summary>
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
		internal List<DataJournalWriteResult> OnWriteJournalData(uint listId,
			ModificationType modificationType, WriteJournalValues[] valuesToWrite)
		{
			TList tList = null;
			lock (ContextLock)
			{
				bool bGetValue = _XiLists.TryGetValue(listId, out tList);
				if ((bGetValue) && (tList != null))
					tList.AuthorizeEndpointUse(typeof(IWrite)); // throws an exception if validation fails
				else
					throw FaultHelpers.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Write Journal Data.");
			}
			return tList.OnWriteJournalData(modificationType, valuesToWrite);
		}

		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// </summary>
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
		internal List<EventIdResult> OnWriteJournalEvents(uint listId,
			ModificationType modificationType, EventMessage[] eventsToWrite)
		{
			TList tList = null;
			lock (ContextLock)
			{
				bool bGetValue = _XiLists.TryGetValue(listId, out tList);
				if ((bGetValue) && (tList != null))
					tList.AuthorizeEndpointUse(typeof(IWrite)); // throws an exception if validation fails
				else
					throw FaultHelpers.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Write Journal Events.");
			}
			return tList.OnWriteJournalEvents(modificationType, eventsToWrite);
		}

		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// </summary>
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
		internal List<EventIdResult> OnAcknowledgeAlarms(uint listId,
			string operatorName, string comment, List<EventId> alarmsToAck)
		{
			TList tList = null;
			lock (ContextLock)
			{
				bool bGetValue = _XiLists.TryGetValue(listId, out tList);
				if ((bGetValue) && (tList != null))
					tList.AuthorizeEndpointUse(typeof(IWrite)); // throws an exception if validation fails
				else
					throw FaultHelpers.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Acknowledge Alarms.");
			}
			return tList.OnAcknowledgeAlarms(operatorName, comment, alarmsToAck);
		}

		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// </summary>
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
		internal virtual PassthroughResult OnPassthrough(string recipientId, int invokeId,
			string passthroughName, ReadOnlyMemory<byte> dataToSend)
		{
			// TODO:  Implement the PassthroughResult method if supported. 
			// If this method is implemented, the corresponding bit in the StandardMib.MethodsSupported must be set.
			throw FaultHelpers.Create(XiFaultCodes.E_NOTIMPL, "IWrite.Passthrough");
		}
	}
}
