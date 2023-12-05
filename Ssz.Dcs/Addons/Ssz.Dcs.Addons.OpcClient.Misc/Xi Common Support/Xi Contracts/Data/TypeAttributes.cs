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
	/// This class provides a type attributes for a data type or 
	/// an object type.
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class TypeAttributes : IExtensibleDataObject
	{
		/// <summary>
		/// This member supports the addition of new members to a data 
		/// contract class by recording versioning information about it.  
		/// </summary>
		ExtensionDataObject IExtensibleDataObject.ExtensionData { get; set; }

		/// <summary>
		/// The identifier for the type.
		/// </summary>
		[DataMember] public TypeId TypeId;

		/// <summary>
		/// This string provides the display name.  Names are not permitted 
		/// to contain the forward slash ('/') character.
		/// </summary>
		[DataMember] public string Name;

		/// <summary>
		/// The description of the type.
		/// </summary>
		[DataMember] public string Description;

		/// <summary>
		/// The ObjectPath for the Branch that contains the list of members for 
		/// this type. The list of members and their ObjectAttributes can be 
		/// retrieved by calling FindObjects() using the MemberPath.  The  
		/// InstanceId of these ObjectAttributes is set to null since these 
		/// ObjectAttributes describe all instances of the member, and  
		/// instances of the member description inherit its ObjectAttributes. 
		/// </summary>
		[DataMember] public ObjectPath MemberPath;

		/// <summary>
		/// A byte-string that contains a detailed specification of the type.  
		/// The SchemaType element of the TypeId indicates the format of 
		/// the schema.  Null if unknown or unused.
		/// </summary>
		[DataMember] public Byte[] Schema { get; set; }
	}
}