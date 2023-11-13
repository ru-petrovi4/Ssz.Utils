using Ssz.Dcs.CentralServer.ServerListItems;
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

namespace Ssz.Dcs.CentralServer
{   
    public abstract class ElementValueListBase<TElementListItem> : ElementListBase<TElementListItem>
        where TElementListItem : ElementValueListItemBase
    {
        #region construction and destruction
        
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
                var items = new List<TElementListItem>();

                for (int index = 0; index < elementValuesCollection.DoubleAliases.Count; index++)
                {
                    TElementListItem? item;
                    ListItemsManager.TryGetValue(elementValuesCollection.DoubleAliases[index], out item);
                    if (item is not null)
                    {
                        item.PendingWriteValueStatusTimestamp = new ValueStatusTimestamp
                            {
                                Value = new Any(elementValuesCollection.DoubleValues[index], (TypeCode)elementValuesCollection.DoubleValueTypeCodes[index], false),
                                ValueStatusCode = elementValuesCollection.DoubleValueStatusCodes[index],
                                TimestampUtc = elementValuesCollection.DoubleTimestamps[index].ToDateTime()
                            };
                        items.Add(item);
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
                            Value = new Any(elementValuesCollection.UintValues[index], (TypeCode)elementValuesCollection.UintValueTypeCodes[index], false),
                            ValueStatusCode = elementValuesCollection.UintValueStatusCodes[index],
                            TimestampUtc = elementValuesCollection.UintTimestamps[index].ToDateTime()
                        };
                        items.Add(item);
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
                                    ValueStatusCode = elementValuesCollection.ObjectValueStatusCodes[index],
                                    TimestampUtc = elementValuesCollection.ObjectTimestamps[index].ToDateTime()
                                };
                                items.Add(item);
                            }
                        }
                    }

                }

                return await OnWriteValuesAsync(items);
            }
        }

        #endregion

        #region protected functions        

        /// <summary>
        ///     Returns failed AliasResults only.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        protected virtual Task<List<AliasResult>> OnWriteValuesAsync(List<TElementListItem> items)
        {
            return Task.FromResult(new List<AliasResult>());
        }

        #endregion

        #region private fields

        private CaseInsensitiveDictionary<ElementValuesCollection> _incompleteElementValuesCollectionToWrite = new CaseInsensitiveDictionary<ElementValuesCollection>();

        #endregion
    }
}
