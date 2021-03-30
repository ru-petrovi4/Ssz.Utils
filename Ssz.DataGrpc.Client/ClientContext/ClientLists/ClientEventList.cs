using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Ssz.DataGrpc.Client.Managers;
using Ssz.DataGrpc.Client.ClientListItems;
using Ssz.DataGrpc.Server;
using Ssz.Utils;
using Ssz.DataGrpc.Common;
using Ssz.DataGrpc.Client.Data;

namespace Ssz.DataGrpc.Client.ClientLists
{
    /// <summary>
    /// 
    /// </summary>
    public class ClientEventList : ClientListRoot        
    {
        #region construction and destruction

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="listParams"></param>
        public ClientEventList(ClientContext context, CaseInsensitiveDictionary<string>? listParams)
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
        public EventIdResult[] AcknowledgeAlarms(string operatorName, string comment, Ssz.Utils.DataSource.EventId[] alarmsToAck)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientEventList.");

            return Context.AcknowledgeAlarms(ListServerAlias, operatorName, comment, alarmsToAck);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ClientEventListItem[] PollEventsChanges()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientEventList.");

            return Context.PollEventsChanges(this);
        }

        /// <summary>
        ///     Returns new ClientEventListItems or null, if waiting next message.
        /// </summary>
        /// <param name="eventMessagesCollection"></param>
        /// <returns></returns>
        public ClientEventListItem[]? EventNotification(EventMessagesCollection eventMessagesCollection)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientEventList.");

            if (eventMessagesCollection.Guid != @"" && _incompleteEventMessagesCollectionCollection.Count > 0)
            {
                var beginEventMessagesCollection = _incompleteEventMessagesCollectionCollection.TryGetValue(eventMessagesCollection.Guid);
                if (beginEventMessagesCollection != null)
                {
                    _incompleteEventMessagesCollectionCollection.Remove(eventMessagesCollection.Guid);
                    beginEventMessagesCollection.Add(eventMessagesCollection);
                    eventMessagesCollection = beginEventMessagesCollection;
                }
            }

            if (eventMessagesCollection.NextCollectionGuid != @"")
            {
                _incompleteEventMessagesCollectionCollection[eventMessagesCollection.NextCollectionGuid] = eventMessagesCollection;

                return null;
            }
            else
            {
                var result = new List<ClientEventListItem>();

                foreach (var eventMessage in eventMessagesCollection.EventMessages)
                {
                    result.Add(new ClientEventListItem(eventMessage));
                }

                return result.ToArray();
            }
        }

        /// <summary>
        ///     Throws or invokes EventNotificationEvent.        
        /// </summary>
        /// <param name="newEventListItems"></param>
        public void RaiseEventNotificationEvent(ClientEventListItem[] newEventListItems)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ClientEventList.");

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
        public event EventNotificationEventHandler EventNotificationEvent = delegate { };

        #endregion

        #region private fields

        /// <summary>
        ///     This data member holds the last exception message encountered by the
        ///     InformationReport callback when calling valuesUpdateEvent().
        /// </summary>
        private CaseInsensitiveDictionary<EventMessagesCollection> _incompleteEventMessagesCollectionCollection = new CaseInsensitiveDictionary<EventMessagesCollection>();

        #endregion
    }
}