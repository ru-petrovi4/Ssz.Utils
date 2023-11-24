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
	/// This class contains the results of attmepting to access a set of historized 
	/// property values for a given data object.
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class JournalDataPropertyValue
	{
		/// <summary>
		/// The result code associated with accessing the property.  
		/// See XiFaultCodes claass for standardized result codes. 
		/// </summary>
		[DataMember] public uint ResultCode { get; set; }

		/// <summary>
		/// The client-assigned alias for the historized data object.
		/// </summary>
		[DataMember] public uint ClientAlias { get; set; }

		/// <summary>
		/// The id of the property being accessed.
		/// </summary>
		[DataMember] public TypeId PropertyId { get; set; }

		/// <summary>
		/// An optional list of history properties of the historized data object.  
		/// </summary>
		[DataMember] public DataValueArrays PropertyValues { get; set; }
	}
}