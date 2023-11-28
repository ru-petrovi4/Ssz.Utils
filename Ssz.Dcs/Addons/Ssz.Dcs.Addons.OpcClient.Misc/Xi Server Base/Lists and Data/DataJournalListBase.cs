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

using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Xi.Server.Base
{
	/// <summary>
	/// The Data Journal List is used to represent a collection of historical 
	/// process data values.  Each value contained by the Data Journal List
	/// contains a collection of data values for a specified time interval.  
	/// There are several options as to the exact nature of this collection 
	/// of data values, the data value collection may be raw values or values 
	/// process (calculated) according to the servers capabilities.
	/// </summary>
	public abstract class DataJournalListBase
		: DataListRoot
	{
		/// <summary>
		/// This constructor is simply a pass through place holder.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="clientId"></param>
		/// <param name="updateRate"></param>
		/// <param name="listType"></param>
		/// <param name="listKey"></param>
		public DataJournalListBase(ContextBase<ListRoot> context,
			uint clientId, uint updateRate, uint bufferingRate, uint listType, uint listKey)
			: base(context, clientId, updateRate, bufferingRate, listType, listKey)
		{
		}

		public override uint OnTouchList()
		{
			return XiFaultCodes.S_OK;
		}

	}
}
