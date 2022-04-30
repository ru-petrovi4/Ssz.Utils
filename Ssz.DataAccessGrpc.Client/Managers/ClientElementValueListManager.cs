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

namespace Ssz.DataAccessGrpc.Client.Managers
{
    internal class ClientElementValueListManager : ClientElementListManagerBase<ClientElementValueListItem, ClientElementValueList>
    {
        #region construction and destruction

        public ClientElementValueListManager(ILogger<GrpcDataAccessProvider> logger) :
            base(logger, false)
        {
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Creates List, adds/removes items.
        ///     No throw.
        /// </summary>
        /// <param name="clientConnectionManager"></param>
        /// <param name="сallbackDispatcher"></param>
        /// <param name="elementValuesCallbackEventHandler"></param>
        /// <param name="callbackIsEnabled"></param>
        /// <param name="ct"></param>
        public void Subscribe(ClientConnectionManager clientConnectionManager, IDispatcher? сallbackDispatcher,
            ElementValuesCallbackEventHandler elementValuesCallbackEventHandler, bool callbackIsEnabled, CancellationToken ct)
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
                        if (clientConnectionManager.ConnectionExists)
                        {
                            DataAccessGrpcList = clientConnectionManager.NewElementValueList(null);
                        }                            
                    }
                    catch (Exception)
                    {   
                    }
                }

                bool connectionError = SubscribeInitial();

