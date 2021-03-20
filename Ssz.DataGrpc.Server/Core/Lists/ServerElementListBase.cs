using System;
using System.Collections.Generic;
using Ssz.DataGrpc.Server.Core.Context;
using Ssz.DataGrpc.Server.Core.ListItems;
using Xi.Common.Support;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Ssz.DataGrpc.Server.Core.Lists
{
    /// <summary>
    ///   This is the root or base class for all lists the report data values either current or historical.
    /// </summary>
    public abstract class ServerElementListBase<TListItemRoot> : ListBase<TListItemRoot>
        where TListItemRoot : ListItemRoot
    {
        #region construction and destruction

        /// <summary>
        ///   Constructs a new instance of the <see cref = "ElementListBase" /> class.
        /// </summary>
        public ElementListBase(ServerContext<ServerListRoot> context, uint clientId, uint updateRate,
                                          uint bufferingRate,
                                          uint listType, uint listKey, StandardMib mib)
            : base(context, clientId, updateRate, bufferingRate, listType, listKey, mib)
        {
        }

        #endregion

        /*
        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;
            if (disposing)
            {
                // Release and Dispose managed resources.
            }
            // Release unmanaged resources.
            // Set large fields to null.
            base.Dispose(disposing);
        }*/

        #region public functions

        /// <summary>
        ///   This method is invoked from Context Base (List Management) 
        ///   to Add Data objects To this List.
        /// </summary>
        /// <param name = "dataObjectsToAdd"></param>
        /// <returns></returns>
        public override List<AddDataObjectResult> OnAddDataObjectsToList(List<ListInstanceId> dataObjectsToAdd)
        {
            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed DataListRoot.");

                if ((dataObjectsToAdd == null) || (dataObjectsToAdd.Count == 0)) return new List<AddDataObjectResult>();

                var resultsList = new List<AddDataObjectResult>(dataObjectsToAdd.Count);

                ItemsBuffer.ClearAndSetCapacity(dataObjectsToAdd.Count);

                foreach (ListInstanceId id in dataObjectsToAdd)
                {
                    if (!InstanceId.IsValid(id.ObjectElementId))
                    {
                        resultsList.Add(new AddDataObjectResult(
                                            XiFaultCodes.E_BADARGUMENT, id.ClientAlias, 0,
                                            null,
                                            false,
                                            false));
                        continue;
                    }

                    TListItemRoot elementValueListItem = OnNewElementValueListItem(id.ClientAlias, 0, id.ObjectElementId);
                    UInt32 handle = Items.Add(elementValueListItem);
                    elementValueListItem.ServerAlias = handle;
                    ItemsBuffer.Add(elementValueListItem);
                }

                OnAddDataObjectsToList(ItemsBuffer, resultsList);

                foreach (AddDataObjectResult ir in resultsList)
                {
                    if (RpcExceptionHelper.Failed(ir.Result))
                    {
                        TListItemRoot item;
                        if (Items.TryGetValue(ir.ServerAlias, out item))
                        {
                            Items.Remove(item.ServerAlias);
                            item.Dispose();
                        }
                    }
                }

                ItemsBuffer.Clear();

                return resultsList;
            }
        }

        /// <summary>
        ///   This method is used to Remove Data Objects From this List.  
        ///   It is invoked from Context Base {List Management} Remove Data Object From List.
        /// </summary>
        /// <param name = "serverAliases"></param>
        /// <returns></returns>
        public override List<AliasResult> OnRemoveDataObjectsFromList(List<uint> serverAliases)
        {
            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed DataListRoot.");

                List<AliasResult> resultsList;

                if (serverAliases == null) // null means to delete all data objects from the list
                {
                    resultsList = new List<AliasResult>(Items.Count);

                    ItemsBuffer.ClearAndSetCapacity(Items.Count);
                    foreach (TListItemRoot item in Items)
                    {
                        ItemsBuffer.Add(item);
                    }
                }
                else
                {
                    resultsList = new List<AliasResult>(serverAliases.Count);

                    ItemsBuffer.ClearAndSetCapacity(serverAliases.Count);

                    foreach (uint serverAlias in serverAliases)
                    {
                        TListItemRoot item = null;
                        if (Items.TryGetValue(serverAlias, out item))
                        {
                            if (item is ElementValueListItem) ItemsBuffer.Add(item);
                        }
                        else
                        {
                            var aliasResult = new AliasResult(XiFaultCodes.E_ALIASNOTFOUND, 0, serverAlias);
                            resultsList.Add(aliasResult);
                        }
                    }
                }

                try
                {
                    OnRemoveDataObjectsFromList(ItemsBuffer, resultsList);
                }
                catch
                {
                }

                foreach (TListItemRoot dlv in ItemsBuffer)
                {
                    Items.Remove(dlv.ServerAlias);
                    dlv.Dispose();
                }

                ItemsBuffer.Clear();

                return resultsList;
            }
        }

        public override List<AliasResult> OnRenewAliases(List<AliasUpdate> newAliases)
        {
            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ListRoot.");

                var results = new List<AliasResult>();
                if (newAliases == null) return results;

                foreach (AliasUpdate aliasUpdate in newAliases)
                {
                    TListItemRoot item;
                    if (Items.TryGetValue(aliasUpdate.ExistingServerAlias, out item))
                        item.ClientAlias = aliasUpdate.NewClientAlias;
                    else
                    {
                        var aResult = new AliasResult(XiFaultCodes.E_ALIASNOTFOUND, aliasUpdate.NewClientAlias,
                                                      aliasUpdate.ExistingServerAlias);
                        results.Add(aResult);
                    }
                }

                return results;
            }
        }

        #endregion

        #region protected functions

        /// <summary>
        ///   Normally an override will be provided in the implementation 
        ///   subclass to add the Data List Value Base 
        ///   instance to the Data List.
        /// </summary>
        /// <param name = "itemsList"></param>
        /// <returns></returns>
        protected virtual void OnAddDataObjectsToList(List<TListItemRoot> itemsList,
                                                      List<AddDataObjectResult> resultsList)
        {
            // Note: _ListLock has been locked

            foreach (TListItemRoot dle in itemsList)
            {
                var result = new AddDataObjectResult(XiFaultCodes.S_OK, dle.ClientAlias, dle.ServerAlias, null, false,
                                                     false);
                resultsList.Add(result);
            }
        }

        /// <summary>
        ///   The implementation subclass provides the implementation of this abstract method 
        ///   to create / construct an instance of a subclass of Data List Item.
        /// </summary>
        /// <param name = "clientAlias"></param>
        /// <param name = "serverAlias"></param>
        /// <param name = "instanceId"></param>
        /// <returns></returns>
        protected abstract TListItemRoot OnNewElementValueListItem(uint clientAlias, uint serverAlias, InstanceId instanceId);

        /// <summary>
        ///   This method should be overridden in the implementation 
        ///   base class to take any actions needed to remove the 
        ///   specified Data List Value Base instances from the list.
        /// </summary>
        /// <param name = "listUintIdRes"></param>
        /// <param name = "_listValueRoot"></param>
        /// <returns></returns>
        protected virtual void OnRemoveDataObjectsFromList(List<TListItemRoot> listValueRoot,
                                                           List<AliasResult> resultsList)
        {
            // Note: SyncRoot has been locked            
        }

        /// <summary>
        ///   Returns a copy of current List Attributes for this List
        ///   Note: This property returns a copy of the List Attributes,
        ///   as the List Attributes can not be changed by simply writing
        ///   to the List Attributes class.  The List Attributes that can
        ///   be changed have methods associated to make those changes.
        /// </summary>
        protected override ListAttributes ListAttributesInternal
        {
            get
            {
                return new ListAttributes
                    {
                        ResultCode = XiFaultCodes.S_OK,
                        ClientId = ClientId,
                        ServerId = ServerId,
                        ListType = ListType,
                        Enabled = Enabled,
                        UpdateRate = UpdateRate,
                        CurrentCount = Items.Count,
                        HowSorted = 0,
                        SortKeys = null,
                        FilterSet = FilterSet_,
                    };
            }
        }

        #endregion
    }
}