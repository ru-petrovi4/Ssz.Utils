using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Ssz.DataGrpc.Client.Managers;
using Ssz.DataGrpc.Client.Core.Context;
using Ssz.DataGrpc.Client.Core.ListItems;
using Ssz.DataGrpc.Server;
using Ssz.DataGrpc.Client.Data.EventHandlers;
using Ssz.Utils;
using Ssz.DataGrpc.Common;

namespace Ssz.DataGrpc.Client.Core.Lists
{
    /// <summary>
    /// 
    /// </summary>
    public class DataGrpcEventList : DataGrpcListRoot        
    {
        #region construction and destruction

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="listParams"></param>
        public DataGrpcEventList(DataGrpcContext context, CaseInsensitiveDictionary<string>? listParams)
            : base(context)
        {
            ListType = (uint)StandardListType.EventList;
            Context.DefineList(this, listParams);
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Returns the array EventIds and result codes for the alarms whose
        ///     acknowledgement failed.
        /// </summary>
        /// <param name="operatorName"></param>
        /// <param name="comment"></param>
        /// <param name="alarmsToAck"></param>
        /// <returns></returns>
        public EventIdResult[] AcknowledgeAlarms(string operatorName, string comment, EventId[] alarmsToAck)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcEventList.");

            return Context.AcknowledgeAlarms(ListServerAlias, operatorName, comment, alarmsToAck);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public DataGrpcEventListItem[] PollEventChanges()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcEventList.");

            return Context.PollEventChanges(this);
        }

        /// <summary>
        ///     Returns new DataGrpcEventListItems or null, if waiting next message.
        /// </summary>
        /// <param name="eventMessageArrays"></param>
        /// <returns></returns>
        public DataGrpcEventListItem[]? EventNotification(EventMessageArrays eventMessageArrays)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcEventList.");

            if (eventMessageArrays.Guid != @"" && _incompleteEventMessageArraysCollection.Count > 0)
            {
                var beginEventMessageArrays = _incompleteEventMessageArraysCollection.TryGetValue(eventMessageArrays.Guid);
                if (beginEventMessageArrays != null)
                {
                    _incompleteEventMessageArraysCollection.Remove(eventMessageArrays.Guid);
                    beginEventMessageArrays.Add(eventMessageArrays);
                    eventMessageArrays = beginEventMessageArrays;
                }
            }

            if (eventMessageArrays.NextArraysGuid != @"")
            {
                _incompleteEventMessageArraysCollection[eventMessageArrays.NextArraysGuid] = eventMessageArrays;

                return null;
            }
            else
            {
                var result = new List<DataGrpcEventListItem>();

                foreach (var eventMessage in eventMessageArrays.EventMessages)
                {
                    result.Add(new DataGrpcEventListItem(eventMessage));
                }

                return result.ToArray();
            }
        }

        /// <summary>
        ///     Throws or invokes EventNotificationEvent.        
        /// </summary>
        /// <param name="newEventListItems"></param>
        public void RaiseEventNotificationEvent(DataGrpcEventListItem[] newEventListItems)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed DataGrpcEventList.");

            try
            {
                EventNotificationEvent(this, newEventListItems);
            }
            catch
            {
                //Logger.LogWarning(ex, "");
            }
        }

        /// <summary>
        ///     This event is used to notify the client application when new events are received.
        /// </summary>
        public event DataGrpcEventNotificationEventHandler EventNotificationEvent = delegate { };

        #endregion

        #region private fields

        /// <summary>
        ///     This data member holds the last exception message encountered by the
        ///     InformationReport callback when calling valuesUpdateEvent().
        /// </summary>
        private CaseInsensitiveDictionary<EventMessageArrays> _incompleteEventMessageArraysCollection = new CaseInsensitiveDictionary<EventMessageArrays>();

        #endregion
    }
}