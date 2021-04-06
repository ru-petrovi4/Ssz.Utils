using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Utils;
using Ssz.DataGrpc.Client.ClientListItems;
using Ssz.DataGrpc.Server;
using Ssz.DataGrpc.Common;
using System.IO;
using Ssz.Utils.Serialization;
using Ssz.DataGrpc.Client.Data;
using Ssz.Utils.DataAccess;

namespace Ssz.DataGrpc.Client.ClientLists
{
    /// <summary>
    /// 
    /// </summary>
    public class ClientElementValueList : ClientElementListBase<ClientElementValueListItem>
    {
        #region construction and destruction

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="listParams"></param>
        public ClientElementValueList(ClientContext context, CaseInsensitiveDictionary<string>? listParams)
            : base(context)
        {
            ListType = (uint)StandardListType.ElementValueList;
            Context.DefineList(this, listParams);
        }

        #endregion

        #region public functions

        /// <summary>
        /// 
        /// </summary>
        /// <param name="elementId"></param>
        /// <returns></returns>
        public override ClientElementValueListItem PrepareAddItem(string elementId)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientElementValueList.");

            var dataListItem = new ClientElementValueListItem(elementId);
            dataListItem.ClientAlias = ListItemsManager.Add(dataListItem);
            dataListItem.IsInClientList = true;
            dataListItem.PreparedForAdd = true;
            return dataListItem;
        }

        public override IEnumerable<ClientElementValueListItem>? CommitAddItems()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientElementValueList.");

