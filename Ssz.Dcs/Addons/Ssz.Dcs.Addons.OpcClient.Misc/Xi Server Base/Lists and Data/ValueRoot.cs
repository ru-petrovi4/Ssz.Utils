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

using Xi.Contracts.Data;
using Xi.Contracts.Constants;
using Xi.Common.Support;

namespace Xi.Server.Base
{
	/// <summary>
	/// This is the Root Class for all types of List Entries
	/// </summary>
	public abstract class ValueRoot
		: IDisposable
	{
		/// <summary>
		/// Default constructor 
		/// </summary>
		public ValueRoot()
		{
			_listElementOptions = ListElementOptions.Default;
		}

		/// <summary>
		/// Constructor that requires the clientAlias and serverAlias to be specified.
		/// </summary>
		/// <param name="clientAlias"></param>
		/// <param name="serverAlias"></param>
		public ValueRoot(uint clientAlias, uint serverAlias)
		{
			_clientAlias = clientAlias;
			_serverAlias = serverAlias;
			_listElementOptions = ListElementOptions.Default;
		}

		/// <summary>
		/// ClientAlias is in reference to the Xi client.
		/// </summary>
		protected uint _clientAlias;

		/// <summary>
		/// Property used to get or set the client alias for this data list entry.
		/// </summary>
		public uint ClientAlias { get { return _clientAlias; } set { _clientAlias = value; } }

		/// <summary>
		/// ServerAlias is in referece to the Xi server.
		/// </summary>
		protected uint _serverAlias;

		/// <summary>
		/// Property used to get or set the server alias for this data list entry.
		/// DO NOT change the server alias unless the dictionary held by the owning 
		/// list is also updated with the new value!
		/// </summary>
		public uint ServerAlias { get { return _serverAlias; } set { _serverAlias = value; } }

		/// <summary>
		/// Keep a copy of the InstanceId for local use
		/// </summary>
		protected InstanceId _instanceId;

		/// <summary>
		/// The full object identification for this data entry.
		/// </summary>
		public InstanceId InstanceId { get { return _instanceId; } set { _instanceId = value; } }

		/// <summary>
		/// This flag being true indicates that this value has changed since the 
		/// last poll request or data change callback was issued.
		/// </summary>
		protected bool _entryChanged;

		/// <summary>
		/// Property used to set or clear the entry queued flag.
		/// <para>CAUTION! Setting this property to true has the side 
		/// effect of adding this data list entry to the queue 
		/// of changed Entry Root maintained by List Root.</para>
		/// </summary>
		public bool EntryQueued
		{
			get { return _entryChanged; }
			set { _entryChanged = value; }
		}

		/// <summary>
		/// The Xi Status for this data value
		/// </summary>
		public virtual uint StatusCode { get; set; }

		/// <summary>
		/// This property is provides the data type used to transported the value.
		/// </summary>
		public virtual TransportDataType ValueTransportTypeKey { get; protected set; }

		/// <summary>
		/// This flag represents additional instructions on the data object 
		/// </summary>
		protected ListElementOptions _listElementOptions;

		/// <summary>
		/// Property used to set or clear the list element options flag.
		/// </summary>
		public ListElementOptions ListElementOptions
		{
			get { return _listElementOptions; }
			set { _listElementOptions = value; }
		}
		~ValueRoot()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			if (Dispose(true))
				GC.SuppressFinalize(this);
		}

		protected virtual bool Dispose(bool isDisposing)
		{
			if (_hasBeenDisposed)
				return false;

			_hasBeenDisposed = true;
			return true;
		}
		protected bool _hasBeenDisposed = false;
	}

	/// <summary>
	/// This class defines ValueRoot objects that are used as markers (delimiters) in 
	/// the list queue that is used for polling.
	/// </summary> 
	public class QueueMarker
		: ValueRoot
	{
		public QueueMarker()
		{
		}
	}

}
