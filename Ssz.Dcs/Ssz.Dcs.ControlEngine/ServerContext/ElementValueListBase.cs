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

namespace Ssz.Dcs.ControlEngine
{   
    public abstract class ElementValueListBase<TElementListItem> : ElementListBase<TElementListItem>
        where TElementListItem : ElementValueListItemBase
    {
        #region construction and destruction

        /// <summary>
        ///   Constructs a new instance of the <see cref = "ElementListBase" /> class.
        /// </summary>
        public ElementValueListBase(ServerContext serverContext, uint listClientAlias, CaseInsensitiveDictionary<string?> listParams)
            : base(serverContext, listClientAlias, listParams)
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

        public override ServerContext.ElementValuesCallbackMessage? GetElementValuesCallbackMessage()
        {
            ServerContext.ElementValuesCallbackMessage? result = null;

            foreach (ElementValueListItemBase item in ListItemsManager)
            {
                if (item.Changed)
                {
                    if (result is null)
                    {
                        result = new ServerContext.ElementValuesCallbackMessage
                        {
                            ListClientAlias = ListClientAlias
                        };
                    }

                    result.ElementValues[item.ClientAlias] = item.ValueStatusTimestamp;

                    item.Changed = false;
                }
            }

            return result;
        }

        /// <summary>
        ///     Returns failed AliasResults only.
        /// </summary>
        /// <param name="elementValuesCollection"></param>
        /// <returns></returns>
        public override async Task<List<AliasResult>> WriteElementValuesAsync(ElementValuesCollection elementValuesCollection)
        {
            if (elementValuesCollection.Guid != @"" && _incompleteElementValuesCollectionToWrite.Count > 0)
            {
                var beginElementValuesCollection = _incompleteElementValuesCollectionToWrite.TryGetValue(elementValuesCollection.Guid);
                if (beginElementValuesCollection is not null)
                {
                    _incompleteElementValuesCollectionToWrite.Remove(elementValuesCollection.Guid);
                    beginElementValuesCollection.CombineWith(elementValuesCollection);
                    elementValuesCollection = beginElementValuesCollection;
                }
            }

            if (elementValuesCollection.NextCollectionGuid != @"")
            {
                _incompleteElementValuesCollectionToWrite[elementValuesCollection.NextCollectionGuid] = elementValuesCollection;

                return new List<AliasResult>();
            }
            else
            {
                var changedListItems = new List<TElementListItem>();

                for (int index = 0; index < elementValuesCollection.DoubleAliases.Count; index++)
                {
                    TElementListItem? item;
                    ListItemsManager.TryGetValue(elementValuesCollection.DoubleAliases[index], out item);
                    if (item is not null)
                    {
                        item.PendingWriteValueStatusTimestamp = new ValueStatusTimestamp
                            {
                                Value = AnyHelper.GetAny(elementValuesCollection.DoubleValues[index], (Ssz.Utils.Any.TypeCode)elementValuesCollection.DoubleValueTypeCodes[index], false),
                                StatusCode = elementValuesCollection.DoubleStatusCodes[index],
                                TimestampUtc = elementValuesCollection.DoubleTimestamps[index].ToDateTime()
                            };
                        changedListItems.Add(item);
                    }
                }
                for (int index = 0; index < elementValuesCollection.UintAliases.Count; index++)
                {
                    TElementListItem? item;
                    ListItemsManager.TryGetValue(elementValuesCollection.UintAliases[index], out item);
                    if (item is not null)
                    {
                        item.PendingWriteValueStatusTimestamp = new ValueStatusTimestamp
                        {
                            Value = AnyHelper.GetAny(elementValuesCollection.UintValues[index], (Ssz.Utils.Any.TypeCode)elementValuesCollection.UintValueTypeCodes[index], false),
                            StatusCode = elementValuesCollection.UintStatusCodes[index],
                            TimestampUtc = elementValuesCollection.UintTimestamps[index].ToDateTime()
                        };
                        changedListItems.Add(item);
                    }
                }
                if (elementValuesCollection.ObjectAliases.Count > 0)
                {
                    using (var memoryStream = new MemoryStream(elementValuesCollection.ObjectValues.ToByteArray()))
                    using (var reader = new SerializationReader(memoryStream))
                    {
                        for (int index = 0; index < elementValuesCollection.ObjectAliases.Count; index++)
                        {
                            object? objectValue = reader.ReadObject();
                            TElementListItem? item;
                            ListItemsManager.TryGetValue(elementValuesCollection.ObjectAliases[index], out item);
                            if (item is not null)
                            {
                                item.PendingWriteValueStatusTimestamp = new ValueStatusTimestamp
                                {
                                    Value = new Any(objectValue),
                                    StatusCode = elementValuesCollection.ObjectStatusCodes[index],
                                    TimestampUtc = elementValuesCollection.ObjectTimestamps[index].ToDateTime()
                                };
                                changedListItems.Add(item);
                            }
                        }
                    }

                }

                return await OnWriteValuesAsync(changedListItems);
            }
        }

        #endregion

        #region protected functions        

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

        #region private fields        

        private CaseInsensitiveDictionary<ElementValuesCollection> _incompleteElementValuesCollectionToWrite = new CaseInsensitiveDictionary<ElementValuesCollection>();

        #endregion
    }
}
