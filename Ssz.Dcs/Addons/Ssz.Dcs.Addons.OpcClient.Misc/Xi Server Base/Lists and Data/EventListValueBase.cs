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

using Xi.Contracts.Data;
using Xi.Common.Support;

namespace Xi.Server.Base
{
	public abstract class EventListValueBase
		: ValueRoot
	{
		public EventListValueBase(uint clientAlias, uint serverAlias)
			: base(clientAlias, serverAlias)
		{
		}

		public EventMessage EventMessage
		{
			get { return _eventMessage; }
			protected set { _eventMessage = value; }
		}
		private EventMessage _eventMessage;

		private uint _statusCode;
		/// <summary>
		/// 
		/// </summary>
		public override uint StatusCode { get { return _statusCode; } set { _statusCode = value; } }

		/// <summary>
		/// 
		/// </summary>
		public override TransportDataType ValueTransportTypeKey
		{
			get { return TransportDataType.EventMessage; }
			protected set { }
		}
	}
}
