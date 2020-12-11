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
	/// This class contains dynamic information about the server.   
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class ServerStatus
	{
		/// <summary>
		/// The name of the Xi server or wrapped server. For the Xi server, 
		/// this is the ServerName contained in the ServerDescription object.  
		/// For wrapped OPC COM servers, the Prog Id of the server.
		/// </summary>
		[DataMember] public string? ServerName;

		/// <summary>
		/// The type of the server for which the status is being reported.
		/// The Xi.Contracts.Constants.ServerType enumeration is used to 
		/// identify the type of the server. 
		/// </summary>
		[DataMember] public uint? ServerType;

		/// <summary>
		/// The current time in the server.
		/// </summary>
		[DataMember] public DateTime CurrentTime;

		/// <summary>
		/// The current state of the server.
		/// </summary>
		[DataMember] public ServerState? ServerState;

		/// <summary>
		/// Text string specific to the current state of the server.
		/// for example, when the server state is aborting, this string contains the reason.
		/// </summary>
		[DataMember] public string? Info;

	}
}