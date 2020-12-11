using System;
using System.Collections.Generic;
using Ssz.Xi.Client.Api;
using Ssz.Xi.Client.Api.ListItems;
using Ssz.Xi.Client.Internal.Endpoints;
using Ssz.Xi.Client.Internal.Lists;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Internal.Context
{
    /// <summary>
    ///     This partial class defines the Callback and Polling related aspects of the XiContext class.
    /// </summary>
    internal partial class XiContext
    {
        #region public functions

        /// <summary>
        ///     <para> Throws or returns changed IXiDataListItems (not null, but possibly zero-lenghth). </para>
        ///     <para> dataList != null </para>
        ///     <para>
        ///         This method is used to poll the endpoint for changes to a specific data list. It is also used as a
        ///         keep-alive for the poll endpoint by setting the listId parameter to 0. In this case, null is returned
        ///         immediately.
        ///     </para>
        ///     <para> Changes consists of: </para>
        ///     <para> 1) values for data objects that were added to the list, </para>
        ///     <para>
        ///         2) values for data objects whose current values have changed since the last time they were reported to the
        ///         client via this interface. If a deadband filter has been defined for the list, floating point values are not
        ///         considered to have changed unless they have changed by the deadband amount.
        ///     </para>
        ///     <para> 3) historical values that meet the list filter criteria, including the deadband. </para>
        ///     <para>
        ///         This method returns the list of changed values to the client application using the InformationReport
        ///         callback.. The list of changed values is null if this is a keep-alive. The following two standard data objects
        ///         can also be returned.
        ///     </para>
        ///     <para>
        ///         The first is identified by a ListId of 0 and a ClientId of 0. It contains a ServerStatus object value that
        ///         indicates to the client that the server or one of its wrapped servers is shutting down. When present, this will
        ///         always be the first value in the returned OBJECT value array.
        ///     </para>
        ///     <para>
        ///         The second is identified by its ListId and a ClientId of 0. It contains a UInt32 value that indicates to the
        ///         client how many data changes have been discarded for the specified list since the last poll response. If this
        ///         condition persists, the client should increase its poll frequency. When present, this will always be the first
        ///         value in the returned UINT value array.
        ///     </para>
        /// </summary>
        /// <param name="dataList"> The data list to poll. </param>
        public IXiDataListItem[] PollDataChanges(XiDataList dataList)
        {
            if (dataList == null) throw new ArgumentNullException(@"dataList");

            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            if (_pollEndpoint == null) throw new Exception("No Poll Endpoint.");

            if (_pollEndpoint.Disposed) throw new Exception("Poll Endpoint is Disposed.");

            DataValueArraysWithAlias? readValueList = null;
            if (XiEndpointRoot.CreateChannelIfNotCreated(_pollEndpoint))
            {
                try
                {
                    readValueList = _pollEndpoint.Proxy.PollDataChanges(ContextId, dataList.ServerListId);

                    _pollEndpoint.LastCallUtc = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    ProcessRemoteMethodCallException(ex);
                }
            }

            IXiDataListItem[]? changedListItems = InformationReportInternal(dataList, readValueList);
            if (changedListItems == null) throw new Exception("PollDataChanges() error.");
            return changedListItems;
        }

        /// <summary>
        ///     <para> Throws or returns new IXiEventListItems (not null, but possibly zero-lenghth). </para>
        ///     <para> eventList != null </para>
        ///     <para>
        ///         This method is used to poll the endpoint for changes to a specific event list. Event messages are sent when
        ///         there has been a change to the specified event list. A new alarm or event that has been added to the list, a
        ///         change to an alarm already in the list, or the deletion of an alarm from the list constitutes a change to the
        ///         list.
        ///     </para>
        ///     <para>
        ///         Once an event has been reported from the list, it is automatically deleted from the list. Alarms are only
        ///         deleted from the list when they transition to inactive and acknowledged.
        ///     </para>
        ///     <para>
        ///         This method return a list of event messages to the client application via the EventNotification callback
        ///         method. The list consists of alarm/event messages for new alarms/events in the Event List, and alarm/event
        ///         messages that represent state changes to alarms that are already in the list, including alarm/event messages
        ///         that identify state changes that caused alarms to tbe deleted from the list.
        ///     </para>
        ///     <para>
        ///         Null is returned as a keep-alive message when there have been no new alarm/event messages since the last
        ///         poll.
        ///     </para>
        ///     <para>
        ///         In addition, a special event message is included as the first item in the list to indicate to the client
        ///         that one or more event message have been discarded due to queue size limitations. All fields of this message
        ///         are set to null with the exception of the following:
        ///     </para>
        ///     <para> OccurrenceTime = current time of the response </para>
        ///     <para> EventType = EventType.DiscardedMessage </para>
        ///     <para> TextMessage = the number of event/alarm messages discarded since the last poll response was returned. </para>
        /// </summary>
        /// <param name="eventList"> The event list to poll (reported). </param>
        /// <param name="filterSet">
        ///     Optional set of filters to further refine the selection from the alarms and events in the
        ///     list. The event list itself is created using a filter.
        /// </param>
        public IXiEventListItem[] PollEventChanges(XiEventList eventList, FilterSet? filterSet)
        {
            if (eventList == null) throw new ArgumentNullException(@"eventList");            

            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiContext.");

            if (_pollEndpoint == null) throw new Exception("No Poll Endpoint");

            if (_pollEndpoint.Disposed) throw new Exception("Poll Endpoint is Disposed.");

            EventMessage[]? eventMessages = null;
            if (XiEndpointRoot.CreateChannelIfNotCreated(_pollEndpoint))
            {
                try
                {
                    eventMessages = _pollEndpoint.Proxy.PollEventChanges(ContextId,
                        eventList.ServerListId,
                        filterSet);


                    _pollEndpoint.LastCallUtc = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    ProcessRemoteMethodCallException(ex);
                }
            }

            IXiEventListItem[]? newEventListItems = EventNotificationInternal(eventList, eventMessages);
            if (newEventListItems == null) throw new Exception("PollEventChanges() error.");
            return newEventListItems;
        }


        /// <summary>
        ///     <para>
        ///         This callback method is implemented by the client to be notified when the server server state changes to
        ///         Aborting. Clients that use the poll interface instead of this callback interface can add the ServerDescription
        ///         object to a data object list to be notified when the server state transitions to the aborting state.
        ///     </para>
        /// </summary>
        /// <param name="serverStatus">
        ///     The ServerStatus object for the server or wrapped server for which the abort is being
        ///     reported.
        /// </param>
        /// <param name="reason"> The reason the context is being closed. </param>
        public void Abort(ServerStatus serverStatus, string reason)
        {
            ServerContextIsClosing = true;
            RaiseContextNotifyEvent(this,
                new XiContextNotificationData(XiContextNotificationType.Shutdown, serverStatus));
        }

        /// <summary>
        ///     <para> No throws.</para>
        ///     <para> This callback method is implemented by the client to receive data changes. </para>
        ///     <para>
        ///         Servers send data changes to the client that have not been reported to the client via this method. Changes
        ///         consists of:
        ///     </para>
        ///     <para> 1) values for data objects that were added to the list, </para>
        ///     <para>
        ///         2) values for data objects whose current values have changed since the last time they were reported to the
        ///         client via this interface. If a deadband filter has been defined for the list, floating point values are not
        ///         considered to have changed unless they have changed by the deadband amount.
        ///     </para>
        ///     <para> 3) historical values that meet the list filter criteria, including the deadband. </para>
        ///     <para>
        ///         In addition, the server may insert a special value that indicates the server or one of its wrapped servers
        ///         are shutting down.
        ///     </para>
        ///     <para>
        ///         This value is inserted as the first value in the list of values in the callback. Its ListId and ClientId are
        ///         both 0 and its data type is ServerStatus.
        ///     </para>
        /// </summary>
        /// <param name="clientListId"> The client identifier of the list for which data changes are being reported. </param>
        /// <param name="updatedValues"> The values being reported. </param>
        public void InformationReport(uint clientListId, DataValueArraysWithAlias updatedValues)
        {
            if (_disposed) return;

            if (_callbackEndpoint != null) _callbackEndpoint.LastCallUtc = DateTime.UtcNow;

            XiDataList datalist = GetDataList(clientListId);

            InformationReportInternal(datalist, updatedValues);
        }

        /// <summary>
        ///     <para> This callback method is implemented by the client to receive alarms and events. </para>
        ///     <para>
        ///         Servers send event messages to the client via this interface. Event messages are sent when there has been a
        ///         change to the specified event list. A new alarm or event that has been added to the list, a change to an alarm
        ///         already in the list, or the deletion of an alarm from the list constitutes a change to the list.
        ///     </para>
        ///     <para>
        ///         Once an event has been reported from the list, it is automatically deleted from the list. Alarms are only
        ///         deleted from the list when they transition to inactive and acknowledged.
        ///     </para>
        /// </summary>
        /// <param name="clientListId"> The client identifier of the list for which alarms/events are being reported. </param>
        /// <param name="eventMessages"> The array of alarms/events are being reported. </param>
        public void EventNotification(uint clientListId, EventMessage[] eventMessages)
        {
            if (_disposed) return;

            if (_callbackEndpoint != null) _callbackEndpoint.LastCallUtc = DateTime.UtcNow;

            XiEventList eventList = GetEventList(clientListId);

            EventNotificationInternal(eventList, eventMessages);
        }

        /// <summary>
        ///     This method returns the results of invoking an asynchronous passthrough.
        /// </summary>
        /// <param name="invokeId"> The identifier for this invocation of the passthrough defined by the client in the request. </param>
        /// <param name="passthroughResult">
        ///     The result of executing the passthrough, consisting of the result code, the invokeId
        ///     supplied in the request, and a byte array. It is up to the client application to interpret this byte array.
        /// </param>
        public void PassthroughCallback(int invokeId, PassthroughResult passthroughResult)
        {
            // TODO: Add code if the PassthroughCallback method is supported
        }

        #endregion

        #region private functions

        /// <summary>
        ///     <para> Invokes DataList.InformationReport event, if changed items count > 0. </para>  
        ///     <para> No throws. If error, returns null. Otherwise changed IXiDataListItems (not null, but possibly zero-lenghth). </para>        
        ///     <para> This callback method is implemented by the client to receive data changes. </para>
        ///     <para>
        ///         Servers send data changes to the client that have not been reported to the client via this method. Changes
        ///         consists of:
        ///     </para>
        ///     <para> 1) values for data objects that were added to the list, </para>
        ///     <para>
        ///         2) values for data objects whose current values have changed since the last time they were reported to the
        ///         client via this interface. If a deadband filter has been defined for the list, floating point values are not
        ///         considered to have changed unless they have changed by the deadband amount.
        ///     </para>
        ///     <para> 3) historical values that meet the list filter criteria, including the deadband. </para>
        ///     <para>
        ///         In addition, the server may insert a special value that indicates the server or one of its wrapped servers
        ///         are shutting down.
        ///     </para>
        ///     <para>
        ///         This value is inserted as the first value in the list of values in the callback. Its ListId and ClientId are
        ///         both 0 and its data type is ServerStatus.
        ///     </para>
        /// </summary>
        /// <param name="dataList"> The client identifier of the list for which data changes are being reported. </param>
        /// <param name="updatedValues"> The values being reported. </param>
        private IXiDataListItem[]? InformationReportInternal(XiDataList? dataList, DataValueArraysWithAlias? updatedValues)
        {
            if (dataList == null || dataList.Disposed) return null;

            try
            {
                List<IXiDataListItem>? changedListItems = dataList.OnInformationReport(updatedValues);
                if (changedListItems == null) return null;
                if (changedListItems.Count > 0)
                {
                    List<XiValueStatusTimestamp> changedValuesList = new List<XiValueStatusTimestamp>(changedListItems.Count);
                    foreach (IXiDataListItem changedListItem in changedListItems)
                    {
                        changedValuesList.Add(changedListItem.XiValueStatusTimestamp);
                    }
                    dataList.RaiseInformationReportEvent(changedListItems, changedValuesList);
                }
                return changedListItems.ToArray();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        ///     <para> Invokes EventList.EventNotificationEvent, if new items count > 0. </para>
        ///     <para> No throws. If error, returns null. Otherwise new IXiEventListItems (not null, but possibly zero-lenghth). </para>
        ///     <para> This callback method is implemented by the client to receive alarms and events. </para>
        ///     <para>
        ///         Servers send event messages to the client via this interface. Event messages are sent when there has been a
        ///         change to the specified event list. A new alarm or event that has been added to the list, a change to an alarm
        ///         already in the list, or the deletion of an alarm from the list constitutes a change to the list.
        ///     </para>
        ///     <para>
        ///         Once an event has been reported from the list, it is automatically deleted from the list. Alarms are only
        ///         deleted from the list when they transition to inactive and acknowledged.
        ///     </para>
        /// </summary>
        /// <param name="eventList"> The client identifier of the list for which alarms/events are being reported. </param>
        /// <param name="eventMessages"> The array of alarms/events are being reported. </param>
        private IXiEventListItem[]? EventNotificationInternal(XiEventList eventList, EventMessage[]? eventMessages)
        {
            if (eventList == null || eventList.Disposed) return null;

            try
            {
                List<IXiEventListItem> newEventListItems = eventList.EventNotification(eventMessages);
                if (newEventListItems.Count > 0)
                {
                    eventList.RaiseEventNotificationEvent(newEventListItems);
                }
                return newEventListItems.ToArray();
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}