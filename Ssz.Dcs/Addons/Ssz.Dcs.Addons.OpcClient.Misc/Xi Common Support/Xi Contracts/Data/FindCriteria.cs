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
	/// This class is used to define the criteria used by the server to 
	/// find objects or types.
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class FindCriteria
	{
		/// <summary>
		/// <para>Identifies the position in the object/type hierarchy from which 
		/// this FindObjects or FindTypes request will begin. This path is specified 
		/// as an ordered list of object/type names that begins with either the 
		/// root or with a specific object/type.  Paths that begin with the root are 
		/// considered to be "Root Paths" and those that begin with an object/type are 
		/// considered to be "Relative Paths".</para>
		/// <para> For Root Paths, the first element of the path is an InstanceId string 
		/// whose LocalId is empty (e.g. DA:/").  For Relative Paths, the first element of 
		/// the path is set to the InstanceId string of the object used as the base of 
		/// the path. Note that Xi servers that wrap OPC HDA servers are not capable of 
		/// supporing relative paths since OPC HDA servers are not capable of changing 
		/// the browse position using an ItemId.</para>
		/// <para>Null or empty paths indicate that the find is to begin at the root.</para> 
		/// </summary>
		[DataMember] public ObjectPath StartingPath;

		/// <summary>
		/// <para>The FilterSet to be used to find objects. </para>
		/// <para>The default behavior for filtering is to look for both branches and 
		/// leaves, and therefore, the BranchOrLeaf filter operand is to select only branches 
		/// or only leaves.</para>  
		/// <para>The default behavior for filtering is to look only for objects that are 
		/// children of the StartingPath.  Therefore, two filter operands are defined to allow 
		/// the client to modify this behavior. The StartingObjectAttributes filter operand 
		/// allows the client to request that object attributes of the object 
		/// identified by the StartingPath also be selected and returned.  The 
		/// StartingObjectAttributesOnly filter operand, on the other hand, allows the client 
		/// to request that object attributes of only the object identified by the StartingPath 
		/// be selected and returned (the object attributes of the children are not returned).</para>  
		/// </summary>
		[DataMember] public FilterSet FilterSet;

	}
}