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
	/// This class identifies a data object to be added 
	/// to a list. 
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class ListInstanceId
	{
		/// <summary>
		/// The Object LocalId for the object, typically obtained 
		/// using the FindObjects() method, plus an optional 
		/// element identifier for elements of a constructed 
		/// data type.
		/// </summary>
		[DataMember] public InstanceId ObjectElementId;

		/// <summary>
		/// The client-assigned alias for the data object.
		/// This alias is used to refer to the data object 
		/// within the context of the list to which it is 
		/// added. The value 0 is reserved and cannot be used. 
		/// </summary>
		[DataMember] public uint ClientAlias;

		/// <summary>
		/// Specifies additional options for the list element
		/// (e.g., treat a data object as a string type)
		/// </summary>
		[DataMember] public uint ListElementOptions;
	}
}