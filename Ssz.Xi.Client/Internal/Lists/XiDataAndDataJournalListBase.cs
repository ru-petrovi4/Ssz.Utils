using System;
using System.Collections.Generic;
using Ssz.Utils;
using Ssz.Xi.Client.Internal.Context;
using Ssz.Xi.Client.Internal.ListItems;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Internal.Lists
{
    /// <summary>
    ///     This abstract class definition allows for the implantation of methods that are
    ///     common to two or more Xi List types.  The Xi Values maintained by this class
    ///     must be a subclass of Xi Value Base.  In general the only time a declaration
    ///     of this type would be used is when the data type can also be processed
    ///     as being of type Xi Value Base.
    /// </summary>
    /// <typeparam name="TXiDataAndDataJournalListItemBase"> The Xi Value type for this Xi List. </typeparam>
    internal abstract class XiDataAndDataJournalListBase<TXiDataAndDataJournalListItemBase> : XiListRoot
        where TXiDataAndDataJournalListItemBase : XiDataAndDataJournalListItemBase
    {
        #region construction and destruction

        /// <summary>
        ///     Xi List Base is the common base class for all Xi Lists defined within
        ///     the Client Base Assembly.
        /// </summary>
        /// <param name="context"> </param>
        protected internal XiDataAndDataJournalListBase(XiContext context)
            : base(context)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                // Release and Dispose managed resources.

                foreach (TXiDataAndDataJournalListItemBase listItem in ListItemsManager)
                {
                    listItem.ClientAlias = 0; // IsInClientList becomes false
                    listItem.IsInServerList = false;
                    listItem.Dispose();
                }
            }            

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        /// <summary>
        ///     This method returns data objects selected from the list by the match predicate.
        /// </summary>
        /// <param name="match"> The predicate that searches the list for matches against DataListItemBase properties. </param>
        /// <returns> Returns data objects selected by the match predicate. </returns>
        public TXiDataAndDataJournalListItemBase? Find(Predicate<TXiDataAndDataJournalListItemBase> match)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiDataAndDataJournalListBase.");

            foreach (TXiDataAndDataJournalListItemBase listItem in ListItemsManager)
            {
                if (match(listItem)) return listItem;
            }

            return null;
        }

        #endregion

        /*
        public void Func()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiDataListBase.");
        }*/

        #region protected functions

        /// <summary>
        ///     This method requests the server to add elements to the list that have been added to the local ClientBase copy
        ///     of the list. For example, after using the AddNewDataObjectToList() method add a set of data objects to the local
        ///     ClientBase copy of the list, this method is called to add them to the server's copy of the list in a single call.
        /// </summary>
        /// <returns> The list of elements that were not added to the server or null is call to server failed.</returns>
        protected IEnumerable<TXiDataAndDataJournalListItemBase>? CommitAddItemsInternal()
        {
            var listInstanceIdsCollection = new List<ListInstanceId>();

            foreach (TXiDataAndDataJournalListItemBase listItem in ListItemsManager)
            {
                if (!listItem.IsInServerList && listItem.PreparedForAdd)
                {
                    var listInstanceId = new ListInstanceId
                    {
                        ObjectElementId = listItem.InstanceId,
                        ClientAlias = listItem.ClientAlias,
                    };
                    listInstanceIdsCollection.Add(listInstanceId);
                }
                listItem.PreparedForAdd = false;
            }

            var resultItems = new List<TXiDataAndDataJournalListItemBase>();

            if (listInstanceIdsCollection.Count > 0)
            {
                try
                {
                    List<AddDataObjectResult>? result = Context.AddDataObjectsToList(ServerListId,
                        listInstanceIdsCollection);

                    if (result is not null)
                    foreach (AddDataObjectResult r in result)
                    {
                        TXiDataAndDataJournalListItemBase? listItem = null;
                        if (ListItemsManager.TryGetValue(r.ClientAlias, out listItem))
                        {
                            listItem.ServerAlias = r.ServerAlias;
                            listItem.ResultCode = r.Result;
                            listItem.ValueTypeId = r.DataTypeId;
                            
                            listItem.IsReadable = true;
                            listItem.IsWritable = true;

                            if (listItem.ResultCode == XiFaultCodes.S_OK || listItem.ResultCode == XiFaultCodes.S_FALSE)
                            {
                                listItem.IsInServerList = true;
                                listItem.Enabled = false;
                            }
                            else
                            {
                                ListItemsManager.Remove(listItem.ClientAlias);
                                    // remove values that the server failed to add
                                listItem.ClientAlias = 0; //IsInClientList = false;
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
                        ar.ClientAlias = 0; //IsInClientList = false;
                    }
                    return null;
                }

                GetListAttributes();
            }

            return resultItems;
        }

        /// <summary>
        ///     <para>
        ///         This method requests the server to remove elements from the list. The elements to be removed are those that
        ///         have been tagged for removal by the IXiValue PrepForRemove() method. The PrepForRemove() is called individually
        ///         on each list element to be removed, and followed by the CommitRemoveableElements().
        ///     </para>
        ///     <para>
        ///         The CommitRemoveableElements() method loops through the list to find the elements that have been prepared
        ///         for removal and makes a single call to the server to have them removed from the server's list.
        ///     </para>
        /// </summary>
        /// <returns> The list of elements that could not be removed from the server list or null is call to server failed.</returns>
        protected IEnumerable<TXiDataAndDataJournalListItemBase>? CommitRemoveItemsInternal()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiDataAndDataJournalListBase.");

            // find all values in the client's list that have been prep'd for removal
            // and add them to the serverAliasesToRemove list and to the 
            // clientValuesToRemove list
            var serverAliasesToRemove = new List<uint>();
            var listItemsToRemove = new List<TXiDataAndDataJournalListItemBase>();

            foreach (TXiDataAndDataJournalListItemBase listItem in ListItemsManager)
            {
                if (listItem.IsInServerList && listItem.PreparedForRemove)
                {
                    listItemsToRemove.Add(listItem);
                    serverAliasesToRemove.Add(listItem.ServerAlias);
                }
                listItem.PreparedForRemove = false;
            }

            var erroredXiValuesToReturn = new List<TXiDataAndDataJournalListItemBase>();
            if (listItemsToRemove.Count > 0)
            {
                try
                {
                    // Remove the items from the list in the server
                    List<AliasResult>? aliasResultList = null;
                    // a null list means all were successfully removed or are no longer defined in the server
                    if (Context.ResourceManagement is not null) // if still connected to the server
                        aliasResultList = Context.RemoveDataObjectsFromList(ServerListId, serverAliasesToRemove);


                    // Remove each value from the client list unless there was an error and it could not be removed
                    // if there were errors and the value could not be removed from the server

                    if (aliasResultList is not null && aliasResultList.Count > 0)
                    {
                        foreach (TXiDataAndDataJournalListItemBase removedListItem in listItemsToRemove)
                        {
                            // look for the server alias since if the server did not find it, it will not return a client alias
                            AliasResult? aliasResult =
                                aliasResultList.Find(ar => ar.ServerAlias == removedListItem.ServerAlias);
                            if (aliasResult is not null)
                            {
                                if (aliasResult.Result == XiFaultCodes.E_ALIASNOTFOUND)
                                {
                                    // server doesn't have the item if result code is E_ALIASNOTFOUND, so ok to take it out here
                                    ListItemsManager.Remove(removedListItem.ClientAlias);
                                    removedListItem.IsInServerList = false;
                                    removedListItem.ClientAlias = 0; //IsInClientList = false;
                                    removedListItem.Dispose();
                                }
                                else
                                    // otherwise the value was not deleted from the server, so add it to the list to return
                                {
                                    removedListItem.ResultCode = aliasResult.Result;
                                    erroredXiValuesToReturn.Add(removedListItem);
                                }
                            }
                            else // no error for this one, so remove it
                            {
                                ListItemsManager.Remove(removedListItem.ClientAlias);
                                removedListItem.IsInServerList = false;
                                removedListItem.ClientAlias = 0; //IsInClientList = false;
                                removedListItem.Dispose();
                            }
                        }
                    }
                    else // Otherwise, no errors, so remove them all from the client list
                    {
                        foreach (TXiDataAndDataJournalListItemBase listItem in listItemsToRemove)
                        {
                            ListItemsManager.Remove(listItem.ClientAlias);
                            listItem.IsInServerList = false;
                            listItem.ClientAlias = 0; //IsInClientList = false;
                            listItem.Dispose();
                        }
                    }
                }
                catch (Exception)
                {
                    foreach (TXiDataAndDataJournalListItemBase listItem in listItemsToRemove)
                    {
                        ListItemsManager.Remove(listItem.ClientAlias);
                        listItem.IsInServerList = false;
                        listItem.ClientAlias = 0; //IsInClientList = false;
                        listItem.Dispose();
                    }
                    return null;
                }

                GetListAttributes();
            }
            return erroredXiValuesToReturn;
        }

        /*
        /// <summary>
        ///   <para> This method is used to cause one or more data objects of a list to be "touched". Data objects that are in the disabled state (see the EnableListElementUpdating() method) are not affected by this method. This method cannot be used with event lists. </para>
        ///   <para> Touching an enabled data object causes the server to update the data object, mark it as changed (even if their values did not change), and then return it to the client in the next callback or poll. </para>
        /// </summary>
        /// <param name="dataObjects"> The data objects to touch. </param>
        /// <returns> </returns>
        public IEnumerable<AliasResult> TouchDataObjects(List<TXiDataAndDataJournalListItemBase> dataObjects)
        {
            var serverAliases = new List<uint>();
            foreach (XiDataAndDataJournalListItemBase xiValue in dataObjects)
            {
                serverAliases.Add(xiValue.ServerAlias);
            }
            return Context.TouchDataObjects(ServerListId, serverAliases);
        }
         */

        /// <summary>
        ///     This KeyedCollection holds the collection of Xi...ListValue instances,
        ///     where the type is dependent on the type of list.  The clientListId for this
        ///     KeyedCollection is the ClientAlias and is a property of DataListItemBase.
        /// </summary>
        protected ObjectManager<TXiDataAndDataJournalListItemBase> ListItemsManager
        {
            get { return _listItemsManager; }
        }

        #endregion

        #region private fields

        /// <summary>
        ///     This KeyedCollection holds the collection of Xi...ListValue instances,
        ///     where the type is dependent on the type of list.  The clientListId for this
        ///     KeyedCollection is the ClientAlias and is a property of DataListItemBase.
        /// </summary>
        private readonly ObjectManager<TXiDataAndDataJournalListItemBase> _listItemsManager =
            new ObjectManager<TXiDataAndDataJournalListItemBase>(256);

        #endregion
    }
}