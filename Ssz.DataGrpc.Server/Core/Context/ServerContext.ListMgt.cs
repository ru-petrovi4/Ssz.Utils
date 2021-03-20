using System;
using System.Collections.Generic;
using Ssz.DataGrpc.Server.Core.Lists;
using Xi.Common.Support;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Ssz.DataGrpc.Server.Core.Context
{
    /// <summary>
    ///   This partial class defines the methods to be overridden by the server implementation 
    ///   to support the List Management methods of the IResourceManagement interface.
    /// </summary>
    public partial class ServerContext        
    {
        #region public functions

        /// <summary>
        ///   This method is to be overridden by the context implementation in the 
        ///   Server Implementation project.
        /// </summary>
        /// <param name = "clientId">
        ///   The Client LocalId for this list.  Used in callbacks to allow the 
        ///   client to identify this list.
        /// </param>
        /// <param name = "listType">
        ///   Indicates the type of list to be created.
        ///   Standard list types as defined by the ListAttributes class 
        ///   are: 
        ///   1) Data List, 
        ///   2) History Data List, 
        ///   3) Event List 
        ///   4) History Event List
        /// </param>
        /// <param name = "updateRate">
        ///   The requested update rate in milliseconds for the list. The  
        ///   update rate indicates how often the server updates the 
        ///   values of elements in the list.  A value of 0 indicates 
        ///   that updating is exception-based. The server may negotiate 
        ///   this value, up or down as necessary to support its efficient 
        ///   operation.
        /// </param>
        /// <param name = "bufferingRate">
        ///   <para>An optional-use parameter that indicates that the server is 
        ///     to buffer data updates, rather than overwriting them, until either 
        ///     the time span defined by the buffering rate expires or the values 
        ///     are transmitted to the client in a callback or poll response. If 
        ///     the time span expires, then the oldest value for a data object is 
        ///     discarded when a new value is received from the underlying system.</para>
        ///   <para>The value of the bufferingRate is set to TimeSpan.Zero to indicate 
        ///     that it is not to be used and that new values overwrite (replace) existing 
        ///     cached values.  </para>
        ///   <para>When used, this parameter contains the client-requested buffering 
        ///     rate, which the server may negotiate up or down, or to TimeSpan.Zero if the 
        ///     server does not support the buffering rate. </para>
        ///   <para>The FeaturesSupported member of the StandardMib is used to indicate 
        ///     server support for the buffering rate.</para>
        /// </param>
        /// <param name = "filterSet">
        ///   The set of filters to use to select elements of the list.  
        /// </param>
        /// <returns>
        ///   The attributes created for the list.
        /// </returns>
        public abstract ListAttributes OnDefineList(uint clientId, uint listType, uint scanRate, uint bufferingRate,
                                                    FilterSet filterSet);

        #endregion

        #region internal functions

        /// <summary>
        ///   This method is to be overridden by the context implementation in the 
        ///   Server Implementation project.
        /// </summary>
        /// <param name = "listIds">
        ///   The identifiers for the lists whose attributes are to be 
        ///   retrieved.
        /// </param>
        /// <returns>
        ///   The list of requested List Attributes. The size and order 
        ///   of this list matches the size and order of the listAliases 
        ///   parameter.  
        /// </returns>
        internal List<ListAttributes> OnGetListAttributes(List<uint> listIds)
        {
            var lisxiListAttrs = new List<ListAttributes>(listIds.Count);
            var listTList = new List<TListRoot>(listIds.Count);

            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                foreach (uint listKey in listIds)
                {
                    TListRoot xiList = null;
                    if (_listManager.TryGetValue(listKey, out xiList)) listTList.Add(xiList);
                    else
                    {
                        lisxiListAttrs.Add(new ListAttributes
                            {
                                ClientId = listKey,
                                ServerId = listKey,
                                ListType = 0,
                                Enabled = false,
                                UpdateRate = 0,
                                CurrentCount = 0,
                                HowSorted = 0,
                                SortKeys = null,
                                FilterSet = null,
                                ResultCode = XiFaultCodes.E_BADLISTID,
                            });
                    }
                }
            }

            foreach (TListRoot xiList in listTList)
            {
                try
                {
                    lisxiListAttrs.Add(xiList.ListAttributes);
                }
                catch (ObjectDisposedException)
                {
                }
            }

            return lisxiListAttrs;
        }

        /// <summary>
        ///   This method is to be overridden by the context implementation in the 
        ///   Server Implementation project.
        /// </summary>
        /// <param name = "listId">
        ///   The identifier for the list whose aliases are to be updated.
        /// </param>
        /// <param name = "newAliases">
        ///   The list of current and new alias values.
        /// </param>
        /// <returns>
        ///   The list of updated aliases. The size and order of this list matches 
        ///   the size and order of the listAliases parameter.  Each AliasResult in 
        ///   the list contains the new client alias from the request and its 
        ///   corresponding new server alias assigned by the server.
        /// </returns>
        internal List<AliasResult> OnRenewAliases(uint listId, List<AliasUpdate> newAliases)
        {
            TListRoot xiList = null;

            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                if (!_listManager.TryGetValue(listId, out xiList))
                    throw RpcExceptionHelper.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Renew Aliases.");
            }

            return xiList.OnRenewAliases(newAliases);
        }

        /// <summary>
        ///   This method is to be overridden by the context implementation in the 
        ///   Server Implementation project.
        /// </summary>
        /// <param name = "listIds">
        ///   The identifiers for the lists to be deleted.  If this parameter is null,
        ///   then all lists for the context is to be deleted.
        /// </param>
        /// <returns>
        ///   The list identifiers and result codes for the lists whose 
        ///   deletion failed. Returns null if all deletes succeeded.  
        /// </returns>
        internal List<AliasResult> OnDeleteLists(List<uint> listIds)
        {
            List<AliasResult> resultsList = null;
            var listsToDispose = new List<TListRoot>();

            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                // null means to delete all lists, so put all lists into listIds
                if (listIds == null)
                {
                    // Remove each list from the endpoints to which it is assigned
                    foreach (KeyValuePair<string, EndpointEntry<TListRoot>> ep in _endpoints)
                    {
                        ep.Value.ClearLists();
                    }

                    listsToDispose = _lists;
                    _listManager.Clear();
                }
                else
                {
                    foreach (uint listId in listIds)
                    {
                        TListRoot list = null;
                        if (_listManager.TryGetValue(listId, out list))
                        {
                            // Remove each list from the endpoints to which it is assigned
                            foreach (KeyValuePair<string, EndpointEntry<TListRoot>> ep in _endpoints)
                            {
                                ep.Value.RemoveList(list);
                            }

                            listsToDispose.Add(list);
                            _listManager.Remove(listId);
                        }
                        else
                        {
                            if (resultsList == null) resultsList = new List<AliasResult>();
                            resultsList.Add(new AliasResult(XiFaultCodes.E_BADLISTID, 0, listId));
                        }
                    }
                }

                _lists = _listManager.ToList();
            }

            // Dispose of the lists outside of the lock to prevent deadlock.
            foreach (TListRoot list in listsToDispose)
            {
                list.Dispose();
            }

            return resultsList;
        }

        /// <summary>
        ///   This method is to be overridden by the context implementation in the 
        ///   Server Implementation project.
        /// </summary>
        /// <param name = "listId">
        ///   The identifier for the list to which data objects are to be 
        ///   added.
        /// </param>
        /// <param name = "dataObjectsToAdd">
        ///   The data objects to add.
        /// </param>
        /// <returns>
        ///   The list of results. The size and order of this list matches 
        ///   the size and order of the objectsToAdd parameter.   
        /// </returns>
        internal List<AddDataObjectResult> OnAddDataObjectsToList(uint listId, List<ListInstanceId> dataObjectsToAdd)
        {
            TListRoot xiList = null;

            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                if (!_listManager.TryGetValue(listId, out xiList))
                    throw RpcExceptionHelper.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Add Data Object To List.");
            }

            return xiList.OnAddDataObjectsToList(dataObjectsToAdd);
        }

        /// <summary>
        ///   This method is to be overridden by the context implementation in the 
        ///   Server Implementation project.
        /// </summary>
        /// <param name = "listId">
        ///   The identifier for the list from which data objects are 
        ///   to be removed.
        /// </param>
        /// <param name = "serverAliasesToDelete">
        ///   The server aliases of the data objects to remove.
        /// </param>
        /// <returns>
        ///   The list identifiers and result codes for data objects whose 
        ///   removal failed. Returns null if all removals succeeded.  
        /// </returns>
        internal List<AliasResult> OnRemoveDataObjectsFromList(uint listId, List<uint> serverAliasesToDelete)
        {
            TListRoot xiList = null;

            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                if (!_listManager.TryGetValue(listId, out xiList))
                    throw RpcExceptionHelper.Create(XiFaultCodes.E_BADLISTID,
                                              "List Id not found in Remove Data Objects From List.");
            }

            return xiList.OnRemoveDataObjectsFromList(serverAliasesToDelete);
        }

        /// <summary>
        ///   This method is to be overridden by the context implementation in the 
        ///   Server Implementation project.
        /// </summary>
        /// <param name = "listId">
        ///   The identifier for the list for which the filters are to be changed.
        /// </param>
        /// <param name = "updateRate">
        ///   List update or scan rate.  The server will negotiate this rate to one 
        ///   that it can support.  GexiListAttributes can be used to obtain the current 
        ///   value of this parameter.  Null if the update rate is not to be updated.  
        /// </param>
        /// <param name = "bufferingRate">
        ///   List buffering rate.  The server will negotiate this rate to one 
        ///   that it can support.  GexiListAttributes can be used to obtain the current 
        ///   value of this parameter.  Null if the buffering rate is not to be updated.
        /// </param>
        /// <param name = "filterSet">
        ///   The new set of filters.  The server will negotiate these filters to those 
        ///   that it can support.  GexiListAttributes can be used to obtain the current 
        ///   value of this parameter.  Null if the filters are not to be updated.
        /// </param>
        /// <returns>
        ///   The revised update rate, buffering rate, and filter set.  Attributes 
        ///   that were not updated will be null in this response.
        /// </returns>
        internal ModifyListAttrsResult OnModifyListAttributes(uint listId, uint? updateRate, uint? bufferingRate,
                                                              FilterSet filterSet)
        {
            TListRoot xiList = null;

            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                if (!_listManager.TryGetValue(listId, out xiList))
                    throw RpcExceptionHelper.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Change List Filters.");
            }

            return xiList.OnModifyListAttributes(updateRate, bufferingRate, filterSet);
        }

        ///<summary>
        ///  This method is to be overridden by the context implementation in the 
        ///  Server Implementation project.
        ///</summary>
        ///<param name = "listId">
        ///  The identifier for the list for which updating is to be 
        ///  enabled or disabled.
        ///</param>
        ///<param name = "enableUpdating">
        ///  Indicates, when TRUE, that updating of the list is to be enabled,
        ///  and when FALSE, that updating of the list is to be disabled.
        ///</param>
        internal ListAttributes OnEnableListUpdating(uint listId, bool enableUpdating)
        {
            TListRoot xiList = null;

            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                if (!_listManager.TryGetValue(listId, out xiList))
                    throw RpcExceptionHelper.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Enable List Updating.");
            }

            return xiList.OnEnableListUpdating(enableUpdating);
            //if (enableUpdating) xiList.OnTouchList(); // redundant
        }

        ///<summary>
        ///  This method is to be overridden by the context implementation in the 
        ///  Server Implementation project.
        ///</summary>
        ///<param name = "listId">
        ///  The identifier for the list for which updating is to be 
        ///  enabled or disabled.
        ///</param>
        ///<param name = "enableUpdating">
        ///  Indicates, when TRUE, that updating of the list is to be enabled,
        ///  and when FALSE, that updating of the list is to be disabled.
        ///</param>
        ///<param name = "serverAliases">
        ///  The list of aliases for data objects of a list for 
        ///  which updating is to be enabled or disabled.
        ///  When this value is null updating all elements of the list are to be 
        ///  enabled/disabled. In this case, however, the enable/disable state 
        ///  of the list itself is not changed.
        ///</param>
        ///<returns>
        ///  <para>If the serverAliases parameter was null, returns 
        ///    null if the server was able to successfully enable/disable 
        ///    the list and all its elements.  If not, throws an exception 
        ///    for event lists and for data lists, returns the client and server 
        ///    aliases and result codes for the data objects that could not be 
        ///    enabled/disabled.  </para> 
        ///  <para>If the serverAliases parameter was not null, returns null 
        ///    if the server was able to successfully enable/disable the data 
        ///    objects identified by the serverAliases.  If not, returns the 
        ///    client and server aliases and result codes for the data objects 
        ///    that could not be enabled/disabled.</para> 
        ///</returns>
        internal List<AliasResult> OnEnableListElementUpdating(uint listId, bool enableUpdating,
                                                               List<uint> serverAliases)
        {
            TListRoot xiList = null;

            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                if (!_listManager.TryGetValue(listId, out xiList))
                    throw RpcExceptionHelper.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Enable List Updating.");
            }

            return xiList.OnEnableListElementUpdating(enableUpdating, serverAliases);

            /* // redundant
				if (enableUpdating)
					xiList.OnTouchDataObjects(serverAliases);
				 */
        }

        ///<summary>
        ///  This method is to be overridden by the context implementation in the 
        ///  Server Implementation project.
        ///</summary>
        ///<param name = "listId">
        ///  The identifier for the list for which event message fields are being added. 
        ///</param>
        ///<param name = "categoryId">
        ///  The category for which event message fields are being added.
        ///</param>
        ///<param name = "fieldObjectTypeIds">
        ///  The list of category-specific fields to be included in the event 
        ///  messages generated for alarms and events of the category.  Each field 
        ///  is identified by its ObjectType LocalId obtained from the EventMessageFields 
        ///  contained in the EventCategoryConfigurations Standard MIB element.
        ///</param>
        ///<returns>
        ///  The ObjectTypeIds and result codes for the fields that could not be  
        ///  added to the event message. Returns null if all succeeded.  
        ///</returns>
        internal List<TypeIdResult> OnAddEventMessageFields(uint listId, uint categoryId,
                                                            List<TypeId> fieldObjectTypeIds)
        {
            TListRoot xiList = null;

            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                if (!_listManager.TryGetValue(listId, out xiList))
                    throw RpcExceptionHelper.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Add Event Message Fields.");
            }

            return xiList.OnAddEventMessageFields(categoryId, fieldObjectTypeIds);
        }

        /// <summary>
        ///   This method is to be overridden by the context implementation in the 
        ///   Server Implementation project.
        /// </summary>
        /// <param name = "listId">
        ///   The identifier for the lists whose data objects are to be touched.
        /// </param>
        /// <param name = "serverAliases">
        ///   The aliases for the data objects to touch.
        /// </param>
        /// <returns>
        ///   The list of aliases whose touch failed and the result code that 
        ///   indicates why it failed.  Null if all succeeded.
        /// </returns>
        internal List<AliasResult> OnTouchDataObjects(uint listId, List<uint> serverAliases)
        {
            TListRoot xiList = null;

            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                if (!_listManager.TryGetValue(listId, out xiList))
                    throw RpcExceptionHelper.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Touch.");
            }

            return xiList.OnTouchDataObjects(serverAliases);
        }

        /// <summary>
        ///   This method is to be overridden by the context implementation in the 
        ///   Server Implementation project.
        /// </summary>
        /// <param name = "listId">
        ///   The identifier for the list whose data objects are to be touched.
        /// </param>
        internal uint OnTouchList(uint listId)
        {
            TListRoot xiList = null;

            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                if (!_listManager.TryGetValue(listId, out xiList))
                    throw RpcExceptionHelper.Create(XiFaultCodes.E_BADLISTID, "List Id not found in Touch All.");
            }

            return xiList.OnTouchList();
        }

        #endregion
    }
}