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
using System.Configuration;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Threading;
using System.Net.PeerToPeer;
using System.Reflection;
using Ssz.Utils;
using Xi.Contracts;
using Xi.Contracts.Data;
using Xi.Contracts.Constants;
using Xi.Common.Support;
using Xi.Common.Support.Extensions;
using Ssz.Utils.Net4;

namespace Xi.Server.Base
{
	/// <summary>
	/// This is the root class for Xi Servers.  It supports the Server
	/// Discovery interface allowing all servers derived from it to 
	/// be Discovery Servers.  It also includes some essential
	/// startup and stop functionality for the server.
	/// </summary>
	public partial class ServerRoot
		: IServerDiscovery
	{
		/// <summary>
		/// This property is used to obtain the number of server types 
		/// supported by this server
		/// </summary>
		public static uint NumServerTypes
		{
			get { return _NumServerTypes; }
		}

		/// <summary>
		/// The number of server types supported by this server. 
		/// Implementation subclasses of this base class must update this value.
		/// </summary>
		protected static uint _NumServerTypes = 0;

		/// <summary>
		/// protect list updates
		/// </summary>
		private static Mutex _ServerEntriesLock;		

		/// <summary>
		/// The implementation subclass of this base class must set this value!
		/// If this server is a wrapper, then the server should set the states 
		/// that override the states of the underlying server. For example, when 
		/// the server first comes up, the initializing state should be set, and 
		/// when shutting down, the aborting state should be set.
		/// </summary>
		protected static ServerState _ServerState = 0;

		/// <summary>
		/// This property is used to obtain the state of this Xi Server
		/// </summary>
		public static ServerState ServerState
		{
			get { return _ServerState; }
			protected set { _ServerState = value; }
		}

		static ServerRoot()
		{
		}		

		/// <summary>
		/// This is used to create the service host object
		/// </summary>
		/// <param name="serviceType"></param>
		public static void Initialize()
		{		    
			_ThisServerEntry = new ServerEntry();
			_ThisServerEntry.ServerDescription = new ServerDescription();
			_ThisServerEntry.ServerDescription.HostName = System.Environment.MachineName;			
        }		
	}
}
