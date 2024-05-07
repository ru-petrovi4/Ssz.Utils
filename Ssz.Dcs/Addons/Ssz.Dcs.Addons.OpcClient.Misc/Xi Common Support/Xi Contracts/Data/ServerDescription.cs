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
	/// This class contains descriptive information about the 
	/// server.   
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class ServerDescription
	{
		/// <summary>
		/// The URL used to access the server's IServerDiscoveryEndpoint.
		/// </summary>
		[DataMember]
		public string ServerDiscoveryUrl { get; set; }

		/// <summary>
		/// The version number of the Xi Contracts used by this server.
		/// The version number is defined by the version number of the 
		/// Xi Contracts assembly.  If this member is null, then the version 
		/// number defaults to 1.0.0.0.
		/// </summary>
		[DataMember] public string XiContractsVersionNumber { get; set; }

        /// <summary>
        /// The server types supported by this server. Standard types are defined 
        /// by the ServerType class.
        /// <see cref="Xi.Contracts.Constants.ServerType" />
        /// </summary>
        [DataMember] public uint ConfiguredServerTypes;

		/// <summary>
		/// <para>Name of the server software vendor.  </para> 
		/// </summary>
		[DataMember] public string VendorName;

		/// <summary>
		/// <para>Namespace for types defined by this vendor.  This may or 
		/// may not be the same as the VendorName.  Null or empty if not used.</para> 
		/// </summary>
		[DataMember] public string VendorNamespace;

		/// <summary>
		/// <para>Name of the server software.</para> 
		/// </summary>
		[DataMember] public string ServerName;

		/// <summary>
		/// <para>Namespace for server-specific types. Null or empty if not used.</para> 
		/// <para>This name is typically a concatentation of the VendorNamespace 
		/// and the ServerName (separated by a '/' character) 
		/// (e.g "MyVendorNamespace/MyServer").</para>
		/// </summary>
		[DataMember] public string ServerNamespace;

		/// <summary>
		/// <para>The HostName of the machine in which the server resides (runs).  The 
		/// HostName is used as part of the object path in InstanceIds of the 
		/// server's objects.</para> 
		/// </summary>
		[DataMember] public string HostName;

		/// <summary>
		/// <para>The name of the WCF service provided by the server. </para> 
		/// </summary>
		[DataMember] public string ServiceName;

		/// <summary>
		/// <para>The name of the system that contains the objects accessible 
		/// through the server.  Null or empty if the server provides access 
		/// to objects from more than one system. </para> 
		/// </summary>
		[DataMember] public string SystemName;

		/// <summary>
		/// The list of InstanceId System property values supported by the server. 
		/// See the description of InstanceId for the description of valid values. 
		/// May be null if the server provides access to only one system.
		/// </summary>
		[DataMember] public List<string> InstanceIdSystemProperties;

		/// <summary>
		/// The URL for the Security Token Service. Null if the Security 
		/// Token Service is not present.
		/// </summary>
		[DataMember] public string SecurityTokenServiceUrl;

		/// <summary>
		/// Supported locale ids (the native language is first entry)
		/// </summary>
		[DataMember] public List<uint> SupportedLocaleIds;

		/// <summary>
		/// User/deployment-specific information about the server.
		/// </summary>
		[DataMember] public string UserInfo;

		/// <summary>
		/// Detailed information about the server.
		/// Set to null if the ServerDescription is being 
		/// accessed without a client context.
		/// </summary>
		[DataMember] public ServerDetails ServerDetails;
	}
}