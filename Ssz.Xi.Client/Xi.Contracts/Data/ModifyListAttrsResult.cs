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
	/// This class contains list attributes that can be modified. 
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class ModifyListAttrsResult
	{
		/// <summary>
		/// The updated Update Rate.  Null if UpdateRate was not modified.
		/// </summary>
		[DataMember] public Nullable<uint> RevisedUpdateRate { get; set; }

		/// <summary>
		/// The updated Buffering Rate.  Null if BufferingRate was not modified.
		/// </summary>
		[DataMember] public Nullable<uint> RevisedBufferingeRate { get; set; }

		/// <summary>
		/// The updated FilterSet.  Null if FilterSet was not modified.
		/// </summary>
		[DataMember] public FilterSet? RevisedFilterSet { get; set; }

	}
}