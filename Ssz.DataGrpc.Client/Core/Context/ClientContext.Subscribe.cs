using System;
using System.Collections.Generic;
using Ssz.DataGrpc.Client.Core.Lists;
using Ssz.DataGrpc.Server;
using Ssz.DataGrpc.Client.Core.ListItems;
using Ssz.DataGrpc.Common;
using System.Linq;

namespace Ssz.DataGrpc.Client.Core.Context
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
        public ClientElementValueListItem[] PollDataChanges(ClientElementValueList tagValueList)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed ClientContext.");

            if (!ServerContextIsOperational) throw new InvalidOperationException();

            try
            {
                while (true)
                {
                    var request = new PollDataChangesRequest
                    {
                        ContextId = _serverContextId,
                        ListServerAlias = tagValueList.ListServerAlias
                    };
                    PollDataChangesReply reply = _resourceManagementClient.PollDataChanges(request);
                    SetResourceManagementLastCallUtc();

                    var changedItems = InformationReportInternal(tagValueList, reply.ElementValueArrays);
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
        public ClientEventListItem[] PollEventChanges(ClientEventList eventList)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed ClientContext.");

            if (!ServerContextIsOperational) throw new InvalidOperationException();

            try
            {
                while (true)
                {
                    var request = new PollEventChangesRequest
                    {
                        ContextId = _serverContextId,
                        ListServerAlias = eventList.ListServerAlias
                    };
                    PollEventChangesReply reply = _resourceManagementClient.PollEventChanges(request);
                    SetResourceManagementLastCallUtc();

                    var newItems = EventNotificationInternal(eventList, reply.EventMessageArrays);
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
        /// <param name="elementValueArrays"></param>
        public void InformationReport(uint clientListId, ElementValueArrays elementValueArrays)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed ClientContext.");

            ClientElementValueList datalist = GetElementValueList(clientListId);

            InformationReportInternal(datalist, elementValueArrays);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientListId"></param>
        /// <param name="eventMessageArrays"></param>
        public void EventNotification(uint clientListId, EventMessageArrays eventMessageArrays)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed ClientContext.");

            ClientEventList eventList = GetEventList(clientListId);

            EventNotificationInternal(eventList, eventMessageArrays);
        }

        #endregion

        #region private functions

        /// <summary>
        ///     Returns null, if incomplete ElementValueArrays.
        /// </summary>
        /// <param name="dataList"></param>
        /// <param name="elementValueArrayss"></param>
        /// <returns></returns>
        private ClientElementValueListItem[]? InformationReportInternal(ClientElementValueList dataList, ElementValueArrays elementValueArrayss)
        {
            ClientElementValueListItem[]? changedListItems = dataList.OnInformationReport(elementValueArrayss);
            if (changedListItems != null && changedListItems.Length > 0)
            {
                List<DataGrpcValueStatusTimestamp> changedValuesList = new List<DataGrpcValueStatusTimestamp>(changedListItems.Length);
                foreach (ClientElementValueListItem changedListItem in changedListItems)
                {
                    changedValuesList.Add(changedListItem.DataGrpcValueStatusTimestamp);
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
        private ClientEventListItem[]? EventNotificationInternal(ClientEventList eventList, EventMessageArrays eventMessageArrays)
        {
            ClientEventListItem[]? newEventListItems = eventList.EventNotification(eventMessageArrays);
            if (newEventListItems != null && newEventListItems.Length > 0)
            {
                eventList.RaiseEventNotificationEvent(newEventListItems);
            }
            return newEventListItems;
        }

        #endregion
    }
}