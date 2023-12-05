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
	/// <para>The boolean attributes of an object.</para> 
	/// </summary>
	[Flags]
	[DataContract(Namespace = "urn:xi/data")]
	public enum ObjectAttributeFlags : uint
	{
		/// <summary>
		/// Indicates, when TRUE, that the object can be read.  
		/// The value is FALSE if the object is not a data object.
		/// IsReadable is nullable to allow it to be set to null when 
		/// the server does not know whether or not the object is readable.
		/// </summary>
		[EnumMember] IsReadable          = 0x00000001,

		/// <summary>
		/// Indicates, when TRUE, that the object can be written.  
		/// The value is FALSE if the object is not a data object.  
		/// IsWritable is nullable to allow it to be set to null when 
		/// the server does not know whether or not the object is writable.
		/// </summary>
		[EnumMember] IsWritable          = 0x00000002,

		/// <summary>
		/// Indicates, when TRUE, that this object is not permitted to have 
		/// children in the tree.
		/// </summary>
		[EnumMember] IsLeaf              = 0x00000004,

		/// <summary>
		/// Indicates, when TRUE, that the object is currently 
		/// collecting historical values.  This attribute applies to 
		/// data objects and to objects that are Event Sources (for 
		/// event/alarm collection)
		/// </summary>
		[EnumMember] IsCollectingHistory = 0x00000008,

		/// <summary>
		/// Indicates, when TRUE, that this object is a data list.  The members of 
		/// the list are leaves of this object.
		/// </summary>
		[EnumMember] IsDataList          = 0x00000010,

		/// <summary>
		/// Indicates, when TRUE, that this object is an event list.  Event list 
		/// objects must have a leaf item beneath them of type FilterSet that 
		/// defines (selects) the members of the list.
		/// </summary>
		[EnumMember] IsEventList         = 0x00000020,

		/// <summary>
		/// Indicates, when FALSE, that the object can be accessed.  
		/// The value is TRUE if the object cannot be accessed. This 
		/// may occur if, for example, the object represents a wrapped 
		/// server and that server is not currently accessible by the 
		/// Xi server wrapper.
		/// </summary>
		[EnumMember] NotAccessible       = 0x00000040,

	}
}