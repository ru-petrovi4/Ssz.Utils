using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Utils;
using Ssz.DataGrpc.Client.ClientListItems;
using Ssz.DataGrpc.Server;
using Ssz.Utils.DataAccess;
using System.IO;
using Ssz.Utils.Serialization;
using Ssz.DataGrpc.Client.Data;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

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
        public IEnumerable<ClientElementValueListItem> CommitWriteElementValueListItems()
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
                                fullElementValuesCollection.DoubleValueStatusCodes.Add(valueStatusTimestamp.ValueStatusCode);
                                fullElementValuesCollection.DoubleTimestamps.Add(DateTimeHelper.ConvertToTimestamp(valueStatusTimestamp.TimestampUtc));
                                fullElementValuesCollection.DoubleValues.Add(valueStatusTimestamp.Value.StorageDouble);
                                break;
                            case Ssz.Utils.Any.StorageType.UInt32:
                                fullElementValuesCollection.UintAliases.Add(alias);
                                fullElementValuesCollection.UintValueStatusCodes.Add(valueStatusTimestamp.ValueStatusCode);
                                fullElementValuesCollection.UintTimestamps.Add(DateTimeHelper.ConvertToTimestamp(valueStatusTimestamp.TimestampUtc));
                                fullElementValuesCollection.UintValues.Add(valueStatusTimestamp.Value.StorageUInt32);
                                break;
                            case Ssz.Utils.Any.StorageType.Object:
                                fullElementValuesCollection.ObjectAliases.Add(alias);
                                fullElementValuesCollection.ObjectValueStatusCodes.Add(valueStatusTimestamp.ValueStatusCode);
                                fullElementValuesCollection.ObjectTimestamps.Add(DateTimeHelper.ConvertToTimestamp(valueStatusTimestamp.TimestampUtc));
                                writer.WriteObject(valueStatusTimestamp.Value.StorageObject);
                                break;
                        }
                        item.HasWritten(StatusCode.OK);
                    }
                }
                memoryStream.Position = 0;
                fullElementValuesCollection.ObjectValues = Google.Protobuf.ByteString.FromStream(memoryStream);
            }

            var result = new List<ClientElementValueListItem>();
            foreach (ElementValuesCollection elementValuesCollection in fullElementValuesCollection.SplitForCorrectGrpcMessageSize())
            {
                AliasResult[] listAliasesResult = Context.WriteElementValues(ListServerAlias, elementValuesCollection);
                foreach (AliasResult aliasResult in listAliasesResult)
                {
                    ClientElementValueListItem? item = null;
                    if (ListItemsManager.TryGetValue(aliasResult.ClientAlias, out item))
                    {
                        item.HasWritten((StatusCode)aliasResult.StatusCode);
                        result.Add(item);
                    }
                }
            }
            return result;
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
                            elementValuesCollection.DoubleValueStatusCodes[index],
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
                            elementValuesCollection.UintValueStatusCodes[index],
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
                                    elementValuesCollection.ObjectValueStatusCodes[index],
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
        /// <param name="changedValues"></param>
        public void RaiseElementValuesCallbackEvent(ClientElementValueListItem[] changedListItems,
            ValueStatusTimestamp[] changedValues)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientElementValueList.");

            try
            {
                ElementValuesCallback(this, changedListItems, changedValues);
            }
            catch
            {
            }
        }

        /// <summary>
        ///     DataGrpc clients subscribe to this event to obtain the data update callbacks.
        /// </summary>
        public event ElementValuesCallbackEventHandler ElementValuesCallback = delegate { };

        public IEnumerable<ClientElementValueListItem> ListItems
        {
            get { return ListItemsManager.ToArray(); }
        }

        #endregion

        #region private fields
        
        private CaseInsensitiveDictionary<ElementValuesCollection> _incompleteElementValuesCollection = new CaseInsensitiveDictionary<ElementValuesCollection>();

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

//    if (readValueArrays is not null)
//    {
//        UpdateData(readValueArrays, false);
//    }

//    return null; // TODO: !!!
//}