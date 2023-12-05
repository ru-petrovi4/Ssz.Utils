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

namespace Xi.Contracts.Constants
{
	/// <summary>
	/// This enumeration specifies how a list is sorted.
	/// The sort keys are defined by the list attributes.
	/// </summary>
	public enum SortType : ushort
	{
		/// <summary>
		/// The list is not sorted.
		/// </summary>
		NotSorted = 0,

		/// <summary>
		/// The list is sorted in ascending order.
		/// </summary>
		Ascending = 1,

		/// <summary>
		/// The list is sorted in descending order.
		/// </summary>
		Descending = 2,
	}
}
