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
	/// This class defines standard constant values for filter operands.
	/// All values are case independent and should be up-shifted or down-shifted by 
	/// the server when used in comparisons.. They are defined here in camel case 
	/// for read-ability in displays.
	/// </summary>
	public class FilterOperandValues
	{
		/// <summary>
		/// A valid value for FilterOperand.BranchOrLeaf.  
		/// This value is used to select objects that are branches.
		/// </summary>
		public const string Branch = "Branch";

		/// <summary>
		/// A valid value for FilterOperand.BranchOrLeaf.
		/// This value is used to select objects that are leaves.
		/// </summary>
		public const string Leaf = "Leaf";

		/// <summary>
		/// A valid value for FilterOperand.AccessRight.
		/// This value is used to select readable objects.
		/// </summary>
		public const string Read = "Read";

		/// <summary>
		/// A valid FilterOperand.AccessRight value.
		/// This value is used to select writable objects.
		/// </summary>
		public const string Write = "Write";

		/// <summary>
		/// A valid FilterOperand.ExceptionDeviationType value.
		/// This value is used to select values whose change is calcuated 
		/// using absolute value.
		/// </summary>
		public const string AbsoluteValue = "AbsoluteValue";

		/// <summary>
		/// A valid FilterOperand.ExceptionDeviationType value.
		/// This value is used to select values whose change is calcuated 
		/// using percent of span.
		/// </summary>
		public const string PercentOfSpan = "PercentOfSpan";

		/// <summary>
		/// A valid FilterOperand.ExceptionDeviationType value.
		/// This value is used to select values whose change is calcuated 
		/// using percent of value.
		/// </summary>
		public const string PercentOfValue = "PercentOfValue";

	}
}
