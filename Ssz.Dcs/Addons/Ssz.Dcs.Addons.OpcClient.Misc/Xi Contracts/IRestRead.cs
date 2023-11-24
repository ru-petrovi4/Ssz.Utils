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

using System.ServiceModel;
//using System.ServiceModel.Web;

using Xi.Contracts.Data;

// To support the Web RestRead the following changes need to be made.
// 1) In the App.config files for Xi ServiceHost OPCWrapper Console and Xi ServiceHost OPCWrapper WinService
//    include the line or lines under <!--webHttpBinding Endpoints--> to enable the binding.
// 2) In IServerDiscovery.cs include the "using System.ServiceModel.Web" and change the attributes of
//    "List<ServerEntry> DiscoverServers()" to "[OperationContract, WebGet]"
// 3) In IRestRead.cs include the "using System.ServiceModel.Web" and change the attributes of 
//    "DataValueArraysWithAlias RestReadData(string contextId, string listId)" to
//    "[OperationContract, WebGet(UriTemplate = "/datalist/changes/{contextId}/{listId}")]"
// Note that all of the code to change has been commented out and simply changing the comment "//"
// lines will allow for the uses of the web browser web get.

namespace Xi.Contracts
{
	/// <summary>
	/// <para>NOTE: Support for the REST Web Services has been removed 
	/// to allow the Xi.Contracts assembly to be used with the 
	/// .NET Framework 4 Client Profile.  See comment in source file 
	/// for how to re enable this feature.</para>
	/// <para>This interface is composed of methods used to retrieve 
	/// data, alarms, and events and their histories from the 
	/// server using REST Web Services.</para>
	/// </summary>
	[ServiceContract(Namespace = "urn:xi/contracts")]
	public interface IRestRead
	{
		/// <summary>
		/// <para>This method is used to read the values of the 
		/// data objects in a list.</para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listId">
		/// The identifier of the list that contains data objects to be read.
		/// </param>
		/// <returns>
		/// The list of requested values. The size and order of this list 
		/// matches the size and order of serverAliases parameter.
		/// </returns>
		//[OperationContract, WebGet(UriTemplate = "/datalist/changes/{contextId}/{listId}")]
		[OperationContract, FaultContract(typeof(XiFault))]
		DataValueArraysWithAlias RestReadData(string contextId, string listId);

		//[OperationContract, WebInvoke(UriTemplate = "/datalist/Journal/{contextId}/{listId}", Method = "POST")]
		//List<HistoryReturnValues> GetJournalList(string contextId, string listId, List<Int64> timeRange,
		//                                         List<FilterCriterion> filters, List<JournalDataId> HistoryDataToRead);

		//[OperationContract, WebInvoke(UriTemplate = "/datalist/JournalEvents/{contextId}/{listId}", Method = "POST")]
		//List<EventMessage> GetJournalEventsList(string contextId, string listId, List<FilterCriterion> filters);
	}
}
