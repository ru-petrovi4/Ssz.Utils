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
	public enum ContextOptions : uint
	{
		/// <summary>
		/// No options are set for the Context.  
		/// </summary>
		NoOptions		                     = 0x00000000,

		/// <summary>
		/// ErrorInfo objects with non-empty ErrorMessages are enabled 
		/// for the Context.
		/// </summary>
		EnableEnhancedErrorInfo              = 0x00000001,

		/// <summary>
		/// Enable tracing of Xi Server method invocations for 
		/// this context.  When enabled, all calls to Xi Contract
		/// methods are logged by the server.
		/// </summary>
		EnableMethodTrace	                 = 0x00000002,

		/// <summary>
		/// Enable tracing of internal method invocations. 
		/// Implementation is server dependent.  It is 
		/// intended to provide additional details to the
		/// method invocations logged by EnableMethodTrace.
		/// </summary>
		EnableDetailTrace	                 = 0x00000004,

		/// <summary>
		/// Enable the logging of error conditions that occur while
		/// the server is running.
		/// </summary>
		EnableErrorLogging                   = 0x00000008,

		/// <summary>
		/// Performance counters are enabled for the Context.
		/// </summary>
		EnablePerfCounters                   = 0x00000010,

		/// <summary>
		/// Allows a ReInitiate() request to be issued from a 
		/// different IpAddress than that used by the previously 
		/// connected WCF client connection.
		/// </summary>
		AllowDifferentClientIpAddress        = 0x00000020,

		/// <summary>
		/// <para>Requests the server to use context options selected or 
		/// configured for the server.  How these options are selected by or 
		/// configured is server-specific.</para>
		/// <para>These options may include any of the defined context options 
		/// whose values are less than this option and may also include additional 
		/// server-specific options.  Selection of this option causes all of the 
		/// included context options to be used, even if they were not individually
		/// selected.</para>
		/// </summary>
		UseServerOptions                     = 0x00080000,

		/// <summary>
		/// Selectively enable access to the server for data.
		/// If one of the EnableAccessTo bits is not set,
		/// then all types of access are enabled. 
		/// </summary>
		EnableDataAccess                     = 0x00100000,

		/// <summary>
		/// Connect to the server for Alarms and Events Access.
		/// If one of the EnableAccessTo bits is not set,
		/// then all types of access are enabled. 
		/// </summary>
		EnableAlarmsAndEventsAccess          = 0x00200000,

		/// <summary>
		/// Connect to the server for Historical Data Access.
		/// If one of the EnableAccessTo bits is not set,
		/// then all types of access are enabled. 
		/// </summary>
		EnableJournalDataAccess              = 0x00400000,

		/// <summary>
		/// Connect to the server for Historical Alarms and Events Access.
		/// If one of the EnableAccessTo bits is not set,
		/// then all types of access are enabled. 
		/// </summary>
		EnableJournalAlarmsAndEventsAccess   = 0x00800000,

	}
}
