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
	/// This class contains the changed historical data values for a specific data object.
	/// <para>NOTE: The HistoricalValues parameter is null for this type!</para>
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class JournalDataChangedValues
	{
		/// <summary>
		/// Result Code for the corresponding server alias / client alias.
		/// </summary>
		[DataMember] public uint ResultCode { get; set; }

		/// <summary>
		/// The client-assigned alias for a data object.  
		/// </summary>
		[DataMember] public uint ClientAlias { get; set; }

		/// <summary>
		/// The calculation used to derive these Historical Values
		/// </summary>
		[DataMember] public TypeId? Calculation { get; set; }

		/// <summary>
		/// The servers start time for the response values.
		/// </summary>
		[DataMember] public DateTime StartTime { get; set; }

		/// <summary>
		/// The servers end time for the response values.
		/// </summary>
		[DataMember] public DateTime EndTime { get; set; }

		/// <summary>
		/// The overall Error Info for this Journal Data Value
		/// or null if there is no additional error information.
		/// </summary>
		[DataMember] public ErrorInfo? ErrorInfo { get; set; }

		/// <summary>
		/// The attributes that describe the changes to the HistoricalValues element of 
		/// the JournalDataReturnValues base class. 
		/// </summary>
		[DataMember] public ModificationAttributesList? ModificationAttributes { get; set; }

	}
}