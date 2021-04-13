using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Ssz.Utils;
using Ssz.DataGrpc.Client.ClientListItems;
using Microsoft.Extensions.Logging;
using Ssz.DataGrpc.Client.ClientLists;
using Ssz.DataGrpc.Common;
using Ssz.Utils.DataAccess;

namespace Ssz.DataGrpc.Client.Managers
{
    public class ClientElementValueListManager : ClientElementListManagerBase<ClientElementValueListItem, ClientElementValueList>
    {
        #region construction and destruction

        public ClientElementValueListManager(ILogger<DataGrpcProvider> logger) :
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
                if (!DataGrpcItemsMustBeAddedOrRemoved) return;               

                bool firstTimeDataConnection = (DataGrpcList == null);

                if (firstTimeDataConnection)
                {
                    try
                    {
                        if (clientConnectionManager.ConnectionExists)
                        {
                            DataGrpcList = clientConnectionManager.NewElementValueList(null);
                        }                            
                    }
                    catch (Exception)
                    {   
                    }
                }

                bool connectionError = SubscribeInitial();

                try
                {
                    if (!connectionError && DataGrpcList != null && !DataGrpcList.Disposed)
                    {
                        if (firstTimeDataConnection)
                        {
                            DataGrpcList.ElementValuesCallback +=
                                (ClientElementValueList dataList, ClientElementValueListItem[] items,
                                    ValueStatusTimestamp[] values) =>
                                {
                                    var changedClientObjs = new List<object>(items.Length);
                                    var changedValues = new List<ValueStatusTimestamp>(items.Length);
                                    int i = 0;
                                    foreach (ClientElementValueListItem dataGrpcElementValueListItem in items)
                                    {
                                        var o = dataGrpcElementValueListItem.Obj as DataGrpcListItemWrapper;
                                        if (o == null) throw new InvalidOperationException();
                                        foreach (var modelItem in o.ClientObjectInfosCollection)
                                        {
                                            modelItem.ForceNotifyClientObj = false;
                                            if (modelItem.ClientObj != null)
                                            {
                                                changedClientObjs.Add(modelItem.ClientObj);
                                                changedValues.Add(values[i]);
                                            }
                                        }
                                        i++;
                                    }
                                    if (ct.IsCancellationRequested) return;
                                    Logger.LogDebug("DataGrpcList.ElementValuesCallback");
                                    if (сallbackDispatcher != null)
                                    {
                                        try
                                        {
                                            сallbackDispatcher.BeginInvoke(ct =>
                                            {
                                                Logger.LogDebug("DataGrpcList.ElementValuesCallback dispatched");
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
                                DataGrpcList.EnableListCallback(true);
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
                    foreach (DataGrpcListItemWrapper dataGrpcListItemWrapper in DataGrpcListItemWrappersDictionary.Values)
                    {                        
                        foreach (var modelItem in dataGrpcListItemWrapper.ClientObjectInfosCollection)
                        {
                            if (modelItem.ForceNotifyClientObj)
                            {
                                modelItem.ForceNotifyClientObj = false;
                                if (modelItem.ClientObj != null)
                                {
                                    if (dataGrpcListItemWrapper.ItemDoesNotExist)
                                    {
                                        changedClientObjs.Add(modelItem.ClientObj);
                                        changedValues.Add(new ValueStatusTimestamp(new Any(), StatusCodes.ItemDoesNotExist, utcNow));
                                    }
                                    else if (dataGrpcListItemWrapper.DataGrpcListItem != null)
                                    {
                                        changedClientObjs.Add(modelItem.ClientObj);
                                        changedValues.Add(dataGrpcListItemWrapper.DataGrpcListItem.ValueStatusTimestamp);
                                    }
                                    else
                                    {
                                        changedClientObjs.Add(modelItem.ClientObj);
                                        changedValues.Add(new ValueStatusTimestamp(new Any(), StatusCodes.Unknown, utcNow));
                                    }                                                                                                  
                                }                                
                            }
                        }                                              
                    }                    
                    if (changedClientObjs.Count > 0)
                    {
                        if (ct.IsCancellationRequested) return;
                        if (сallbackDispatcher != null)
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
                    DataGrpcItemsMustBeAddedOrRemoved = false;
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
            if (DataGrpcList == null || DataGrpcList.Disposed) return null;
            try
            {
                var changedClientObjs = new List<object>();
                ClientElementValueListItem[] changedClientElementValueListItems = DataGrpcList.PollElementValuesChanges();
                foreach (ClientElementValueListItem dataGrpcElementValueListItem in changedClientElementValueListItems)
                {
                    var o = dataGrpcElementValueListItem.Obj as DataGrpcListItemWrapper;
                    if (o == null) throw new InvalidOperationException();
                    foreach (var modelItem in o.ClientObjectInfosCollection)
                    {
                        if (modelItem.ClientObj != null)
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
            if (DataGrpcList == null || DataGrpcList.Disposed) return clientObjs;

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
                
                if (modelItem.DataGrpcListItemWrapper == null ||
                    modelItem.DataGrpcListItemWrapper.DataGrpcListItem == null ||
                    modelItem.DataGrpcListItemWrapper.DataGrpcListItem.ResultCode != DataGrpcResultCodes.S_OK)
                {
                    result.Add(clientObj);
                    continue;
                }
                ClientElementValueListItem dataGrpcElementValueListItem = modelItem.DataGrpcListItemWrapper.DataGrpcListItem;
                dataGrpcElementValueListItem.PrepareForWrite(valueStatusTimestamps[i]);
            }

            IEnumerable<ClientElementValueListItem>? failedItems = null;
            try
            {
                failedItems = DataGrpcList.CommitWriteElementValueListItems();
            }
            catch
            {
                return clientObjs;
            }

            if (failedItems != null)
            {
                foreach (var dataGrpcElementValueListItem in failedItems)
                {
                    var o = dataGrpcElementValueListItem.Obj as DataGrpcListItemWrapper;
                    if (o == null) throw new InvalidOperationException();
                    foreach (var modelItem in o.ClientObjectInfosCollection)
                    {
                        if (modelItem.ClientObj != null)
                        {
                            result.Add(modelItem.ClientObj);
                        }
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
            if (DataGrpcList == null || DataGrpcList.Disposed) return;

            ClientObjectInfo? modelItem;
            if (!ModelItemsDictionary.TryGetValue(clientObj, out modelItem)) return;
            
            if (modelItem.DataGrpcListItemWrapper == null || modelItem.DataGrpcListItemWrapper.DataGrpcListItem == null || modelItem.DataGrpcListItemWrapper.DataGrpcListItem.ResultCode != DataGrpcResultCodes.S_OK)
            {
                return;
            }

            ClientElementValueListItem dataGrpcElementValueListItem = modelItem.DataGrpcListItemWrapper.DataGrpcListItem;

            try
            {                
                dataGrpcElementValueListItem.PrepareForWrite(valueStatusTimestamp);

                try
                {
                    DataGrpcList.CommitWriteElementValueListItems();
                }
                catch
                {
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "DataGrpcList.CommitWriteElementValueListItems() exception");
            }
        }

        /*
        /// <summary>
        ///     Reads current value from cache, not remote server.
        /// </summary>
        public Any TryRead(string id)
        {
            var dataGrpcListItem = DataGrpcListItemsDictionary.TryGetValue(id);
            if (dataGrpcListItem == null) return new Any();
            
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