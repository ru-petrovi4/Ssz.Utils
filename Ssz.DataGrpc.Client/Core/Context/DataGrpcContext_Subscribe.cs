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
    ///     This partial class defines the Callback and Polling related aspects of the DataGrpcContext class.
    /// </summary>
    public partial class DataGrpcContext
    {
        #region public functions

        /// <summary>        
        /// </summary>
        /// <param name="tagValueList"></param>
        /// <returns></returns>
        public DataGrpcElementValueListItem[] PollDataChanges(DataGrpcElementValueList tagValueList)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcContext.");

            if (!ServerContextIsOperational) throw new InvalidOperationException();

            try
            {
                while (true)
                {
                    var request = new PollDataChangesRequest
                    {
                        ContextId = _contextId,
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
        public DataGrpcEventListItem[] PollEventChanges(DataGrpcEventList eventList)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcContext.");

            if (!ServerContextIsOperational) throw new InvalidOperationException();

            try
            {
                while (true)
                {
                    var request = new PollEventChangesRequest
                    {
                        ContextId = _contextId,
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
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcContext.");

            DataGrpcElementValueList datalist = GetElementValueList(clientListId);

            InformationReportInternal(datalist, elementValueArrays);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientListId"></param>
        /// <param name="eventMessageArrays"></param>
        public void EventNotification(uint clientListId, EventMessageArrays eventMessageArrays)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcContext.");

            DataGrpcEventList eventList = GetEventList(clientListId);

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
        private DataGrpcElementValueListItem[]? InformationReportInternal(DataGrpcElementValueList dataList, ElementValueArrays elementValueArrayss)
        {
            DataGrpcElementValueListItem[]? changedListItems = dataList.OnInformationReport(elementValueArrayss);
            if (changedListItems != null && changedListItems.Length > 0)
            {
                List<DataGrpcValueStatusTimestamp> changedValuesList = new List<DataGrpcValueStatusTimestamp>(changedListItems.Length);
                foreach (DataGrpcElementValueListItem changedListItem in changedListItems)
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
        private DataGrpcEventListItem[]? EventNotificationInternal(DataGrpcEventList eventList, EventMessageArrays eventMessageArrays)
        {
            DataGrpcEventListItem[]? newEventListItems = eventList.EventNotification(eventMessageArrays);
            if (newEventListItems != null && newEventListItems.Length > 0)
            {
                eventList.RaiseEventNotificationEvent(newEventListItems);
            }
            return newEventListItems;
        }

        #endregion
    }
}