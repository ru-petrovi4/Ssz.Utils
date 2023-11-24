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

namespace Xi.Common.Support
{
	/// <summary>
	/// Possible base or offset types for relative times.
	/// </summary>
	public enum RelativeTime
	{
		/// <summary>
		/// Start from the current time.
		/// </summary>
		Now,

		/// <summary>
		/// The start of the current second or an offset in seconds.
		/// </summary>
		Second,

		/// <summary>
		/// The start of the current minutes or an offset in minutes.
		/// </summary>
		Minute,

		/// <summary>
		/// The start of the current hour or an offset in hours.
		/// </summary>
		Hour,

		/// <summary>
		/// The start of the current day or an offset in days.
		/// </summary>
		Day,

		/// <summary>
		/// The start of the current week or an offset in weeks.
		/// </summary>
		Week,

		/// <summary>
		/// The start of the current month or an offset in months.
		/// </summary>
		Month,

		/// <summary>
		/// The start of the current year or an offset in years.
		/// </summary>
		Year
	}
}
