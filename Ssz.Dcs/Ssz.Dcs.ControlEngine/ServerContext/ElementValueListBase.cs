using Ssz.Dcs.ControlEngine.ServerListItems;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils.DataAccess;
using Ssz.Utils;
using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AliasResult = Ssz.Utils.DataAccess.AliasResult;

namespace Ssz.Dcs.ControlEngine
{   
    public abstract class ElementValueListBase<TElementListItem> : ElementListBase<TElementListItem>
        where TElementListItem : ElementValueListItemBase
    {
        #region construction and destruction

        /// <summary>
        ///   Constructs a new instance of the <see cref = "ElementListBase" /> class.
        /// </summary>
        public ElementValueListBase(ServerWorkerBase serverWorker, ServerContext serverContext, uint listClientAlias, CaseInsensitiveDictionary<string?> listParams)
            : base(serverWorker, serverContext, listClientAlias, listParams)
        {
            string? updateRate = listParams.TryGetValue("UpdateRateMs");
            if (updateRate is not null)
            {
                UpdateRateMs = (uint)(new Any(updateRate).ValueAsInt32(false));
            }
        }

        #endregion

        #region public functions

        public uint UpdateRateMs { get; set; }

        public override void TouchList()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed DataList.");

            foreach (TElementListItem item in ListItemsManager)
            {
                item.Touch();
            }
        }

        public override void EnableListCallback(bool enable)
        {
            if (ListCallbackIsEnabled == enable) return;

            ListCallbackIsEnabled = enable;
            if (ListCallbackIsEnabled)
            {
                TouchList();
            }            
        }

        public override ElementValuesCallbackMessage? GetElementValuesCallbackMessage()
        {
            ElementValuesCallbackMessage? result = null;

            foreach (ElementValueListItemBase item in ListItemsManager)
            {
                if (item.Changed)
                {
                    if (result is null)
                    {
                        result = new ElementValuesCallbackMessage
                        {
                            ListClientAlias = ListClientAlias
                        };
                    }

                    result.ElementValues.Add((item.ClientAlias, item.ValueStatusTimestamp));

                    item.Changed = false;
                }
            }

            return result;
        }

        #endregion

        #region protected functions   

        protected override async Task<List<AliasResult>> WriteElementValuesAsync(List<(uint, ValueStatusTimestamp)> elementValuesCollection)
        {
            var items = new List<TElementListItem>();

            foreach (var it in elementValuesCollection)
            {
                ListItemsManager.TryGetValue(it.Item1, out TElementListItem? item);
                if (item is not null)
                {
                    item.PendingWriteValueStatusTimestamp = it.Item2;
                    items.Add(item);
                }
            }

            return await OnWriteValuesAsync(items);
        }

        /// <summary>
        ///     Returns failed AliasResults only.
        /// </summary>
        /// <param name="changedListItems"></param>
        /// <returns></returns>
        protected virtual Task<List<AliasResult>> OnWriteValuesAsync(List<TElementListItem> changedListItems)
        {
            return Task.FromResult(new List<AliasResult>());
        }

        #endregion        
    }
}
