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
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xi.Contracts.Data;

namespace Xi.Contracts.Constants
{
	/// <summary>
	/// Object Roles are identified using TypeIds. This allows different 
	/// organizations to define ObjectRoles.  This class defines standard 
	/// ObjectRoles.
	/// </summary>
	public class ObjectRoleIds
	{
		/// <summary>
		/// <para>The TypeId for the object that represents the 
		/// plant area root. Plant area roots may themselves be 
		/// plant areas.</para> 
		/// <para>Each system is allowed to have only one plant area 
		/// root to allow clients to easily discover the areas of a 
		/// system.  However, plants may have multiple systems, each 
		/// with its own plant area root.  It is required that the  
		/// area root for a system is located directly below the 
		/// "Root" of the system.  </para>
		/// <para>Plant areas can always be found directly below the 
		/// area root object or directly under another plant area. 
		/// Therefore, the path of plant areas always contain the name 
		/// of the area root followed by one or more area names.</para>
		/// </summary>
		public static TypeId AreaRootRoleId = new TypeId(XiSchemaType.Xi, XiNamespace.Xi, "AreaRoot");

		/// <summary>
		/// The TypeId for objects that represent plant areas.  The 
		/// description of AreaRoot describes the organization of plant 
		/// areas beneath the Area Root.
		/// </summary>
		public static TypeId AreaRoleId = new TypeId(XiSchemaType.Xi, XiNamespace.Xi, "Area");

		/// <summary>
		/// The TypeId for objects that represent event sources.  Event  
		/// soruces can always be found directly below a plant area.  It  
		/// is also possible that they can be found below other objects 
		/// in the system, but there must be at least one path to them in 
		/// which they are a direct child of an area.
		/// </summary>
		public static TypeId EventSourceRoleId = new TypeId(XiSchemaType.Xi, XiNamespace.Xi, "EventSource");

		/// <summary>
		/// The TypeId for objects that represent OPC DA Server Branches.
		/// </summary>
		public static TypeId OpcBranchRoleId = new TypeId(XiSchemaType.Xi, XiNamespace.Xi, "OpcBranch");

		/// <summary>
		/// The TypeId for objects that represent OPC DA Server Leaves.
		/// </summary>
		public static TypeId OpcLeafRoleId = new TypeId(XiSchemaType.Xi, XiNamespace.Xi, "OpcLeaf");

		/// <summary>
		/// The TypeId for objects that represent Opc Server Properties.
		/// </summary>
		public static TypeId OpcPropertyRoleId = new TypeId(XiSchemaType.Xi, XiNamespace.Xi, "OpcProperty");

		/// <summary>
		/// The TypeId for objects that represent OPC HDA Server Branches.
		/// </summary>
		public static TypeId HdaBranchRoleId = new TypeId(XiSchemaType.Xi, XiNamespace.Xi, "HdaBranch");

		/// <summary>
		/// The TypeId for objects that represent OPC HDA Server Leaves.
		/// </summary>
		public static TypeId HdaLeafRoleId = new TypeId(XiSchemaType.Xi, XiNamespace.Xi, "HdaLeaf");

	}
}
