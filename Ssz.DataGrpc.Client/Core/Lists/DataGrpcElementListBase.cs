using System;
using System.Collections.Generic;
using Ssz.Utils;
using Ssz.DataGrpc.Client.Core.Context;
using Ssz.DataGrpc.Client.Core.ListItems;
using Ssz.DataGrpc.Server;
using Ssz.DataGrpc.Common;

namespace Ssz.DataGrpc.Client.Core.Lists
{
    /// <summary>
    ///     This abstract class definition allows for the implantation of methods that are
    ///     common to two or more DataGrpc List types.  The DataGrpc Values maintained by this class
    ///     must be a subclass of DataGrpc Value Base.  In general the only time a declaration
    ///     of this type would be used is when the data type can also be processed
    ///     as being of type DataGrpc Value Base.
    /// </summary>
    /// <typeparam name="TDataGrpcElementListItemBase"> The DataGrpc Value type for this DataGrpc List. </typeparam>
    public abstract class DataGrpcElementListBase<TDataGrpcElementListItemBase> : DataGrpcListRoot
        where TDataGrpcElementListItemBase : DataGrpcElementListItemBase
    {
        #region construction and destruction

        /// <summary>
        ///     DataGrpc List Base is the common base class for all DataGrpc Lists defined within
        ///     the Client Base Assembly.
        /// </summary>
        /// <param name="context"> </param>
        protected DataGrpcElementListBase(DataGrpcContext context)
            : base(context)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                // Release and Dispose managed resources.

                foreach (TDataGrpcElementListItemBase listItem in ListItemsManager)
                {
                    listItem.ClientAlias = 0;
                    listItem.IsInClientList = false;
                    listItem.IsInServerList = false;
                    listItem.Dispose();
                }
            }            

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public abstract TDataGrpcElementListItemBase PrepareAddItem(string elementId);

        public abstract IEnumerable<TDataGrpcElementListItemBase>? CommitAddItems();

        public abstract IEnumerable<TDataGrpcElementListItemBase>? CommitRemoveItems();

        /// <summary>
        ///     This method returns data objects selected from the list by the match predicate.
        /// </summary>
        /// <param name="match"> The predicate that searches the list for matches against ElementValueListItemBase properties. </param>
        /// <returns> Returns data objects selected by the match predicate. </returns>
        public TDataGrpcElementListItemBase? Find(Predicate<TDataGrpcElementListItemBase> match)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcAndElementValueJournalListBase.");

            foreach (TDataGrpcElementListItemBase listItem in ListItemsManager)
            {
                if (match(listItem)) return listItem;
            }

