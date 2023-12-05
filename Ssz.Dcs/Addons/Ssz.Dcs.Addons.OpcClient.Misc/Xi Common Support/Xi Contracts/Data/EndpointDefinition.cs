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

using System.Diagnostics;
using System.Runtime.Serialization;

namespace Xi.Contracts.Data
{
	/// <summary>
	/// <para>This class is used to return the definition of an endpoint 
	/// exposed by the server.</para>
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	[DebuggerDisplay("{Url}")]
	public class EndpointDefinition
	{
		#region Data Members

		/// <summary>
		/// The EndpointId is used to uniquely identify this endpoint definition. 
		/// This identifier is assigned by the server.
		/// </summary>
		[DataMember] public string EndpointId { get; set; }

		/// <summary>
		/// The type of the Xi interface supported by the endpoint.  Values are 
		/// defined using the typeof(IXXX).Name property, where IXXX is the contract 
		/// name (e.g. IRead, IWrite).  This value is also used as the value for the 
		/// Name property of the System.ServiceModel.Description.ServiceEndpoint.Contract 
		/// member.  
		/// </summary>
		[DataMember] public string ContractType { get; set; }

		/// <summary>
		/// <para>The type of the binding (WSHttpBinding, NetTcpBinding, etc.) 
		/// as defined in the config.app file.  For standard bindings,
		/// this is the endpoint binding attribute:</para>
		/// <para>  endpoint binding="wsHttpBinding"</para> 
		/// For custom bindings, this is the name attribute of the binding 
		/// element of the custom binding:
		/// <para>  binding name="binaryHttpBinding" </para>
		/// </summary>
		[DataMember] public string BindingName { get; set; }

		/// <summary>
		/// The URL used to access the endpoint
		/// </summary>
		[DataMember] public string Url { get; set; }
		#endregion

		#region ToString Override
		/// <summary>
		/// This method represents the endpoint as a string using 
		/// its URL.
		/// </summary>
		/// <returns>
		/// The URL of the endpoint.
		/// </returns>
		public override string ToString()
		{
			return Url + " for " + ContractType;
		}
		#endregion
	}
}