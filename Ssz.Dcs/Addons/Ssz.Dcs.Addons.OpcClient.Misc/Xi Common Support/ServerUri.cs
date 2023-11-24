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

namespace Xi.Common.Support
{
	public class ServerUri
	{
		public static void ReconcileServerEntryWithServerDiscoveryUrl
			(ServerEntry serverEntry, string serverDiscoveryUrl)
		{
			UriBuilder ubServerUrl = new UriBuilder(serverDiscoveryUrl);
			serverEntry.ServerDescription.ServerDiscoveryUrl = serverDiscoveryUrl;
			bool found = false;
			foreach (var mexEp in serverEntry.MexEndpoints)
			{
				UriBuilder ubMex = new UriBuilder(mexEp.Url);
				if (ubServerUrl.Host == ubMex.Host)
				{
					found = true;
					break;
				}
			}
			if (!found) // if the server host was not found, insert an entry for it
			{
				UriBuilder ubNew = new UriBuilder(serverEntry.MexEndpoints[0].Url);
				ubNew.Host = ubServerUrl.Host;
				MexEndpointInfo mi = new MexEndpointInfo()
				{
					Description = "Auto Created by Discovery Server to use Discovery Server Host Name or IP Address)",
					EndpointName = "Mex Endpoint for " + ubServerUrl.Host,
					Url = ubNew.Uri.AbsoluteUri
				};
				serverEntry.MexEndpoints.Insert(0, mi);
			}
		}
	}
}
