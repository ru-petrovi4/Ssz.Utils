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
	/// This class defines the results of attempting to retrieve 
	/// a string.
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class RequestedString : IExtensibleDataObject
	{
		/// <summary>
		/// This member supports the addition of new members to a data 
		/// contract class by recording versioning information about it.  
		/// </summary>
		ExtensionDataObject? IExtensibleDataObject.ExtensionData { get; set; }

		/// <summary>
		/// The Result Code associated with retrieving the string.
		/// </summary>
		[DataMember] public uint ResultCode;

		/// <summary>
		/// The requested string.  If the ResultCode for this string 
		/// indicates failure, this string is null.   
		/// </summary>
		[DataMember] public string? String;
	}
}