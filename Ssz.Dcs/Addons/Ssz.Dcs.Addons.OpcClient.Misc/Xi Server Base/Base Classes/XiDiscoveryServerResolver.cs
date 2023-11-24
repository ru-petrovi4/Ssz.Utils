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
using Ssz.Utils.Net4;

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
		private static DiscoveryServerManConfigFile configFile = new DiscoveryServerManConfigFile();

		/// <summary>
		/// This method runs cyclically in its own thread to detect servers via PNRP.  
		/// It is used only by Server Discovery Servers.
		/// </summary>
		protected static void ResolverThread()
		{
			_ServerEntries = new List<ServerEntry>();

			List<string> unresolvedUrls = new List<string>();
			List<string> previousPnrpUrls = null;
			bool manConfigFilePresent = ((null != configFile.FilePath)
										 && (null != configFile.FileName));
			if (manConfigFilePresent)
			{
				// see if PNRP is working
				_PnrpEnabled = true;
				try { PNRPHelper.ResolveServices(PnrpMeshNames.XiServerMesh); }
				catch { _PnrpEnabled = false; }
				// clear the PNRP URLs if PNRP is enabled. If PNRP is not enabled, 
				// use the URLs from the last time PNRP was enabled
				if (_PnrpEnabled)
				{
					configFile.RewriteManualConfigFile(null, null);
					configFile.Close();
				}
			}

			try
			{
				while (true)
				{
					List<string> filePnrpUrls = null;
					List<string> deletedPnrpUrls = null;
					List<string> deletedConfiguredUrls = null;
					List<string> newPnrpUrls = null;
					List<string> newConfiguredUrls = null;

					List<string> pnrpUrls = GetUrlsFromPNRP();
					if (manConfigFilePresent)
						configFile.ProcessManualConfigFile(pnrpUrls, out newPnrpUrls, out deletedPnrpUrls, out filePnrpUrls, out newConfiguredUrls, out deletedConfiguredUrls);
					else
					{
						GetNewAndDeletedUrls(pnrpUrls, previousPnrpUrls, out deletedPnrpUrls, out newPnrpUrls);
						previousPnrpUrls = pnrpUrls;
					}

					List<string> deletedUrls = CombineUrlLists(deletedPnrpUrls, deletedConfiguredUrls);
					if ((deletedUrls != null) && (deletedUrls.Count > 0))
					{
						foreach (var url in deletedUrls)
							unresolvedUrls.Remove(url);
					}

					List<string> newUrls = CombineUrlLists(newPnrpUrls, newConfiguredUrls);
					if ((newUrls != null) && (newUrls.Count > 0))
					{
						unresolvedUrls = CombineUrlLists(unresolvedUrls, newUrls);
					}

					RebuildServerEntries(deletedUrls, unresolvedUrls);

					Thread.Sleep(10000); // check again every 10 seconds
				}
			}
			catch
			{
				configFile.Dispose();
			}
		}

		/// <summary>
		/// This method deletes ServerEntries and/or adds ServerEntries from/to _ServerEntries.
		/// </summary>
		/// <param name="urlsToDelete">Identifies the ServerEntries to be deleted.</param>
		/// <param name="urlsToAdd">Identifies the ServerEntries to be added.</param>
		protected static void RebuildServerEntries(List<string> urlsToDelete, List<string> urlsToAdd)
		{
			if (   ((urlsToAdd != null) && (urlsToAdd.Count > 0))
				|| ((urlsToDelete != null) && (urlsToDelete.Count > 0))
			   )
			{
				_ServerEntriesLock.WaitOne();   // protect list updates 

				// first delete
				List<ServerEntry> serverEntriesToBeDeleted = new List<ServerEntry>();
				if ((urlsToDelete != null) && (urlsToDelete.Count > 0))
				{
					foreach (var se in _ServerEntries)
					{
						// add only the server entries that are not in the urlsToDelete list 
						string deletedUrl = urlsToDelete.Find(url => string.Compare(url, se.ServerDescription.ServerDiscoveryUrl) == 0);
						if (string.IsNullOrEmpty(deletedUrl) == false)
						{
							serverEntriesToBeDeleted.Add(se);
						}
					}
					foreach (var se in serverEntriesToBeDeleted)
					{
						_ServerEntries.Remove(se);
					}
				}

				if ((urlsToAdd != null) && (urlsToAdd.Count > 0))
				{
					List<int> urlsToRemove = new List<int>();
					for (int i = 0; i < urlsToAdd.Count; i++)
					{
						bool found = false;
						foreach (var se in _ServerEntries)
						{
							if (string.Compare(urlsToAdd[i], se.ServerDescription.ServerDiscoveryUrl) == 0)
							{
								found = true;
								urlsToRemove.Add(i); // to be removed from the unresolved list 
								break;
							}
						}
						if (found == false)
						{
							ServerEntry se = GetServerEntry(urlsToAdd[i]);
							if (se != null)
							{
								_ServerEntries.Add(se);
								urlsToRemove.Add(i); // to be removed from the unresolved list 
							}
						}
					}
					for (int i = urlsToRemove.Count; i > 0; i--)
						urlsToAdd.RemoveAt(urlsToRemove[i - 1]); // urlsToAdd is returned to the unresolved list
				}
				_ServerEntriesLock.ReleaseMutex();
			}
		}

		/// <summary>
		/// This method calls the DiscoverServerInfo() for the specified server and determines if there is 
		/// already a ServerEntry for the server in _ServerEntries that contains one of the MEX endpoints 
		/// that is returned by the specified server in its ServerEntry
		/// </summary>
		/// <param name="urlEntry">The UrlEntry of the server</param>
		protected static ServerEntry GetServerEntry(string url)
		{
			ServerEntry serverEntry = null;
			try
			{
				MexEndpointInfo foundMexEP = null;

				// connect to the server at its URL
				ChannelFactory<IServerDiscovery> fac = new ChannelFactory<IServerDiscovery>(new BasicHttpBinding(), url);
				IServerDiscovery urlEntryServer = fac.CreateChannel();

				serverEntry = urlEntryServer.DiscoverServerInfo();
				fac.Close();
				//  should the server be disposed   ?????????
				// see if this server is already in the list based on its mex address
				foundMexEP = null;
				if (serverEntry != null)
				{
					foreach (var mexEp in serverEntry.MexEndpoints)
					{
						foreach (var curServerEntry in _ServerEntries)
						{
							foundMexEP = curServerEntry.MexEndpoints.Find(me => me.Url == mexEp.Url);
							if (foundMexEP != null)
							{
								serverEntry = curServerEntry;
								break; // when found
							}
						}
					}
				}
				if (foundMexEP == null) // add it if it is not yet in the list
				{
					// Servers behind a NAT firewall may not know their "outside"
					// IP address, so update the ServerDescription.ServerDiscoveryURL 
					// with the one just used, and make sure there is a Mex URL with 
					// hostname/IP address used in this URL
					ServerUri.ReconcileServerEntryWithServerDiscoveryUrl(serverEntry, url);
				}
			}
			catch (System.Exception ex)
			{
				// do nothing if server isn't available - don't add it to the list

			    Logger.Verbose(ex);
			}
			return serverEntry;
		}

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

		/// <summary>
		/// Calls PNRP to obtain the list of URLs. Then determines which are new 
		/// and which were deleted (which were in the previous list but not in 
		/// the current list).
		/// </summary>
		/// <returns></returns>
		protected static List<string> GetUrlsFromPNRP()
		{
			List<string> currentPnrpUrls = null;
			if (_PnrpEnabled)
			{
				try
				{
					// Find all the servers that have registered with PNRP
					currentPnrpUrls = PNRPHelper.ResolveServices(PnrpMeshNames.XiServerMesh);
					for (int i = 0; i < currentPnrpUrls.Count; i++)
						currentPnrpUrls[i] = currentPnrpUrls[i].ToUpper();
					// Cull the list since servers with multiple NICs will have an entry for each NIC
					// Cull the list since servers with multiple NICs will have an entry for each NIC
					IEnumerable<string> distinctUrls = currentPnrpUrls.Distinct();
					currentPnrpUrls = distinctUrls.ToList();
				}
				catch
				{
					_PnrpEnabled = false;
					// do nothing if PNRP not enabled
				}
			}
			return currentPnrpUrls;
		}

		#endregion // PNRP Support
	}

	internal class DiscoveryServerManConfigFile : ServerUrlConfigFile
	{
		internal void Open()
		{
			OpenFile();
		}

		internal void Close()
		{
			CloseFile();
		}

		///// <summary>
		///// The list of configured URLs in the manual configuration file that were read the during 
		///// the previous call to ProcessManualConfigFile() 
		///// </summary>
		private List<string> _PreviousConfiguredUrls = new List<string>();

		/// <summary>
		/// This method reads the manual configuration file and updates it if PNRP is also being used.
		/// </summary>
		/// <param name="pnrpUrls">The URLs obtained by PNRP</param>
		/// <param name="newPnrpUrls">The PNRP URLs that are new since the last call to this method.</param>
		/// <param name="deletedPnrpUrls">The PNRP URLs that have been removed since the last call to this method</param>
		/// <param name="filePnrpUrls">The total list of URLs in the file (both manually configured and those added by
		/// using PRNP</param>
		/// <param name="newConfiguredUrls">The configured URLs that are new since the last call to this method.</param>
		/// <param name="deletedConfiguredUrls">The configured URLs that have been removed since the last call to this method</param>
		internal void ProcessManualConfigFile(
			List<string> pnrpUrls, out List<string> newPnrpUrls, out List<string> deletedPnrpUrls,
			out List<string> filePnrpUrls, out List<string> newConfiguredUrls, out List<string> deletedConfiguredUrls)
		{
			List<string> allFileUrls = null;
			List<string> configuredUrls = null;
			OpenFile();
			List<string> manConfigSectionRewrite = GetUrlsFromFile(out allFileUrls, out configuredUrls, out filePnrpUrls);
			// add the two sources of PNRP URLs together

			ServerRoot.GetNewAndDeletedUrls(pnrpUrls, filePnrpUrls, out deletedPnrpUrls, out newPnrpUrls);

			// find pnrp URLs that are also in the manual config section
			// this can happen if the user copies a pnrp url into the manual config section instead of moving it
			if ((pnrpUrls != null) && (pnrpUrls.Count > 0))
			{
				foreach (var pnrpUrl in pnrpUrls)
				{
					if (configuredUrls.Find(url => (url == pnrpUrl)) != null)
						deletedPnrpUrls.Add(pnrpUrl);
				}
				foreach (var url in deletedPnrpUrls)
				{
					pnrpUrls.Remove(url);
				}
			}

			ServerRoot.GetNewAndDeletedUrls(configuredUrls, _PreviousConfiguredUrls, out deletedConfiguredUrls, out newConfiguredUrls);
			_PreviousConfiguredUrls = configuredUrls;

			// Rewrite the manual config file if there was an error in the manual config section 
			// or if a PNRP URL was deleted
			if (   (manConfigSectionRewrite != null)
				|| ((deletedPnrpUrls != null) && (deletedPnrpUrls.Count > 0))
			   )
			{
				RewriteManualConfigFile(manConfigSectionRewrite, pnrpUrls); // overwrite the file with all the PNRP URLs
			}
			// Otherwise, append new PNRP URLs to the end of the file
			else if ((newPnrpUrls != null) && (newPnrpUrls.Count > 0))
			{
				List<string> prnpUrlsToAdd = new List<string>();
				// see if the pnrp urls were also in the manual config file
				// if not, add them to prnpUrlsToAdd
				foreach (var url in newPnrpUrls)
				{
					if (allFileUrls.Find(u => string.Compare(u, url, true) == 0) == null)
						prnpUrlsToAdd.Add(url);
				}
				if (prnpUrlsToAdd.Count > 0)
					AppendPnrpUrlsToManualConfigFile(prnpUrlsToAdd); // append only the new PNRP URLs that are not in the file
			}
			CloseFile();
		}

		///// <summary>
		///// This method appends the supplied URLs to end of the PNRP Section of the  manual configuration file
		///// This method assumes the file has been opened
		///// </summary>
		///// <param name="urls">The URLs to append</param>
		private void AppendPnrpUrlsToManualConfigFile(List<string> urls)
		{
			if (_FileStream != null)
			{
				if (_StreamWriter == null)
					_StreamWriter = new StreamWriter(_FileStream);

				// append the new URLs
			    try
			    {
			        // Go to the end of the file
			        _StreamReader.ReadToEnd();

			        // add the PNRP Section header if it is not already there
			        if (_PnrpSectionPresent == false)
			        {
			            _StreamWriter.WriteLine("#");
			            _StreamWriter.WriteLine(_PnrpSectionHeader);
			            _PnrpSectionPresent = true;
			        }
			        foreach (var url in urls)
			        {
			            _StreamWriter.WriteLine(url);
			        }
			    }
			    catch (Exception e)
			    {
			        Logger.Verbose(e);
			    }
			}
		}

		///// <summary>
		///// This method rewrites the PNRP section of the  manual configuration file with the supplied URLs.
		///// This method assumes the file has been opened
		///// </summary>
		///// <param name="manConfigSection">The lines prior to and including the manual configuration section to write</param>
		///// <param name="urls">The PNRP URLs to write</param>
		internal void RewriteManualConfigFile(List<string> manConfigSection, List<string> pnrpUrlsToWrite)
		{
			if (_FileStream != null)
			{
				if (_StreamWriter == null)
					_StreamWriter = new StreamWriter(_FileStream);

				// rewrite the file replacing the PNRP section with the URLS from curPnrpUrls
				// first use the supplied manual config section lines or copy all the lines up to the PNRP Section Header 
			    try
			    {
			        if (manConfigSection == null)
			        {
			            manConfigSection = new List<string>();
			            ResetStreamToBeginning();
			            while (!_StreamReader.EndOfStream)
			            {
			                string line = _StreamReader.ReadLine().Trim();
			                if (line == _PnrpSectionHeader)
			                    break;
			                else
			                    manConfigSection.Add(line);
			            }
			        }

			        CloseFile();
			        // now rewrite the beginning of the file
			        _FileStream = new FileStream(
			            FileName,
			            FileMode.Create,
			            FileAccess.ReadWrite,
			            FileShare.None);

			        _StreamWriter = new StreamWriter(_FileStream);
			        string lastLine = "";
			        foreach (var line in manConfigSection)
			        {
			            if ((line != "#") || (lastLine != "#")) // get rid of back to back empty comment lines
			                _StreamWriter.WriteLine(line);
			            lastLine = line;
			        }
			        if (manConfigSection[manConfigSection.Count - 1] != "#") // if the last line written is not '#'
			            _StreamWriter.WriteLine("#");
			        _StreamWriter.WriteLine(_PnrpSectionHeader);
			        if (pnrpUrlsToWrite != null)
			        {
			            foreach (var url in pnrpUrlsToWrite)
			            {
			                _StreamWriter.WriteLine(url);
			            }
			        }
			    }
			    catch (Exception e)
			    {
			        Logger.Verbose(e);
			    }
			}
		}
	}
}
