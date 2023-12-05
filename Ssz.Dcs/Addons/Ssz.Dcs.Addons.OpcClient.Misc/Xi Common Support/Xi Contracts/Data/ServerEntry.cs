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
using System.Runtime.Serialization;

namespace Xi.Contracts.Data
{
	/// <summary>
	/// This class defines the resource management endpoints of a server.  
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class ServerEntry
	{
		/// <summary>
		/// The description of the server.
		/// </summary>
		[DataMember] public ServerDescription ServerDescription { get; set; }

		/// <summary>
		/// The names of available metaDataExchange endpoints.
		/// These names can only be used as a selection choice for the client.
		/// The Mex endpoint communication settings must be standardized.
		/// </summary>
		[DataMember] public List<MexEndpointInfo> MexEndpoints { get; set; }

		/// <summary>
		/// Endpoint configuration settings that are not in the endpoint metadata.
		/// </summary>
		[DataMember] public List<EndpointConfiguration> EndpointServerSettings { get; set; }
	}
}