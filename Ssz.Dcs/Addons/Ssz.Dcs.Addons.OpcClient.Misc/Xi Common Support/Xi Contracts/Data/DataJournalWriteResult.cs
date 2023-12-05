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
	/// Result from a data journal write request
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class DataJournalWriteResult
	{
		/// <summary>
		/// Result code for the corresponding WriteJournalValues entry.
		/// </summary>
		[DataMember] public uint ResultCode { get; set; }

		/// <summary>
		/// List alias
		/// </summary>
		[DataMember] public uint ListAlias { get; set; }

		/// <summary>
		/// Server data alias
		/// </summary>
		[DataMember] public uint ServerDataAlias { get; set; }
	}
}