using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Ssz.Xi.Client.Api;
using Ssz.Xi.Client.Api.EventHandlers;
using Ssz.Xi.Client.Api.ListItems;
using Ssz.Xi.Client.Api.Lists;
using Ssz.Xi.Client.Internal.Context;
using Ssz.Xi.Client.Internal.ListItems;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Internal.Lists
{
    /// <summary>
    ///     This class implements the XiDataList interface.
    /// </summary>
    internal class XiDataList : XiDataAndDataJournalListBase<XiDataListItem>, IXiDataListProxy
    {
        #region construction and destruction

        /// <summary>
        ///     This constructor creates a new Data List.
        /// </summary>
        /// <param name="context"> The context to which this data lList belongs. </param>
        /// <param name="updateRate"> The UpdateRate for this data list. </param>
        /// <param name="bufferingRate"> The BufferingRate for this data list. Set to 0 if not used. </param>
        /// <param name="filterSet"> The FilterSet for this data list. Set to null if not used. </param>
        public XiDataList(XiContext context, uint updateRate, uint bufferingRate, FilterSet? filterSet)
            : base(context)
        {
            StandardListType = global::Xi.Contracts.Constants.StandardListType.DataList;
            ListAttributes = Context.DefineList(this, updateRate, bufferingRate, filterSet);
        }

        #endregion

        #region public functions

        /// <summary>
        ///     This method is used to created and add a new data object to a Data List.  The new data
        ///     object is created using its InstanceId.
        /// </summary>
        /// <param name="instanceId"> The InstanceId of the data object to create and add. </param>
        /// <returns> Returns the newly created data object. </returns>
        public IXiDataListItem PrepareAddItem(InstanceId instanceId)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiDataList.");

            var dataListItem = new XiDataListItem(0, instanceId);
            dataListItem.ClientAlias = ListItemsManager.Add(dataListItem);
            dataListItem.PreparedForAdd = true;
            return dataListItem;
        }

        public IEnumerable<IXiDataListItem>? CommitAddItems()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiDataList.");

            return CommitAddItemsInternal();
        }

        public IEnumerable<IXiDataListItem>? CommitRemoveItems()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiDataList.");

            return CommitRemoveItemsInternal();
        }

        /// <summary>
        ///     <para>
        ///         This method is used to enable or disable updating of individual elements of a list. If the
        ///         dataObjectsToEnableOrDisable parameter is null, then all elements of the list are enabled/disabled. This call
        ///         does not change the enabled state of the list itself.
        ///     </para>
        ///     <para>
        ///         When an element of the list is disabled, the server excludes it from participating in callbacks and polls.
        ///         However, at the option of the server, the server may continue updating its cache for the element.
        ///     </para>
        /// </summary>
        /// <param name="enableUpdating">
        ///     Indicates, when TRUE, that updating of the list is to be enabled, and when FALSE, that
        ///     updating of the list is to be disabled.
        /// </param>
        /// <param name="dataObjectsToEnableOrDisable"> The list of data objects to be enabled or disabled. </param>
        public void EnableListElementUpdating(bool enableUpdating,
            IEnumerable<IXiDataListItem>? dataObjectsToEnableOrDisable)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiDataList.");

            List<uint>? serverAliases = null;
            if (null != dataObjectsToEnableOrDisable)
            {
                serverAliases = new List<uint>();
                foreach (XiDataListItem item in dataObjectsToEnableOrDisable)
                {
                    serverAliases.Add(item.ServerAlias);
                    item.Enabled = enableUpdating;
                    item.ResultCode = XiFaultCodes.S_OK;
                }
            }
            else
            {
                foreach (XiDataListItem item in ListItemsManager)
                {
                    item.Enabled = enableUpdating;
                    item.ResultCode = XiFaultCodes.S_OK;
                }
            }

            IEnumerable<AliasResult>? listAliasResult = Context.EnableListElementUpdating(ServerListId, enableUpdating,
                serverAliases);

            if (listAliasResult is not null)
                foreach (AliasResult ar in listAliasResult)
                {
                    // Each result in the list identifies a Data Object that failed. 
                    // Data objects that succeeded are not in the list
                    XiDataListItem? item;
                    ListItemsManager.TryGetValue(ar.ClientAlias, out item);
                    if (item is not null)
                    {
                        item.Enabled = false;
                        item.ResultCode = ar.Result;
                    }
                }
        }

        /// <summary>
        ///     This method is invoked to issue a Read request to the Xi Server to read
        ///     the specified data objects.
        /// </summary>
        public IEnumerable<IXiDataListItem>? CommitReadDataListItems()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiDataList.");

            var serverAliases = new List<uint>();

            foreach (XiDataListItem dataListItem in ListItemsManager)
            {
                if (dataListItem.PreparedForRead)
                {
                    serverAliases.Add(dataListItem.ServerAlias);
                    dataListItem.HasRead();
                }
            }

            DataValueArraysWithAlias? readValueArrays = Context.ReadData(ServerListId, serverAliases);

            UpdateData(readValueArrays, false);

            return null; // TODO: !!!
        }

        public IEnumerable<IXiDataListItem>? CommitTouchDataListItems()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiDataList.");

            List<IXiDataListItem>? result = null;
            var serverAliases = new List<uint>();

            foreach (XiDataListItem dataListItem in ListItemsManager)
            {
                if (dataListItem.PreparedForTouch)
                {
                    serverAliases.Add(dataListItem.ServerAlias);
                    dataListItem.HasTouched();
                }
            }

            IEnumerable<AliasResult>? results = Context.TouchDataObjects(ServerListId, serverAliases);

            if (results is not null)
            {
                result = new List<IXiDataListItem>();
                foreach (AliasResult aliasResult in results)
                {
                    XiDataListItem? item;
                    ListItemsManager.TryGetValue(aliasResult.ClientAlias, out item);
                    if (item is not null) result.Add(item);
                }
            }

            return result;
        }


        /// <summary>
        ///     <para>
        ///         Writing data object values to the server is a two step process composed of preparing a list of data objects
        ///         to be written, followed by writing that list to the server.
        ///     </para>
        ///     <para>
        ///         This method is used in the first step to individually mark each data object in the Data List as ready for
        ///         writing. It examines all data objects in the Data List that are ready for writing and writes them to the server
        ///         .
        ///     </para>
        /// </summary>
        /// <returns>
        ///     The list of data objects whose write failed. Results are not returned data object whose writes succeeded. If
        ///     all writes succeeded, null is returned.
        /// </returns>
        public IEnumerable<IXiDataListItem>? CommitWriteDataListItems()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiDataList.");

            DataValueArraysWithAlias? writeValueArrays = null;
            var writeValueDictionary = new Dictionary<uint, XiDataListItem>();

            int uintCount = 0;
            int dblCount = 0;
            int objCount = 0;

            foreach (XiDataListItem item in ListItemsManager)
            {
                if (item.PendingWriteValueStatusTimestamp is not null &&
                    !ValueStatusCodes.IsUncertain(item.PendingWriteValueStatusTimestamp.Value.ValueStatusCode))
                {
                    switch (item.PendingWriteValueStatusTimestamp.Value.Value.ValueStorageType)
                    {
                        case Any.StorageType.Double:
                            dblCount += 1;
                            break;
                        case Any.StorageType.UInt32:
                            uintCount += 1;
                            break;
                        case Any.StorageType.Object:
                            objCount += 1;
                            break;
                    }

                    writeValueDictionary.Add(item.ClientAlias, item);
                }
            }
            if (0 < dblCount || 0 < uintCount || 0 < objCount)
            {
                writeValueArrays = new DataValueArraysWithAlias(dblCount, uintCount, objCount);
                int intIdx = 0;
                int dblIdx = 0;
                int objIdx = 0;
                foreach (var kvp in writeValueDictionary)
                {
                    XiDataListItem item = kvp.Value;
                    if (item.PendingWriteValueStatusTimestamp is not null &&
                        !ValueStatusCodes.IsUncertain(item.PendingWriteValueStatusTimestamp.Value.ValueStatusCode))
                    {
                        var statusCode = XiStatusCode.MakeStatusCode(
                            XiStatusCode.MakeStatusByte((byte)XiStatusCodeStatusBits.GoodNonSpecific, 0),
                            XiStatusCode.MakeFlagsByte((byte)XiStatusCodeHistoricalValueType.NotUsed, false, false,
                                XiStatusCodeAdditionalDetailType.NotUsed),
                            0);
                        switch (item.PendingWriteValueStatusTimestamp.Value.Value.ValueStorageType)
                        {
                            case Any.StorageType.Double:
                                writeValueArrays.SetDouble(dblIdx++, item.ServerAlias,
                                    statusCode,
                                    item.PendingWriteValueStatusTimestamp.Value.TimestampUtc,
                                    item.PendingWriteValueStatusTimestamp.Value.Value.StorageDouble);
                                break;
                            case Any.StorageType.UInt32:
                                writeValueArrays.SetUint(intIdx++, item.ServerAlias,
                                    statusCode,
                                    item.PendingWriteValueStatusTimestamp.Value.TimestampUtc,
                                    item.PendingWriteValueStatusTimestamp.Value.Value.StorageUInt32);
                                break;
                            case Any.StorageType.Object:
                                writeValueArrays.SetObject(objIdx++, item.ServerAlias,
                                    statusCode,
                                    item.PendingWriteValueStatusTimestamp.Value.TimestampUtc,
                                    item.PendingWriteValueStatusTimestamp.Value.Value.StorageObject);
                                break;
                        }
                    }
                    item.HasWritten(XiFaultCodes.S_OK);
                }
            }

            var listXiValues = new List<XiDataListItem>();
            if (writeValueArrays is not null)
            {
                List<AliasResult>? listAliasResult = Context.WriteData(ServerListId, writeValueArrays);
                if (listAliasResult is not null)
                {
                    foreach (AliasResult aliasResult in listAliasResult)
                    {
                        XiDataListItem? item = null;
                        if (writeValueDictionary.TryGetValue(aliasResult.ClientAlias, out item))
                        {
                            item.HasWritten(aliasResult.Result);
                            listXiValues.Add(item);
                        }
                    }
                }
            }
            writeValueDictionary.Clear();
            return (0 != listXiValues.Count) ? listXiValues : null;
        }

        /// <summary>
        ///     <para> Throws or returns changed IXiDataListItems (not null, but possibly zero-lenghth). </para>
        ///     <para> This method is used to poll the endpoint for changes. </para>
        ///     <para> Changes consists of: </para>
        ///     <para> 1) values for data objects that were added to the list, </para>
        ///     <para>
        ///         2) values for data objects whose current values have changed since the last time they were reported to the
        ///         client via this interface. If a deadband filter has been defined for the list, floating point values are not
        ///         considered to have changed unless they have changed by the deadband amount.
        ///     </para>
        ///     <para> 3) historical values that meet the list filter criteria, including the deadband. </para>
        /// </summary>
        public IXiDataListItem[] PollDataChanges()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiDataList.");

            return Context.PollDataChanges(this);
        }

        /// <summary>
        ///     result is not null.
        ///     This method is invoked as part of the callback processing.  It then invokes
        ///     or fires the event to notify the Xi client of data updates.
        /// </summary>
        /// <param name="dataValueArraysWithAlias"> </param>
        public List<IXiDataListItem>? OnElementValuesCallback(DataValueArraysWithAlias? dataValueArraysWithAlias)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiDataList.");

            return UpdateData(dataValueArraysWithAlias, true);
        }

        /// <summary>
        ///     Throws or invokes ElementValuesCallback event.
        ///     changedListItems is not null, changedValues is not null
        /// </summary>
        /// <param name="changedListItems"></param>
        /// <param name="changedValues"></param>
        public void RaiseElementValuesCallbackEvent(IEnumerable<IXiDataListItem> changedListItems,
            IEnumerable<ValueStatusTimestamp> changedValues)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiDataList.");

            if (changedListItems is null) throw new ArgumentNullException(@"changedListItems");
            if (changedValues is null) throw new ArgumentNullException(@"changedValues");

            try
            {
                XiElementValuesCallbackEventHandler elementValuesCallback = ElementValuesCallback;
                if (elementValuesCallback is not null)
                    elementValuesCallback(this, changedListItems.ToArray(), changedValues.ToArray());
            }
            catch (Exception ex)
            {
                _lastElementValuesCallbackExceptionMessage = ex.Message;
            }
        }

        /// <summary>
        ///     Xi clients subscribe to this event to obtain the data update callbacks.
        /// </summary>
        public event XiElementValuesCallbackEventHandler ElementValuesCallback = delegate { };

        public IEnumerable<IXiDataListItem> ListItems
        {
            get { return ListItemsManager.ToArray(); }
        }

        #endregion

        #region private functions

        /// <summary>
        ///     If returnChangedListItems == true, result is not null. Otherwise result is null.
        ///     This method processes the data value arrays received from the server by locating the data objects in the
        ///     data list for which values have been received and updating their values. If the notificationData parameter
        ///     is present (not null), then this method adds the received values to this notification data.
        /// </summary>
        /// <param name="readValueArrays"> The new values. </param>
        /// <param name="returnChangedListItems"> </param>
        private List<IXiDataListItem>? UpdateData(DataValueArraysWithAlias? readValueArrays, bool returnChangedListItems)
        {
            List<IXiDataListItem>? changedListItems;
            if (returnChangedListItems)
            {
                changedListItems = new List<IXiDataListItem>();
            }
            else
            {
                changedListItems = null;
            }

            if (readValueArrays is not null)
            {
                if (readValueArrays.DoubleAlias is not null)
                {
                    for (int idx = 0; idx < readValueArrays.DoubleAlias.Length; idx++)
                    {
                        XiDataListItem? item;
                        ListItemsManager.TryGetValue(readValueArrays.DoubleAlias[idx], out item);
                        if (item is not null && readValueArrays.DoubleValues is not null &&
                            readValueArrays.DoubleStatusCodes is not null &&
                            readValueArrays.DoubleTimeStamps is not null)
                        {
                            item.UpdateValue(readValueArrays.DoubleValues[idx],
                                readValueArrays.DoubleStatusCodes[idx],
                                readValueArrays.DoubleTimeStamps[idx]
                                );
                            if (changedListItems is not null) changedListItems.Add(item);
                        }
                    }
                }
                if (readValueArrays.UintAlias is not null)
                {
                    // TODO: Verify
                    /*
                    int idx = 0;
                    if (readValueArrays.UintAlias[idx] == 0) // is this a discard message?
                    {
                        Context.ContextNotify(StandardListType,
                                              new XiContextNotificationData(XiContextNotificationType.Discards,
                                                                            readValueArrays.UintValues[idx]));
                    }
                     */
                    for (int idx = 0; idx < readValueArrays.UintAlias.Length; idx++)
                    {
                        XiDataListItem? item;
                        ListItemsManager.TryGetValue(readValueArrays.UintAlias[idx], out item);
                        if (item is not null && readValueArrays.UintValues is not null &&
                            readValueArrays.UintStatusCodes is not null &&
                            readValueArrays.UintTimeStamps is not null)
                        {
                            item.UpdateValue(readValueArrays.UintValues[idx], readValueArrays.UintStatusCodes[idx],
                                readValueArrays.UintTimeStamps[idx]);
                            if (changedListItems is not null) changedListItems.Add(item);
                        }
                    }
                }
                if (readValueArrays.ObjectAlias is not null)
                {
                    for (int idx = 0; idx < readValueArrays.ObjectAlias.Length; idx++)
                    {
                        XiDataListItem? item;
                        ListItemsManager.TryGetValue(readValueArrays.ObjectAlias[idx], out item);
                        if (item is not null && readValueArrays.ObjectValues is not null &&
                            readValueArrays.ObjectStatusCodes is not null &&
                            readValueArrays.ObjectTimeStamps is not null)
                        {
                            item.UpdateValue(readValueArrays.ObjectValues[idx], readValueArrays.ObjectStatusCodes[idx],
                                readValueArrays.ObjectTimeStamps[idx]
                                );
                            if (changedListItems is not null) changedListItems.Add(item);
                        }
                    }
                }
            }

            return changedListItems;
        }

        #endregion

        #region private fields

        /// <summary>
        ///     This data member holds the last exception message encountered by the
        ///     ElementValuesCallback callback when calling valuesUpdateEvent().
        /// </summary>
        private static string? _lastElementValuesCallbackExceptionMessage;

        #endregion
    }
}