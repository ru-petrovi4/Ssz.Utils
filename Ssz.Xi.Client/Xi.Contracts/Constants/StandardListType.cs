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

namespace Xi.Contracts.Constants
{
	/// <summary>
	/// This enumeration specifies the standard types of lists.
	/// The enumerated values between 0 and 4095 inclusive are reserved 
	/// for standard types.
	/// </summary>
	public enum StandardListType
	{
		/// <summary>
		/// The type of list that contains data objects.
		/// </summary>
		DataList         = 1,

		/// <summary>
		/// The type of list that contains historical data objects.
		/// </summary>
		DataJournalList  = 2,

		/// <summary>
		/// The type of list that contains alarms and events.
		/// </summary>
		EventList        = 3,

		/// <summary>
		/// The type of list that contains historical alarms and events.
		/// </summary>
		EventJournalList = 4,
	}
}
