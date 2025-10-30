using System;
using System.Collections.Generic;
using Ssz.Dcs.CentralServer.ServerListItems;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils.DataAccess;
using Ssz.Utils;
using Grpc.Core;
using System.Threading.Tasks;
using AliasResult = Ssz.Utils.DataAccess.AliasResult;
using ListItemInfo = Ssz.Utils.DataAccess.ListItemInfo;

namespace Ssz.Dcs.CentralServer
{
    /// <summary>
    ///   This is the root or base class for all lists the report data values either current or historical.
    /// </summary>
    public abstract class ElementListBase<TElementListItem> : ServerListRoot
        where TElementListItem : ElementListItemBase
    {
        #region construction and destruction
        
        public ElementListBase(DataAccessServerWorkerBase serverWorker, ServerContext serverContext, uint listClientAlias, CaseInsensitiveOrderedDictionary<string?> listParams)
            : base(serverWorker, serverContext, listClientAlias, listParams)
        {            
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                foreach (TElementListItem item in ListItemsManager)
                {
                    item.Dispose();
                }                
                ListItemsManager.Clear();
            }

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        /// <summary>
        ///   This method is invoked from Context Base (List Management) 
        ///   to Add Data objects To this List.
        /// </summary>
        /// <param name="itemsToAdd"></param>
        /// <returns></returns>
        public override Task<List<AliasResult>> AddItemsToListAsync(List<ListItemInfo> itemsToAdd)
        {
            if (Disposed) 
                throw new ObjectDisposedException("Cannot access a disposed DataListRoot.");

            if (itemsToAdd.Count == 0) 
                return Task.FromResult(new List<AliasResult>());

            var resultsList = new List<AliasResult>(itemsToAdd.Count);

            var elementListItems = new List<TElementListItem>(itemsToAdd.Count);

            foreach (ListItemInfo listItemInfo in itemsToAdd)
            {
                if (listItemInfo.ElementId == "")
                {
                    resultsList.Add(new AliasResult
                    {
                        StatusCode = (uint)StatusCode.InvalidArgument,
                        ClientAlias = listItemInfo.ClientAlias
                    });
                    continue;
                }

                TElementListItem elementListItem = OnNewElementListItem(listItemInfo.ClientAlias, 0, listItemInfo.ElementId);
                UInt32 handle = ListItemsManager.Add(elementListItem);
                elementListItem.ServerAlias = handle;
                elementListItems.Add(elementListItem);
            }

            resultsList.AddRange(OnAddElementListItemsToList(elementListItems));
            
            foreach (AliasResult r in resultsList)
            {
                if ((StatusCode)r.StatusCode != StatusCode.OK)
                {
                    TElementListItem? item;
                    if (ListItemsManager.TryGetValue(r.ServerAlias, out item))
                    {
                        ListItemsManager.Remove(item.ServerAlias);
                        item.Dispose();
                    }
                }
            }

            return Task.FromResult(resultsList);
        }

        /// <summary>
        ///     Returns failed AliasResults only.
        /// </summary>
        /// <param name="serverAliasesToRemove"></param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        public override Task<List<AliasResult>> RemoveItemsFromListAsync(List<uint> serverAliasesToRemove)
        {
            if (Disposed) 
                throw new ObjectDisposedException("Cannot access a disposed DataListRoot.");

            List<AliasResult> resultsList;

            resultsList = new List<AliasResult>(serverAliasesToRemove.Count);

            var elementListItems = new List<TElementListItem>(serverAliasesToRemove.Count);           

            foreach (uint serverAlias in serverAliasesToRemove)
            {
                TElementListItem? item = null;
                if (ListItemsManager.TryGetValue(serverAlias, out item))
                {
                    elementListItems.Add(item);
                }
                else
                {
                    var aliasResult = new AliasResult
                     {
                        StatusCode = (uint)StatusCode.NotFound,
                        ClientAlias = 0,
                        ServerAlias = serverAlias
                    };
                    resultsList.Add(aliasResult);
                }
            }

            try
            {
                resultsList.AddRange(OnRemoveElementListItemsFromList(elementListItems));
            }
            catch
            {
            }

            foreach (TElementListItem elementListItem in elementListItems)
            {
                ListItemsManager.Remove(elementListItem.ServerAlias);
                elementListItem.Dispose();
            }

            return Task.FromResult(resultsList);
        }

        #endregion

        #region protected functions

        protected ObjectManager<TElementListItem> ListItemsManager { get; } = new ObjectManager<TElementListItem>(100);

        protected abstract TElementListItem OnNewElementListItem(uint clientAlias, uint serverAlias, string elementId);

        /// <summary>
        ///     elementListItems.ElementId != String.Empty
        /// </summary>
        /// <param name="elementListItems"></param>        
        protected virtual List<AliasResult> OnAddElementListItemsToList(List<TElementListItem> elementListItems)
        {
            return new List<AliasResult>();
        }

        /// <summary>
        ///     Returns failed AliasResults only.
        /// </summary>
        /// <param name="elementListItems"></param>        
        protected virtual List<AliasResult> OnRemoveElementListItemsFromList(List<TElementListItem> elementListItems)
        {
            return new List<AliasResult>();
        }

        #endregion
    }
}