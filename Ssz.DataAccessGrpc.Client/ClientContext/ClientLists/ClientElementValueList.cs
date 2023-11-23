using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Utils;
using Ssz.DataAccessGrpc.Client.ClientListItems;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils.DataAccess;
using System.IO;
using Ssz.Utils.Serialization;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.Client.ClientLists
{
    /// <summary>
    /// 
    /// </summary>
    internal class ClientElementValueList : ClientElementListBase<ClientElementValueListItem>
    {
        #region construction and destruction

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="listParams"></param>
        public ClientElementValueList(ClientContext context)
            : base(context)
        {
            ListType = (uint)StandardListType.ElementValueList;            
        }

        #endregion

        #region public functions

        /// <summary>
        /// 
        /// </summary>
        /// <param name="listParams"></param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        public async Task DefineListAsync(CaseInsensitiveDictionary<string>? listParams)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientEventList.");

            await Context.DefineListAsync(this, listParams);
        }

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

        /// <summary>
        ///     Returns failed items only.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        public override async Task<IEnumerable<ClientElementValueListItem>?> CommitAddItemsAsync()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientElementValueList.");

            return await CommitAddItemsInternalAsync();
        }

        public override async Task<IEnumerable<ClientElementValueListItem>?> CommitRemoveItemsAsync()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientElementValueList.");

            return await CommitRemoveItemsInternalAsync();
        }

        /// <summary>
        ///     Returns failed items.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<ClientElementValueListItem>> CommitWriteElementValueListItemsAsync()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientElementValueList.");

            var fullElementValuesCollection = new ElementValuesCollection();
            using (var memoryStream = new MemoryStream(1024))
            {
                using (var writer = new SerializationWriter(memoryStream))
                {
                    foreach (ClientElementValueListItem item in ListItemsManager)
                    {
                        if (item.PendingWriteValueStatusTimestamp is null) continue;

                        uint alias = item.ServerAlias;
                        ValueStatusTimestamp valueStatusTimestamp = item.PendingWriteValueStatusTimestamp.Value;

                        switch (valueStatusTimestamp.Value.ValueStorageType)
                        {
                            case Ssz.Utils.Any.StorageType.Double:
                                fullElementValuesCollection.DoubleAliases.Add(alias);
                                fullElementValuesCollection.DoubleValues.Add(valueStatusTimestamp.Value.StorageDouble);
                                fullElementValuesCollection.DoubleValueTypeCodes.Add((uint)valueStatusTimestamp.Value.ValueTypeCode);
                                fullElementValuesCollection.DoubleStatusCodes.Add(valueStatusTimestamp.StatusCode);
                                fullElementValuesCollection.DoubleTimestamps.Add(DateTimeHelper.ConvertToTimestamp(valueStatusTimestamp.TimestampUtc));                                
                                break;
                            case Ssz.Utils.Any.StorageType.UInt32:
                                fullElementValuesCollection.UintAliases.Add(alias);
                                fullElementValuesCollection.UintValues.Add(valueStatusTimestamp.Value.StorageUInt32);
                                fullElementValuesCollection.UintValueTypeCodes.Add((uint)valueStatusTimestamp.Value.ValueTypeCode);
                                fullElementValuesCollection.UintStatusCodes.Add(valueStatusTimestamp.StatusCode);
                                fullElementValuesCollection.UintTimestamps.Add(DateTimeHelper.ConvertToTimestamp(valueStatusTimestamp.TimestampUtc));                                
                                break;
                            case Ssz.Utils.Any.StorageType.Object:
                                fullElementValuesCollection.ObjectAliases.Add(alias);
                                writer.WriteObject(valueStatusTimestamp.Value.StorageObject);
                                fullElementValuesCollection.ObjectStatusCodes.Add(valueStatusTimestamp.StatusCode);
                                fullElementValuesCollection.ObjectTimestamps.Add(DateTimeHelper.ConvertToTimestamp(valueStatusTimestamp.TimestampUtc));                                
                                break;
                        }
                        item.HasWritten(ResultInfo.GoodResultInfo);
                    }
                }
                memoryStream.Position = 0;
                fullElementValuesCollection.ObjectValues = Google.Protobuf.ByteString.FromStream(memoryStream);
            }

            var failedItems = new List<ClientElementValueListItem>();
            foreach (ElementValuesCollection elementValuesCollection in fullElementValuesCollection.SplitForCorrectGrpcMessageSize())
            {
                AliasResult[] failedAliasResults = await Context.WriteElementValuesAsync(ListServerAlias, elementValuesCollection);
                foreach (AliasResult failedAliasResult in failedAliasResults)
                {                    
                    if (ListItemsManager.TryGetValue(failedAliasResult.ClientAlias, out ClientElementValueListItem? item))
                    {
                        item.HasWritten(failedAliasResult.GetResultInfo());
                        failedItems.Add(item);
                    }
                }
            }
            return failedItems;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<ClientElementValueListItem[]> PollElementValuesChangesAsync()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientElementValueList.");

            return await Context.PollElementValuesChangesAsync(this);
        }

        /// <summary>
        ///     Returns changed ClientElementValueListItems or null, if waiting next message.
        /// </summary>
        /// <param name="elementValuesCollection"></param>
        /// <returns></returns>
        public ClientElementValueListItem[]? OnElementValuesCallback(ElementValuesCollection elementValuesCollection)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientElementValueList.");

            if (elementValuesCollection.Guid != @"" && _incompleteElementValuesCollection.Count > 0)
            {
                var beginElementValuesCollection = _incompleteElementValuesCollection.TryGetValue(elementValuesCollection.Guid);
                if (beginElementValuesCollection is not null)
                {
                    _incompleteElementValuesCollection.Remove(elementValuesCollection.Guid);
                    beginElementValuesCollection.CombineWith(elementValuesCollection);
                    elementValuesCollection = beginElementValuesCollection;
                }
            }

            if (elementValuesCollection.NextCollectionGuid != @"")
            {
                _incompleteElementValuesCollection[elementValuesCollection.NextCollectionGuid] = elementValuesCollection;

                return null;
            }
            else
            {
                var changedListItems = new List<ClientElementValueListItem>();

                for (int index = 0; index < elementValuesCollection.DoubleAliases.Count; index++)
                {
                    ClientElementValueListItem? item;
                    ListItemsManager.TryGetValue(elementValuesCollection.DoubleAliases[index], out item);
                    if (item is not null)
                    {
                        item.UpdateValue(elementValuesCollection.DoubleValues[index],
                            (TypeCode)elementValuesCollection.DoubleValueTypeCodes[index],
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
                    if (item is not null)
                    {
                        item.UpdateValue(elementValuesCollection.UintValues[index],
                            (TypeCode)elementValuesCollection.UintValueTypeCodes[index],
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
                            if (item is not null)
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
        ///     Throws or invokes ElementValuesCallback event.        
        /// </summary>
        /// <param name="changedListItems"></param>
        /// <param name="changedValueStatusTimestamps"></param>
        public void RaiseElementValuesCallbackEvent(ClientElementValueListItem[] changedListItems,
            ValueStatusTimestamp[] changedValueStatusTimestamps)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientElementValueList.");

            try
            {
                ElementValuesCallback(this, new ElementValuesCallbackEventArgs(changedListItems, changedValueStatusTimestamps));
            }
            catch
            {
            }
        }

        /// <summary>
        ///     DataAccessGrpc clients subscribe to this event to obtain the data update callbacks.
        /// </summary>
        public event EventHandler<ElementValuesCallbackEventArgs> ElementValuesCallback = delegate { };

        public IEnumerable<ClientElementValueListItem> ListItems
        {
            get { return ListItemsManager.ToArray(); }
        }

        #endregion

        #region private fields
        
        private CaseInsensitiveDictionary<ElementValuesCollection> _incompleteElementValuesCollection = new CaseInsensitiveDictionary<ElementValuesCollection>();

        #endregion

        public class ElementValuesCallbackEventArgs : EventArgs
        {
            public ElementValuesCallbackEventArgs(ClientElementValueListItem[] changedListItems, ValueStatusTimestamp[] changedValueStatusTimestamps)
            {
                ChangedListItems = changedListItems;
                ChangedValueStatusTimestamps = changedValueStatusTimestamps;
            }

            public ClientElementValueListItem[] ChangedListItems { get; }

            public ValueStatusTimestamp[] ChangedValueStatusTimestamps { get; }
        }
    }
}


///// <summary>
/////     This method is invoked to issue a Read request to the DataAccessGrpc Server to read
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

//    if (readValueArrays is not null)
//    {
//        UpdateData(readValueArrays, false);
//    }

//    return null; // TODO: !!!
//}