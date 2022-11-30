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
            EventHandler<ElementValuesCallbackEventArgs> elementValuesCallbackEventHandler, bool callbackIsEnabled, CancellationToken ct)
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
                                    ValueStatusTimestamp[] vsts) =>
                                {
                                    ElementValuesCallbackEventArgs elementValuesCallbackEventArgs = new();
                                    elementValuesCallbackEventArgs.ElementValuesCallbackChanges = new(items.Length);
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
                                                elementValuesCallbackEventArgs.ElementValuesCallbackChanges.Add(new ElementValuesCallbackChange
                                                {
                                                    ClientObj = modelItem.ClientObj,
                                                    ValueStatusTimestamp = vsts[i],
                                                    DataTypeId = dataGrpcElementValueListItem.DataTypeId?.ToTypeId(),
                                                    IsReadable = dataGrpcElementValueListItem.IsReadable,
                                                    IsWritable = dataGrpcElementValueListItem.IsWritable
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
                                        elementValuesCallbackEventArgs.ElementValuesCallbackChanges.Add(new ElementValuesCallbackChange
                                        {
                                            ClientObj = modelItem.ClientObj,
                                            ValueStatusTimestamp = new ValueStatusTimestamp { ValueStatusCode = ValueStatusCodes.ItemDoesNotExist },                                            
                                        });
                                    }
                                    else if (dataGrpcListItemWrapper.DataAccessGrpcListItem is not null)
                                    {
                                        elementValuesCallbackEventArgs.ElementValuesCallbackChanges.Add(new ElementValuesCallbackChange
                                        {
                                            ClientObj = modelItem.ClientObj,
                                            ValueStatusTimestamp = dataGrpcListItemWrapper.DataAccessGrpcListItem.ValueStatusTimestamp,
                                            DataTypeId = dataGrpcListItemWrapper.DataAccessGrpcListItem.DataTypeId?.ToTypeId(),
                                            IsReadable = dataGrpcListItemWrapper.DataAccessGrpcListItem.IsReadable,
                                            IsWritable = dataGrpcListItemWrapper.DataAccessGrpcListItem.IsWritable
                                        });
                                    }
                                    else
                                    {
                                        elementValuesCallbackEventArgs.ElementValuesCallbackChanges.Add(new ElementValuesCallbackChange
                                        {
                                            ClientObj = modelItem.ClientObj,
                                            ValueStatusTimestamp = new ValueStatusTimestamp(new Any(), ValueStatusCodes.Unknown, utcNow),
                                        });
                                    }                                                                                                  
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
        public (object[], uint[]) Write(object[] clientObjs, ValueStatusTimestamp[] valueStatusTimestamps)
        {
            if (DataAccessGrpcList is null || DataAccessGrpcList.Disposed) 
                return (clientObjs, Enumerable.Repeat(JobStatusCodes.FailedPrecondition, clientObjs.Length).ToArray());

            int i = -1;
            var resultObjects = new List<object>();
            var resultStatusCodes = new List<uint>();
            foreach (var clientObj in clientObjs)
            {
                i++;

                ClientObjectInfo? clientObjectInfo;
                if (!ClientObjectInfosDictionary.TryGetValue(clientObj, out clientObjectInfo))
                {
                    resultObjects.Add(clientObj);
                    resultStatusCodes.Add(JobStatusCodes.InvalidArgument);
                    continue;
                }                
                
                if (clientObjectInfo.DataAccessGrpcListItemWrapper is null ||
                    clientObjectInfo.DataAccessGrpcListItemWrapper.DataAccessGrpcListItem is null ||
                    clientObjectInfo.DataAccessGrpcListItemWrapper.DataAccessGrpcListItem.StatusCode != StatusCode.OK)
                {
                    resultObjects.Add(clientObj);
                    resultStatusCodes.Add(JobStatusCodes.FailedPrecondition);
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
                return (clientObjs, Enumerable.Repeat(JobStatusCodes.UnknownError, clientObjs.Length).ToArray());
            }

            foreach (var dataGrpcElementValueListItem in failedItems)
            {
                var o = dataGrpcElementValueListItem.Obj as DataAccessGrpcListItemWrapper;
                if (o is null) throw new InvalidOperationException();
                foreach (var modelItem in o.ClientObjectInfosCollection)
                {
                    if (modelItem.ClientObj is not null)
                    {
                        resultObjects.Add(modelItem.ClientObj);
                        resultStatusCodes.Add(dataGrpcElementValueListItem.WriteStatusCode);
                    }
                }
            }

            return (resultObjects.ToArray(), resultStatusCodes.ToArray());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientObj"></param>
        /// <param name="valueStatusTimestamp"></param>
        public uint Write(object clientObj, ValueStatusTimestamp valueStatusTimestamp)
        {
            if (DataAccessGrpcList is null || DataAccessGrpcList.Disposed)
                return JobStatusCodes.FailedPrecondition;

            ClientObjectInfo? clientObjectInfo;
            if (!ClientObjectInfosDictionary.TryGetValue(clientObj, out clientObjectInfo))
                return JobStatusCodes.InvalidArgument;
            
            if (clientObjectInfo.DataAccessGrpcListItemWrapper is null || clientObjectInfo.DataAccessGrpcListItemWrapper.DataAccessGrpcListItem is null || clientObjectInfo.DataAccessGrpcListItemWrapper.DataAccessGrpcListItem.StatusCode != StatusCode.OK)
                return JobStatusCodes.InvalidArgument;

            ClientElementValueListItem dataGrpcElementValueListItem = clientObjectInfo.DataAccessGrpcListItemWrapper.DataAccessGrpcListItem;

            try
            {                
                dataGrpcElementValueListItem.PrepareForWrite(valueStatusTimestamp);

                try
                {
                    DataAccessGrpcList.CommitWriteElementValueListItems();

                    return dataGrpcElementValueListItem.WriteStatusCode;
                }
                catch
                {
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "DataAccessGrpcList.CommitWriteElementValueListItems() exception");
            }

            return JobStatusCodes.UnknownError;
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
    }

    public class ElementValuesCallbackEventArgs : EventArgs
    {
        public List<ElementValuesCallbackChange> ElementValuesCallbackChanges { get; set; } = null!;
    }

    public class ElementValuesCallbackChange
    {
        public object ClientObj = null!;

        public ValueStatusTimestamp ValueStatusTimestamp;

        public Ssz.Utils.DataAccess.TypeId? DataTypeId;

        public bool? IsReadable;

        public bool? IsWritable;
    }
}