using System;
using System.Collections.Generic;
using Ssz.DataGrpc.Client.ClientLists;
using Ssz.DataGrpc.Server;
using Ssz.DataGrpc.Client.ClientListItems;
using Ssz.DataGrpc.Common;
using System.Linq;
using Ssz.Utils.DataSource;

namespace Ssz.DataGrpc.Client
{
    /// <summary>
    ///     This partial class defines the Callback and Polling related aspects of the ClientContext class.
    /// </summary>
    public partial class ClientContext
    {
        #region public functions

        /// <summary>        
        /// </summary>
        /// <param name="tagValueList"></param>
        /// <returns></returns>
        public ClientElementValueListItem[] PollElementValuesChanges(ClientElementValueList tagValueList)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed ClientContext.");

            if (!ServerContextIsOperational) throw new InvalidOperationException();

            try
            {
                while (true)
                {
                    var request = new PollElementValuesChangesRequest
                    {
                        ContextId = _serverContextId,
                        ListServerAlias = tagValueList.ListServerAlias
                    };
                    PollElementValuesChangesReply reply = _resourceManagementClient.PollElementValuesChanges(request);
                    SetResourceManagementLastCallUtc();

                    var changedItems = InformationReportInternal(tagValueList, reply.ElementValuesCollection);
                    if (changedItems != null) return changedItems;
                }
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventList"></param>
        /// <returns></returns>
        public ClientEventListItem[] PollEventsChanges(ClientEventList eventList)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed ClientContext.");

            if (!ServerContextIsOperational) throw new InvalidOperationException();

            try
            {
                while (true)
                {
                    var request = new PollEventsChangesRequest
                    {
                        ContextId = _serverContextId,
                        ListServerAlias = eventList.ListServerAlias
                    };
                    PollEventsChangesReply reply = _resourceManagementClient.PollEventsChanges(request);
                    SetResourceManagementLastCallUtc();

                    var newItems = EventNotificationInternal(eventList, reply.EventMessagesCollection);
                    if (newItems != null) return newItems;
                }
            }
            catch (Exception ex)
            {
                ProcessRemoteMethodCallException(ex);
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientListId"></param>
        /// <param name="elementValuesCollection"></param>
        public void InformationReport(uint clientListId, ElementValuesCollection elementValuesCollection)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed ClientContext.");

            ClientElementValueList datalist = GetElementValueList(clientListId);

            InformationReportInternal(datalist, elementValuesCollection);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientListId"></param>
        /// <param name="eventMessagesCollection"></param>
        public void EventNotification(uint clientListId, EventMessagesCollection eventMessagesCollection)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed ClientContext.");

            ClientEventList eventList = GetEventList(clientListId);

            EventNotificationInternal(eventList, eventMessagesCollection);
        }

        #endregion

        #region private functions

        /// <summary>
        ///     Returns null, if incomplete ElementValuesCollection.
        /// </summary>
        /// <param name="dataList"></param>
        /// <param name="elementValuesCollections"></param>
        /// <returns></returns>
        private ClientElementValueListItem[]? InformationReportInternal(ClientElementValueList dataList, ElementValuesCollection elementValuesCollections)
        {
            ClientElementValueListItem[]? changedListItems = dataList.OnInformationReport(elementValuesCollections);
            if (changedListItems != null && changedListItems.Length > 0)
            {
                List<ValueStatusTimestamp> changedValuesList = new List<ValueStatusTimestamp>(changedListItems.Length);
                foreach (ClientElementValueListItem changedListItem in changedListItems)
                {
                    changedValuesList.Add(changedListItem.ValueStatusTimestamp);
                }
                dataList.RaiseInformationReportEvent(changedListItems, changedValuesList.ToArray());
            }
            return changedListItems;
        }

        /// <summary>
        ///     Returns null, if incomplete EventMessageArray.
        /// </summary>
        /// <param name="eventList"></param>
        /// <param name="eventMessages"></param>
        /// <returns></returns>
        private ClientEventListItem[]? EventNotificationInternal(ClientEventList eventList, EventMessagesCollection eventMessagesCollection)
        {
            ClientEventListItem[]? newEventListItems = eventList.EventNotification(eventMessagesCollection);
            if (newEventListItems != null && newEventListItems.Length > 0)
            {
                eventList.RaiseEventNotificationEvent(newEventListItems);
            }
            return newEventListItems;
        }

        #endregion
    }
}