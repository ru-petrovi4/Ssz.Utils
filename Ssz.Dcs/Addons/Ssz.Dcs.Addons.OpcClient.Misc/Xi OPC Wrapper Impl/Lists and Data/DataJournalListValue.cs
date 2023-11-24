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
using Xi.Server.Base;

namespace Xi.OPC.Wrapper.Impl
{
	/// <summary>
	/// The DataJournalListValue class is used to hold the actual OPC HDA Data Value.
	/// </summary>
	public class DataJournalListValue
		: DataJournalListValueBase
	{
		public DataJournalListValue(uint clientAlias, uint serverAlias)
			: base(clientAlias, serverAlias)
		{
			_dataValueKey = TransportDataType.Unknown;
		}

		// hServer is the OPC HDA XiOPCWrapper handle for this data value.  It is generated
		// by the HDA XiOPCWrapper and is never passed back to the Xi Client.
		private uint _svrHdl;
		public uint hServer { get { return _svrHdl; } set { _svrHdl = value; } }

		// hClient is the OPC HDA Client handle for this data value
		// ServerAlias is in referece to the Xi server.
		public uint hClient { get { return _serverAlias; } set { _serverAlias = value; } }

		private string _itemId;
		/// <summary>
		/// The OPC HDA Item Id for this data value.
		/// </summary>
		public string ItemId { get { return _itemId; } set { _itemId = value; } }

		private uint _statusCode;
		/// <summary>
		/// 
		/// </summary>
		public override uint StatusCode { get { return _statusCode; } set { _statusCode = value; } }

		private TransportDataType _dataValueKey;
		/// <summary>
		/// The Data Value Type property is used to determine this instance value type.
		/// </summary>
		public override TransportDataType ValueTransportTypeKey
		{
			get { return _dataValueKey; }
			protected set { _dataValueKey = value; }
		}
	}
}