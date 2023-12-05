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

namespace Xi.Contracts.Constants
{
	/// <summary>
	/// This enumeration defines the valid values for the StartingObjectAttributes 
	/// filter operand.  All values for this operand are passed in FilterCriterion as 
	/// integers.
	/// </summary>
	public enum StartingObjectFilterValues
	{
		/// <summary>
		/// A valid FilterOperand.StartingObjectAttributes value.
		/// This value is used to specify that the server is to return 
		/// ObjectAttributes only for the object identified by the starting 
		/// path.
		/// </summary>
		StartingObjectOnly = 1,

		/// <summary>
		/// A valid FilterOperand.StartingObjectAttributes value.
		/// This value is used to specify that the server is to return 
		/// ObjectAttributes for the object identified by the starting 
		/// path AND for the objects found below it.
		/// </summary>
		AllObjects         = 2,
	}
}
