/**********************************************************************
 * Copyright © 2009, 2010, 2011, 2012 OPC Foundation, Inc. 
 *
 * The source code and all binaries built with the OPC .NET 3.0 source
 * code are subject to the terms of the Express Interface Public
 * License (Xi-PL).  See http://www.opcfoundation.org/License/Xi-PL/
 *
 * You must not remove this notice, or any other, from this software.
 *
 *********************************************************************/

using System;
using System.Runtime.Serialization;

namespace Xi.Contracts.Data
{
	/// <summary>
	/// The enabled state of an alarm area or source.
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class AlarmEnabledState
	{
		/// <summary>
		/// Result Code for the corresponding area or source.
		/// </summary>
		[DataMember] public uint ResultCode { get; set; }

		/// <summary>
		/// TRUE if the area/source is enabled, FALSE if it is disabled. 
		/// Note that the state of the area/source may be set independently of the OPC .NET server. 
		/// </summary>
		[DataMember] public bool Enabled { get; set; }

		/// <summary>
		/// TRUE if the area/source is enabled and all areas within the hierarchy of its containing areas are enabled. 
		/// FALSE if the area is disabled or any area within the hierarchy of its containing areas is disabled.
		/// </summary>
		[DataMember] public bool EffectiveyEnabled { get; set; }

	}
}