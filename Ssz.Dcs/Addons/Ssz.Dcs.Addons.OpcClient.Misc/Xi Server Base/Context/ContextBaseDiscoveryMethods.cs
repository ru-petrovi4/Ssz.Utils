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

using System.Collections.Generic;

using Xi.Contracts.Data;

namespace Xi.Server.Base
{
	/// <summary>
	/// This partial class defines the methods to be overridden by the server implementation 
	/// to support the Discovery Methods of the IResourceManagement interface.
	/// </summary>
	public abstract partial class ContextBase<TList>
		where TList : ListRoot
	{

		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// </summary>
		/// <returns>
		/// The status of the Xi server and the status of wrapped servers. 
		/// </returns>
		public abstract List<ServerStatus> OnStatus();

		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// </summary>
		/// <param name="resultCodes">
		/// The result codes for which text descriptions are being requested.
		/// </param>
		/// <returns>
		/// The list of result codes and if a result code indicates success, 
		/// the requested text descriptions. The size and order of this 
		/// list matches the size and order of the resultCodes parameter.
		/// </returns>
		public abstract List<RequestedString> OnLookupResultCodes(List<uint> resultCodes);

		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// </summary>
		/// <param name="findCriteria">
		/// The criteria used by the server to find objects.  If this 
		/// parameter is null, then this call is a continuation of the 
		/// previous find.
		/// </param>
		/// <param name="numberToReturn">
		/// The maximum number of objects  to return in a single response.
		/// </param>
		/// <returns>
		/// <para>The list of object attributes for the objects that met 
		/// the filter criteria. </para>  
		/// <para>Returns an empty list if the starting object is a leaf, or 
		/// no objects were found that meet the filter criteria, or if the call 
		/// was made with a null findCriteria and there are no more objects to 
		/// return.</para>
		/// <para>May also return null if there is nothing (left) to return.</para>
		/// </returns>
		public abstract List<ObjectAttributes> OnFindObjects(FindCriteria findCriteria, uint numberToReturn);

		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// </summary>
		/// <param name="findCriteria">
		/// The criteria used by the server to find types.  If this 
		/// parameter is null, then this call is a continuation of the 
		/// previous find.
		/// </param>
		/// <param name="numberToReturn">
		/// The maximum number of objects to return in a single response.
		/// </param>
		/// <returns>
		/// <para>The list of type attributes for the type that met 
		/// the filter criteria. </para>  
		/// <para>Returns null if the starting type is a leaf, or no types 
		/// were found that meet the filter criteria, or if the call was made 
		/// with a null findCriteria and there are no more types to return.</para>
		/// </returns>
		public abstract List<TypeAttributes> OnFindTypes(FindCriteria findCriteria, uint numberToReturn);

		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// </summary>
		/// <param name="objectPath">
		/// The root path that identifies the object for which alternate 
		/// root paths are being requested. 
		/// </param>
		/// <returns>
		/// The list of additional root paths to the specified object.  
		/// Null if specified objectPath is the only root path to the 
		/// object. An exception is thrown if the specified objectPath is 
		/// invalid.  
		/// </returns>
		public abstract List<ObjectPath> OnFindRootPaths(ObjectPath objectPath);
	}
}
