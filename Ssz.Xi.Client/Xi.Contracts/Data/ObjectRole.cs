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
	/// <para>This class is used by the server to define the roles 
	/// for objects that it supports. Roles can be assigned to 
	/// objects to allow them to be found using the FindObjects() 
	/// method.</para>  
	/// <para>Note that the role of an object may be relative 
	/// to another object or the role of an object may be independent 
	/// of its relationship with another object.  For example, a company 
	/// may be a customer (role) of one company, and a supplier (role) 
	/// of another.  Or the company may just have the role of 
	/// manufacturer.</para>
	/// <para>Two standard relationship independent roles are defined 
	/// for control systems, Area and EventSource.  Each is defined 
	/// using a constant.</para>
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class ObjectRole
	{
		#region Data Members

		/// <summary>
		/// TypeId of the Role. Standard RoleIds are defined by the 
		/// Xi.Contracts.Constants.ObjectRoleId class.
		/// </summary>
		[DataMember] public TypeId? RoleId;

		/// <summary>
		/// TypeId of the Role. 
		/// </summary>
		[DataMember] public string? Name;

		/// <summary>
		/// Description of the Role. 
		/// </summary>
		[DataMember] public string? Description;

		#endregion

		#region Constructors

		/// <summary>
		/// The default constructor.
		/// </summary>
		public ObjectRole(){}

		/// <summary>
		/// This constructor initializes an Object Role with the supplied namespace and 
		/// id of the role's InstanceId, and the name and description of the role.
		/// </summary>
		/// <param name="schemaType">
		/// The schemaType component of the role's TypeId.
		/// </param>
		/// <param name="roleNamespace">
		/// The namespace component of the role's TypeId.
		/// </param>
		/// <param name="roleId">
		/// The identifier for the role's TypeId.
		/// </param>
		/// <param name="name">
		/// The name of the role.
		/// </param>
		/// <param name="description">
		/// The text description of the role.
		/// </param>
		public ObjectRole(string schemaType, string roleNamespace, string roleId, string name, string description)
		{
			RoleId = new TypeId(schemaType, roleNamespace, roleId);
			Name = name;
			Description = description;
		}

		#endregion

		#region Methods
		//-------------------------------
		/// <summary>
		/// Determines if the specified list of roles contains the specified role.
		/// </summary>
		/// <param name="roles">The list of roles.</param>
		/// <param name="role">The specified role.  </param>
		/// <returns>TRUE, if the specified role is in the list.
		/// Otherwise, FALSE.</returns>
		public static bool IsRole(TypeId[] roles, TypeId role)
		{
			if (roles is not null)
			{
				foreach (TypeId rl in roles)
				{
					if (rl.Compare(role))
						return true;
				}
			}
			return false;
		}
		#endregion
	}
}