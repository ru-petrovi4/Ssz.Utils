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
	/// <para>Context Options provides a set of flags that may be set as part 
	/// of the IResourceManagement.Initiate() method to allow for debug/tracing 
	/// and other options to be set for the client's context with the server.
	/// The implementation of ContextOptions is vendor-specific.</para>
	/// <para>Values below 0xFFFFFF (the low order 24-bits) are reserved. 
	/// Vendors may use the high order 8 bits.</para>
	/// </summary>
	[Flags]
	public enum AccessibleServerTypes : uint
	{		
		None		                     = 0x00000000,
		
		DataAccess                     = 0x00100000,
		
		AlarmsAndEventsAccess          = 0x00200000,
		
		JournalDataAccess              = 0x00400000,
		
		JournalAlarmsAndEventsAccess   = 0x00800000,
	}
}
