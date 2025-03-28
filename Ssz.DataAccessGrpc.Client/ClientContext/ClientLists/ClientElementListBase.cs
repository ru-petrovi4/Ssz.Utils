using System;
using System.Collections.Generic;
using Ssz.Utils;
using Ssz.DataAccessGrpc.Client.ClientListItems;
using Ssz.DataAccessGrpc.Common;
using Ssz.Utils.DataAccess;
using Grpc.Core;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.Client.ClientLists
{
    /// <summary>
    ///     This abstract class definition allows for the implantation of methods that are
    ///     common to two or more DataAccessGrpc List types.  The DataAccessGrpc Values maintained by this class
    ///     must be a subclass of DataAccessGrpc Value Base.  In general the only time a declaration
    ///     of this type would be used is when the data type can also be processed
    ///     as being of type DataAccessGrpc Value Base.
    /// </summary>
    /// <typeparam name="TClientElementListItemBase"> The DataAccessGrpc Value type for this DataAccessGrpc List. </typeparam>
    internal abstract class ClientElementListBase<TClientElementListItemBase> : ClientListRoot
        where TClientElementListItemBase : ClientElementListItemBase
    {
        #region construction and destruction

        /// <summary>
        ///     DataAccessGrpc List Base is the common base class for all DataAccessGrpc Lists defined within
        ///     the Client Base Assembly.
        /// </summary>
        /// <param name="context"> </param>
        protected ClientElementListBase(ClientContext context)
            : base(context)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                // Release and Dispose managed resources.

                foreach (TClientElementListItemBase listItem in ListItemsManager)
                {
                    listItem.ClientAlias = 0;
                    listItem.IsInClientList = false;
                    listItem.IsInServerList = false;                    
                }
            }            

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public abstract TClientElementListItemBase PrepareAddItem(string elementId);

        /// <summary>
        ///     Returns failed items only.
        /// </summary>
        /// <returns></returns>
        public abstract Task<IEnumerable<TClientElementListItemBase>?> CommitAddItemsAsync();

        public abstract Task<IEnumerable<TClientElementListItemBase>?> CommitRemoveItemsAsync();

        /// <summary>
        ///     This method returns data objects selected from the list by the match predicate.
        /// </summary>
        /// <param name="match"> The predicate that searches the list for matches against ElementValueListItemBase properties. </param>
        /// <returns> Returns data objects selected by the match predicate. </returns>
        public TClientElementListItemBase? Find(Predicate<TClientElementListItemBase> match)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed DataAccessGrpcAndElementValuesJournalListBase.");

            foreach (TClientElementListItemBase listItem in ListItemsManager)
            {
                if (match(listItem)) return listItem;
            }

            return null;
        }

        #endregion        

        #region protected functions

        /// <summary>
        ///     Returns failed items only.
        /// </summary>
        /// <returns></returns>
        protected async Task<IEnumerable<TClientElementListItemBase>?> CommitAddItemsInternalAsync()
        {
            var listInstanceIdsCollection = new List<Common.ListItemInfo>();

            foreach (TClientElementListItemBase listItem in ListItemsManager)
            {
                if (!listItem.IsInServerList && listItem.PreparedForAdd)
                {
                    var listInstanceId = new Common.ListItemInfo
                    {
                        ElementId = listItem.ElementId,
                        ClientAlias = listItem.ClientAlias,
                    };
                    listInstanceIdsCollection.Add(listInstanceId);
                }
                listItem.PreparedForAdd = false;
            }

            var failedItems = new List<TClientElementListItemBase>();

            if (listInstanceIdsCollection.Count > 0)
            {
                try
                {
                    List<Common.AliasResult> result = await Context.AddItemsToListAsync(ListServerAlias,
                        listInstanceIdsCollection);
                    
                    foreach (Common.AliasResult r in result)
                    {
                        TClientElementListItemBase? listItem = null;
                        if (ListItemsManager.TryGetValue(r.ClientAlias, out listItem))
                        {
                            listItem.ServerAlias = r.ServerAlias;
                            listItem.AddItemResultInfo = r.GetResultInfo();
                            if (StatusCodes.IsGood(r.StatusCode))
                            {
                                listItem.IsInServerList = true;                                
                            }
                            else
                            {
                                ListItemsManager.Remove(listItem.ClientAlias);
                                    // remove values that the server failed to add
                                listItem.ClientAlias = 0;
                                listItem.IsInClientList = false;
                                failedItems.Add(listItem);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    foreach (Common.ListItemInfo ar in listInstanceIdsCollection)
                    {
                        ListItemsManager.Remove(ar.ClientAlias); // remove values that the server failed to add                                            
                    }
                    return null;
                }
            }

            return failedItems;
        }

        /// <summary>
        ///     <para>
        ///         This method requests the server to remove elements from the list. The elements to be removed are those that
        ///         have been tagged for removal by the IDataAccessGrpcValue PrepForRemove() method. The PrepForRemove() is called individually
        ///         on each list element to be removed, and followed by the CommitRemoveableElements().
        ///     </para>
        ///     <para>
        ///         The CommitRemoveableElements() method loops through the list to find the elements that have been prepared
        ///         for removal and makes a single call to the server to have them removed from the server's list.
        ///     </para>
        /// </summary>
        /// <returns> The list of elements that could not be removed from the server list or null is call to server failed.</returns>
        protected async Task<IEnumerable<TClientElementListItemBase>?> CommitRemoveItemsInternalAsync()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed DataAccessGrpcAndElementValuesJournalListBase.");

            // find all values in the client's list that have been prep'd for removal
            // and add them to the serverAliasesToRemove list and to the 
            // clientValuesToRemove list
            var serverAliasesToRemove = new List<uint>();
            var listItemsToRemove = new List<TClientElementListItemBase>();

            foreach (TClientElementListItemBase listItem in ListItemsManager)
            {
                if (listItem.IsInServerList && listItem.PreparedForRemove)
                {
                    listItemsToRemove.Add(listItem);
                    serverAliasesToRemove.Add(listItem.ServerAlias);
                }
                listItem.PreparedForRemove = false;
            }

            var erroredDataAccessGrpcValuesToReturn = new List<TClientElementListItemBase>();
            if (listItemsToRemove.Count > 0)
            {
                try
                {
                    // Remove the items from the list in the server
                    List<Common.AliasResult>? aliasResultList = null;
                    // a null list means all were successfully removed or are no longer defined in the server
                    if (Context.ContextIsOperational) // if still connected to the server
                        aliasResultList = await Context.RemoveItemsFromListAsync(ListServerAlias, serverAliasesToRemove);

                    // Remove each value from the client list unless there was an error and it could not be removed
                    // if there were errors and the value could not be removed from the server

                    if (aliasResultList is not null && aliasResultList.Count > 0)
                    {
                        foreach (TClientElementListItemBase removedListItem in listItemsToRemove)
                        {
                            // look for the server alias since if the server did not find it, it will not return a client alias
                            Common.AliasResult? aliasResult =
                                aliasResultList.Find(ar => ar.ServerAlias == removedListItem.ServerAlias);
                            if (aliasResult is not null)
                            {
                                if ((StatusCode)aliasResult.StatusCode == StatusCode.NotFound)
                                {
                                    // server doesn't have the item if result code is E_ALIASNOTFOUND, so ok to take it out here
                                    ListItemsManager.Remove(removedListItem.ClientAlias);
                                    removedListItem.IsInServerList = false;
                                    removedListItem.ClientAlias = 0;
                                    removedListItem.IsInClientList = false;                                    
                                }
                                else                                    
                                {
                                    // otherwise the value was not deleted from the server, so add it to the list to return
                                    //removedListItem.AddItemStatusCode = aliasResult.StatusCode;
                                    erroredDataAccessGrpcValuesToReturn.Add(removedListItem);
                                }
                            }
                            else // no error for this one, so remove it
                            {
                                ListItemsManager.Remove(removedListItem.ClientAlias);
                                removedListItem.IsInServerList = false;
                                removedListItem.ClientAlias = 0;
                                removedListItem.IsInClientList = false;                                
                            }
                        }
                    }
                    else // Otherwise, no errors, so remove them all from the client list
                    {
                        foreach (TClientElementListItemBase listItem in listItemsToRemove)
                        {
                            ListItemsManager.Remove(listItem.ClientAlias);
                            listItem.IsInServerList = false;
                            listItem.ClientAlias = 0;
                            listItem.IsInClientList = false;                            
                        }
                    }
                }
                catch (Exception)
                {
                    foreach (TClientElementListItemBase listItem in listItemsToRemove)
                    {
                        ListItemsManager.Remove(listItem.ClientAlias);
                        listItem.IsInServerList = false;
                        listItem.ClientAlias = 0;
                        listItem.IsInClientList = false;                        
                    }
                    return null;
                }
            }
            return erroredDataAccessGrpcValuesToReturn;
        }

        /// <summary>
        ///     This KeyedCollection holds the collection of DataAccessGrpc...ListValue instances,
        ///     where the type is dependent on the type of list.  The clientListId for this
        ///     KeyedCollection is the ClientAlias and is a property of ElementValueListItemBase.
        /// </summary>
        protected ObjectManager<TClientElementListItemBase> ListItemsManager
        {
            get { return _listItemsManager; }
        }

        #endregion

        #region private fields

        /// <summary>
        ///     This KeyedCollection holds the collection of DataAccessGrpc...ListValue instances,
        ///     where the type is dependent on the type of list.  The clientListId for this
        ///     KeyedCollection is the ClientAlias and is a property of ElementValueListItemBase.
        /// </summary>
        private readonly ObjectManager<TClientElementListItemBase> _listItemsManager =
            new ObjectManager<TClientElementListItemBase>(256);

        #endregion
    }
}