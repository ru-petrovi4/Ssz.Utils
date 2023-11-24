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

using System.Runtime.Serialization;

namespace Xi.Contracts.Data
{
	/// <summary>
	/// This class defines the details of a MEX endpoint to give the
	/// client enough information to select an endpoint in case the
	/// server has multiple MEX endpoints and to access this MEX endpoint.
	/// The Binding details cannot be communicated and must be standardized.
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class MexEndpointInfo
	{
		/// <summary>
		/// The description of the endpoint.
		/// </summary>
		[DataMember] public string Description { get; set; }

		/// <summary>
		/// The names of the metaDataExchange endpoint.
		/// </summary>
		[DataMember] public string EndpointName { get; set; }

		/// <summary>
		/// The URL the client needs to use to access the endpoint.
		/// </summary>
		[DataMember] public string Url { get; set; }
	}
}