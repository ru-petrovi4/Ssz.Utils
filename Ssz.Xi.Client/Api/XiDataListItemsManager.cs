using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        #region public functions

        /// <summary>
        ///     Creates List, adds/removes items.
        ///     No throw.
        /// </summary>
        /// <param name="xiServerProxy"></param>
        /// <param name="сallbackDoer"></param>
        /// <param name="elementValuesCallbackEventHandler"></param>
        /// <param name="callbackable"></param>
        /// <param name="ct"></param>
        public void Subscribe(XiServerProxy xiServerProxy, IDispatcher? сallbackDoer,
            ElementValuesCallbackEventHandler elementValuesCallbackEventHandler, bool callbackable, CancellationToken ct)
        {
            try
            {
                if (ct.IsCancellationRequested) return;                
                if (!XiItemsMustBeAddedOrRemoved) return;               

                bool firstTimeDataConnection = (XiList == null);

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

                bool connectionError = SubscribeInitial();

                try
                {
                    if (!connectionError && XiList != null && !XiList.Disposed)
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
                                        if (o == null) throw new InvalidOperationException();
                                        foreach (var modelItem in o.ModelItems)
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
                                    Logger.Verbose("XiList.ElementValuesCallback");
                                    if (сallbackDoer != null)
                                    {
                                        try
                                        {
                                            сallbackDoer.BeginInvoke(ct =>
                                            {
                                                Logger.Verbose("XiList.ElementValuesCallback dispatched");
                                                elementValuesCallbackEventHandler(changedClientObjs.ToArray(), changedValues.ToArray());
                                            });
                                        }
                                        catch (Exception)
                                        {
                                        }
                                    }
                                };
                            if (callbackable) XiList.Callbackable = true;
                            XiList.Pollable = true;
                            XiList.Readable = true;
                            XiList.Writeable = true;

                            XiList.EnableListUpdating(true);
                        }

                        XiList.EnableListElementUpdating(true, null);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex);
                    connectionError = true;
                }

                {
                    var utcNow = DateTime.UtcNow;
                    var changedClientObjs = new List<object>();
                    var changedValues = new List<ValueStatusTimestamp>();
                    foreach (XiListItemWrapper xiListItemWrapper in XiListItemWrappersDictionary.Values)
                    {                        
                        foreach (var modelItem in xiListItemWrapper.ModelItems)
                        {
                            if (modelItem.ForceNotifyClientObj)
                            {
                                modelItem.ForceNotifyClientObj = false;
                                if (modelItem.ClientObj != null)
                                {
                                    if (xiListItemWrapper.ItemDoesNotExist)
                                    {
                                        changedClientObjs.Add(modelItem.ClientObj);
                                        changedValues.Add(new ValueStatusTimestamp(new Any(), StatusCodes.ItemDoesNotExist, utcNow));
                                    }
                                    else if (xiListItemWrapper.XiListItem != null)
                                    {
                                        changedClientObjs.Add(modelItem.ClientObj);
                                        changedValues.Add(xiListItemWrapper.XiListItem.ValueStatusTimestamp);
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
                        if (сallbackDoer != null)
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
            if (XiList == null || XiList.Disposed) return null;
            if (XiList.Pollable)
            {
                try
                {
                    var changedClientObjs = new List<object>();
                    IXiDataListItem[] changedXiDataListItems = XiList.PollDataChanges();
                    foreach (IXiDataListItem xiDataListItem in changedXiDataListItems)
                    {
                        var o = xiDataListItem.Obj as XiListItemWrapper;
                        if (o == null) throw new InvalidOperationException();
                        foreach (var modelItem in o.ModelItems)
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
            }
            return null;
        }

        /// <summary>
        ///     Invokes XiList.PollDataChanges() if XiList Pollable and not Callbackable.
        ///     No throw.
        /// </summary>
        public void PollChangesIfNotCallbackable()
        {
            if (XiList == null || XiList.Disposed) return;
            if (XiList.Pollable && !XiList.Callbackable)
            {
                try
                {
                    XiList.PollDataChanges();                    
                }
                catch
                {                    
                }
            }            
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
            if (XiList == null || XiList.Disposed) return clientObjs;

            int i = -1;
            var result = new List<object>();
            foreach (var clientObj in clientObjs)
            {
                i++;

                ModelItem? modelItem;
                if (!ModelItemsDictionary.TryGetValue(clientObj, out modelItem))
                {
                    result.Add(clientObj);
                    continue;
                }                
                
                if (modelItem.XiListItemWrapper == null ||
                    modelItem.XiListItemWrapper.XiListItem == null ||
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

            if (failedItems != null)
            {
                foreach (var xiDataListItem in failedItems)
                {
                    var o = xiDataListItem.Obj as XiListItemWrapper;
                    if (o == null) throw new InvalidOperationException();
                    foreach (var modelItem in o.ModelItems)
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

        
        public void Write(object clientObj, ValueStatusTimestamp valueStatusTimestamp)
        {
            if (XiList == null || XiList.Disposed) return;

            ModelItem? modelItem;
            if (!ModelItemsDictionary.TryGetValue(clientObj, out modelItem)) return;
            
            if (modelItem.XiListItemWrapper == null || modelItem.XiListItemWrapper.XiListItem == null || modelItem.XiListItemWrapper.XiListItem.ResultCode != XiFaultCodes.S_OK)
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
                Logger.Warning(ex);
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
            if (xiListItem == null) return new Any();
            
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