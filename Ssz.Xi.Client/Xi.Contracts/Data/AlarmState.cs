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
	/// <para>The state of the Alarm.</para> 
	/// <para>The alarm states accessible through this interface 
	/// are defined in EEMUA Publication 191 "Alarm Systems: 
	/// A Guide to Design, Management and Procurement".
	/// See http://www.eemua.org</para>
	/// </summary>
	[Flags]
	[DataContract(Namespace = "urn:xi/data")]
	public enum AlarmState
	{
		/// <summary>
		/// The default value of 0 is the starting state, 
		/// which is inactive (cleared), acknowledged, enabled, 
		/// and unsuppressed.
		/// </summary>
		[EnumMember] Initial    = 0x00,

		/// <summary>
		/// The generation/detection of the alarm is disabled 
		/// even though the base condition may be active.
		/// </summary>
		[EnumMember] Disabled   = 0x01,

		/// <summary>
		/// The Alarm has been detected and its condition 
		/// continues to persist. This state is also referred 
		/// to as "raised" or "standing".  The inactive state 
		/// is indicated by not setting the Alarm to active.
		/// </summary>
		[EnumMember] Active     = 0x02,

		/// <summary>
		/// The Alarm has not been acknowledged.
		/// </summary>
		[EnumMember] Unacked    = 0x04,

		/// <summary>
		/// Automatic generation/detection of the alarm is 
		/// disabled, even though the base condition may be active.  
		/// </summary>
		[EnumMember] Suppressed = 0x08,

	}
}