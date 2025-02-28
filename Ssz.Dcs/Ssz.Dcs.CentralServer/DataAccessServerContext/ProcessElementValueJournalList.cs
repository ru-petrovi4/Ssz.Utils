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
using Ssz.Dcs.CentralServer.Common;

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
        
        public ProcessElementValuesJournalList(ServerWorkerBase serverWorker, ServerContext serverContext, uint listClientAlias, CaseInsensitiveDictionary<string?> listParams)
			: base(serverWorker, serverContext, listClientAlias, listParams)
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

        public override async Task<ElementValuesJournal[]> ReadElementValuesJournalsAsync(
            DateTime firstTimeStampUtc,
            DateTime secondTimeStampUtc,
            uint numValuesPerSubscription,
            Ssz.DataAccessGrpc.ServerBase.TypeId calculation,
            CaseInsensitiveDictionary<string?> params_,
            List<uint> serverAliases)
        {
            var result = new ElementValuesJournal[serverAliases.Count];
            foreach (int index in Enumerable.Range(0, serverAliases.Count))
            {
                result[index] = new ElementValuesJournal();
            }

            var dataProviderRequests = new List<(IDataAccessProvider, List<(ValuesJournalSubscription, int)>)>();

            foreach (int resultIndex in Enumerable.Range(0, serverAliases.Count))
            {
                if (!ListItemsManager.TryGetValue(serverAliases[resultIndex], out var listItem))
                    continue;        
                
                foreach (var valueJournalSubscription in listItem.ValuesJournalSubscriptionsCollection)
                {
                    List<(ValuesJournalSubscription, int)> list;
                    int dataProviderRequestIndex = dataProviderRequests.FindIndex(it => ReferenceEquals(it.Item1, valueJournalSubscription.DataAccessProvider));
                    if (dataProviderRequestIndex == -1)
                    {
                        list = new();
                        dataProviderRequests.Add((valueJournalSubscription.DataAccessProvider, list));
                    }
                    else
                    {
                        list = dataProviderRequests[dataProviderRequestIndex].Item2;
                    }
                    list.Add((valueJournalSubscription, resultIndex));                    
                }                
            }

            var tasks = new List<Task<ElementValuesJournal[]?>>(_engineSessions.Count);            

            var calculation2 = calculation.ToTypeId();
            foreach (var dataProviderRequest in dataProviderRequests)
            {        
                tasks.Add(
                    dataProviderRequest.Item1.ReadElementValuesJournalsAsync(firstTimeStampUtc, secondTimeStampUtc, numValuesPerSubscription, calculation2, params_, dataProviderRequest.Item2.Select(t => t.Item1).ToArray())
                    );                
            }
            
            var dataProvidersReplyData = await Task.WhenAll(tasks);

            foreach (int dataProviderRequestIndex in Enumerable.Range(0, dataProvidersReplyData.Length))
            {   
                ElementValuesJournal[]? dataProviderData = dataProvidersReplyData[dataProviderRequestIndex];
                if (dataProviderData != null)
                {
                    var dataProviderRequest = dataProviderRequests[dataProviderRequestIndex];
                    foreach (int subscriptionIndex in Enumerable.Range(0, dataProviderData.Length))
                    {                        
                        var elementValuesJournal = dataProviderData[subscriptionIndex];
                        if (elementValuesJournal.IsEmpty())
                            continue;
                        result[dataProviderRequest.Item2[subscriptionIndex].Item2] = elementValuesJournal;
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

        protected override List<AliasResult> OnAddElementListItemsToList(List<ProcessElementValuesJournalListItem> elementListItems)
        {
            var results = new List<AliasResult>();

            if (elementListItems.Count == 0) 
                return results;            

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

            return results;
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