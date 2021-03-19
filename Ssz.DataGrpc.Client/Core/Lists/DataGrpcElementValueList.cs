using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Utils;
using Ssz.DataGrpc.Client.Core.Context;
using Ssz.DataGrpc.Client.Core.ListItems;
using Ssz.DataGrpc.Server;
using Ssz.DataGrpc.Common;
using Ssz.DataGrpc.Client.Data.EventHandlers;
using System.IO;
using Ssz.Utils.Serialization;

namespace Ssz.DataGrpc.Client.Core.Lists
{
    /// <summary>
    /// 
    /// </summary>
    public class DataGrpcElementValueList : DataGrpcElementListBase<DataGrpcElementValueListItem>
    {
        #region construction and destruction

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="listParams"></param>
        public DataGrpcElementValueList(DataGrpcContext context, CaseInsensitiveDictionary<string>? listParams)
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
        public override DataGrpcElementValueListItem PrepareAddItem(string elementId)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcElementValueList.");

            var dataListItem = new DataGrpcElementValueListItem(elementId);
            dataListItem.ClientAlias = ListItemsManager.Add(dataListItem);
            dataListItem.IsInClientList = true;
            dataListItem.PreparedForAdd = true;
            return dataListItem;
        }

        public override IEnumerable<DataGrpcElementValueListItem>? CommitAddItems()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcElementValueList.");

