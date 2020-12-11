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
using System.ServiceModel;
using Xi.Contracts.Data;

namespace Xi.Contracts
{
	/// <summary>
	/// This interface is composed of methods to be implemented by the 
	/// client and called by the server to send data, alarms, and 
	/// events to the client.
	/// </summary>
	[ServiceContract(Namespace = "urn:xi/contracts")]
	public interface ICallback
	{
		/// <summary>
		/// <para>This callback method is implemented by the client to 
		/// be notified when the server server state changes to Aborting,
		/// or if the server wraps other servers, when the wrapped server 
		/// state changes to Aborting. The Aborting state is entered when the 
		/// server begins shutting down.
		/// Clients that use the poll interface instead of this callback 
		/// interface are notified of aborting servers by calling the Status() 
		/// method, by receiving exceptions that are thrown when attempting to 
		/// access a server that is shutting down, or by the XiStatusCode that 
		/// indicates a wrapped server is not communicating.</para> 
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="serverStatus">
		/// The ServerStatus object that describes the server that is shutting down.
		/// </param>
		/// <param name="reason">
		/// The reason the context is being closed.
		/// </param>
		[OperationContract(IsOneWay = true)]
		void Abort(string contextId, ServerStatus serverStatus, string reason);

		/// <summary>
		/// <para>This callback method is implemented by the client 
		/// to receive data changes. </para> 
		/// <para> Servers send data changes to the client that have 
		/// not been reported to the client via this method.  
		/// Changes consists of:</para>
		/// <para>1) values for data objects that were added to the list,</para> 
		/// <para>2) values for data objects whose current values 
		/// have changed since the last time they were reported to the 
		/// client via this interface.  If a deadband filter has been 
		/// defined for the list, floating point values are not considered 
		/// to have changed unless they have changed by the deadband amount.</para>
		/// <para>3) historical values that meet the list filter criteria, 
		/// including the deadband.</para> 
		/// <para>In addition, the server may insert a special value that 
		/// indicates the server or one of its wrapped servers are shutting down.  </para>
		/// <para>This value is inserted as the first value in the list of values 
		/// in the callback. Its ListId and ClientId are both 0 and its data type is 
		/// ServerStatus. </para>
		/// <para>Finally, if the server does not have any values to send within the time period 
		/// established with the IRegisterForCallback.SetCallback() method, then the server should 
		/// call the InformationReport() method with a null updatedValues parameter, and the client 
		/// should interpret this call as a keep-alive for the ICallback endpoint connection. </para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listId">
		/// The client identifier of the list for which data changes are being 
		/// reported.
		/// </param>
		/// <param name="updatedValues">
		/// The values being reported.
		/// </param>
		[OperationContract(IsOneWay = true)]
		void InformationReport(string contextId, uint listId, DataValueArraysWithAlias updatedValues);

		/// <summary>
		/// <para>This callback method is implemented by the client to 
		/// receive alarms and events.</para> 
		/// <para> Servers send event messages to the client via this 
		/// interface.  Event messages are sent when there has been a 
		/// change to the specified event list. A new alarm or event 
		/// that has been added to the list, a change to an alarm already 
		/// in the list, or the deletion of an alarm from the list 
		/// constitutes a change to the list.</para>
		/// <para>Once an event has been reported from the list, it 
		/// is automatically deleted from the list.  Alarms are only 
		/// deleted from the list when they transition to inactive and 
		/// acknowledged.  </para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listId">
		/// The client identifier of the list for which alarms/events are being 
		/// reported.
		/// </param>
		/// <param name="eventList">
		/// The list of alarms/events are being reported, transferred as an array.
		/// </param>
		[OperationContract(IsOneWay = true)]
		void EventNotification(string contextId, uint listId, EventMessage[] eventList);

		/// <summary>
		/// This method returns the results of invoking an asynchronous passthrough.
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="invokeId">
		/// The identifier for this invocation of the passthrough defined by the client 
		/// in the request.  
		/// </param>
		/// <param name="passthroughResult">
		/// The result of executing the passthrough, consisting of the result code, the invokeId 
		/// supplied in the request, and a byte array.  It is up to the client application to 
		/// interpret this byte array.  
		/// </param>
		[OperationContract(IsOneWay = true)]
		void PassthroughCallback(string contextId, int invokeId, PassthroughResult passthroughResult);

	}

	/// <summary>
	/// This interface is used to register for callbacks so that list updates are passed
	/// back asynchronously.
	/// </summary>
	[ServiceContract(Namespace = "urn:xi/contracts", CallbackContract = typeof(ICallback))]
	public interface IRegisterForCallback
	{
		/// <summary>
		/// This method is invoked to allow the client to set or change the 
		/// keepAliveSkipCount and callbackRate. The first time this method is 
		/// invoked the server obtains the callback interface from the client.  
		/// Therefore, this method must be called at least once for each 
		/// callback endpoint to enable the server to make the callbacks.
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="keepAliveSkipCount">
		/// The client-requested keepAliveSkipCount for lists that the server may negotiate 
		/// up or down. The keepAliveSkipCount indicates the number of consecutive 
		/// UpdateRate cycles for a list that occur with nothing to send before an empty 
		/// callback is sent to indicate a keep-alive message. For example, if the value 
		/// of this parameter is 1, then a keep-alive callback will be sent each UpdateRate 
		/// cycle for each list assigned to the callback for which there is nothing to send. 
		/// A value of 0 indicates that keep-alives are not to be sent for any list assigned 
		/// to the callback.
		/// </param>
		/// <param name="callbackRate">
		/// <para>The callback rate indicates the maximum time between callbacks that are sent 
		/// to the client. The server may negotiate this value up or down, but a null value or 
		/// a value representing 0 time is not valid.  </para>
		/// <para>If there are no callbacks to be sent containing data or events for this period 
		/// of time, an empty callback will be sent as a keep-alive.  The timer for this 
		/// time-interval starts when the SetCallback() response is returned by the server.  </para>
		/// </param>
		/// <returns>
		/// The results of the operation, including the negotiated keep-alive skip count and callback rate.
		/// </returns>
		[OperationContract]
		SetCallbackResult SetCallback(string contextId, uint keepAliveSkipCount, TimeSpan callbackRate);

	}

}
