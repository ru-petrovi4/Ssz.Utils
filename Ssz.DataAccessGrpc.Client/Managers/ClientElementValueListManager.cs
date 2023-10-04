using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Ssz.Utils;
using Ssz.DataAccessGrpc.Client.ClientListItems;
using Microsoft.Extensions.Logging;
using Ssz.DataAccessGrpc.Client.ClientLists;
using Ssz.Utils.DataAccess;
using Grpc.Core;
using static Ssz.DataAccessGrpc.Client.Managers.ClientElementValueListManager;

namespace Ssz.DataAccessGrpc.Client.Managers
{
    internal class ClientElementValueListManager : ClientElementListManagerBase<ClientElementValueListItem, ClientElementValueList>
    {
        #region construction and destruction

        public ClientElementValueListManager(ILogger<GrpcDataAccessProvider> logger) :
            base(logger)
        {
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Creates List, adds/removes items.
        ///     No throw.
        /// </summary>
        /// <param name="clientContextManager"></param>
        /// <param name="сallbackDispatcher"></param>
        /// <param name="elementValuesCallbackEventHandler"></param>
        /// <param name="unsubscribeItemsFromServer"></param>
        /// <param name="callbackIsEnabled"></param>
        /// <param name="ct"></param>
        public void Subscribe(ClientContextManager clientContextManager, 
            IDispatcher? сallbackDispatcher,
            EventHandler<ElementValuesCallbackEventArgs> elementValuesCallbackEventHandler,
            bool unsubscribeItemsFromServer,
            bool callbackIsEnabled, 
            CancellationToken ct)
        {
            try
            {
                if (ct.IsCancellationRequested) return;                
                if (!DataAccessGrpcItemsMustBeAddedOrRemoved) return;               

                bool firstTimeDataConnection = (DataAccessGrpcList is null);

                if (firstTimeDataConnection)
                {
                    try
                    {
                        if (clientContextManager.ConnectionExists)
                        {
                            DataAccessGrpcList = clientContextManager.NewElementValueList(null);
                        }                            
                    }
                    catch (Exception)
                    {   
                    }
                }

                bool connectionError = SubscribeInitial(unsubscribeItemsFromServer);

                try
                {
                    if (!connectionError && DataAccessGrpcList is not null && !DataAccessGrpcList.Disposed)
                    {
                        if (firstTimeDataConnection)
                        {
                            DataAccessGrpcList.ElementValuesCallback +=
                                (object? sender, ClientElementValueList.ElementValuesCallbackEventArgs args) =>
                                {
                                    ClientElementValueList dataList = (ClientElementValueList)sender!;
                                    ClientElementValueListItem[] changedListItems = args.ChangedListItems;
                                    ValueStatusTimestamp[] changedValues = args.ChangedValueStatusTimestamps;
                                    ElementValuesCallbackEventArgs elementValuesCallbackEventArgs = new();
                                    elementValuesCallbackEventArgs.ElementValuesCallbackChanges = new(changedListItems.Length);
                                    int i = 0;
                                    foreach (ClientElementValueListItem dataGrpcElementValueListItem in changedListItems)
                                    {
                                        var o = dataGrpcElementValueListItem.Obj as DataAccessGrpcListItemWrapper;
                                        if (o is null) throw new InvalidOperationException();
                                        foreach (var clientObjectInfo in o.ClientObjectInfosCollection)
                                        {
                                            clientObjectInfo.NotifyClientObj_ValueStatusTimestamp = false;
                                            if (clientObjectInfo.ClientObj is not null)
                                            {
                                                elementValuesCallbackEventArgs.ElementValuesCallbackChanges.Add(new ElementValuesCallbackChange
                                                {
                                                    ClientObj = clientObjectInfo.ClientObj,                                                    
                                                    ValueStatusTimestamp = changedValues[i],
                                                });                                             
                                            }
                                        }
                                        i++;
                                    }
                                    if (ct.IsCancellationRequested) return;
                                    Logger.LogDebug("DataAccessGrpcList.ElementValuesCallback");
                                    if (сallbackDispatcher is not null)
                                    {
                                        try
                                        {
                                            сallbackDispatcher.BeginInvoke(ct =>
                                            {
                                                Logger.LogDebug("DataAccessGrpcList.ElementValuesCallback dispatched");
                                                elementValuesCallbackEventHandler(this, elementValuesCallbackEventArgs);
                                            });
                                        }
                                        catch (Exception)
                                        {
                                        }
                                    }
                                };
                            if (callbackIsEnabled)
                            {
                                DataAccessGrpcList.EnableListCallback(true);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "ClientElementValueListItemsManager.Subscribe exception");
                    connectionError = true;
                }

                {
                    var utcNow = DateTime.UtcNow;
                    ElementValuesCallbackEventArgs elementValuesCallbackEventArgs = new();
                    elementValuesCallbackEventArgs.ElementValuesCallbackChanges = new();
                    foreach (DataAccessGrpcListItemWrapper dataAccessGrpcListItemWrapper in DataAccessGrpcListItemWrappersDictionary.Values)
                    {                        
                        foreach (var clientObjectInfo in dataAccessGrpcListItemWrapper.ClientObjectInfosCollection)
                        {
                            bool notifyClientObj_AddItemResult = clientObjectInfo.NotifyClientObj_AddItemResult;
                            bool notifyClientObj_ValueStatusTimestamp = clientObjectInfo.NotifyClientObj_ValueStatusTimestamp;
                            if (notifyClientObj_AddItemResult || notifyClientObj_ValueStatusTimestamp)
                            {
                                clientObjectInfo.NotifyClientObj_AddItemResult = false;
                                clientObjectInfo.NotifyClientObj_ValueStatusTimestamp = false;
                                if (clientObjectInfo.ClientObj is not null)
                                {
                                    var elementValuesCallbackChange = new ElementValuesCallbackChange
                                    {
                                        ClientObj = clientObjectInfo.ClientObj
                                    };
                                    if (dataAccessGrpcListItemWrapper.FailedAddItemResult is not null)
                                    {
                                        if (notifyClientObj_AddItemResult)
                                        {
                                            elementValuesCallbackChange.AddItemResult = dataAccessGrpcListItemWrapper.FailedAddItemResult;
                                        }
                                        if (notifyClientObj_ValueStatusTimestamp)
                                        {
                                            elementValuesCallbackChange.ValueStatusTimestamp = new ValueStatusTimestamp { ValueStatusCode = ValueStatusCodes.ItemDoesNotExist };
                                        }                                        
                                    }
                                    else if (dataAccessGrpcListItemWrapper.DataAccessGrpcListItem is not null &&
                                            dataAccessGrpcListItemWrapper.DataAccessGrpcListItem.AddItemResult is not null)
                                    {
                                        if (notifyClientObj_AddItemResult)
                                        {
                                            elementValuesCallbackChange.AddItemResult = dataAccessGrpcListItemWrapper.DataAccessGrpcListItem.AddItemResult;
                                        }
                                        if (notifyClientObj_ValueStatusTimestamp)
                                        {
                                            elementValuesCallbackChange.ValueStatusTimestamp = dataAccessGrpcListItemWrapper.DataAccessGrpcListItem.ValueStatusTimestamp;
                                        }                                       
                                    }
                                    else
                                    {
                                        if (notifyClientObj_AddItemResult)
                                        {
                                            elementValuesCallbackChange.AddItemResult = AddItemResult.UnknownAddItemResult;
                                        }
                                        if (notifyClientObj_ValueStatusTimestamp)
                                        {
                                            elementValuesCallbackChange.ValueStatusTimestamp = new ValueStatusTimestamp(new Any(), ValueStatusCodes.Unknown, utcNow);
                                        }                                        
                                    }
                                    elementValuesCallbackEventArgs.ElementValuesCallbackChanges.Add(elementValuesCallbackChange);
                                }                                
                            }
                        }                                              
                    }                    
                    if (elementValuesCallbackEventArgs.ElementValuesCallbackChanges.Count > 0)
                    {
                        if (ct.IsCancellationRequested) return;
                        if (сallbackDispatcher is not null)
                        {
                            try
                            {
                                сallbackDispatcher.BeginInvoke(ct =>
                                    elementValuesCallbackEventHandler(this, elementValuesCallbackEventArgs));
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }

                if (!connectionError)
                {
                    DataAccessGrpcItemsMustBeAddedOrRemoved = false;
                }   
            }
            finally
            {
                SubscribeFinal();
            }
        }

        /// <summary>        
        ///     No throw. Returns null or changed clientObjs (not null, but possibly zero-lenghth).
        /// </summary>
        public object[]? PollChanges()
        {
            if (DataAccessGrpcList is null || DataAccessGrpcList.Disposed) return null;
            try
            {
                var changedClientObjs = new List<object>();
                ClientElementValueListItem[] changedClientElementValueListItems = DataAccessGrpcList.PollElementValuesChanges();
                foreach (ClientElementValueListItem dataGrpcElementValueListItem in changedClientElementValueListItems)
                {
                    var o = dataGrpcElementValueListItem.Obj as DataAccessGrpcListItemWrapper;
                    if (o is null) throw new InvalidOperationException();
                    foreach (var clientObjectInfo in o.ClientObjectInfosCollection)
                    {
                        if (clientObjectInfo.ClientObj is not null)
                        {
                            changedClientObjs.Add(clientObjectInfo.ClientObj);
                        }
                    }
                }
                return changedClientObjs.ToArray();
            }
            catch
            {
            }
            return null;
        }

        /// <summary>
        ///     Returns clientObjs whose write failed.
        ///     If connection error, no throw and returns all clientObjs. 
        /// </summary>
        /// <param name="clientObjs"></param>
        /// <param name="valueStatusTimestamps"></param>
        /// <returns></returns>
        public (object[], ResultInfo[]) Write(object[] clientObjs, ValueStatusTimestamp[] valueStatusTimestamps)
        {
            if (DataAccessGrpcList is null || DataAccessGrpcList.Disposed)
            {
                var failedResultInfo = new ResultInfo { StatusCode = JobStatusCodes.FailedPrecondition };
                return (clientObjs, Enumerable.Repeat(failedResultInfo, clientObjs.Length).ToArray());
            }                

            int i = -1;
            var objects = new List<object>();
            var resultInfos = new List<ResultInfo>();
            foreach (var clientObj in clientObjs)
            {
                i++;

                ClientObjectInfo? clientObjectInfo;
                if (!ClientObjectInfosDictionary.TryGetValue(clientObj, out clientObjectInfo))
                {
                    objects.Add(clientObj);
                    resultInfos.Add(new ResultInfo { StatusCode = JobStatusCodes.InvalidArgument });
                    continue;
                }                
                
                if (clientObjectInfo.DataAccessGrpcListItemWrapper is null ||
                    clientObjectInfo.DataAccessGrpcListItemWrapper.DataAccessGrpcListItem is null ||
                    clientObjectInfo.DataAccessGrpcListItemWrapper.DataAccessGrpcListItem.AddItemResult is null ||
                    clientObjectInfo.DataAccessGrpcListItemWrapper.DataAccessGrpcListItem.AddItemResult.ResultInfo.StatusCode != JobStatusCodes.OK)
                {
                    objects.Add(clientObj);
                    resultInfos.Add(new ResultInfo { StatusCode = JobStatusCodes.FailedPrecondition });
                    continue;
                }
                ClientElementValueListItem dataGrpcElementValueListItem = clientObjectInfo.DataAccessGrpcListItemWrapper.DataAccessGrpcListItem;
                dataGrpcElementValueListItem.PrepareForWrite(valueStatusTimestamps[i]);
            }

            IEnumerable<ClientElementValueListItem> failedItems;
            try
            {
                failedItems = DataAccessGrpcList.CommitWriteElementValueListItems();
            }
            catch
            {                
                return (clientObjs, Enumerable.Repeat(ResultInfo.UnknownResultInfo, clientObjs.Length).ToArray());
            }

            foreach (var failedItem in failedItems)
            {
                var w = failedItem.Obj as DataAccessGrpcListItemWrapper;
                if (w is null) throw new InvalidOperationException();
                foreach (var clientObjectInfo in w.ClientObjectInfosCollection)
                {
                    if (clientObjectInfo.ClientObj is not null)
                    {
                        objects.Add(clientObjectInfo.ClientObj);
                        resultInfos.Add(failedItem.WriteResultInfo!);
                    }
                }
            }

            return (objects.ToArray(), resultInfos.ToArray());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientObj"></param>
        /// <param name="valueStatusTimestamp"></param>
        public ResultInfo Write(object clientObj, ValueStatusTimestamp valueStatusTimestamp)
        {
            if (DataAccessGrpcList is null || DataAccessGrpcList.Disposed)
                return new ResultInfo { StatusCode = JobStatusCodes.FailedPrecondition };

            ClientObjectInfo? clientObjectInfo;
            if (!ClientObjectInfosDictionary.TryGetValue(clientObj, out clientObjectInfo))
                return new ResultInfo { StatusCode = JobStatusCodes.InvalidArgument };
            
            if (clientObjectInfo.DataAccessGrpcListItemWrapper is null || 
                clientObjectInfo.DataAccessGrpcListItemWrapper.DataAccessGrpcListItem is null ||
                clientObjectInfo.DataAccessGrpcListItemWrapper.DataAccessGrpcListItem.AddItemResult is null ||
                clientObjectInfo.DataAccessGrpcListItemWrapper.DataAccessGrpcListItem.AddItemResult.ResultInfo.StatusCode != JobStatusCodes.OK)
                return new ResultInfo { StatusCode = JobStatusCodes.FailedPrecondition };

            ClientElementValueListItem dataGrpcElementValueListItem = clientObjectInfo.DataAccessGrpcListItemWrapper.DataAccessGrpcListItem;

            try
            {                
                dataGrpcElementValueListItem.PrepareForWrite(valueStatusTimestamp);

                try
                {
                    DataAccessGrpcList.CommitWriteElementValueListItems();

                    return dataGrpcElementValueListItem.WriteResultInfo!;
                }
                catch
                {
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "DataAccessGrpcList.CommitWriteElementValueListItems() exception");
            }

            return ResultInfo.UnknownResultInfo;
        }

        /*
        /// <summary>
        ///     Reads current value from cache, not remote ServerBase.
        /// </summary>
        public Any TryRead(string id)
        {
            var dataAccessGrpcListItem = DataAccessGrpcListItemsDictionary.TryGetValue(id);
            if (dataAccessGrpcListItem is null) return new Any();
            
            return dataAccessGrpcListItem.ValueStatusTimestamp.Value;
        }
        */

        #endregion

        public class ElementValuesCallbackEventArgs : EventArgs
        {
            public List<ElementValuesCallbackChange> ElementValuesCallbackChanges { get; set; } = null!;
        }

        public class ElementValuesCallbackChange
        {
            public object ClientObj = null!;
            
            /// <summary>
            ///     Need update if not null
            /// </summary>
            public AddItemResult? AddItemResult;

            /// <summary>
            ///     Need update if not null
            /// </summary>
            public ValueStatusTimestamp? ValueStatusTimestamp;
        }
    }    
}