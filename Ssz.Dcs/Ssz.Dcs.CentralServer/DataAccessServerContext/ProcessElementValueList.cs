using Grpc.Core;
using Ssz.Dcs.CentralServer.Common;
using Ssz.Dcs.CentralServer.ServerListItems;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Ssz.Dcs.CentralServer.ServerWorker;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using AliasResult = Ssz.Utils.DataAccess.AliasResult;

namespace Ssz.Dcs.CentralServer
{
    /// <summary>
    ///     The Data List class is used to represent a list of current process data values.
    ///     The data values held by Items list represents current process values with a status
    ///     and a time stamp.
    /// </summary>
    public class ProcessElementValueList : ElementValueListBase<ProcessElementValueListItem>
    {
        #region construction and destruction

        public ProcessElementValueList(DataAccessServerWorkerBase serverWorker, ServerContext serverContext, uint listClientAlias, CaseInsensitiveDictionary<string?> listParams)
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

        public override void DoWork(DateTime nowUtc, CancellationToken token)
        {
            if (Disposed) return;

            if (!ListCallbackIsEnabled) return; // Callback is not Enabled.            

            if (nowUtc >= LastCallbackTime.AddMilliseconds(UpdateRateMs))
            {
                string dataGuids = "";
                foreach (EngineSession engineSession in _engineSessions)
                {
                    dataGuids += engineSession.DataAccessProvider.DataGuid.ToString();
                }
                if (_dataGuids == dataGuids) return;
                _dataGuids = dataGuids;

                LastCallbackTime = nowUtc;

                ElementValuesCallbackMessage? elementValuesCallbackMessage = GetElementValuesCallbackMessage();

                if (elementValuesCallbackMessage is not null)
                {
                    ServerContext.AddCallbackMessage(elementValuesCallbackMessage);                    
                }
            }
        }

        public override void TouchList()
        {
            _dataGuids = null;

            base.TouchList();
        }

        #endregion

        #region protected functions

        protected override ProcessElementValueListItem OnNewElementListItem(uint clientAlias, uint serverAlias, string elementId)
        {
            return new ProcessElementValueListItem(clientAlias, serverAlias, elementId);
        }

        protected override List<AliasResult> OnAddElementListItemsToList(List<ProcessElementValueListItem> items)
        {
            var results = new List<AliasResult>();
            if (items.Count == 0) 
                return results;            

            foreach (ProcessElementValueListItem item in items)
            {
                foreach (EngineSession engineSession in _engineSessions)
                {                    
                    item.ValueSubscriptionsCollection.Add(
                         new ValueSubscription(engineSession.DataAccessProvider, item.ElementId, item.ValueSubscriptionOnValueChanged));
                }

                results.Add(new AliasResult
                {
                    StatusCode = StatusCodes.Good,
                    ServerAlias = item.ServerAlias,
                    ClientAlias = item.ClientAlias
                });
            }

            _dataGuids = null;

            return results;
        }

        /// <summary>
        ///     Returns failed AliasResults only.
        /// </summary>
        /// <param name="elementListItems"></param>
        /// <returns></returns>
        protected override List<AliasResult> OnRemoveElementListItemsFromList(List<ProcessElementValueListItem> elementListItems)
        {
            List<AliasResult> results = new List<AliasResult>();

            foreach (ProcessElementValueListItem item in elementListItems)
            {
                foreach (ValueSubscription valueSubscription in item.ValueSubscriptionsCollection)
                {
                    valueSubscription.Dispose();
                }
                item.ValueSubscriptionsCollection.Clear();
            }

            return results;
        }

