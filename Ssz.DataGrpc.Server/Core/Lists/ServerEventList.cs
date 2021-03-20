using System;
using System.Collections.Generic;
using Ssz.DataGrpc.Server.Core.Context;
using Ssz.DataGrpc.Server.Core.ListItems;
using Xi.Common.Support;
using Xi.Contracts.Data;

namespace Ssz.DataGrpc.Server.Core.Lists
{
    /// <summary>
    ///   This is the base class from which an implementation of a Xi server 
    ///   would subclass to provide access event and alarm data to a client.
    ///   The functionality of the server may be such that a collection of 
    ///   interesting alarms are kept by the server and may be obtained by the
    ///   client application as desired.
    /// </summary>
    public class ServerEventList : ServerListRoot
    {
        #region construction and destruction

        protected EventListBase(ServerContext<ServerListRoot> context, uint clientId, uint updateRate, uint bufferingRate,
                         uint listType, uint listKey, StandardMib mib)
            : base(context, clientId, updateRate, bufferingRate, listType, listKey, mib)
        {
        }

        #endregion

        #region public functions

        /// <summary>
        ///   This method is used to request that category-specific fields be 
        ///   included in event messages generated for alarms and events of 
        ///   the category for this Event/Alarm List.
        /// </summary>
        /// <param name = "categoryId">
        ///   The category for which event message fields are being added.
        /// </param>
        /// <param name = "fieldObjectTypeIds">
        ///   The list of category-specific fields to be included in the event 
        ///   messages generated for alarms and events of the category.  Each field 
        ///   is identified by its ObjectType LocalId obtained from the EventMessageFields 
        ///   contained in the EventCategoryConfigurations Standard MIB element.
        /// </param>
        /// <returns>
        ///   The client alias and result codes for the fields that could not be  
        ///   added to the event message. Returns null if all succeeded.  
        /// </returns>
        public override List<TypeIdResult> OnAddEventMessageFields(uint categoryId, List<TypeId> fieldObjectTypeIds)
        {
            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed EventListBase.");

                var TypeIdResults = new List<TypeIdResult>();
                return TypeIdResults;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name = "filterSet"></param>
        /// <returns></returns>
        public override EventMessage[] OnPollEventChanges(FilterSet filterSet)
        {
            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed EventListBase.");

                if (PollEndpointEntry == null) throw RpcExceptionHelper.Create("List not attached to the IPoll endpoint.");
                if (!Enabled) throw RpcExceptionHelper.Create("List not Enabled.");

                return GetEventMessagesFromQueueOfChangedItems();
            }
        }

        #endregion

        #region protected functions

        protected EventListItem OnNewEventListItem(EventMessage eventMessage)
        {
            var eventListItem = EventListItem.New();
            eventListItem.EventMessage = eventMessage;
            return eventListItem;
        }

        protected virtual EventMessage[] GetEventMessagesFromQueueOfChangedItems()
        {
            int numEvtMsgs = (DiscardedQueueEntries > 0) ? ChangedItemsQueue.Count + 1 : ChangedItemsQueue.Count;

            if (numEvtMsgs == 0) return null;

            var eventMessages = new EventMessage[numEvtMsgs];

            int idx = 0; // index for eventMessages

            // Add the discard message
            if (DiscardedQueueEntries > 0)
            {
                var discardMessage = new EventMessage();
                discardMessage.OccurrenceTime = DateTime.UtcNow;
                discardMessage.EventType = EventType.DiscardedMessage;
                discardMessage.TextMessage = DiscardedQueueEntries.ToString();
                eventMessages[idx] = discardMessage;
                idx++;
            }

            // if there are queued messages to send
            foreach (EventListItem eventListItem in ChangedItemsQueue)
            {
                if (eventListItem.GetType() == typeof (QueueMarker)) continue; // ignore queue markers

                eventMessages[idx++] = eventListItem.EventMessage;
                eventListItem.EventMessage = null;
                eventListItem.EntryQueued = false;
            }

            ChangedItemsQueue.Clear();

            if (idx == 0) // there were no discards and there were no valid event messages in QueueOfChangedItems
                eventMessages = null;

            DiscardedQueueEntries = 0; // reset this counter for each poll or callback

            return eventMessages;
        }

        #endregion
    }
}