using Ssz.Dcs.CentralServer.ServerListItems;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils.DataAccess;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using System.Linq;
using System.IO;
using Ssz.Utils.Serialization;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Ssz.Dcs.CentralServer
{
    /// <summary>
    ///     This implementation of a Data Journal List is used to maintain
    ///     a collection of historical data value collections.  Each value
    ///     maintained by a Data Journal List consists of collection of
    ///     data values where each value is associated with a specific time.
    /// </summary>
    public class ProcessElementValuesJournalList : ElementListBase<ProcessElementValuesJournalListItem>
	{
        #region construction and destruction
        
        public ProcessElementValuesJournalList(ServerContext serverContext, uint listClientAlias, CaseInsensitiveDictionary<string?> listParams)
			: base(serverContext, listClientAlias, listParams)
		{
            _engineSessions = ((ServerWorker)ServerContext.ServerWorker).GetEngineSessions(ServerContext);
            _engineSessions.CollectionChanged += OnEngineSessions_CollectionChanged;
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                _engineSessions.CollectionChanged -= OnEngineSessions_CollectionChanged;
            }

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public override async Task<ElementValuesJournalsCollection> ReadElementValuesJournalsAsync(
            DateTime firstTimeStampUtc,
            DateTime secondTimeStampUtc,
            uint numValuesPerSubscription,
            Ssz.DataAccessGrpc.ServerBase.TypeId calculation,
            CaseInsensitiveDictionary<string?> params_,
            List<uint> serverAliases)
        {
            var result = new ElementValuesJournalsCollection();            

            var dataProviderRequests = new Dictionary<IDataAccessProvider, List<(ValuesJournalSubscription, ElementValuesJournal)>>(ReferenceEqualityComparer.Instance);

            foreach (uint serverAlias in serverAliases)
            {
                var elementValuesJournal = new ElementValuesJournal();
                result.ElementValuesJournals.Add(elementValuesJournal);

                if (!ListItemsManager.TryGetValue(serverAlias, out var listItem))
                    continue;        
                
                foreach (var valueJournalSubscription in listItem.ValuesJournalSubscriptionsCollection)
                {
                    if (!dataProviderRequests.TryGetValue(valueJournalSubscription.DataAccessProvider, out var list))
                    {
                        list = new();
                        dataProviderRequests.Add(valueJournalSubscription.DataAccessProvider, list);
                    }
                    list.Add((valueJournalSubscription, elementValuesJournal));                    
                }                
            }

            var tasks = new List<Task<ValueStatusTimestamp[][]?>>(_engineSessions.Count);
            var resultData = new List<ElementValuesJournal[]>();

            var calculation2 = calculation.ToTypeId();
            foreach (var kvp in dataProviderRequests)
            {
                var valueJournalSubscriptions = kvp.Value.Select(t => t.Item1).ToArray();
                resultData.Add(kvp.Value.Select(t => t.Item2).ToArray());
                tasks.Add(
                    kvp.Key.ReadElementValuesJournalsAsync(firstTimeStampUtc, secondTimeStampUtc, numValuesPerSubscription, calculation2, params_, valueJournalSubscriptions)
                    );                
            }

            var dataProvidersReplyData = await Task.WhenAll(tasks);

            foreach (int index in Enumerable.Range(0, dataProvidersReplyData.Length))
            {
                ElementValuesJournal[] r = resultData[index];
                ValueStatusTimestamp[][]? dataProviderData = dataProvidersReplyData[index];
                if (dataProviderData != null)
                {
                    foreach (int i in Enumerable.Range(0, dataProviderData.Length))
                    {
                        ElementValuesJournal elementValuesJournal = r[i];
                        var vstCollection = dataProviderData[i];
                        if (vstCollection.Length == 0)
                            continue;
                        using (var memoryStream = new MemoryStream(1024))
                        {
                            using (var writer = new SerializationWriter(memoryStream))
                            {
                                foreach (ValueStatusTimestamp vst in dataProviderData[i])
                                {
                                    switch (AnyHelper.GetTransportType(vst.Value))
                                    {
                                        case TransportType.Double:
                                            elementValuesJournal.DoubleValues.Add(vst.Value.ValueAsDouble(false));
                                            elementValuesJournal.DoubleStatusCodes.Add(vst.StatusCode);
                                            elementValuesJournal.DoubleTimestamps.Add(Ssz.DataAccessGrpc.ServerBase.ProtobufHelper.ConvertToTimestamp(vst.TimestampUtc));
                                            break;
                                        case TransportType.UInt32:
                                            elementValuesJournal.UintValues.Add(vst.Value.ValueAsUInt32(false));
                                            elementValuesJournal.UintStatusCodes.Add(vst.StatusCode);
                                            elementValuesJournal.UintTimestamps.Add(Ssz.DataAccessGrpc.ServerBase.ProtobufHelper.ConvertToTimestamp(vst.TimestampUtc));
                                            break;
                                        case TransportType.Object:
                                            vst.Value.SerializeOwnedData(writer, null);                                            
                                            elementValuesJournal.ObjectStatusCodes.Add(vst.StatusCode);
                                            elementValuesJournal.ObjectTimestamps.Add(Ssz.DataAccessGrpc.ServerBase.ProtobufHelper.ConvertToTimestamp(vst.TimestampUtc));
                                            break;
                                    }
                                }
                            }
                            memoryStream.Position = 0;
                            elementValuesJournal.ObjectValues = Google.Protobuf.ByteString.FromStream(memoryStream);
                        }
                    }
                }
            }            

            return result;
        }

        #endregion

        #region protected functions

        protected override ProcessElementValuesJournalListItem OnNewElementListItem(uint clientAlias, uint serverAlias, string elementId)
        {
            return new ProcessElementValuesJournalListItem(clientAlias, serverAlias, elementId);
        }

        protected override Task<List<AliasResult>> OnAddElementListItemsToListAsync(List<ProcessElementValuesJournalListItem> elementListItems)
        {
            var results = new List<AliasResult>();

            if (elementListItems.Count == 0) 
                return Task.FromResult(results);            

            foreach (ProcessElementValuesJournalListItem item in elementListItems)
            {
                foreach (EngineSession engineSession in _engineSessions)
                {
                    var valueSubscription = new ValuesJournalSubscription(engineSession.DataAccessProvider, item.ElementId);
                    item.ValuesJournalSubscriptionsCollection.Add(valueSubscription);
                }

                results.Add(new AliasResult
                {
                    StatusCode = (uint)StatusCode.OK,
                    ServerAlias = item.ServerAlias,
                    ClientAlias = item.ClientAlias
                });
            }

            return Task.FromResult(results);
        }

        #endregion

        #region private functions

        private void OnEngineSessions_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var engineSession in e.NewItems!.OfType<EngineSession>())
                    {
                        foreach (ProcessElementValuesJournalListItem item in ListItemsManager)
                        {
                            var valueSubscription = new ValuesJournalSubscription(engineSession.DataAccessProvider, item.ElementId);
                            item.ValuesJournalSubscriptionsCollection.Add(valueSubscription);
                        }
                    }
                    
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (var engineSession in e.OldItems!.OfType<EngineSession>())
                    {
                        foreach (ProcessElementValuesJournalListItem item in ListItemsManager)
                        {
                            item.ValuesJournalSubscriptionsCollection.RemoveAll(vs => ReferenceEquals(vs.DataAccessProvider, engineSession.DataAccessProvider));
                        }
                    }                    
                    break;

                case NotifyCollectionChangedAction.Replace:
                    break;

                case NotifyCollectionChangedAction.Reset:
                    break;
            }
        }

        #endregion

        #region private fields        

        private ObservableCollection<EngineSession> _engineSessions;

        #endregion
    }
}


//if (_engineSessions.Count == 0)
//{
//    foreach (ProcessElementValuesJournalListItem item in elementListItems)
//    {
//        resultsList.Add(new AliasResult
//        {
//            AliasResult = new AliasResult
//            {
//                StatusCode = (uint)StatusCode.Unavailable,
//                ServerAlias = item.ServerAlias,
//                ClientAlias = item.ClientAlias
//            },
//            IsReadable = true,
//            IsWritable = false
//        });
//    }

//    return;
//}