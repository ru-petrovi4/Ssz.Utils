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
using System.Collections.Generic;

namespace Xi.Contracts.Data
{

	/// <summary>
	/// This class is used to return the attributes of an object.
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class ObjectAttributes : IExtensibleDataObject
	{
		#region Data Members
		/// <summary>
		/// This member supports the addition of new members to a data 
		/// contract class by recording versioning information about it.  
		/// </summary>
		ExtensionDataObject? IExtensibleDataObject.ExtensionData { get; set; }

		/// <summary>
		/// This string provides the display name.  Names are not permitted 
		/// to contain the forward slash ('/') character.
		/// </summary>
		[DataMember] public string? Name;

		/// <summary>
		/// This string provides the description of the object.  Null if unknown.
		/// </summary>
		[DataMember] public string? Description;

		/// <summary>
		/// This unsigned integer contains a set of bit flags, each of which 
		/// defines some boolean aspect of the object.  The ObjectAttributeFlags 
		/// enumeration defines values for this object attribute.
		/// </summary>
		[DataMember] public uint ObjectFlags;

		/// <summary>
		/// <para>The identifier for the object instance.</para>
		/// <para>If the object is a data object that can be added to 
		/// a list, this is the identifier used for that purpose. </para>
		/// <para>This identifier is not used for the ObjectAttributes 
		/// of members of types (see the MemberPath element of TypeAttributes). </para>
		/// </summary>
		[DataMember] public InstanceId? InstanceId;

		/// <summary>
		/// <para>The identifier of the object type.  Setpoint, Process 
		/// Variable, and PID Block are all examples of object types.</para>  
		/// <para>Null if the object type is unknown.</para>
		/// </summary>
		[DataMember] public TypeId? ObjectTypeId;

		/// <summary>
		/// The data type of a data object.  Null if the object is not a 
		/// data object.
		/// </summary>
		[DataMember] public TypeId? DataTypeId;

		/// <summary>
		/// <para>The number of elements in this list specifies 
		/// the number of dimensions of a List object. The value 
		/// of each entry in this list specifies the maximum number 
		/// of elements in each dimension. A value of zero indicates 
		/// that there is no maximum.</para>
		/// <para>Null if this object is not a list.</para>
		/// </summary>
		[DataMember] public List<uint>? ListDimensions;

		/// <summary>
		/// The fastest the server can collect values from the 
		/// underlying system for the object (in milliseconds).  
		/// The value 0 indicates that there is no maximum rate, 
		/// or that the maximum is unknown.  Null if unused. 
		/// </summary>
		[DataMember] public Nullable<uint> FastestScanRate;

		/// <summary>
		/// The list of roles defined for this Xi object. 
		/// See the ObjectRole class for more information about roles.
		/// </summary>
		[DataMember] public List<TypeId>? Roles;
		#endregion // Data Members

		#region Properties
		/// <summary>
		/// This property represents the IsReadable bit in the ObjectFlags data member.
		/// </summary>
		public bool IsReadable
		{
			get 
			{ 
				return ((ObjectFlags & (uint)ObjectAttributeFlags.IsReadable) != 0); 
			}
			set
			{ 
				ObjectFlags = (value)
							? ObjectFlags | (uint)ObjectAttributeFlags.IsReadable
							: ObjectFlags & ~(uint)ObjectAttributeFlags.IsReadable;
			}
		}

		/// <summary>
		/// This property represents the IsWritable bit in the ObjectFlags data member.
		/// </summary>
		public bool IsWritable
		{
			get
			{
				return ((ObjectFlags & (uint)ObjectAttributeFlags.IsWritable) != 0);
			}
			set
			{
				ObjectFlags = (value)
							? ObjectFlags | (uint)ObjectAttributeFlags.IsWritable
							: ObjectFlags & ~(uint)ObjectAttributeFlags.IsWritable;
			}
		}

		/// <summary>
		/// This property represents the IsLeaf bit in the ObjectFlags data member.
		/// </summary>
		public bool IsLeaf
		{
			get
			{
				return ((ObjectFlags & (uint)ObjectAttributeFlags.IsLeaf) != 0);
			}
			set
			{
				ObjectFlags = (value)
							? ObjectFlags | (uint)ObjectAttributeFlags.IsLeaf
							: ObjectFlags & ~(uint)ObjectAttributeFlags.IsLeaf;
			}
		}

		/// <summary>
		/// This property represents the NotAccessible bit in the ObjectFlags data member.
		/// The set accessor is available to allow the client application  to mark this 
		/// object as not accessible.
		/// </summary>
		public bool NotAccessible
		{
			get
			{
				return ((ObjectFlags & (uint)ObjectAttributeFlags.NotAccessible) != 0);
			}
			set
			{
				ObjectFlags = (value)
							? ObjectFlags | (uint)ObjectAttributeFlags.NotAccessible
							: ObjectFlags & ~(uint)ObjectAttributeFlags.NotAccessible;
			}
		}

		/// <summary>
		/// This property represents the IsCollectingHistory bit in the ObjectFlags data member.
		/// </summary>
		public bool IsCollectingHistory
		{
			get
			{
				return ((ObjectFlags & (uint)ObjectAttributeFlags.IsCollectingHistory) != 0);
			}
			set
			{
				ObjectFlags = (value)
							? ObjectFlags | (uint)ObjectAttributeFlags.IsCollectingHistory
							: ObjectFlags & ~(uint)ObjectAttributeFlags.IsCollectingHistory;
			}
		}

		/// <summary>
		/// This property represents the IsDataList bit in the ObjectFlags data member.
		/// </summary>
		public bool IsDataList
		{
			get
			{
				return ((ObjectFlags & (uint)ObjectAttributeFlags.IsDataList) != 0);
			}
			set
			{
				ObjectFlags = (value)
							? ObjectFlags | (uint)ObjectAttributeFlags.IsDataList
							: ObjectFlags & ~(uint)ObjectAttributeFlags.IsDataList;
			}
		}

		/// <summary>
		/// This property represents the IsEventList bit in the ObjectFlags data member.
		/// </summary>
		public bool IsEventList
		{
			get
			{
				return ((ObjectFlags & (uint)ObjectAttributeFlags.IsEventList) != 0);
			}
			set
			{
				ObjectFlags = (value)
							? ObjectFlags | (uint)ObjectAttributeFlags.IsEventList
							: ObjectFlags & ~(uint)ObjectAttributeFlags.IsEventList;
			}
		}
		#endregion // Properties

	}
}