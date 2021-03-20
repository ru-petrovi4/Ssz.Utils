using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Ssz.DataGrpc.Server.Core.Lists;
using Xi.Common.Support;
using Xi.Contracts;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Ssz.DataGrpc.Server.Core.Context
{
    /// <summary>
    ///   This partial class defines the methods to be overridden by the server implementation 
    ///   to support the Endpoint Management methods of the IResourceManagement interface.
    /// </summary>
    public partial class ServerContext        
    {
        #region public functions

        /// <summary>
        ///   This method may be overridden by the context implementation in the 
        ///   Server Implementation project.
        ///   <para>default implementation may be sufficient for some server implemenations.</para>
        /// </summary>
        /// <param name = "contractType">
        ///   <para>The type of the endpoint as specified by the interface that it 
        ///     supports.  IResourceManagement and IServerDiscovery cannot be created.</para>
        ///   <para>The values are defined using the typeof(IXXX).Name property, where IXXX is 
        ///     the contract name (e.g. IRead).  This value is also used as the value of the 
        ///     following property: </para>
        ///   <para> System.ServiceModel.Description.ServiceEndpoint.Contract.Name  </para>
        /// </param>
        /// <param name = "endpointUrl">
        ///   The URL of the endpoint as defined by System.ServiceModel.Description.ServiceEndpoint.Address.Uri.OriginalString.
        /// </param>
        /// <returns>The definition of the endpoint.</returns>
        public virtual EndpointDefinition OnOpenEndpoint(string contractType, string endpointUrl)
        {
            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                // Find an existing endpoint definition.
                EndpointEntry<TListRoot> endpointEntry = null;
                var paramUri = new UriBuilder(endpointUrl);
                try
                {
                    List<EndpointEntry<TListRoot>> endpointEntryList = null;
                    // Frist get a list of just the endpoints where the contract type matches
                    IEnumerable<EndpointEntry<TListRoot>> endpointEntryEnum = (from kvp in _endpoints
                                                                               where
                                                                                   (0 ==
                                                                                    string.Compare(
                                                                                        kvp.Value.EndpointDefinition.
                                                                                            ContractType, contractType,
                                                                                        true))
                                                                               select kvp.Value);
                    if (endpointEntryEnum != null)
                    {
                        endpointEntryList = endpointEntryEnum.ToList();
                        // if this is not a net pipe address, match on the URL
                        if (0 == string.Compare(Uri.UriSchemeNetPipe, paramUri.Scheme, true))
                        {
                            // first try to find the endpoint for the url in the endpoint list.  The url may not match
                            // because the client may have changed "localhost" to the machine name or ip address, or 
                            // vice-versa.  If it wasn't found, loop through the endpoint list and compare on the url 
                            // path that follows the host element of the url.
                            endpointEntry =
                                endpointEntryList.Find(
                                    ep =>
                                    endpointUrl ==
                                    ep.EndpointDefinition.EndpointDescription.Address.Uri.
                                        OriginalString);
                            if (endpointEntry == null)
                            {
                                foreach (EndpointEntry<TListRoot> endpoint in endpointEntryList)
                                {
                                    // only check the net pipe endpoints
                                    if (0 ==
                                        string.Compare(Uri.UriSchemeNetPipe,
                                                       endpoint.EndpointDefinition.EndpointDescription.Address.Uri.
                                                           Scheme,
                                                       true))
                                    {
                                        string paramUrlSuffix = paramUri.Port + paramUri.Path;
                                        string svrUrlSuffix =
                                            endpoint.EndpointDefinition.EndpointDescription.Address.Uri.Port +
                                            endpoint.EndpointDefinition.EndpointDescription.Address.Uri.
                                                LocalPath;
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
                        else
                            // look for the endpoint by matching on the scheme and the substring starting with the port number
                        {
                            foreach (EndpointEntry<TListRoot> endpoint in endpointEntryList)
                            {
                                {
                                    // if the server endpoint has "localhost" as the host name, compare the suffix only
                                    // since more than likely, the client has changed the host name to the machine name or ip address
                                    string host = endpoint.EndpointDefinition.EndpointDescription.Address.Uri.Host;
                                    if (string.Compare(host, "localhost", true) == 0)
                                    {
                                        if (0 ==
                                            string.Compare(paramUri.Scheme,
                                                           endpoint.EndpointDefinition.EndpointDescription.Address.Uri.
                                                               Scheme, true))
                                        {
                                            string paramUrlSuffix = paramUri.Port + paramUri.Path;
                                            string svrUrlSuffix =
                                                endpoint.EndpointDefinition.EndpointDescription.Address.Uri.Port +
                                                endpoint.EndpointDefinition.EndpointDescription.Address.Uri.
                                                    LocalPath;
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
                                        endpointEntry =
                                            endpointEntryList.Find(
                                                ep =>
                                                endpointUrl ==
                                                ep.EndpointDefinition.EndpointDescription.
                                                    Address.Uri.OriginalString);
                                        if (null != endpointEntry) break;
                                    }
                                }
                            }
                        }
                        // If the endpoint was found, set its state to open and return the Endpoint Definition.
                        if (null != endpointEntry)
                        {
                            OnValidateOpenEndpointSecurity(endpointEntry); // check security for the endpoint to open
                            endpointEntry.IsOpen = true;
                            if (endpointEntry.EndpointDefinition.ContractType == typeof (IPoll).Name)
                                _iPollEndpointEntry = endpointEntry;
                        }
                        else throw RpcExceptionHelper.Create("Specified endpoint not found");
                    }
                }
                catch (Exception ex)
                {
                    if (ex is FaultException<XiFault>) throw ex;
                    endpointEntry = null;
                }
                // Return null if the endpoint is not supported.
                return (null == endpointEntry) ? null : endpointEntry.EndpointDefinition;
            }
        }

        /// <summary>
        ///   This method is to be overridden by the context implementation in the 
        ///   Server Implementation project.
        ///   <para>default implementation may be sufficient for some server implemenations.</para>
        /// </summary>
        /// <param name = "endpointId">
        ///   The Xi Server generated Endpoint LocalId
        /// </param>
        /// <param name = "serverListId">
        ///   The identifier of the list to add to the endpoint.
        /// </param>
        /// <returns>
        ///   The list identifier and result code for the list whose 
        ///   add failed. Returns null if the add succeeded.  
        /// </returns>
        public virtual AliasResult OnAddListToEndpoint(string endpointId, uint serverListId)
        {
            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                TListRoot listRoot = null;
                bool bValidListId = _listManager.TryGetValue(serverListId, out listRoot);
                if (!bValidListId) return new AliasResult(XiFaultCodes.E_BADLISTID, 0, serverListId);

                EndpointEntry<TListRoot> endpointEntry = null;
                bool bValidEndpointId = _endpoints.TryGetValue(endpointId, out endpointEntry);
                if (!bValidEndpointId)
                    return new AliasResult(XiFaultCodes.E_BADENDPOINTID, listRoot.ClientId, listRoot.ServerId);

                if (!endpointEntry.IsOpen)
                    return new AliasResult(XiFaultCodes.E_ENDPOINTERROR, listRoot.ClientId, listRoot.ServerId);

                AliasResult result = listRoot.AddEndpoint(endpointEntry as EndpointEntry<ServerListRoot>);

                if (result == null)
                {
                    endpointEntry.AddList(listRoot);
                }

                return result;
            }
        }

        /// <summary>
        ///   This method is to be overridden by the context implementation in the 
        ///   Server Implementation project.
        ///   <para>default implementation may be sufficient for some server implemenations.</para>
        /// </summary>
        /// <param name = "endpointId">
        ///   The Xi Server generated Endpoint LocalId of the endpoint from which the list is to be removed.
        /// </param>
        /// <param name = "listIds">
        ///   The identifiers of the lists to remove from the endpoint.
        /// </param>
        /// <returns>
        ///   The list identifiers and result codes for the lists whose 
        ///   removal failed. Returns null if all removals succeeded.  
        /// </returns>
        public virtual List<AliasResult> OnRemoveListsFromEndpoint(string endpointId, List<uint> listIds)
        {
            var resultsList = new List<AliasResult>();
            EndpointEntry<TListRoot> endpointEntry = null;
            List<TListRoot> listTLists;

            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                bool bValidEndpointId = _endpoints.TryGetValue(endpointId, out endpointEntry);
                if (!bValidEndpointId) throw RpcExceptionHelper.Create(XiFaultCodes.E_BADENDPOINTID, endpointId);

                // if listIds is null, then remove all lists from the endpoint
                // start by populating listIds with the ids of all the lists assigned to the endpoint
                if (listIds == null)
                {
                    listTLists = endpointEntry.Lists;

                    endpointEntry.ClearLists();
                }
                else
                {
                    listTLists = new List<TListRoot>();

                    foreach (uint listId in listIds)
                    {
                        TListRoot list;
                        if (_listManager.TryGetValue(listId, out list))
                        {
                            listTLists.Add(list);

                            endpointEntry.RemoveList(list);
                        }
                        else resultsList.Add(new AliasResult(XiFaultCodes.E_BADLISTID, 0, listId));
                    }
                }
            }

            foreach (TListRoot list in listTLists)
            {
                try
                {
                    AliasResult aliasResult = list.RemoveEndpoint(endpointEntry as EndpointEntry<ServerListRoot>);
                    if (aliasResult != null) resultsList.Add(aliasResult);
                }
                catch (ObjectDisposedException)
                {
                }
            }

            return (resultsList.Count == 0) ? null : resultsList;
        }

        /// <summary>
        ///   This method is to be overridden by the context implementation in the 
        ///   Server Implementation project.
        ///   <para>default implementation may be sufficient for some server implemenations.</para>
        /// </summary>
        /// <param name = "endpointId">
        ///   A string value the uniquely identified the endpoint (may be a GUID) to be deleted.
        /// </param>
        public virtual void OnCloseEndpoint(string endpointId)
        {
            List<TListRoot> listTLists;
            EndpointEntry<TListRoot> endpointEntry;

            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                bool bValidEndpointId = _endpoints.TryGetValue(endpointId, out endpointEntry);
                if (!bValidEndpointId)
                {
                    // TODO
                    return;
                }

                if (endpointEntry.EndpointDefinition.ContractType == typeof (IPoll).Name)
                    _iPollEndpointEntry = endpointEntry;

                listTLists = endpointEntry.Lists;

                endpointEntry.ClearLists();
                endpointEntry.IsOpen = false;
            }

            foreach (TListRoot list in listTLists)
            {
                try
                {
                    list.RemoveEndpoint(endpointEntry as EndpointEntry<ServerListRoot>);
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        #endregion

        #region protected functions

        /// <summary>
        ///   This method is called prior to calling OnOpenEndpoint to validate security for the 
        ///   endpoint to be opened.  If any problems are found, an XiFault should be thrown to 
        ///   communicate them to the client.
        ///   <param name = "endpointEntry">
        ///     The endpoint to be validated.
        ///   </param>
        /// </summary>
        protected abstract void OnValidateOpenEndpointSecurity(EndpointEntry<TListRoot> endpointEntry);

        #endregion
    }
}