            return null;
        }

        #endregion        

        #region protected functions

        /// <summary>
        ///     This method requests the server to add elements to the list that have been added to the local ClientBase copy
        ///     of the list. For example, after using the AddNewDataObjectToList() method add a set of data objects to the local
        ///     ClientBase copy of the list, this method is called to add them to the server's copy of the list in a single call.
        /// </summary>
        /// <returns> The list of elements that were not added to the server or null is call to server failed.</returns>
        protected IEnumerable<TDataGrpcElementListItemBase>? CommitAddItemsInternal()
        {
            var listInstanceIdsCollection = new List<ListInstanceId>();

            foreach (TDataGrpcElementListItemBase listItem in ListItemsManager)
            {
                if (!listItem.IsInServerList && listItem.PreparedForAdd)
                {
                    var listInstanceId = new ListInstanceId
                    {
                        ElementId = listItem.ElementId,
                        ClientAlias = listItem.ClientAlias,
                    };
                    listInstanceIdsCollection.Add(listInstanceId);
                }
                listItem.PreparedForAdd = false;
            }

            var resultItems = new List<TDataGrpcElementListItemBase>();

            if (listInstanceIdsCollection.Count > 0)
            {
                try
                {
                    List<AddDataObjectToListResult> result = Context.AddDataObjectsToList(ListServerAlias,
                        listInstanceIdsCollection);
                    
                    foreach (AddDataObjectToListResult r in result)
                    {
                        TDataGrpcElementListItemBase? listItem = null;
                        if (ListItemsManager.TryGetValue(r.AliasResult.ClientAlias, out listItem))
                        {
                            listItem.ServerAlias = r.AliasResult.ServerAlias;
                            listItem.ResultCode = r.AliasResult.ResultCode;
                            listItem.ValueTypeId = r.DataTypeId;
                            listItem.IsReadable = r.IsReadable;
                            listItem.IsWritable = r.IsWritable;

                            if (listItem.ResultCode == DataGrpcFaultCodes.S_OK || listItem.ResultCode == DataGrpcFaultCodes.S_FALSE)
                            {
                                listItem.IsInServerList = true;                                
                            }
                            else
                            {
                                ListItemsManager.Remove(listItem.ClientAlias);
                                    // remove values that the server failed to add
                                listItem.ClientAlias = 0;
                                listItem.IsInClientList = false;
                                resultItems.Add(listItem);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    foreach (ListInstanceId ar in listInstanceIdsCollection)
                    {
                        ListItemsManager.Remove(ar.ClientAlias); // remove values that the server failed to add                                            
                    }
                    return null;
                }
            }

            return resultItems;
        }

        /// <summary>
        ///     <para>
        ///         This method requests the server to remove elements from the list. The elements to be removed are those that
        ///         have been tagged for removal by the IDataGrpcValue PrepForRemove() method. The PrepForRemove() is called individually
        ///         on each list element to be removed, and followed by the CommitRemoveableElements().
        ///     </para>
        ///     <para>
        ///         The CommitRemoveableElements() method loops through the list to find the elements that have been prepared
        ///         for removal and makes a single call to the server to have them removed from the server's list.
        ///     </para>
        /// </summary>
        /// <returns> The list of elements that could not be removed from the server list or null is call to server failed.</returns>
        protected IEnumerable<TDataGrpcElementListItemBase>? CommitRemoveItemsInternal()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcAndElementValueJournalListBase.");

            // find all values in the client's list that have been prep'd for removal
            // and add them to the serverAliasesToRemove list and to the 
            // clientValuesToRemove list
            var serverAliasesToRemove = new List<uint>();
            var listItemsToRemove = new List<TDataGrpcElementListItemBase>();

            foreach (TDataGrpcElementListItemBase listItem in ListItemsManager)
            {
                if (listItem.IsInServerList && listItem.PreparedForRemove)
                {
                    listItemsToRemove.Add(listItem);
                    serverAliasesToRemove.Add(listItem.ServerAlias);
                }
                listItem.PreparedForRemove = false;
            }

            var erroredDataGrpcValuesToReturn = new List<TDataGrpcElementListItemBase>();
            if (listItemsToRemove.Count > 0)
            {
                try
                {
                    // Remove the items from the list in the server
                    List<AliasResult>? aliasResultList = null;
                    // a null list means all were successfully removed or are no longer defined in the server
                    if (Context.ServerContextIsOperational) // if still connected to the server
                        aliasResultList = Context.RemoveDataObjectsFromList(ListServerAlias, serverAliasesToRemove);

                    // Remove each value from the client list unless there was an error and it could not be removed
                    // if there were errors and the value could not be removed from the server

                    if (aliasResultList != null && aliasResultList.Count > 0)
                    {
                        foreach (TDataGrpcElementListItemBase removedListItem in listItemsToRemove)
                        {
                            // look for the server alias since if the server did not find it, it will not return a client alias
                            AliasResult? aliasResult =
                                aliasResultList.Find(ar => ar.ServerAlias == removedListItem.ServerAlias);
                            if (aliasResult != null)
                            {
                                if (aliasResult.ResultCode == DataGrpcFaultCodes.E_ALIASNOTFOUND)
                                {
                                    // server doesn't have the item if result code is E_ALIASNOTFOUND, so ok to take it out here
                                    ListItemsManager.Remove(removedListItem.ClientAlias);
                                    removedListItem.IsInServerList = false;
                                    removedListItem.ClientAlias = 0;
                                    removedListItem.IsInClientList = false;
                                    removedListItem.Dispose();
                                }
                                else
                                    // otherwise the value was not deleted from the server, so add it to the list to return
                                {
                                    removedListItem.ResultCode = aliasResult.ResultCode;
                                    erroredDataGrpcValuesToReturn.Add(removedListItem);
                                }
                            }
                            else // no error for this one, so remove it
                            {
                                ListItemsManager.Remove(removedListItem.ClientAlias);
                                removedListItem.IsInServerList = false;
                                removedListItem.ClientAlias = 0;
                                removedListItem.IsInClientList = false;
                                removedListItem.Dispose();
                            }
                        }
                    }
                    else // Otherwise, no errors, so remove them all from the client list
                    {
                        foreach (TDataGrpcElementListItemBase listItem in listItemsToRemove)
                        {
                            ListItemsManager.Remove(listItem.ClientAlias);
                            listItem.IsInServerList = false;
                            listItem.ClientAlias = 0;
                            listItem.IsInClientList = false;
                            listItem.Dispose();
                        }
                    }
                }
                catch (Exception)
                {
                    foreach (TDataGrpcElementListItemBase listItem in listItemsToRemove)
                    {
                        ListItemsManager.Remove(listItem.ClientAlias);
                        listItem.IsInServerList = false;
                        listItem.ClientAlias = 0;
                        listItem.IsInClientList = false;
                        listItem.Dispose();
                    }
                    return null;
                }
            }
            return erroredDataGrpcValuesToReturn;
        }

        /// <summary>
        ///     This KeyedCollection holds the collection of DataGrpc...ListValue instances,
        ///     where the type is dependent on the type of list.  The clientListId for this
        ///     KeyedCollection is the ClientAlias and is a property of ElementValueListItemBase.
        /// </summary>
        protected ObjectManager<TDataGrpcElementListItemBase> ListItemsManager
        {
            get { return _listItemsManager; }
        }

        #endregion

        #region private fields

        /// <summary>
        ///     This KeyedCollection holds the collection of DataGrpc...ListValue instances,
        ///     where the type is dependent on the type of list.  The clientListId for this
        ///     KeyedCollection is the ClientAlias and is a property of ElementValueListItemBase.
        /// </summary>
        private readonly ObjectManager<TDataGrpcElementListItemBase> _listItemsManager =
            new ObjectManager<TDataGrpcElementListItemBase>(256);

        #endregion
    }
}