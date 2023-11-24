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
using System.Runtime.Serialization;

namespace Xi.Contracts.Data
{
	/// <summary>
	/// <para>This class is used pass endpoint/binding configuration settings to the client
	/// that are not contained in the meatadata.</para>
	/// <para>This client can use this data to as default settings that mach the server setting.
	/// The client can use different settings. The metadata doen=sn't contain this information
	/// because the settings don't have necessarily be the same in server and client.</para>
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	[KnownType(typeof(TimeSpan))]
	public class EndpointConfiguration
	{
		#region Data Members
		/// <summary>
		/// The endpoint configuration name as specified by the endpoint name attribute 
		/// in the App.config file.
		/// </summary>
		[DataMember] public string EndpointName { get; set; }

		/// <summary>
		/// The URL of the endpoint. This corresponds to 
		/// System.ServiceModel.Description.ServiceEndpoint.Address.Uri.OriginalString.
		/// The EndpointUrl, when combined with the ContractType, uniquely identifies 
		/// the endpoint.
		/// </summary>
		[DataMember] public string EndpointUrl { get; set; }

		/// <summary>
		/// <para>The type of the contract associated with this endpoint.</para>
		/// <para>The values are defined using the typeof(IXXX).Name property, where IXXX is 
		/// the contract name (e.g. IRead).  This value is also used as the value of the 
		/// following property: </para>
		/// <para> System.ServiceModel.Description.ServiceEndpoint.Contract.Name  </para>
		/// <para> The EndpointUrl, when combined with the ContractType, uniquely identifies 
		/// the endpoint.</para>
		/// </summary>
		[DataMember] public string ContractType { get; set; }

		/// <summary>
		/// The buffer size from the server binding configuration.
		/// This member corresponds to the MaxBufferSize attribute in the 
		/// binding element associated with this endpoint in the server's 
		/// app.config file.
		/// </summary>
		[DataMember] public long MaxBufferSize { get; set; }

		/// <summary>
		/// The MaxItemsInObjectGraph attribute of the dataContractSerializer behavior 
		/// associated with the endpoint in the server.
		/// </summary>
		[DataMember] public int MaxItemsInObjectGraph { get; set; }

		/// <summary>
		/// The timeout setting from the server binding configuration.
		/// This member corresponds to the openTimeout attribute in the 
		/// binding element associated with this endpoint in the server's 
		/// app.config file.
		/// </summary>
		[DataMember] public TimeSpan OpenTimeout { get; set; }

		/// <summary>
		/// The timeout setting from the server binding configuration.
		/// This member corresponds to the closeTimeout attribute in the 
		/// binding element associated with this endpoint in the server's 
		/// app.config file.
		/// </summary>
		[DataMember] public TimeSpan CloseTimeout { get; set; }

		/// <summary>
		/// The timeout setting from the server binding configuration.
		/// This member corresponds to the sendTimeout attribute in the 
		/// binding element associated with this endpoint in the server's 
		/// app.config file.
		/// </summary>
		[DataMember] public TimeSpan SendTimeout { get; set; }

		/// <summary>
		/// The timeout setting from the server binding configuration.
		/// This member corresponds to the receiveTimeout attribute in the 
		/// binding element associated with this endpoint in the server's 
		/// app.config file.
		/// </summary>
		[DataMember] public TimeSpan ReceiveTimeout { get; set; }

		#endregion
	}
}