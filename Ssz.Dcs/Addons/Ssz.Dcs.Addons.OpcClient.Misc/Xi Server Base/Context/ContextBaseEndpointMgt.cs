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
using System.Linq;
using Ssz.Utils;
using Xi.Common.Support;
using Xi.Contracts.Constants;
using Xi.Contracts;
using Xi.Contracts.Data;
using Ssz.Utils.Net4;

namespace Xi.Server.Base
{
	/// <summary>
	/// This partial class defines the methods to be overridden by the server implementation 
	/// to support the Endpoint Management methods of the IResourceManagement interface.
	/// </summary>
	public abstract partial class ContextBase<TList>
		where TList : ListRoot
	{
		/// <summary>
		/// This method may be overridden by the context implementation in the 
		/// Server Implementation project.
		/// <para>default implementation may be sufficient for some server implemenations.</para>
		/// </summary>
		/// <param name="contractType">
		/// <para>The type of the endpoint as specified by the interface that it 
		/// supports.  IResourceManagement and IServerDiscovery cannot be created.</para>
		/// <para>The values are defined using the typeof(IXXX).Name property, where IXXX is 
		/// the contract name (e.g. IRead).  This value is also used as the value of the 
		/// following property: </para>
		/// <para> System.ServiceModel.Description.ServiceEndpoint.Contract.Name  </para>
		/// </param>
		/// <param name="endpointUrl">
		/// The URL of the endpoint as defined by System.ServiceModel.Description.ServiceEndpoint.Address.Uri.OriginalString.
		/// </param>
		/// <returns>The definition of the endpoint.</returns>
		public virtual EndpointDefinition OnOpenEndpoint(string contractType, string endpointUrl)
		{
			lock (ContextLock)
			{
				// Find an existing endpoint definition.
				EndpointEntry<TList> endpointEntry = null;
				UriBuilder paramUri = new UriBuilder(endpointUrl);
				try
				{
					List<EndpointEntry<TList>> endpointEntryList = null;
					// Frist get a list of just the endpoints where the contract type matches
					IEnumerable<EndpointEntry<TList>> endpointEntryEnum = (from kvp in _XiEndpoints
																		   where (0 == string.Compare(kvp.Value.EndpointDefinition.ContractType, contractType, true))
																		   select kvp.Value);
					if (endpointEntryEnum != null)
					{
						endpointEntryList = endpointEntryEnum.ToList<EndpointEntry<TList>>();
						// if this is not a net pipe address, match on the URL
						if (0 == string.Compare(Uri.UriSchemeNetPipe, paramUri.Scheme, true))
						{
							// first try to find the endpoint for the url in the endpoint list.  The url may not match
							// because the client may have changed "localhost" to the machine name or ip address, or 
							// vice-versa.  If it wasn't found, loop through the endpoint list and compare on the url 
							// path that follows the host element of the url.
							endpointEntry = endpointEntryList.Find(ep =>
								endpointUrl == ep.EndpointDefinition.EndpointDescription.Address.Uri.OriginalString);
							if (endpointEntry == null)
							{
								foreach (var endpoint in endpointEntryList)
								{
									// only check the net pipe endpoints
									if (0 == string.Compare(Uri.UriSchemeNetPipe,
															endpoint.EndpointDefinition.EndpointDescription.Address.Uri.Scheme,
															true))
									{
										string paramUrlSuffix = paramUri.Port + paramUri.Path;
										string svrUrlSuffix = endpoint.EndpointDefinition.EndpointDescription.Address.Uri.Port +
											endpoint.EndpointDefinition.EndpointDescription.Address.Uri.LocalPath;
										if (0 == string.Compare(paramUrlSuffix, svrUrlSuffix, true))
										{
											// a match
											endpointEntry = endpoint;
											break;
										}
									}
								}
							}
						}
						else  // look for the endpoint by matching on the scheme and the substring starting with the port number
						{
							foreach (var endpoint in endpointEntryList)
							{
								{
									// if the server endpoint has "localhost" as the host name, compare the suffix only
									// since more than likely, the client has changed the host name to the machine name or ip address
									string host = endpoint.EndpointDefinition.EndpointDescription.Address.Uri.Host;
									if (string.Compare(host, "localhost", true) == 0)
									{
										if (0 == string.Compare(paramUri.Scheme,
																endpoint.EndpointDefinition.EndpointDescription.Address.Uri.Scheme,
																true))
										{
											string paramUrlSuffix = paramUri.Port + paramUri.Path;
											string svrUrlSuffix = endpoint.EndpointDefinition.EndpointDescription.Address.Uri.Port +
												endpoint.EndpointDefinition.EndpointDescription.Address.Uri.LocalPath;
											if (0 == string.Compare(paramUrlSuffix, svrUrlSuffix, true))
											{
												// a match
												endpointEntry = endpoint;
												break;
											}
										}
									}
									// else the server endpoint has the machine name or ip address as the host name, so the url from the client has to match exactly
									// this allows the server to make the server endpoint available only at the ip addresses it specified
									// this may be useful when the server machine has NIC cards on both private and public networks
									else
									{
										endpointEntry = endpointEntryList.Find(
											ep => endpointUrl == ep.EndpointDefinition.EndpointDescription.Address.Uri.OriginalString);
										if (null != endpointEntry)
											break;
									}
								}
							}
						}
						// If the endpoint was found, set its state to open and return the Endpoint Definition.
						if (null != endpointEntry)
						{
							OnValidateOpenEndpointSecurity(endpointEntry); // check security for the endpoint to open
							endpointEntry.IsOpen = true;
							if (endpointEntry.EndpointDefinition.ContractType == typeof(IPoll).Name)
								_iPollEndpointEntry = endpointEntry;
						}
						else
						{
							throw FaultHelpers.Create("Specified endpoint not found");
						}
					}
				}
				catch (Exception ex)
				{
					if (ex is System.ServiceModel.FaultException<XiFault>)
						throw;
		            
                    Logger.Verbose(ex);
                    endpointEntry = null;
				}
				// Return null if the endpoint is not supported.
				return (null == endpointEntry) ? null : endpointEntry.EndpointDefinition;
			}
		}

		/// <summary>
		/// This method is called prior to calling OnOpenEndpoint to validate security for the 
		/// endpoint to be opened.  If any problems are found, an XiFault should be thrown to 
		/// communicate them to the client.
		/// <param name="endpointEntry">
		/// The endpoint to be validated.
		/// </param>
		/// </summary>
		protected abstract void OnValidateOpenEndpointSecurity(EndpointEntry<TList> endpointEntry);

		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// <para>default implementation may be sufficient for some server implemenations.</para>
		/// </summary>
		/// <param name="endpointId">
		/// The Xi Server generated Endpoint LocalId
		/// </param>
		/// <param name="serverListId">
		/// The identifier of the list to add to the endpoint.
		/// </param>
		/// <returns>
		/// The list identifier and result code for the list whose 
		/// add failed. Returns null if the add succeeded.  
		/// </returns>
		public virtual AliasResult OnAddListToEndpoint(string endpointId, uint serverListId)
		{
			AliasResult aliasResult = null;
			EndpointEntry<TList> endpointEntry0 = null;
			EndpointEntry<ListRoot> endpointEntry1 = null;
			bool bValidEndpointId = false;
			TList tList = null;
			bool bValidListId = false;
			lock (ContextLock)
			{
				bValidEndpointId = _XiEndpoints.TryGetValue(endpointId, out endpointEntry0);
				bValidListId = _XiLists.TryGetValue(serverListId, out tList);

				if ((!bValidListId) || (tList == null))
					aliasResult = new AliasResult(XiFaultCodes.E_BADLISTID, 0, serverListId);
				else if ((bValidEndpointId == false) || (endpointEntry0 == null))
					aliasResult = new AliasResult(XiFaultCodes.E_BADENDPOINTID, tList.ClientId, tList.ServerId);
				else if (!endpointEntry0.IsOpen)
					aliasResult = new AliasResult(XiFaultCodes.E_ENDPOINTERROR, tList.ClientId, tList.ServerId);
				else
					endpointEntry1 = endpointEntry0 as EndpointEntry<ListRoot>;
			}
			if (null != endpointEntry1 && null != tList)
			{
				aliasResult = tList.OnAddListToEndpoint(endpointEntry1);
			}
			return aliasResult;
		}

		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// <para>default implementation may be sufficient for some server implemenations.</para>
		/// </summary>
		/// <param name="endpointId">
		/// The Xi Server generated Endpoint LocalId of the endpoint from which the list is to be removed.
		/// </param>
		/// <param name="listIds">
		/// The identifiers of the lists to remove from the endpoint.
		/// </param>
		/// <returns>
		/// The list identifiers and result codes for the lists whose 
		/// removal failed. Returns null if all removals succeeded.  
		/// </returns>
		public virtual List<AliasResult> OnRemoveListsFromEndpoint(string endpointId, List<uint> listIds)
		{
			List<AliasResult> listAliasResult = new List<AliasResult>();
			EndpointEntry<TList> endpointEntry0 = null;
			EndpointEntry<ListRoot> endpointEntry1 = null;
			List<TList> listTLists = new List<TList>();

			lock (ContextLock)
			{
				if (_XiEndpoints.TryGetValue(endpointId, out endpointEntry0))
				{
					// if listIds is null, then remove all lists from the endpoint
					// start by populating listIds with the ids of all the lists assigned to the endpoint
					if (listIds == null)
					{
						listIds = new List<uint>();
						foreach (var list in endpointEntry0.XiLists)
						{
							listIds.Add(list.ServerId);
						}
					}
					foreach (var serverListId in listIds)
					{
						TList tList = null;
						if (_XiLists.TryGetValue(serverListId, out tList))
							listTLists.Add(tList);
						else
							listAliasResult.Add(
								new AliasResult(XiFaultCodes.E_BADLISTID, 0, serverListId));
					}
				}
				else
				{
					throw FaultHelpers.Create(XiFaultCodes.E_BADENDPOINTID, endpointId);
				}
			}

			if (0 < listTLists.Count)
			{
				endpointEntry1 = endpointEntry0 as EndpointEntry<ListRoot>;
				foreach (var tList in listTLists)
				{
					AliasResult aliasResult = tList.OnRemoveListFromEndpoint(endpointEntry1);
					if (aliasResult != null)
						listAliasResult.Add(aliasResult);
				}
			}
			// return null if there were no errors
			return (listAliasResult.Count == 0) ? null : listAliasResult;
		}

		/// <summary>
		/// This method is to be overridden by the context implementation in the 
		/// Server Implementation project.
		/// <para>default implementation may be sufficient for some server implemenations.</para>
		/// </summary>
		/// <param name="endpointId">
		/// A string value the uniquely identified the endpoint (may be a GUID) to be deleted.
		/// </param>
		public virtual void OnCloseEndpoint(string endpointId)
		{
			EndpointEntry<TList> endpointEntry = null;
			lock (ContextLock)
			{
				_XiEndpoints.TryGetValue(endpointId, out endpointEntry);
				if (null != endpointEntry)
				{
					if (endpointEntry.EndpointDefinition.ContractType == typeof(IPoll).Name)
						_iPollEndpointEntry = endpointEntry;
					endpointEntry.OnCloseEndpoint();
				}
			}
		}

	}
}
