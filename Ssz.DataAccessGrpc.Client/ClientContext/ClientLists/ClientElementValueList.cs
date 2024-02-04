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
using Google.Protobuf;

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

            byte[] fullElementValuesCollection;
            using (var memoryStream = new MemoryStream(1024))
            {
                using (var writer = new SerializationWriter(memoryStream))
                {
                    using (writer.EnterBlock(1))
                    {
                        writer.Write(
                            ((IEnumerable<ClientElementValueListItem>)ListItemsManager).Count(item => item.PendingWriteValueStatusTimestamp is not null)
                        );
                        foreach (ClientElementValueListItem item in ListItemsManager)
                        {
                            if (item.PendingWriteValueStatusTimestamp is null)
                                continue;

                            writer.Write(item.ServerAlias);
                            ValueStatusTimestamp valueStatusTimestamp = item.PendingWriteValueStatusTimestamp.Value;
                            valueStatusTimestamp.SerializeOwnedData(writer, null);

                            item.HasWritten(ResultInfo.GoodResultInfo);
                        }
                    }
                }                
                fullElementValuesCollection = memoryStream.ToArray();
            }

            var failedItems = new List<ClientElementValueListItem>();
            AliasResult[] failedAliasResults = await Context.WriteElementValuesAsync(ListServerAlias, fullElementValuesCollection);
            foreach (AliasResult failedAliasResult in failedAliasResults)
            {
                if (ListItemsManager.TryGetValue(failedAliasResult.ClientAlias, out ClientElementValueListItem? item))
                {
                    item.HasWritten(failedAliasResult.GetResultInfo());
                    failedItems.Add(item);
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
        /// <param name="elementValuesCollectionk"></param>
        /// <returns></returns>
        public ClientElementValueListItem[]? OnElementValuesCallback(DataChunk elementValuesCollection)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientElementValueList.");

            if (elementValuesCollection.Guid != @"" && _incompleteElementValuesCollections.Count > 0)
            {
                var beginElementValuesCollection = _incompleteElementValuesCollections.TryGetValue(elementValuesCollection.Guid);
                if (beginElementValuesCollection is not null)
                {
                    _incompleteElementValuesCollections.Remove(elementValuesCollection.Guid);
                    beginElementValuesCollection.CombineWith(elementValuesCollection);
                    elementValuesCollection = beginElementValuesCollection;
                }
            }

            if (elementValuesCollection.NextDataChunkGuid != @"")
            {
                _incompleteElementValuesCollections[elementValuesCollection.NextDataChunkGuid] = elementValuesCollection;

                return null;
            }
            else
            {
                var changedListItems = new List<ClientElementValueListItem>();

                using (var memoryStream = new MemoryStream(elementValuesCollection.Bytes.ToArray())) // TODO Optimization needed
                using (var reader = new SerializationReader(memoryStream))
                {
                    using (Block block = reader.EnterBlock())
                    {
                        switch (block.Version)
                        {
                            case 1:
                                int count = reader.ReadInt32();
                                for (int index = 0; index < count; index += 1)
                                {
                                    uint alias = reader.ReadUInt32();
                                    ValueStatusTimestamp vst = new();
                                    vst.DeserializeOwnedData(reader, null);

                                    ClientElementValueListItem? item;
                                    ListItemsManager.TryGetValue(alias, out item);
                                    if (item is not null)
                                    {
                                        item.Update(vst);
                                        changedListItems.Add(item);
                                    }
                                }
                                break;
                            default:
                                throw new BlockUnsupportedVersionException();
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
        
        private CaseInsensitiveDictionary<DataChunk> _incompleteElementValuesCollections = new CaseInsensitiveDictionary<DataChunk>();

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