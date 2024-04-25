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
using System.IO;
using Microsoft.Extensions.Logging;
using Ssz.Utils;
using Xi.Common.Support;
using Xi.Contracts;
using Xi.Contracts.Data;


namespace Xi.Server.Base
{
	/// <summary>
	/// This partial class implements the IServerDiscovery interface
	/// </summary>
	public partial class ServerRoot:
		IServerDiscovery
	{
		/// <summary>
		/// The list is used by Directory Servers to record the Xi Resource Management Endpoints 
		/// that it returns in the DiscoverServers() method.  It is null if the server is not a 
		/// a Directory Server.
		/// </summary>
		protected static List<ServerEntry> _ServerEntries = null;

		/// <summary>
		/// The ServerEntry for this server.  It records the server description and the list of 
		/// Resource Management Endpoints supported by the server.
		/// </summary>
		protected static ServerEntry _ThisServerEntry;

		/// <summary>
		/// The publicly accessible property for ThisServerEntry. 
		/// </summary>
		public static ServerEntry ThisServerEntry
		{
			get { return _ThisServerEntry; }
			private set { }
		}		

		/// <summary>
		/// The publicly accessible property for the ServerDescription. 
		/// </summary>
		public static ServerDescription ServerDescription
		{
			get { return (null == _ThisServerEntry) ? null : _ThisServerEntry.ServerDescription; }
			private set { }
		}

		/// <summary>
		/// This method returns the list of servers the client is 
		/// authorized to discover.
		/// </summary>
		/// <returns>
		/// List of server entries.
		/// </returns>
		List<ServerEntry> IServerDiscovery.DiscoverServers()
		{
			string serverCount = null;
			if ((_ServerEntries != null) && (_ServerEntries.Count > -1))
				serverCount = "Server Count = " + _ServerEntries.Count.ToString();
            //using (StaticLogger.Logger.EnterMethod(serverCount))
			{
				try
				{
					return OnDiscoverServers();
				}				
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}

		/// <summary>
		/// This is called for the IServerDiscovery.DiscoverServers meethod to return the
		/// ServerEntry list.
		/// </summary>
		/// <returns>ServerEntry list identifying endpoints and other servers</returns>
		protected virtual List<ServerEntry> OnDiscoverServers()
		{
			if (null == _ServerEntriesLock)
				throw FaultHelpers.Create("This server does not support IServerDiscovery.DiscoverServers!");
			List<ServerEntry> servers = new List<ServerEntry>();
			try
			{
				_ServerEntriesLock.WaitOne();
				servers.AddRange(_ServerEntries);
			}
			finally
			{
				_ServerEntriesLock.ReleaseMutex();
			}
			return servers;
		}

		/// <summary>
		/// <para>This method is used to get the description of the 
		/// server.  </para>
		/// </summary>
		/// <returns>
		/// The description of the server. 
		/// </returns>
		ServerEntry IServerDiscovery.DiscoverServerInfo()
		{
            //using (StaticLogger.Logger.EnterMethod())
			{
				if (null == _ThisServerEntry
					|| null == _ThisServerEntry.ServerDescription
					|| string.IsNullOrEmpty(_ThisServerEntry.ServerDescription.ServerDiscoveryUrl)
					|| (ServerState & ServerState.Initializing) != 0
					|| (ServerState & ServerState.Faulted) != 0
					|| (ServerState & ServerState.Aborting) != 0
				   )
				{
					StaticLogger.Logger.LogInformation("DiscoverServerInfo() failed, ServerState=" + ServerState.ToString("G"));
					throw FaultHelpers.Create("Server is initializing");
				}

				try
				{
					// otherwise return the server entry
					ServerEntry serverEntry                                = new ServerEntry();
					serverEntry.ServerDescription                          = new ServerDescription();
					serverEntry.ServerDescription.ServerDiscoveryUrl       = _ThisServerEntry.ServerDescription.ServerDiscoveryUrl;
					serverEntry.ServerDescription.XiContractsVersionNumber = _ThisServerEntry.ServerDescription.XiContractsVersionNumber;
					serverEntry.ServerDescription.HostName                 = _ThisServerEntry.ServerDescription.HostName;
					serverEntry.ServerDescription.ServiceName              = _ThisServerEntry.ServerDescription.ServiceName;
					serverEntry.ServerDescription.SecurityTokenServiceUrl  = _ThisServerEntry.ServerDescription.SecurityTokenServiceUrl;
					serverEntry.ServerDescription.ServerName               = _ThisServerEntry.ServerDescription.ServerName;
					serverEntry.ServerDescription.ServerTypes              = _ThisServerEntry.ServerDescription.ServerTypes;
					serverEntry.ServerDescription.SupportedLocaleIds       = _ThisServerEntry.ServerDescription.SupportedLocaleIds;
					serverEntry.ServerDescription.VendorName               = _ThisServerEntry.ServerDescription.VendorName;
					serverEntry.ServerDescription.UserInfo                 = _ThisServerEntry.ServerDescription.UserInfo;
					serverEntry.MexEndpoints                               = _ThisServerEntry.MexEndpoints;
					serverEntry.EndpointServerSettings                     = _ThisServerEntry.EndpointServerSettings;
					serverEntry.ServerDescription.ServerDetails            = null; // get this through the Identify method - DiscoverServerInfo can be called without a context
					return serverEntry;
				}				
				catch (Exception ex)
				{
					throw FaultHelpers.Create(ex);
				}
			}
		}		
	}
}
