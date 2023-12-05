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

		/// <summary>
		/// This starts the XiServer class - one of the Create methods must be called
		/// prior to this.
		/// </summary>
		/// <param name="serverMesh">
		/// This is server mesh name as defined in EndpointDefinition
		/// XiDiscoveryServerMesh or XiServerMesh
		/// </param>
		/// <returns></returns>
		public static bool Start(string serverMesh, bool isIIShosted)
		{
			Logger.Verbose("Server is starting.");
			try
			{
				//// set the base elements of the server description to support 
				//// Identify() method calls called without a context.
				//NameValueCollection appSettings = ConfigurationManager.AppSettings;

				//if (0 == string.Compare(PnrpMeshNames.XiDiscoveryServerMesh, serverMesh, true))
				//{
				//    _ThisServerEntry.ServerDescription.ServerTypes = ServerType.Xi_ServerDiscoveryServer;
				//    _NumServerTypes++;
				//}

				//string vendorName = appSettings["Vendor"];
				//if (null != vendorName && 0 < vendorName.Length)
				//{
				//    _ThisServerEntry.ServerDescription.VendorName = vendorName;
				//    //Set the VendorNamespace to the VendorName
				//    _ThisServerEntry.ServerDescription.VendorNamespace = vendorName;
				//}

				//string userInfo = appSettings["UserInfo"];
				//if (null != userInfo && 0 < vendorName.Length)
				//{
				//    _ThisServerEntry.ServerDescription.UserInfo = userInfo;
				//}

				//string serverName = appSettings["Server"];
				//if (null != serverName && 0 < serverName.Length)
				//{
				//    _ThisServerEntry.ServerDescription.ServerName = serverName;
				//}

				//_ThisServerEntry.ServerDescription.ServiceName = "XiServices";
				//string serviceName = appSettings["Service"];
				//if (null != serviceName && 0 < serviceName.Length)
				//{
				//    _ThisServerEntry.ServerDescription.ServiceName = serviceName;
				//}

				//string systemName = appSettings["System"];
				//if (null != systemName && 0 < systemName.Length)
				//{
				//    _ThisServerEntry.ServerDescription.SystemName = systemName;
				//}

				// copy a selection of endpoint setting into the server info
				//_ThisServerEntry.EndpointServerSettings = CopyEndpointSettings(ServerRoot.ServiceHost.Description);							
			}

			catch (Exception ex)
			{
				Logger.Error(ex);
			}

			return true;
		}
	}
}
