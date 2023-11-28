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

using Xi.Common.Support;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;
using Xi.Server.Base;

namespace Xi.OPC.Wrapper.Impl
{
	public class EventListJournal : EventJournalListBase
	{
		internal EventListJournal(ContextImpl context, uint clientId, uint updateRate, uint bufferingRate,
									uint listType, uint listKey, FilterSet filterSet)
			: base(context, clientId, updateRate, bufferingRate, listType, listKey)
		{
			throw FaultHelpers.Create(XiFaultCodes.E_NOTIMPL, "EventListJournal...");
		}

		protected override uint OnNegotiateBufferingRate(uint requestedBufferingRate)
		{
			uint negotiatedBufferingRate = 0;
			// TODO:  Negotiate Buffering Rate if Buffering Rate is supported.
			//        Also add code to implement buffering rate.
			return negotiatedBufferingRate;
		}

		protected override bool Dispose(bool isDisposing)
		{
			throw FaultHelpers.Create(XiFaultCodes.E_NOTIMPL, "EventListJournal...");
		}

	}
}
