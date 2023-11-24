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
using System.ServiceModel.Web;
using System.IO;

using Xi.Contracts.Data;

namespace Xi.Contracts
{
	/// <summary>
	/// This interface is used to locate Xi servers on the network 
	/// and their Resource Management endpoints.  Servers that 
	/// implement this interface may apply access controls to limit 
	/// the servers a client may discover.  
	/// </summary>
	[ServiceContract(Namespace = "urn:xi/contracts")]
	public interface IServerDiscovery
	{
		/// <summary>
		/// This method returns the list of servers the client is 
		/// authorized to discover.
		/// </summary>
		/// <returns>
		/// List of server entries.
		/// </returns>
		//[OperationContract, WebGet]
		[OperationContract, FaultContract(typeof(XiFault))]
		List<ServerEntry> DiscoverServers();

		/// <summary>
		/// <para>This method is used to get the description of the 
		/// server.  It is intended to be used by Xi Directory Services 
		/// servers to identify an Xi server and obtain its list of 
		/// Mex endpoint names.</para>
		/// </summary>
		/// <returns>
		/// The description of the server. 
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		ServerEntry DiscoverServerInfo();

		/// <summary>
		/// This method returns an abbreviated set of endpoint definition parameters for 
		/// use by Silverlight clients and other clients that cannot use Metadata Exhange 
		/// to retrieve complete endpoint descriptions from the server.  Client 
		/// applications capable of using Metadata Exchange should not call this method.
		/// </summary>
		/// <returns>
		/// Returns a list of EndpointConfigurationEx objects, one for each endpoint 
		/// supported by the server.
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		List<EndpointConfigurationEx> DiscoverAbbreviatedEndpointInfo();

		/// <summary>
		/// This method reads the clientaccesspolicy.xml from the EXE directory and returns 
		/// the content to the requester.
		/// </summary>
		/// <returns>Returns the clientaccesspolicy.xml as a stream.</returns>
		[OperationContract, WebGet(UriTemplate = "clientaccesspolicy.xml")]
		Stream GetSilverlightPolicy();
	}
}
