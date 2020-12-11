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
	/// This class defines the response messsage that is returned  
	/// to the client for a passthrough request.  
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class PassthroughResult : IExtensibleDataObject
	{
		/// <summary>
		/// This member supports the addition of new members to a data 
		/// contract class by recording versioning information about it.  
		/// </summary>
		ExtensionDataObject? IExtensibleDataObject.ExtensionData { get; set; }

		/// <summary>
		/// The Result Code returned by the passthrough mechanism.
		/// </summary>
		[DataMember]
		public uint ResultCode { get; set; }

		/// <summary>
		/// The InvokeId passed in the Passthrough() request.
		/// </summary>
		[DataMember] public int InvokeId { get; set; }

		/// <summary>
		/// The data returned by the passthough mechanism.
		/// </summary>
		[DataMember] public byte[]? ReturnData { get; set; }
	}
}