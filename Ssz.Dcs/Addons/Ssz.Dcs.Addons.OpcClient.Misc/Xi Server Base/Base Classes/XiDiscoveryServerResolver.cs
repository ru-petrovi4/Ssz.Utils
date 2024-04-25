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
using System.Linq;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading;
using System.IO;
using Ssz.Utils;
using Xi.Contracts;
using Xi.Contracts.Data;
using Xi.Contracts.Constants;
using Xi.Common.Support;


namespace Xi.Server.Base
{
	public class UrlEntry
	{
		public UrlEntry(string url)
		{
			Url = url;
			ServerEntry = null;
		}
		public string Url;
		public ServerEntry ServerEntry;
	}

	/// <summary>
	/// This is the root class for Xi Servers.  It supports the Server
	/// Discovery interface allowing all servers derived from it to 
	/// be Discovery Servers.  It also includes some essential
	/// startup and stop functionality for the server.
	/// </summary>
	public partial class ServerRoot
		: IServerDiscovery
	{
		#region ResolverThread Support

		private static Thread _resolverThread;		

		/// <summary>
		///  This method adds a ServerEntry to the list of ServerEntries to be returned to clients 
		///  by the DiscoverServers() method
		/// </summary>
		/// <param name="urlEntry">The UrlEntry of the server</param>
		protected static void AddServerToServerEntries(UrlEntry urlEntry)
		{
			// add it if the list of ServerEntries is empty
			if (_ServerEntries.Count == 0)
				_ServerEntries.Add(urlEntry.ServerEntry);
			else// otherwise see if it's Server Entry has already been added to the list of ServerEntries under a different URL.
			{
				MexEndpointInfo foundMexEP = null;
				foreach (var mexEp in urlEntry.ServerEntry.MexEndpoints)
				{
					foreach (var curServerEntry in _ServerEntries)
					{
						foundMexEP = curServerEntry.MexEndpoints.Find(me => me.Url == mexEp.Url);
						if (foundMexEP != null)
						{
							break; // when found
						}
					}
				}
				// then add it back into the new list if it wasn't already in the list
				if (foundMexEP == null)
					_ServerEntries.Add(urlEntry.ServerEntry);
			}
		}

		/// <summary>
		/// This method combines two URL lists into a single distinct list
		/// </summary>
		/// <param name="list1">URL List 1</param>
		/// <param name="list2">URL List 2</param>
		/// <returns></returns>
		private static List<string> CombineUrlLists(List<string> list1, List<string> list2)
		{
			List<string> combinedUrls = null;
			if (((list1 != null) && (list1.Count > 0))
				|| ((list2 != null) && (list2.Count > 0))
			   )
			{
				combinedUrls = new List<string>();
				if ((list1 != null) && (list1.Count > 0))
					combinedUrls.AddRange(list1);
				if ((list2 != null) && (list2.Count > 0))
					combinedUrls.AddRange(list2);
				IEnumerable<string> distinctDeletedUrls = combinedUrls.Distinct();
				combinedUrls = distinctDeletedUrls.ToList();
			}
			return combinedUrls;
		}

		/// <summary>
		/// This method compares the input URLs with the previous URLs and returns those that have 
		/// been deleted (no longer in the current URLs) and those that are new.
		/// </summary>
		/// <param name="currentUrls">The current list of URLs</param>
		/// <param name="previousUrls">The previous list of URLs (the last loop of the Resolver Thread)</param>
		/// <param name="deletedUrls">The URLs that are in the previous list but not in the current list</param>
		/// <param name="newUrls">The URLs that are in the current list but not in the previous list</param>
		internal static void GetNewAndDeletedUrls(List<string> currentUrls, List<string> previousUrls,
												   out List<string> deletedUrls, out List<string> newUrls)
		{
			if (currentUrls != null)
			{
				IEnumerable<string> distinctUrls = currentUrls.Distinct();
				currentUrls = distinctUrls.ToList();
			}
			else
				currentUrls = new List<string>();

			// Find the Urls that have been deleted
			deletedUrls = new List<string>();
			if (previousUrls == null)
				previousUrls = new List<string>();
			else
			{
				foreach (var url in previousUrls)
				{
					string deletedUrl = currentUrls.Find(u => u == url);
					if (deletedUrl == null) // not in the list
						deletedUrls.Add(url);
				}
			}

			// Extract the new servers
			newUrls = new List<string>();
			foreach (var url in currentUrls)
			{
				string newUrl = previousUrls.Find(u => u == url);
				if (newUrl == null) // not in the list
					newUrls.Add(url);
			}
		}

		#endregion // ResolverThread Support

		#region PNRP Support

		/// <summary>
		/// Indicates if PNRP can be accessed. Set to false by the catch handler if
		/// the call to PNRP throws and exception
		/// </summary>
		private static bool _PnrpEnabled = true;		

		#endregion // PNRP Support
	}	
}
