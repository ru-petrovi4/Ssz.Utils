﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Ssz.Xi.Client.Api.ListItems;
using Ssz.Xi.Client.Api.Lists;
using Ssz.Xi.Client.Internal.ListItems;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Api
{
    public class XiDataListItemsManager : XiListItemsManager<IXiDataListItem, IXiDataListProxy>
    {
        private bool _callbackable;

        #region public functions

        /// <summary>
        ///     Creates List, adds/removes items.
        ///     No throw.
        /// </summary>
        /// <param name="xiServerProxy"></param>
        /// <param name="сallbackDoer"></param>
        /// <param name="elementValuesCallbackEventHandler"></param>
        /// <param name="callbackable"></param>
        /// <param name="cancellationToken"></param>
        public void Subscribe(XiServerProxy xiServerProxy, IDispatcher? сallbackDoer,
            ElementValuesCallbackEventHandler elementValuesCallbackEventHandler, bool callbackable, bool unsubscribeItemsFromServer, CancellationToken cancellationToken)
        {
            _callbackable = callbackable;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!XiItemsMustBeAddedOrRemoved) return;               

                bool firstTimeDataConnection = (XiList is null);

                if (firstTimeDataConnection)
                {
                    try
                    {
                        if (xiServerProxy.ContextExists)
                            XiList = xiServerProxy.NewDataList(0, 0, null);
                    }
                    catch (Exception)
                    {   
                    }
                }

                bool connectionError = SubscribeInitial(unsubscribeItemsFromServer);

                try
                {
                    if (!connectionError && XiList is not null && !XiList.Disposed)
                    {
                        if (firstTimeDataConnection)
                        {
                            XiList.ElementValuesCallback +=
                                (IXiDataListProxy dataList, IXiDataListItem[] items,
                                    ValueStatusTimestamp[] values) =>
                                {
                                    var changedClientObjs = new List<object>(items.Length);
                                    var changedValues = new List<ValueStatusTimestamp>(items.Length);
                                    int i = 0;
                                    foreach (IXiDataListItem xiDataListItem in items)
                                    {
                                        var o = xiDataListItem.Obj as XiListItemWrapper;
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
                                    cancellationToken.ThrowIfCancellationRequested();
                                    Logger?.LogDebug("XiList.ElementValuesCallback");
                                    if (сallbackDoer is not null)
                                    {
                                        try
                                        {
                                            сallbackDoer.BeginInvoke(ct =>
                                            {
                                                Logger?.LogDebug("XiList.ElementValuesCallback dispatched");
                                                elementValuesCallbackEventHandler(changedClientObjs.ToArray(), changedValues.ToArray());
                                            });
                                        }
                                        catch (Exception)
                                        {
                                        }
                                    }
                                };                                                      

                            XiList.EnableListUpdating(true);
                        }

                        XiList.EnableListElementUpdating(true, null);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Logger?.LogWarning(ex, "Exception");
                    connectionError = true;
                }

                {
                    var utcNow = DateTime.UtcNow;
                    var changedClientObjs = new List<object>();
                    var changedValues = new List<ValueStatusTimestamp>();
                    foreach (XiListItemWrapper xiListItemWrapper in XiListItemWrappersDictionary.Values)
                    {                        
                        foreach (var modelItem in xiListItemWrapper.ClientObjectInfosCollection)
                        {
                            if (modelItem.ForceNotifyClientObj)
                            {
                                modelItem.ForceNotifyClientObj = false;
                                if (modelItem.ClientObj is not null)
                                {
                                    if (xiListItemWrapper.ItemDoesNotExist)
                                    {
                                        changedClientObjs.Add(modelItem.ClientObj);
                                        changedValues.Add(new ValueStatusTimestamp { StatusCode = StatusCodes.BadNodeIdUnknown });
                                    }
                                    else if (xiListItemWrapper.XiListItem is not null)
                                    {
                                        changedClientObjs.Add(modelItem.ClientObj);
                                        changedValues.Add(xiListItemWrapper.XiListItem.ValueStatusTimestamp);
                                    }
                                    else
                                    {
                                        changedClientObjs.Add(modelItem.ClientObj);
                                        changedValues.Add(new ValueStatusTimestamp(new Any(), StatusCodes.Uncertain, utcNow));
                                    }                                                                                                  
                                }                                
                            }
                        }                                              
                    }                    
                    if (changedClientObjs.Count > 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (сallbackDoer is not null)
                        {
                            try
                            {
                                сallbackDoer.BeginInvoke(ct =>
                                    elementValuesCallbackEventHandler(changedClientObjs.ToArray(), changedValues.ToArray()));
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }

                if (!connectionError)
                    XiItemsMustBeAddedOrRemoved = false;                
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
            if (XiList is null || XiList.Disposed) return null;
            //if (XiList.Pollable)
            //{
            //    try
            //    {
            //        var changedClientObjs = new List<object>();
            //        IXiDataListItem[] changedXiDataListItems = XiList.PollDataChanges();
            //        foreach (IXiDataListItem xiDataListItem in changedXiDataListItems)
            //        {
            //            var o = xiDataListItem.Obj as XiListItemWrapper;
            //            if (o is null) throw new InvalidOperationException();
            //            foreach (var modelItem in o.ClientObjectInfosCollection)
            //            {                            
            //                if (modelItem.ClientObj is not null)
            //                {
            //                    changedClientObjs.Add(modelItem.ClientObj);                                
            //                }
            //            }                        
            //        }
            //        return changedClientObjs.ToArray();
            //    }
            //    catch
            //    {   
            //    }
            //}
            return null;
        }

        /// <summary>
        ///     Invokes XiList.PollDataChanges() if XiList Pollable and not Callbackable.
        ///     No throw.
        /// </summary>
        public void PollChangesIfNotCallbackable()
        {
            if (XiList is null || XiList.Disposed) return;
            //if (XiList.Pollable && !XiList.Callbackable)
            //{
            //    try
            //    {
            //        XiList.PollDataChanges();                    
            //    }
            //    catch
            //    {                    
            //    }
            //}            
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
            if (XiList is null || XiList.Disposed) return clientObjs;

            int i = -1;
            var result = new List<object>();
            foreach (var clientObj in clientObjs)
            {
                i++;

                ClientObjectInfo? modelItem;
                if (!ClientObjectInfosDictionary.TryGetValue(clientObj, out modelItem))
                {
                    result.Add(clientObj);
                    continue;
                }                
                
                if (modelItem.XiListItemWrapper is null ||
                    modelItem.XiListItemWrapper.XiListItem is null ||
                    modelItem.XiListItemWrapper.XiListItem.ResultCode != XiFaultCodes.S_OK)
                {
                    result.Add(clientObj);
                    continue;
                }
                IXiDataListItem xiDataListItem = modelItem.XiListItemWrapper.XiListItem;
                xiDataListItem.PrepareForWrite(valueStatusTimestamps[i]);
            }

            IEnumerable<IXiDataListItem>? failedItems = null;
            try
            {
                failedItems = XiList.CommitWriteDataListItems();
            }
            catch
            {
                return clientObjs;
            }

            if (failedItems is not null)
            {
                foreach (var xiDataListItem in failedItems)
                {
                    var o = xiDataListItem.Obj as XiListItemWrapper;
                    if (o is null) throw new InvalidOperationException();
                    foreach (var modelItem in o.ClientObjectInfosCollection)
                    {
                        if (modelItem.ClientObj is not null)
                        {
                            result.Add(modelItem.ClientObj);
                        }
                    }
                }
            }

            return result.ToArray();
        }

        
        public void Write(object clientObj, ValueStatusTimestamp valueStatusTimestamp)
        {
            if (XiList is null || XiList.Disposed) return;

            ClientObjectInfo? modelItem;
            if (!ClientObjectInfosDictionary.TryGetValue(clientObj, out modelItem)) return;
            
            if (modelItem.XiListItemWrapper is null || modelItem.XiListItemWrapper.XiListItem is null || modelItem.XiListItemWrapper.XiListItem.ResultCode != XiFaultCodes.S_OK)
            {
                return;
            }

            IXiDataListItem xiDataListItem = modelItem.XiListItemWrapper.XiListItem;

            try
            {                
                xiDataListItem.PrepareForWrite(valueStatusTimestamp);

                try
                {
                    XiList.CommitWriteDataListItems();
                }
                catch
                {
                }
            }
            catch (Exception ex)
            {
                Logger?.LogWarning(ex, "Exception");
            }
        }

        public override InstanceId GetInstanceId(string id)
        {
            return new InstanceId(InstanceIds.ResourceType_DA, XiSystem, id);
        }

        /*
        /// <summary>
        ///     Reads current value from cache, not remote server.
        /// </summary>
        public Any TryRead(string id)
        {
            var xiListItem = XiListItemsDictionary.TryGetValue(id);
            if (xiListItem is null) return new Any();
            
            return xiListItem.ValueStatusTimestamp.Value;
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