            return CommitAddItemsInternal();
        }

        public override IEnumerable<ClientElementValueListItem>? CommitRemoveItems()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientElementValueList.");

            return CommitRemoveItemsInternal();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ClientElementValueListItem>? CommitWriteElementValueListItems()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientElementValueList.");

            ElementValuesCollectionManager? writeElementValuesCollectionManager = null;
            var writeValueDictionary = new Dictionary<uint, ClientElementValueListItem>();

            int uintCount = 0;
            int dblCount = 0;
            int objCount = 0;

            foreach (ClientElementValueListItem item in ListItemsManager)
            {
                if (item.PendingWriteValueStatusTimestamp != null &&
                    item.PendingWriteValueStatusTimestamp.Value.ValueTypeCode != TypeCode.Empty)
                {
                    switch (item.PendingWriteValueStatusTimestamp.Value.ValueStorageType)
                    {
                        case Any.StorageType.Double:
                            dblCount += 1;
                            break;
                        case Any.StorageType.UInt32:
                            uintCount += 1;
                            break;
                        case Any.StorageType.Object:
                            objCount += 1;
                            break;
                    }

                    writeValueDictionary.Add(item.ClientAlias, item);
                }
            }
            if (0 < dblCount || 0 < uintCount || 0 < objCount)
            {
                writeElementValuesCollectionManager = new ElementValuesCollectionManager(dblCount, uintCount, objCount);
                foreach (var kvp in writeValueDictionary)
                {
                    ClientElementValueListItem item = kvp.Value;
                    if (item.PendingWriteValueStatusTimestamp != null &&
                        item.PendingWriteValueStatusTimestamp.Value.ValueTypeCode != TypeCode.Empty)
                    {
                        switch (item.PendingWriteValueStatusTimestamp.Value.ValueStorageType)
                        {
                            case Any.StorageType.Double:
                                writeElementValuesCollectionManager.AddDouble(item.ServerAlias,
                                    item.PendingWriteValueStatusTimestamp.StatusCode,
                                    item.PendingWriteValueStatusTimestamp.TimestampUtc,
                                    item.PendingWriteValueStatusTimestamp.Value.StorageDouble);
                                break;
                            case Any.StorageType.UInt32:
                                writeElementValuesCollectionManager.AddUint(item.ServerAlias,
                                    item.PendingWriteValueStatusTimestamp.StatusCode,
                                    item.PendingWriteValueStatusTimestamp.TimestampUtc,
                                    item.PendingWriteValueStatusTimestamp.Value.StorageUInt32);
                                break;
                            case Any.StorageType.Object:
                                writeElementValuesCollectionManager.AddObject(item.ServerAlias,
                                    item.PendingWriteValueStatusTimestamp.StatusCode,
                                    item.PendingWriteValueStatusTimestamp.TimestampUtc,
                                    item.PendingWriteValueStatusTimestamp.Value.StorageObject);
                                break;
                        }
                    }
                    item.HasWritten(DataGrpcResultCodes.S_OK);
                }
            }

            var listDataGrpcValues = new List<ClientElementValueListItem>();
            if (writeElementValuesCollectionManager != null)
            {
                AliasResult[] listAliasesResult = Context.WriteData(ListServerAlias, writeElementValuesCollectionManager.GetElementValuesCollection());
                foreach (AliasResult aliasResult in listAliasesResult)
                {
                    ClientElementValueListItem? item = null;
                    if (writeValueDictionary.TryGetValue(aliasResult.ClientAlias, out item))
                    {
                        item.HasWritten(aliasResult.ResultCode);
                        listDataGrpcValues.Add(item);
                    }
                }
            }
            writeValueDictionary.Clear();
            return (0 != listDataGrpcValues.Count) ? listDataGrpcValues : null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ClientElementValueListItem[] PollElementValuesChanges()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientElementValueList.");

            return Context.PollElementValuesChanges(this);
        }

        /// <summary>
        ///     Returns changed ClientElementValueListItems or null, if waiting next message.
        /// </summary>
        /// <param name="elementValuesCollection"></param>
        /// <returns></returns>
        public ClientElementValueListItem[]? OnInformationReport(ElementValuesCollection elementValuesCollection)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientElementValueList.");

            if (elementValuesCollection.Guid != @"" && _incompleteElementValuesCollectionCollection.Count > 0)
            {
                var beginElementValuesCollection = _incompleteElementValuesCollectionCollection.TryGetValue(elementValuesCollection.Guid);
                if (beginElementValuesCollection != null)
                {
                    _incompleteElementValuesCollectionCollection.Remove(elementValuesCollection.Guid);
                    beginElementValuesCollection.Add(elementValuesCollection);
                    elementValuesCollection = beginElementValuesCollection;
                }
            }

            if (elementValuesCollection.NextCollectionGuid != @"")
            {
                _incompleteElementValuesCollectionCollection[elementValuesCollection.NextCollectionGuid] = elementValuesCollection;

                return null;
            }
            else
            {
                var changedListItems = new List<ClientElementValueListItem>();

                for (int index = 0; index < elementValuesCollection.DoubleAliases.Count; index++)
                {
                    ClientElementValueListItem? item;
                    ListItemsManager.TryGetValue(elementValuesCollection.DoubleAliases[index], out item);
                    if (item != null)
                    {
                        item.UpdateValue(elementValuesCollection.DoubleValues[index],
                            elementValuesCollection.DoubleStatusCodes[index],
                            elementValuesCollection.DoubleTimestamps[index].ToDateTime()
                            );
                        changedListItems.Add(item);
                    }
                }
                for (int index = 0; index < elementValuesCollection.UintAliases.Count; index++)
                {
                    ClientElementValueListItem? item;
                    ListItemsManager.TryGetValue(elementValuesCollection.UintAliases[index], out item);
                    if (item != null)
                    {
                        item.UpdateValue(elementValuesCollection.UintValues[index],
                            elementValuesCollection.UintStatusCodes[index],
                            elementValuesCollection.UintTimestamps[index].ToDateTime()
                            );
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
                            ClientElementValueListItem? item;
                            ListItemsManager.TryGetValue(elementValuesCollection.ObjectAliases[index], out item);
                            if (item != null)
                            {
                                item.UpdateValue(objectValue,
                                    elementValuesCollection.ObjectStatusCodes[index],
                                    elementValuesCollection.ObjectTimestamps[index].ToDateTime()
                                    );
                                changedListItems.Add(item);
                            }
                        }
                    }

                }  
                
                return changedListItems.ToArray();
            }
        }

        /// <summary>
        ///     Throws or invokes InformationReport event.        
        /// </summary>
        /// <param name="changedListItems"></param>
        /// <param name="changedValues"></param>
        public void RaiseInformationReportEvent(ClientElementValueListItem[] changedListItems,
            ValueStatusTimestamp[] changedValues)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientElementValueList.");

            try
            {
                InformationReport(this, changedListItems, changedValues);
            }
            catch
            {
            }
        }

        /// <summary>
        ///     DataGrpc clients subscribe to this event to obtain the data update callbacks.
        /// </summary>
        public event InformationReportEventHandler InformationReport = delegate { };

        public IEnumerable<ClientElementValueListItem> ListItems
        {
            get { return ListItemsManager.ToArray(); }
        }

        #endregion

        #region private fields

        /// <summary>
        ///     This data member holds the last exception message encountered by the
        ///     InformationReport callback when calling valuesUpdateEvent().
        /// </summary>
        private CaseInsensitiveDictionary<ElementValuesCollection> _incompleteElementValuesCollectionCollection = new CaseInsensitiveDictionary<ElementValuesCollection>();

        #endregion
    }
}


///// <summary>
/////     This method is invoked to issue a Read request to the DataGrpc Server to read
/////     the specified data objects.
///// </summary>
//public IEnumerable<ClientElementValueListItem>? CommitReadElementValueListItems()
//{
//    if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientElementValueList.");

//    var serverAliaseses = new List<uint>();

//    foreach (ClientElementValueListItem dataListItem in ListItemsManager)
//    {
//        if (dataListItem.PreparedForRead)
//        {
//            serverAliaseses.Add(dataListItem.ServerAlias);
//            dataListItem.HasRead();
//        }
//    }

//    ElementValuesCollection? readValueArrays = Context.ReadData(ListServerAlias, serverAliaseses);

//    if (readValueArrays != null)
//    {
//        UpdateData(readValueArrays, false);
//    }

//    return null; // TODO: !!!
//}