        /// <summary>
        ///     Returns failed AliasResults only.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        protected override async Task<List<AliasResult>> OnWriteValuesAsync(List<ProcessElementValueListItem> items)
        {
            if (items.Count == 0) 
                return new List<AliasResult>();

            if (_engineSessions.Count == 0)
            {
                var failedAliasResults = new List<AliasResult>();

                foreach (ProcessElementValueListItem item in items)
                {
                    failedAliasResults.Add(new AliasResult
                        {
                            StatusCode = StatusCodes.Uncertain,
                            ServerAlias = item.ServerAlias,
                            ClientAlias = item.ClientAlias
                        });
                }

                return failedAliasResults;
            }

            Dictionary<IDataAccessProvider, PendingWrite> pendingWriteDictionary = new(ReferenceEqualityComparer.Instance);

            foreach (EngineSession engineSession in _engineSessions)
            {
                 pendingWriteDictionary[engineSession.DataAccessProvider] = new PendingWrite();
            }

            OperatorSession? operatorSession = ((ServerWorker)ServerContext.ServerWorker).OperatorSessionsCollection.TryGetValue(ServerContext.ContextParams.TryGetValue(@"OperatorSessionId") ?? "");

            Dictionary<IValueSubscription, AliasResult> aliasResultsDicitionary = new(ReferenceEqualityComparer.Instance);

            foreach (ProcessElementValueListItem item in items)
            {
                ValueStatusTimestamp? valueStatusTimestamp = item.PendingWriteValueStatusTimestamp;
                if (valueStatusTimestamp is not null)
                {
                    foreach (ValueSubscription valueSubscription in item.ValueSubscriptionsCollection)
                    {
                        if (StatusCodes.IsBad(valueSubscription.ValueStatusTimestamp.StatusCode))
                            continue;
                        pendingWriteDictionary.TryGetValue(valueSubscription.DataAccessProvider, out PendingWrite? pendingWrite);                        
                        if (pendingWrite is null) 
                            continue;
                        pendingWrite.ValueSubscriptions.Add(valueSubscription);
                        pendingWrite.ValueStatusTimestamps.Add(valueStatusTimestamp.Value);
                        aliasResultsDicitionary[valueSubscription] = new AliasResult
                            {
                                StatusCode = StatusCodes.Good,
                                ClientAlias = item.ClientAlias,
                                ServerAlias = item.ServerAlias,
                            };                        
                    }

                    if (operatorSession is not null) // Process context with operatorSessionId.
                    {
                        ((ServerWorker)ServerContext.ServerWorker).OnChangeElementValueAction(
                            operatorSession.ProcessModelingSession,
                            operatorSession.OperatorRoleId,
                            operatorSession.OperatorRoleName,
                            operatorSession.OperatorUserName,
                            item.ElementId,
                            item.ValueStatusTimestamp,
                            valueStatusTimestamp.Value);

                    }
                    else // TODO Instructor actions, consider secondary server
                    {
                        ProcessModelingSession? processModelingSession = ((ServerWorker)ServerContext.ServerWorker).GetProcessModelingSessionOrNull(ServerContext.SystemNameToConnect);
                        if (processModelingSession is not null) // Process context with no operatorSessionId.
                        {
                            ((ServerWorker)ServerContext.ServerWorker).OnChangeElementValueAction(
                                processModelingSession,
                                ProcessModelingSessionConstants.Instructor_RoleId,
                                Common.Properties.Resources.Instructor_RoleName,
                                processModelingSession.InstructorUserName,
                                item.ElementId,
                                item.ValueStatusTimestamp,
                                valueStatusTimestamp.Value);

                        }
                    }
                }
            }

            List<Task<(IValueSubscription[], ResultInfo[])>> tasks = new();            

            foreach (EngineSession engineSession in _engineSessions)
            {
                pendingWriteDictionary.TryGetValue(engineSession.DataAccessProvider, out PendingWrite? pendingWrite);                           
                if (pendingWrite is null || pendingWrite.ValueSubscriptions.Count == 0) 
                    continue;
                var t = engineSession.DataAccessProvider.WriteAsync(pendingWrite.ValueSubscriptions.ToArray(), pendingWrite.ValueStatusTimestamps.ToArray());
                tasks.Add(t);                
            }  
            
            foreach (var task in tasks)
            {
                try
                {
                    var (failedValueSubscriptions, failedResultInfos) = await task;
                    foreach (int i in Enumerable.Range(0, failedValueSubscriptions.Length))
                    {
                        var failedValueSubscription = failedValueSubscriptions[i];
                        AliasResult? aliasResult;
                        if (!aliasResultsDicitionary.TryGetValue(failedValueSubscription, out aliasResult))
                            continue;
                        var resultInfo = failedResultInfos[i];
                        aliasResult.StatusCode = resultInfo.StatusCode;
                        aliasResult.Info = resultInfo.Info;
                        aliasResult.Label = resultInfo.Label;
                        aliasResult.Details = resultInfo.Details;
                    }
                }
                catch (Exception) 
                {
                }                
            }

            return aliasResultsDicitionary.Values
                .GroupBy(r => r.ServerAlias)
                .Where(g => g.Any(r => !StatusCodes.IsGood(r.StatusCode)))
                .Select(g => g.First(r => !StatusCodes.IsGood(r.StatusCode)))
                .ToList();
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
                        foreach (ProcessElementValueListItem item in ListItemsManager)
                        {
                            var valueSubscription = new ValueSubscription(engineSession.DataAccessProvider, item.ElementId, item.ValueSubscriptionOnValueChanged);
                            item.ValueSubscriptionsCollection.Add(valueSubscription);
                            item.ValueSubscriptionsOnValueChanged();
                        }                        
                    }
                    _dataGuids = null;
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (var engineSession in e.OldItems!.OfType<EngineSession>())
                    {
                        foreach (ProcessElementValueListItem item in ListItemsManager)
                        {                            
                            item.ValueSubscriptionsCollection.RemoveAll(vs => ReferenceEquals(vs.DataAccessProvider, engineSession.DataAccessProvider));
                            item.ValueSubscriptionsOnValueChanged();
                        }
                    }
                    _dataGuids = null;
                    break;

                case NotifyCollectionChangedAction.Replace:
                    break;

                case NotifyCollectionChangedAction.Reset:
                    break;
            }
        }

        #endregion

        #region private fields

        private string? _dataGuids;

        private ObservableCollection<EngineSession> _engineSessions;

        #endregion

        private class PendingWrite
        {
            public readonly List<ValueSubscription> ValueSubscriptions = new();

            public readonly List<ValueStatusTimestamp> ValueStatusTimestamps = new();            
        }
    }
}


//if (_engineSessions.Count == 0)
//{
//    foreach (ProcessElementValueListItem item in elementListItems)
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
//            IsWritable = true
//        });
//    }

//    return;
//}