            return CommitAddItemsInternal();
        }

        public override IEnumerable<DataGrpcElementValueListItem>? CommitRemoveItems()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcElementValueList.");

            return CommitRemoveItemsInternal();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<DataGrpcElementValueListItem>? CommitWriteElementValueListItems()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcElementValueList.");

            ElementValueArraysManager? writeValueArraysManager = null;
            var writeValueDictionary = new Dictionary<uint, DataGrpcElementValueListItem>();

            int uintCount = 0;
            int dblCount = 0;
            int objCount = 0;

            foreach (DataGrpcElementValueListItem item in ListItemsManager)
            {
                if (item.PendingWriteDataGrpcValueStatusTimestamp != null &&
                    item.PendingWriteDataGrpcValueStatusTimestamp.Value.ValueTypeCode != TypeCode.Empty)
                {
                    switch (item.PendingWriteDataGrpcValueStatusTimestamp.Value.ValueStorageType)
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
                writeValueArraysManager = new ElementValueArraysManager(dblCount, uintCount, objCount);
                foreach (var kvp in writeValueDictionary)
                {
                    DataGrpcElementValueListItem item = kvp.Value;
                    if (item.PendingWriteDataGrpcValueStatusTimestamp != null &&
                        item.PendingWriteDataGrpcValueStatusTimestamp.Value.ValueTypeCode != TypeCode.Empty)
                    {
                        switch (item.PendingWriteDataGrpcValueStatusTimestamp.Value.ValueStorageType)
                        {
                            case Any.StorageType.Double:
                                writeValueArraysManager.AddDouble(item.ServerAlias,
                                    item.PendingWriteDataGrpcValueStatusTimestamp.StatusCode,
                                    item.PendingWriteDataGrpcValueStatusTimestamp.TimestampUtc,
                                    item.PendingWriteDataGrpcValueStatusTimestamp.Value.StorageDouble);
                                break;
                            case Any.StorageType.UInt32:
                                writeValueArraysManager.AddUint(item.ServerAlias,
                                    item.PendingWriteDataGrpcValueStatusTimestamp.StatusCode,
                                    item.PendingWriteDataGrpcValueStatusTimestamp.TimestampUtc,
                                    item.PendingWriteDataGrpcValueStatusTimestamp.Value.StorageUInt32);
                                break;
                            case Any.StorageType.Object:
                                writeValueArraysManager.AddObject(item.ServerAlias,
                                    item.PendingWriteDataGrpcValueStatusTimestamp.StatusCode,
                                    item.PendingWriteDataGrpcValueStatusTimestamp.TimestampUtc,
                                    item.PendingWriteDataGrpcValueStatusTimestamp.Value.StorageObject);
                                break;
                        }
                    }
                    item.HasWritten(DataGrpcFaultCodes.S_OK);
                }
            }

            var listDataGrpcValues = new List<DataGrpcElementValueListItem>();
            if (writeValueArraysManager != null)
            {
                AliasResult[] listAliasesResult = Context.WriteData(ListServerAlias, writeValueArraysManager.GetElementValueArrays());
                foreach (AliasResult aliasResult in listAliasesResult)
                {
                    DataGrpcElementValueListItem? item = null;
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
        public DataGrpcElementValueListItem[] PollDataChanges()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcElementValueList.");

            return Context.PollDataChanges(this);
        }

        /// <summary>
        ///     Returns changed DataGrpcElementValueListItems or null, if waiting next message.
        /// </summary>
        /// <param name="elementValueArrays"></param>
        /// <returns></returns>
        public DataGrpcElementValueListItem[]? OnInformationReport(ElementValueArrays elementValueArrays)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcElementValueList.");

            if (elementValueArrays.Guid != @"" && _incompleteElementValueArraysCollection.Count > 0)
            {
                var beginElementValueArrays = _incompleteElementValueArraysCollection.TryGetValue(elementValueArrays.Guid);
                if (beginElementValueArrays != null)
                {
                    _incompleteElementValueArraysCollection.Remove(elementValueArrays.Guid);
                    beginElementValueArrays.Add(elementValueArrays);
                    elementValueArrays = beginElementValueArrays;
                }
            }

            if (elementValueArrays.NextArraysGuid != @"")
            {
                _incompleteElementValueArraysCollection[elementValueArrays.NextArraysGuid] = elementValueArrays;

                return null;
            }
            else
            {
                var changedListItems = new List<DataGrpcElementValueListItem>();

                for (int index = 0; index < elementValueArrays.DoubleAliases.Count; index++)
                {
                    DataGrpcElementValueListItem? item;
                    ListItemsManager.TryGetValue(elementValueArrays.DoubleAliases[index], out item);
                    if (item != null)
                    {
                        item.UpdateValue(elementValueArrays.DoubleValues[index],
                            elementValueArrays.DoubleStatusCodes[index],
                            elementValueArrays.DoubleTimestamps[index].ToDateTime()
                            );
                        changedListItems.Add(item);
                    }
                }
                for (int index = 0; index < elementValueArrays.UintAliases.Count; index++)
                {
                    DataGrpcElementValueListItem? item;
                    ListItemsManager.TryGetValue(elementValueArrays.UintAliases[index], out item);
                    if (item != null)
                    {
                        item.UpdateValue(elementValueArrays.UintValues[index],
                            elementValueArrays.UintStatusCodes[index],
                            elementValueArrays.UintTimestamps[index].ToDateTime()
                            );
                        changedListItems.Add(item);
                    }
                }
                if (elementValueArrays.ObjectAliases.Count > 0)
                {
                    using (var memoryStream = new MemoryStream(elementValueArrays.ObjectValues.ToByteArray()))
                    using (var reader = new SerializationReader(memoryStream))
                    {
                        for (int index = 0; index < elementValueArrays.ObjectAliases.Count; index++)
                        {
                            object? objectValue = reader.ReadObject();
                            DataGrpcElementValueListItem? item;
                            ListItemsManager.TryGetValue(elementValueArrays.ObjectAliases[index], out item);
                            if (item != null)
                            {
                                item.UpdateValue(objectValue,
                                    elementValueArrays.ObjectStatusCodes[index],
                                    elementValueArrays.ObjectTimestamps[index].ToDateTime()
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
        public void RaiseInformationReportEvent(DataGrpcElementValueListItem[] changedListItems,
            DataGrpcValueStatusTimestamp[] changedValues)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcElementValueList.");

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
        public event DataGrpcInformationReportEventHandler InformationReport = delegate { };

        public IEnumerable<DataGrpcElementValueListItem> ListItems
        {
            get { return ListItemsManager.ToArray(); }
        }

        #endregion

        #region private fields

        /// <summary>
        ///     This data member holds the last exception message encountered by the
        ///     InformationReport callback when calling valuesUpdateEvent().
        /// </summary>
        private CaseInsensitiveDictionary<ElementValueArrays> _incompleteElementValueArraysCollection = new CaseInsensitiveDictionary<ElementValueArrays>();

        #endregion
    }
}


///// <summary>
/////     This method is invoked to issue a Read request to the DataGrpc Server to read
/////     the specified data objects.
///// </summary>
//public IEnumerable<DataGrpcElementValueListItem>? CommitReadElementValueListItems()
//{
//    if (Disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcElementValueList.");

//    var serverAliaseses = new List<uint>();

//    foreach (DataGrpcElementValueListItem dataListItem in ListItemsManager)
//    {
//        if (dataListItem.PreparedForRead)
//        {
//            serverAliaseses.Add(dataListItem.ServerAlias);
//            dataListItem.HasRead();
//        }
//    }

//    ElementValueArrays? readValueArrays = Context.ReadData(ListServerAlias, serverAliaseses);

//    if (readValueArrays != null)
//    {
//        UpdateData(readValueArrays, false);
//    }

//    return null; // TODO: !!!
//}