                try
                {
                    if (!connectionError && DataAccessGrpcList is not null && !DataAccessGrpcList.Disposed)
                    {
                        if (firstTimeDataConnection)
                        {
                            DataAccessGrpcList.ElementValuesCallback +=
                                (ClientElementValueList dataList, ClientElementValueListItem[] items,
                                    ValueStatusTimestamp[] values) =>
                                {
                                    var changedClientObjs = new List<object>(items.Length);
                                    var changedValues = new List<ValueStatusTimestamp>(items.Length);
                                    int i = 0;
                                    foreach (ClientElementValueListItem dataGrpcElementValueListItem in items)
                                    {
                                        var o = dataGrpcElementValueListItem.Obj as DataAccessGrpcListItemWrapper;
                                        if (o is null) throw new InvalidOperationException();
                                        foreach (var modelItem in o.ClientObjectInfosCollection)
                                        {
                                            modelItem.ForceNotifyClientObj = false;
                                            if (modelItem.ClientObj is not null)
                                            {
                                                changedClientObjs.Add(modelItem.ClientObj);
                                                changedValues.Add(values[i]);
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
                                                elementValuesCallbackEventHandler(changedClientObjs.ToArray(), changedValues.ToArray());
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
                    var changedClientObjs = new List<object>();
                    var changedValues = new List<ValueStatusTimestamp>();
                    foreach (DataAccessGrpcListItemWrapper dataGrpcListItemWrapper in DataAccessGrpcListItemWrappersDictionary.Values)
                    {                        
                        foreach (var modelItem in dataGrpcListItemWrapper.ClientObjectInfosCollection)
                        {
                            if (modelItem.ForceNotifyClientObj)
                            {
                                modelItem.ForceNotifyClientObj = false;
                                if (modelItem.ClientObj is not null)
                                {
                                    if (dataGrpcListItemWrapper.ItemDoesNotExist)
                                    {
                                        changedClientObjs.Add(modelItem.ClientObj);
                                        changedValues.Add(new ValueStatusTimestamp { ValueStatusCode = ValueStatusCode.ItemDoesNotExist });
                                    }
                                    else if (dataGrpcListItemWrapper.DataAccessGrpcListItem is not null)
                                    {
                                        changedClientObjs.Add(modelItem.ClientObj);
                                        changedValues.Add(dataGrpcListItemWrapper.DataAccessGrpcListItem.ValueStatusTimestamp);
                                    }
                                    else
                                    {
                                        changedClientObjs.Add(modelItem.ClientObj);
                                        changedValues.Add(new ValueStatusTimestamp(new Any(), ValueStatusCode.Unknown, utcNow));
                                    }                                                                                                  
                                }                                
                            }
                        }                                              
                    }                    
                    if (changedClientObjs.Count > 0)
                    {
                        if (ct.IsCancellationRequested) return;
                        if (сallbackDispatcher is not null)
                        {
                            try
                            {
                                сallbackDispatcher.BeginInvoke(ct =>
                                    elementValuesCallbackEventHandler(changedClientObjs.ToArray(), changedValues.ToArray()));
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
                    foreach (var modelItem in o.ClientObjectInfosCollection)
                    {
                        if (modelItem.ClientObj is not null)
                        {
                            changedClientObjs.Add(modelItem.ClientObj);
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
        public object[] Write(object[] clientObjs, ValueStatusTimestamp[] valueStatusTimestamps)
        {
            if (DataAccessGrpcList is null || DataAccessGrpcList.Disposed) return clientObjs;

            int i = -1;
            var result = new List<object>();
            foreach (var clientObj in clientObjs)
            {
                i++;

                ClientObjectInfo? modelItem;
                if (!ModelItemsDictionary.TryGetValue(clientObj, out modelItem))
                {
                    result.Add(clientObj);
                    continue;
                }                
                
                if (modelItem.DataAccessGrpcListItemWrapper is null ||
                    modelItem.DataAccessGrpcListItemWrapper.DataAccessGrpcListItem is null ||
                    modelItem.DataAccessGrpcListItemWrapper.DataAccessGrpcListItem.StatusCode != StatusCode.OK)
                {
                    result.Add(clientObj);
                    continue;
                }
                ClientElementValueListItem dataGrpcElementValueListItem = modelItem.DataAccessGrpcListItemWrapper.DataAccessGrpcListItem;
                dataGrpcElementValueListItem.PrepareForWrite(valueStatusTimestamps[i]);
            }

            IEnumerable<ClientElementValueListItem> failedItems;
            try
            {
                failedItems = DataAccessGrpcList.CommitWriteElementValueListItems();
            }
            catch
            {
                return clientObjs;
            }

            foreach (var dataGrpcElementValueListItem in failedItems)
            {
                var o = dataGrpcElementValueListItem.Obj as DataAccessGrpcListItemWrapper;
                if (o is null) throw new InvalidOperationException();
                foreach (var modelItem in o.ClientObjectInfosCollection)
                {
                    if (modelItem.ClientObj is not null)
                    {
                        result.Add(modelItem.ClientObj);
                    }
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientObj"></param>
        /// <param name="valueStatusTimestamp"></param>
        public void Write(object clientObj, ValueStatusTimestamp valueStatusTimestamp)
        {
            if (DataAccessGrpcList is null || DataAccessGrpcList.Disposed) return;

            ClientObjectInfo? modelItem;
            if (!ModelItemsDictionary.TryGetValue(clientObj, out modelItem)) return;
            
            if (modelItem.DataAccessGrpcListItemWrapper is null || modelItem.DataAccessGrpcListItemWrapper.DataAccessGrpcListItem is null || modelItem.DataAccessGrpcListItemWrapper.DataAccessGrpcListItem.StatusCode != StatusCode.OK)
            {
                return;
            }

            ClientElementValueListItem dataGrpcElementValueListItem = modelItem.DataAccessGrpcListItemWrapper.DataAccessGrpcListItem;

            try
            {                
                dataGrpcElementValueListItem.PrepareForWrite(valueStatusTimestamp);

                try
                {
                    DataAccessGrpcList.CommitWriteElementValueListItems();
                }
                catch
                {
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "DataAccessGrpcList.CommitWriteElementValueListItems() exception");
            }
        }

        /*
        /// <summary>
        ///     Reads current value from cache, not remote ServerBase.
        /// </summary>
        public Any TryRead(string id)
        {
            var dataGrpcListItem = DataAccessGrpcListItemsDictionary.TryGetValue(id);
            if (dataGrpcListItem is null) return new Any();
            
            return dataGrpcListItem.ValueStatusTimestamp.Value;
        }
        */

        #endregion        

        /// <summary>
        ///     This delegate defines the callback for reporting data updates to the client application.
        /// </summary>
        public delegate void ElementValuesCallbackEventHandler(
            object[] changedClientObjs, ValueStatusTimestamp[] changedValues);
    }
}