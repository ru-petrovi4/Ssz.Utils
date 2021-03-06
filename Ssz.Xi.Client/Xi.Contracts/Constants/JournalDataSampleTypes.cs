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
	/// Standard sample types for historical data
	/// </summary>
	public class JournalDataSampleTypes
	{
		/// <summary>
		/// This is the Calculation LocalId for Raw Data Reads.
		/// </summary>
		public const uint RawDataSamples = 2000000001u;

		/// <summary>
		/// This is the Calculation LocalId for Specific Times.
		/// </summary>
		public const uint AtTimeDataSamples = 2000000002u;

		/// <summary>
		/// This is the Calculation LocalId for Changed Samples.
		/// </summary>
		public const uint ChangedDataSamples = 2000000003u;

		/// <summary>
		/// Values equal to or greater than this value for Calculation LocalId are undefined.
		/// They are reserved to indicated that the Calculation LocalId has not beeen set.
		/// value should be considered reserved.
		/// </summary>
		public const uint DataSampleTypeUndefined = 2200000000u;

	}
}
