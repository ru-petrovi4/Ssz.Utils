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
	/// <para>This class extends the EndpointConfiguration class. It is included to support Silverlight 
	/// clients and other clients that cannot use Metadata Exhange to retrieve complete service endpoint 
	/// descriptions from the server.</para>  
	/// <para>A list of EndpointConfigurationEx objects are returned by the IServerDiscovery.DiscoverEndpoints() 
	/// method. This method should not be called by client applications capable of using Metadata Exchange.</para>
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class EndpointConfigurationEx : EndpointConfiguration
	{
		/// <summary>
		/// Corresponds to "typeof(System.ServerModel.Description.ServiceEndpoint.Binding).ToString()" value 
		/// of the server endpoint.
		/// </summary>
		[DataMember] public string? BindingType { get; set; }

		/// <summary>
		/// Corresponds to "System.ServerModel.Description.ServiceEndpoint.ListenUri.Scheme" value 
		/// of the server endpoint.
		/// </summary>
		[DataMember] public string? BindingScheme { get; set; }

		/// <summary>
		/// Corresponds to "System.ServerModel.Description.ServiceEndpoint.Binding.Security.Mode" value 
		/// of the server endpoint.
		/// </summary>
		[DataMember] public string? SecurityMode { get; set; }

		/// <summary>
		/// Corresponds to "System.ServerModel.Description.ServiceEndpoint.Binding.Security.Transport.ClientCredentialType.ToString()" 
		/// value f the server endpoint.
		/// </summary>
		[DataMember] public string? ClientCredentialType { get; set; }
	}

}