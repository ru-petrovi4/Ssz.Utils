using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
    ///     This class implements the XiEventList interface.
    /// </summary>
    internal class XiEventList : XiListRoot,
        IXiEventListProxy
    {
        #region construction and destruction

        /// <summary>
        ///     This constructor creates a new event list for the specified context.
        /// </summary>
        /// <param name="context"> The context that owns the event list. </param>
        /// <param name="updateRate"> The update rate for the event list. </param>
        /// <param name="bufferingRate"> The BufferingRate for this event list. Set to 0 if not used. </param>
        /// <param name="filterSet"> The FilterSet for this event list. Set to null if not used. </param>
        public XiEventList(XiContext context, uint updateRate, uint bufferingRate, FilterSet filterSet)
            : base(context)
        {
            StandardListType = StandardListType.EventList;
            ListAttributes = Context.DefineList(this, updateRate, bufferingRate, filterSet);

            if (Context.StandardMib.EventCategoryConfigurations != null)
            {
                foreach (CategoryConfiguration category in Context.StandardMib.EventCategoryConfigurations)
                {
                    if (category.EventMessageFields != null && category.EventMessageFields.Count > 0)
                    {
                        var categoryFields = new XiCategorySpecificFields(category.CategoryId);
                        foreach (ParameterDefinition? field in category.EventMessageFields)
                        {
                            if (field == null) throw new InvalidOperationException();
                            var optField = new XiEventMsgFieldDesc(field.Name ?? "", field.Description ?? "", field.ObjectTypeId ?? new TypeId(),
                                field.DataTypeId ?? new TypeId());
                            categoryFields.OptionalEventMsgFields.Add(optField);
                        }
                        CategorySpecificFieldDictionary[category.Name ?? ""] = categoryFields;
                    }
                }
            }
            // TODO: enable the following four lines to test new sorting algorithms for event messages
            // this tests the initial sort
            //EventMessage[] eventsArray = GetEventMessages();
            //EventNotification(eventsArray);

            // this tests the sort of the new messages and their merge into the Event List
            //eventsArray = GetEventMessages2();
            //EventNotification(eventsArray);
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;
            if (disposing)
            {
                // Release and Dispose managed resources.
                // create a separate list to iterate through to allow the event values 
                // to be removed from _ListElements

                foreach (XiEventListItem item in _items)
                {
                    item.Dispose();
                }
                _items.Clear();
            }

            // Release unmanaged resources.
            // Set large fields to null.
            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        /// <summary>
        ///     <para>This method is used to acknowledge one or more alarms.</para>
        /// </summary>
        /// <param name="operatorName">
        ///     The name or other identifier of the operator who is acknowledging
        ///     the alarm.
        /// </param>
        /// <param name="comment">
        ///     An optional comment submitted by the operator to accompany the
        ///     acknowledgement.
        /// </param>
        /// <param name="alarmsToAck">
        ///     The list of alarms to acknowledge.
        /// </param>
        /// <returns>
        ///     The list EventIds and result codes for the alarms whose
        ///     acknowledgement failed. Returns null if all acknowledgements
        ///     succeeded.
        /// </returns>
        public List<EventIdResult>? AcknowledgeAlarms(string? operatorName, string? comment, List<EventId> alarmsToAck)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiEventList.");

            return Context.AcknowledgeAlarms(ServerListId, operatorName, comment, alarmsToAck);
        }

        /*
        /// <summary>
        ///   The method sorts an array of event messages using a combination of their state 
        ///   and priority. New active, unacknowledged alarms are added first, followed by inactive, 
        ///   unacknowledged alarms. Inactive, acknowledged are added next, followed by alerts, and 
        ///   finally event messages. Each of these sets is sorted in descending priority order.
        /// </summary>
        /// <param name="eventsArray"> The event messages to be sorted </param>
        /// <param name="numActiveUnacked"> number of activeUnacked messages in the sorted list </param>
        /// <param name="numInactiveUnacked"> number of inactiveUnacked messages in the sorted list </param>
        /// <param name="numActiveAcked"> number of activeAacked messages in the sorted list </param>
        /// <param name="numInactiveAcked"> number of inactiveUnacked messages in the sorted list </param>
        /// <param name="numAlert"> number of alert messages in the sorted list </param>
        /// <param name="numEvent"> number of event messages in the sorted list </param>
        // <returns>The list of sorted XiEventListItems</returns>
        public static List<XiEventListItem> SortEventMessages(EventMessage[] eventsArray, out int numActiveUnacked,
                                                              out int numInactiveUnacked, out int numActiveAcked,
                                                              out int numInactiveAcked, out int numAlert,
                                                              out int numEvent)
        {
            List<EventMessage> eventMessagesList = eventsArray.ToList();
            if (eventMessagesList[0].EventType == EventType.DiscardedMessage) eventMessagesList.RemoveAt(0);
            var sortedEventListItems = new List<XiEventListItem>();

            // Set these counts to verify the insert algorithm
            // Debug asserts below are used to indicate when the counts calculated here don't 
            // match the number of inserts
            int activeUnackedCount = 0;
            int activeAckedCount = 0;
            int inactiveUnackedCount = 0;
            int inactiveAckedCount = 0;
            foreach (EventMessage em in eventMessagesList)
            {
                if (em.AlarmData != null)
                {
                    if (em.AlarmData.AlarmState == AlarmState.Active) activeAckedCount++;
                    else if (em.AlarmData.AlarmState == AlarmState.Initial) inactiveAckedCount++;
                    else if (em.AlarmData.AlarmState == AlarmState.Unacked) inactiveUnackedCount++;
                    else if (((em.AlarmData.AlarmState & AlarmState.Active) > 0) &&
                             ((em.AlarmData.AlarmState & AlarmState.Unacked) > 0)) activeUnackedCount++;
                }
            }

            // insert the alarms first
            numActiveUnacked = InsertAlarmsByState(_activeUnacked, eventMessagesList, sortedEventListItems);
            Debug.Assert(numActiveUnacked == activeUnackedCount);

            numInactiveUnacked = InsertAlarmsByState(_inactiveUnacked, eventMessagesList, sortedEventListItems);
            Debug.Assert(numInactiveUnacked == inactiveUnackedCount);

            numActiveAcked = InsertAlarmsByState(_activeAcked, eventMessagesList, sortedEventListItems);
            Debug.Assert(numActiveAcked == activeAckedCount);

            numInactiveAcked = InsertAlarmsByState(_inactiveAcked, eventMessagesList, sortedEventListItems);
            Debug.Assert(numInactiveAcked == inactiveAckedCount);

            // find the alerts next
            int eventMessageListIdx = 0;
            // the end of the last block is the beginning of this block
            int blockStartIdx = sortedEventListItems.Count;
            while (eventMessageListIdx < eventMessagesList.Count)
            {
                if (eventMessagesList[eventMessageListIdx].EventType == EventType.Alert)
                {
                    InsertMessageByPriority(sortedEventListItems, blockStartIdx, eventMessagesList[eventMessageListIdx]);
                    // this element of the eventMessageList was processed, so remove it
                    eventMessagesList.RemoveAt(eventMessageListIdx);
                }
                else eventMessageListIdx++;
            }
            numAlert = sortedEventListItems.Count - blockStartIdx;

            // Add the remaining - they should be the events 
            eventMessageListIdx = 0;
            blockStartIdx = sortedEventListItems.Count;
            while (eventMessageListIdx < eventMessagesList.Count)
            {
                if ((eventMessagesList[eventMessageListIdx].EventType == EventType.SystemEvent) ||
                    (eventMessagesList[eventMessageListIdx].EventType == EventType.OperatorActionEvent))
                    InsertMessageByPriority(sortedEventListItems, blockStartIdx, eventMessagesList[eventMessageListIdx]);
                eventMessageListIdx++;
            }
            numEvent = sortedEventListItems.Count - blockStartIdx;

            return sortedEventListItems;
        }

        /// <summary>
        ///   This method is used to set the size and time limits for the event list.
        /// </summary>
        /// <param name="keepAllEventsAge"> This parameter establishes the time the general event notifications will be maintained in the event list. These are generally event messages that fall into two general categories. First, are events that do not require any action and are primarily informational in nature. Second, are events that do require some form of action. These actions may be performed by an operator or automatically. However, in general these events are of low interest. All event occupancies may be kept by an event historian, this is not the purpose of this real time event list. </param>
        /// <param name="maxAnyEventAge"> No events are kept in the list that exceed this age limit. </param>
        /// <param name="maxEventListItems"> This is the maximum number of events that may be kept in this list. </param>
        /// <returns> </returns>
        public bool SetListLimits(TimeSpan keepAllEventsAge, TimeSpan maxAnyEventAge, uint maxEventListItems)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiEventList.");

            bool bFlag = true;
            if (new TimeSpan(60, 0, 0, 0) < keepAllEventsAge)
            {
                bFlag = false;
                MaxKeepAllEventsAge = new TimeSpan(60, 0, 0, 0);
            }
            else if (new TimeSpan(0, 1, 0) > keepAllEventsAge)
            {
                bFlag = false;
                MaxKeepAllEventsAge = new TimeSpan(0, 1, 0);
            }
            else MaxKeepAllEventsAge = keepAllEventsAge;

            if (new TimeSpan(360, 0, 0, 0) < maxAnyEventAge)
            {
                bFlag = false;
                MaxAnyEventAge = new TimeSpan(360, 0, 0, 0);
            }
            else if (new TimeSpan(0, 1, 0) > maxAnyEventAge)
            {
                bFlag = false;
                MaxAnyEventAge = new TimeSpan(0, 1, 0);
            }
            else MaxAnyEventAge = maxAnyEventAge;

            if (12000 < maxEventListItems)
            {
                bFlag = false;
                MaxEventListItems = 12000;
            }
            else if (36 > maxEventListItems)
            {
                bFlag = false;
                MaxEventListItems = 36;
            }
            else MaxEventListItems = maxEventListItems;

            return bFlag;
        }

        /// <summary>
        ///   This method is used to request that category-specific fields be 
        ///   included in event messages generated for alarms and events of 
        ///   the category for this event list.
        /// </summary>
        /// <param name="categoryId"> The category for which event message fields are being added. </param>
        /// <param name="fieldObjectTypeIds"> </param>
        /// <returns> </returns>
        public IEnumerable<TypeIdResult> AddEventMessageFields(uint categoryId, IEnumerable<TypeId> fieldObjectTypeIds)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiEventList.");

            return Context.AddEventMessageFields(ServerListId, categoryId, fieldObjectTypeIds);
        }

        /// <summary>
        ///   This method returns a list of event messages that can be submitted to EventNotification() for testing
        ///   The event messages created can be reorganized to allow for different ordering that have to be sorted
        /// </summary>
        /// <returns> A list of event messages </returns>
        public EventMessage[] GetEventMessages()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiEventList.");

            var msgs = new EventMessage[31];

            AlarmMessageData alm = null;
            EventId eventId;
            EventMessage msg = null;
            int i = 0;

            msg = new EventMessage
                      {
                          EventType = EventType.DiscardedMessage,
                          TextMessage = "3"
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C1")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = AlarmState.Initial
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 1,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(10),
                          TextMessage = "C1 Acked-Inactive P1 " + DateTime.UtcNow.AddMinutes(10)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C2")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = AlarmState.Initial
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 3,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(6),
                          TextMessage = "C2 Acked-Inactive P3 " + DateTime.UtcNow.AddMinutes(6)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C3")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = AlarmState.Initial
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 5,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(3),
                          TextMessage = "C3 Acked-Inactive P5 " + DateTime.UtcNow.AddMinutes(3)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C4")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = AlarmState.Initial
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 4,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(7),
                          TextMessage = "C4 Acked-Inactive P4 " + DateTime.UtcNow.AddMinutes(7)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C5")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = AlarmState.Initial
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 2,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(8),
                          TextMessage = "C5 Acked-Inactive P2 " + DateTime.UtcNow.AddMinutes(8)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C6")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = AlarmState.Active
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 2,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(6),
                          TextMessage = "C6 Acked-Active P2 " + DateTime.UtcNow.AddMinutes(6)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C7")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = AlarmState.Active
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 5,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(5),
                          TextMessage = "C7 Acked-Active P5 " + DateTime.UtcNow.AddMinutes(5)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C8")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = AlarmState.Active
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 3,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(3),
                          TextMessage = "C8 Acked-Active P3 " + DateTime.UtcNow.AddMinutes(3)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C9")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = AlarmState.Active
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 1,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(7),
                          TextMessage = "C9 Acked-Active P1 " + DateTime.UtcNow.AddMinutes(7)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C10")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = AlarmState.Active
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 2,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(9),
                          TextMessage = "C10 Acked-Active P2 " + DateTime.UtcNow.AddMinutes(9)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C11")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = AlarmState.Active | AlarmState.Unacked
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 4,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(3),
                          TextMessage = "C11 Unacked-Active P4 " + DateTime.UtcNow.AddMinutes(3)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C12")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = AlarmState.Active | AlarmState.Unacked
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 5,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(1),
                          TextMessage = "C12 Unacked-Active P5 " + DateTime.UtcNow.AddMinutes(1)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C13")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = AlarmState.Active | AlarmState.Unacked
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 3,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(4),
                          TextMessage = "C13 Unacked-Active P3 " + DateTime.UtcNow.AddMinutes(4)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C14")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = AlarmState.Active | AlarmState.Unacked
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 1,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(7),
                          TextMessage = "C14 Unacked-Active P1 " + DateTime.UtcNow.AddMinutes(7)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C15")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = AlarmState.Active | AlarmState.Unacked
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 2,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(3),
                          TextMessage = "C15 Unacked-Active P2 " + DateTime.UtcNow.AddMinutes(3)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C16")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = AlarmState.Unacked
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 5,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(2),
                          TextMessage = "C16 Unacked-Inactive P5 " + DateTime.UtcNow.AddMinutes(2)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C17")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = AlarmState.Unacked
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 4,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(21),
                          TextMessage = "C17 Unacked-Inactive P4 " + DateTime.UtcNow.AddMinutes(21)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C18")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = AlarmState.Unacked
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 3,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(10),
                          TextMessage = "C18 Unacked-Inactive P3 " + DateTime.UtcNow.AddMinutes(10)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C19")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = AlarmState.Unacked
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 2,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(8),
                          TextMessage = "C19 Unacked-Inactive P2 " + DateTime.UtcNow.AddMinutes(8)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C20")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = AlarmState.Unacked
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 1,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(12),
                          TextMessage = "C20 Unacked-Inactive P1 " + DateTime.UtcNow.AddMinutes(12)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C21")
                                              }
                          };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.Alert,
                          Priority = 5,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(4),
                          TextMessage = "C21 Alert P5 " + DateTime.UtcNow.AddMinutes(4)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C22")
                                              }
                          };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.Alert,
                          Priority = 2,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(5),
                          TextMessage = "C22 Alert P2 " + DateTime.UtcNow.AddMinutes(5)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C23")
                                              }
                          };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.Alert,
                          Priority = 3,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(4),
                          TextMessage = "C23 Alert P3 " + DateTime.UtcNow.AddMinutes(4)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C24")
                                              }
                          };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.Alert,
                          Priority = 1,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(3),
                          TextMessage = "C24 Alert P1 " + DateTime.UtcNow.AddMinutes(3)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C25")
                                              }
                          };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.Alert,
                          Priority = 4,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(6),
                          TextMessage = "C25 Alert P4 " + DateTime.UtcNow.AddMinutes(6)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C26")
                                              }
                          };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SystemEvent,
                          Priority = 2,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(1),
                          TextMessage = "C26 SystemEvent P2 " + DateTime.UtcNow.AddMinutes(1)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C27")
                                              }
                          };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SystemEvent,
                          Priority = 5,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(2),
                          TextMessage = "C27 SystemEvent P5 " + DateTime.UtcNow.AddMinutes(2)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C28")
                                              }
                          };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SystemEvent,
                          Priority = 2,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(3),
                          TextMessage = "C28 SystemEvent P2 " + DateTime.UtcNow.AddMinutes(3)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C29")
                                              }
                          };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SystemEvent,
                          Priority = 2,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(1),
                          TextMessage = "C29 SystemEvent P2 " + DateTime.UtcNow.AddMinutes(1)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C30")
                                              }
                          };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SystemEvent,
                          Priority = 5,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(4),
                          TextMessage = "C30 SystemEvent P5 " + DateTime.UtcNow.AddMinutes(4)
                      };
            msgs[i++] = msg;

            return msgs;
        }

        /// <summary>
        ///   This method returns a list of event messages that can be submitted to EventNotification() for testing
        ///   The event messages created can be reorganized to allow for different ordering that have to be sorted
        /// </summary>
        /// <returns> </returns>
        public EventMessage[] GetEventMessages2()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiEventList.");

            var msgs = new EventMessage[17];

            AlarmMessageData alm = null;
            EventId eventId;
            EventMessage msg = null;
            int i = 0;

            msg = new EventMessage
                      {
                          EventType = EventType.DiscardedMessage,
                          TextMessage = "3"
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C1")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = AlarmState.Active | AlarmState.Unacked
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 1,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(2),
                          TextMessage = "C1 Unacked-Active P1 " + DateTime.UtcNow.AddMinutes(2)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C6")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = AlarmState.Initial
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 2,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(4),
                          TextMessage = "C6 Acked-Inactive P2 " + DateTime.UtcNow.AddMinutes(4)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C11")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = AlarmState.Active
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 4,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(6),
                          TextMessage = "C11 Acked-Active P4 " + DateTime.UtcNow.AddMinutes(6)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C16")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = AlarmState.Initial
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 5,
                          OccurrenceTime = DateTime.UtcNow,
                          TextMessage = "C16 Acked-Inactive P5 " + DateTime.UtcNow
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C101")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = 0
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 1,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(3),
                          TextMessage = "C101 Acked-Inactive P1 " + DateTime.UtcNow.AddMinutes(3)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C102")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = 0
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 3,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(21),
                          TextMessage = "C102 Acked-Inactive P3 " + DateTime.UtcNow.AddMinutes(21)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C201")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = AlarmState.Active
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 2,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(12),
                          TextMessage = "C201 Acked-Active P2 " + DateTime.UtcNow.AddMinutes(12)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C202")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = AlarmState.Active
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 5,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(11),
                          TextMessage = "C202 Acked-Active P5 " + DateTime.UtcNow.AddMinutes(11)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C301")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = AlarmState.Active | AlarmState.Unacked
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 4,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(4),
                          TextMessage = "C301 Unacked-Active P4 " + DateTime.UtcNow.AddMinutes(4)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C302")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = AlarmState.Active | AlarmState.Unacked
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 5,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(6),
                          TextMessage = "C302 Unacked-Active P5 " + DateTime.UtcNow.AddMinutes(6)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C401")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = AlarmState.Unacked
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 5,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(9),
                          TextMessage = "C401 Unacked-Inactive P5 " + DateTime.UtcNow.AddMinutes(9)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C402")
                                              }
                          };
            alm = new AlarmMessageData
                      {
                          AlarmState = AlarmState.Unacked
                      };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SimpleAlarm,
                          AlarmData = alm,
                          Priority = 4,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(5),
                          TextMessage = "C402 Unacked-Inactive P4 " + DateTime.UtcNow.AddMinutes(5)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C501")
                                              }
                          };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.Alert,
                          Priority = 5,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(1),
                          TextMessage = "C501 Alert P5 " + DateTime.UtcNow.AddMinutes(1)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C502")
                                              }
                          };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.Alert,
                          Priority = 2,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(10),
                          TextMessage = "C502 Alert P2 " + DateTime.UtcNow.AddMinutes(10)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C601")
                                              }
                          };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SystemEvent,
                          Priority = 2,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(1),
                          TextMessage = "C601 SystemEvent P2 " + DateTime.UtcNow.AddMinutes(1)
                      };
            msgs[i++] = msg;

            eventId = new EventId
                          {
                              SourceId = new InstanceId("EventSource1"),
                              Condition = new List<TypeId>
                                              {
                                                  new TypeId("", "", "C602")
                                              }
                          };
            msg = new EventMessage
                      {
                          EventId = eventId,
                          EventType = EventType.SystemEvent,
                          Priority = 5,
                          OccurrenceTime = DateTime.UtcNow.AddMinutes(2),
                          TextMessage = "C602 SystemEvent P5 " + DateTime.UtcNow.AddMinutes(2)
                      };
            msgs[i++] = msg;

            return msgs;
        } */

        /// <summary>
        ///     Throws or returns new IXiEventListItems (not null, but possibly zero-lenghth).
        ///     This method is used to poll the endpoint for changes to a specific event list.
        ///     Event messages are sent by the server when there has been a change to the specified
        ///     event list. A new alarm or event that has been added to the list, a change to an
        ///     alarm already in the list, or the deletion of an alarm from the list constitutes a
        ///     change to the list.
        ///     <para>
        ///         Once the poll completes, this method calls the EventNotification() method to add the received events to the
        ///         event list.
        ///     </para>
        /// </summary>
        /// <param name="filterSet">
        ///     The filter set used to filter event messages. This filter is sent to the server where it is
        ///     used to select event messages to return.
        /// </param>
        public IXiEventListItem[] PollEventChanges(FilterSet? filterSet)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiEventList.");

            return Context.PollEventChanges(this, filterSet);
        }

        /// <summary>
        ///     <para> result != null </para>
        ///     <para>
        ///         This callback method receives event messages sent by the server that contain both events and alarms. Servers
        ///         send event messages when there has been a change to its Event List. A new alarm or event that has been added to
        ///         the Event List, a change to an alarm already in the list, or the deletion of an alarm from the list constitutes
        ///         a change to the list. Client applications may read this list using the enumerator defined for this class.
        ///     </para>
        ///     <para>
        ///         Upon receipt of an event notification, this method adds the messages representing events to the end of the
        ///         Event List. It inserts alarm messages into the list using a combination of their state and priority. New
        ///         active, unacknowledged alarms are added first, followed by inactive, unacknowledged alarms. Inactive,
        ///         acknowledged are added next, followed by alerts, and finally event messages. Each of these sets is sorted in
        ///         descending priority order. If an alarm is already in the list, it is removed from the list and its new alarm
        ///         message is inserted in its appropriate location.
        ///     </para>
        ///     <para> Once the list has been updated, the client application is notified with the received event messages. </para>
        ///     <para>
        ///         Periodically, this method performs routine maintenance on the Event List. Event messages are automatically
        ///         deleted from the list after a period of time. Alarms, are also automatically deleted from the list after a
        ///         period of time if they have transitioned to inactive and acknowledged.
        ///     </para>
        ///     <para>
        ///         Additionally, any message that been in the list a maximum amount time are automatically deleted, and if the
        ///         maximum size of the Event List has been reached, the oldest messages are deleted from the list.
        ///     </para>
        /// </summary>
        /// <param name="eventMessages"> </param>
        public List<IXiEventListItem> EventNotification(EventMessage[]? eventMessages)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiEventList.");

            var result = new List<IXiEventListItem>();

            if (eventMessages != null)
            {
                foreach (var eventMessage in eventMessages)
                {
                    result.Add(new XiEventListItem(eventMessage));
                }
            }

            return result;

            /*
            int idx = 0;
            if (eventMessages[idx].EventType == EventType.DiscardedMessage) // is this a discard message?
            {
                try
                {
                    uint numberOfDiscardedValues = Convert.ToUInt32(eventMessages[idx].TextMessage);
                    Context.RaiseContextNotifyEvent(StandardListType,
                                          new XiContextNotificationData(XiContextNotificationType.Discards,
                                                                        numberOfDiscardedValues));
                }
                catch
                {
                    Context.RaiseContextNotifyEvent(this,
                                          new XiContextNotificationData(XiContextNotificationType.TypeConversionError,
                                                                        "A Discard Event Message whose Text Message could not be converted to the number of discarded messages was received. The Text Message was:\n" +
                                                                        eventMessages[idx].TextMessage));
                }
            }

            // Sort the incoming messages
            int numActiveUnackedNew = 0; // number of activeUnacked messages in the sorted list
            int numInactiveUnackedNew = 0; // number of inactiveUnacked messages in the sorted list
            int numActiveAckedNew = 0; // number of activeAacked messages in the sorted list
            int numInactiveAckedNew = 0; // number of inactiveUnacked messages in the sorted list
            int numAlertNew = 0; // number of alert messages in the sorted list
            int numEventNew = 0; // number of event messages in the sorted list
            List<XiEventListItem> newEventListItems = SortEventMessages(eventMessages, out numActiveUnackedNew,
                                                                           out numInactiveUnackedNew,
                                                                           out numActiveAckedNew,
                                                                           out numInactiveAckedNew, out numAlertNew,
                                                                           out numEventNew);

            // loop through the newly created list and remove alarms from the 
            // existing _ListElements that are in the newly received event message list 
            foreach (XiEventListItem item in newEventListItems)
            {
                RemoveElementFromList(item);
            }

            // remove all events from the list - they remain in the list one cycle
            
            var itemsToRemove = new List<XiEventListItem>();
            foreach (var item in _items)
            {
                if ((item.EventMessage.EventType == EventType.Alert) ||
                    (item.EventMessage.EventType == EventType.OperatorActionEvent) ||
                    (item.EventMessage.EventType == EventType.SystemEvent)) itemsToRemove.Add(item);
            }
            foreach (XiEventListItem item in itemsToRemove)
            {
                RemoveElementFromList(item);
            }

            // Now see if it is time to removed aged elements from the list
            if (_lastListMaintenanceTime == DateTime.MinValue) _lastListMaintenanceTime = DateTime.UtcNow;
            DoListMaintenance();

            // Add the newly received event list elements to the list
            MergeNewElementsIntoList(newEventListItems, numActiveUnackedNew, numInactiveUnackedNew, numActiveAckedNew,
                                     numInactiveAckedNew, numAlertNew, numEventNew);

            // This removal is done to force the list not to exceed a maximum size.
            // This removal is done regardless of event type or state.
            
            if (MaxEventListItems < _items.Count)
            {
                long numToRemove = MaxEventListItems - _items.Count;
                for (long i = 0; i < numToRemove; i++)
                {
                    _items.RemoveAt(_items.Count - 1);
                }
            }

            // roll the event notification count over to 1
            _eventNotificationCount = (_eventNotificationCount == uint.MaxValue) ? 1 : _eventNotificationCount + 1;

            return newEventListItems;*/
        }

        /// <summary>
        ///     Throws or invokes EventNotificationEvent.        
        /// </summary>
        /// <param name="newEventListItems"></param>
        public void RaiseEventNotificationEvent(IEnumerable<IXiEventListItem> newEventListItems)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiEventList.");

            try
            {
                EventNotificationEvent(this, newEventListItems);
            }
            catch (Exception ex)
            {
                _lastEventNotificationExceptionMessage = ex.Message;
            }
        }

        /// <summary>
        ///     This event is used to notify the client application when new events are received.
        /// </summary>
        public event XiEventNotificationEventHandler EventNotificationEvent = delegate { };

        /// <summary>
        ///     All events should be kept for at least this time span.
        /// </summary>
        public TimeSpan MaxKeepAllEventsAge
        {
            get { return _maxKeepAllEventsAge; }
            protected set { _maxKeepAllEventsAge = value; }
        }

        /// <summary>
        ///     No events are allowed to exceed this time span.
        /// </summary>
        public TimeSpan MaxAnyEventAge
        {
            get { return _maxAnyEventAge; }
            protected set { _maxAnyEventAge = value; }
        }

        /// <summary>
        ///     This list is not allowed to exceed this number of events.
        /// </summary>
        public uint MaxEventListItems
        {
            get { return _maxEventListItems; }
            protected set { _maxEventListItems = value; }
        }

        ///// <summary>
        /////     This property provides a count of the number of notification events (callbacks)
        /////     that have been issued to the client application for this list.
        ///// </summary>
        //public uint EventNotificationCount
        //{
        //    get { return _eventNotificationCount; }
        //}

        ///// <summary>
        /////     This property provides a count of the total number of event
        /////     messages notifications that have been received by this list
        /////     and delivered to the client application.
        ///// </summary>
        //public uint TotalDeliveredEventMessages
        //{
        //    get { return _totalDeliveredEventMessages; }
        //}

        /// <summary>
        ///     This property is the publically visible ReadOnlyCollection of Category Specific Event Message Fields
        /// </summary>
        public ReadOnlyCollection<KeyValuePair<string, XiCategorySpecificFields>> CategorySpecificFieldCollection
        {
            get
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiEventList.");

                return
                    new ReadOnlyCollection<KeyValuePair<string, XiCategorySpecificFields>>(
                        _CategorySpecificFieldDict.ToList());
            }
        }

        #endregion

        #region protected functions

        /// <summary>
        ///     This property is the internally visible Dictionary of Category Specific Event Message Fields
        /// </summary>
        protected Dictionary<string, XiCategorySpecificFields> CategorySpecificFieldDictionary
        {
            get { return _CategorySpecificFieldDict; }
        }

        /*
        /// <summary>
        ///   The method inserts the specified alarms into the sorted event list and returns the 
        ///   nunmber of alarms inserted.
        /// </summary>
        /// <param name="alarmState"> The alarm state of the alarms to be inserted. Event messages in the input list that are not alarms of this state are not inserted into output list. </param>
        /// <param name="eventMessagesList"> The list of event messages that contains the alarms to be inserted </param>
        /// <param name="sortedEventListItems"> The XiEventListItems list into which the alarms are to be inserted </param>
        /// <returns> Returns the number of inserted alarms. </returns>
        protected static int InsertAlarmsByState(int alarmState, List<EventMessage> eventMessagesList,
                                                 List<XiEventListItem> sortedEventListItems)
        {
            int eventMessageListIdx = 0;
            // the end of the last block is the beginning of this block
            int blockStartIdx = sortedEventListItems.Count;
            while (eventMessageListIdx < eventMessagesList.Count)
            {
                if ((eventMessagesList[eventMessageListIdx].EventType == EventType.SimpleAlarm) ||
                    (eventMessagesList[eventMessageListIdx].EventType == EventType.GroupedAlarm) ||
                    (eventMessagesList[eventMessageListIdx].EventType == EventType.EclipsedAlarm))
                {
                    if (eventMessagesList[eventMessageListIdx].AlarmData != null)
                    {
                        if ((int) eventMessagesList[eventMessageListIdx].AlarmData.AlarmState == alarmState)
                        {
                            InsertMessageByPriority(sortedEventListItems, blockStartIdx, eventMessagesList[eventMessageListIdx]);
                            // this element of the eventMessageList was processed, so remove it
                            eventMessagesList.RemoveAt(eventMessageListIdx);
                        }
                        else eventMessageListIdx++;
                    }
                    else
                    {
                        // discard this message since it is an alarm without alarm data
                        eventMessagesList.RemoveAt(eventMessageListIdx);
                    }
                }
                else eventMessageListIdx++;
            }
            return sortedEventListItems.Count - blockStartIdx;
        }

        protected static int InsertMessageByPriority(List<XiEventListItem> sortedEventListItems, int beginningIdx,
                                                     EventMessage eventMessage)
        {
            int blockStartIdx = beginningIdx;
            int insertIndex = -1;
            if (sortedEventListItems.Count == 0)
            {
                try
                {
                    string msgKey = eventMessage.MakeMessageKey();
                    sortedEventListItems.Add(new XiEventListItem(msgKey, eventMessage));
                }
                catch
                {
                    // ignore this event message if the messsage clientListId cannot be created 
                }
            }
            else
            {
                for (insertIndex = blockStartIdx; insertIndex < sortedEventListItems.Count; insertIndex++)
                {
                    // Insert this message before the first event message of the same priority
                    if (eventMessage.Priority == sortedEventListItems[insertIndex].EventMessage.Priority)
                    {
                        InsertMessageByOccurrenceTime(sortedEventListItems, insertIndex, eventMessage);
                        break;
                    }
                    if (eventMessage.Priority > sortedEventListItems[insertIndex].EventMessage.Priority)
                    {
                        string msgKey = eventMessage.MakeMessageKey();
                        sortedEventListItems.Insert(insertIndex, new XiEventListItem(msgKey, eventMessage));
                        break;
                    }
                }
                if (insertIndex == sortedEventListItems.Count)
                    // if its priority is lower than the others, add it to the end of the list
                {
                    string msgKey = eventMessage.MakeMessageKey();
                    sortedEventListItems.Insert(insertIndex, new XiEventListItem(msgKey, eventMessage));
                }
            }
            return insertIndex;
        }

        protected static int InsertMessageByOccurrenceTime(List<XiEventListItem> sortedEventListItems,
                                                           int beginningIdx, EventMessage eventMessage)
        {
            int blockStartIdx = beginningIdx;
            int insertIndex = -1;
            if (sortedEventListItems.Count == 0)
            {
                try
                {
                    string msgKey = eventMessage.MakeMessageKey();
                    sortedEventListItems.Add(new XiEventListItem(msgKey, eventMessage));
                }
                catch
                {
                    // ignore this event message if the messsage clientListId cannot be created 
                }
            }
            else
            {
                for (insertIndex = blockStartIdx; insertIndex < sortedEventListItems.Count; insertIndex++)
                {
                    // Insert this message before the first event message of the same priority
                    if (eventMessage.OccurrenceTime > sortedEventListItems[insertIndex].EventMessage.OccurrenceTime)
                    {
                        string msgKey = eventMessage.MakeMessageKey();
                        sortedEventListItems.Insert(insertIndex, new XiEventListItem(msgKey, eventMessage));
                        break;
                    }
                }
                if (insertIndex == sortedEventListItems.Count)
                    // if its priority is lower than the others, add it to the end of the list
                {
                    string msgKey = eventMessage.MakeMessageKey();
                    sortedEventListItems.Add(new XiEventListItem(msgKey, eventMessage));
                }
            }
            return insertIndex;
        }

        /// <summary>
        ///   This method is called to remove elements from the list that have expired or that whose age is 
        ///   greater than the maximum allowed.  Events expire after they have been delivered to the client 
        ///   application and alarms in the Acked/Inactive state expire after they have been delivered. 
        ///   Alarms in any other state do not expire.
        /// </summary>
        protected void DoListMaintenance()
        {
            TimeSpan timeDiff = DateTime.UtcNow - _lastListMaintenanceTime;
            _lastListMaintenanceTime = DateTime.UtcNow;

            if (timeDiff >= _listMaintenanceInterval)
            {
                DateTime now = DateTime.UtcNow;

                var itemsToRemove = new List<XiEventListItem>();

                foreach (var item in _items)
                {
                    // Remove any elements that should no longer be of interest.
                    // These are events that occurred some time ago that are either 
                    // events that did not require an action or events that may have 
                    // required an action and that action has been done.  Actions may 
                    // be done automatically or by an operator.
                    if (MaxKeepAllEventsAge < (now - item.EventMessage.OccurrenceTime))
                    {
                        switch (item.EventMessage.EventType)
                        {
                            case EventType.SystemEvent:
                            case EventType.OperatorActionEvent:
                                itemsToRemove.Add(item);
                                break;

                            case EventType.SimpleAlarm:
                            case EventType.EclipsedAlarm:
                            case EventType.GroupedAlarm:
                                if (null != item.EventMessage.AlarmData)
                                {
                                    if (AlarmState.Initial == item.EventMessage.AlarmData.AlarmState)
                                        // if inactive and acked, remove it
                                        itemsToRemove.Add(item);
                                }
                                else itemsToRemove.Add(item);
                                break;

                            case EventType.Alert:
                                itemsToRemove.Add(item);
                                break;

                            default:
                                break;
                        }
                    }
                    // A second remove is to remove any element that is older than a maximum age.  
                    // This is done to help prevent the event list from becoming excessive in size.
                    // This removal is done regardless of event type or state.
                    if (MaxAnyEventAge < (now - item.EventMessage.OccurrenceTime))
                        itemsToRemove.Add(item);
                }

                foreach (XiEventListItem item in itemsToRemove)
                {
                    RemoveElementFromList(item);
                }
            }
        }

        /// <summary>
        ///   This method removes the specified list element from the list, if present, and updates 
        ///   the associated list counter
        /// </summary>
        /// <param name="item"> The element to remove </param>
        /// <returns> Returns TRUE if the element was removed from _ListElements </returns>
        protected bool RemoveElementFromList(XiEventListItem item)
        {
            bool removed = false;
            XiEventListItem item2 = _items.SingleOrDefault(it => it.MessageKey == item.MessageKey);
            if (item2 != null)
            {
                if ((item2.EventMessage.EventType == EventType.SimpleAlarm) ||
                    (item2.EventMessage.EventType == EventType.GroupedAlarm) ||
                    (item2.EventMessage.EventType == EventType.EclipsedAlarm))
                {
                    if ((int) item2.EventMessage.AlarmData.AlarmState == _activeUnacked) _numUnackedActiveInList--;
                    else if ((int) item2.EventMessage.AlarmData.AlarmState == _inactiveUnacked)
                        _numUnackedInactiveInList--;
                    else if ((int) item2.EventMessage.AlarmData.AlarmState == _activeAcked) _numAckedActiveInList--;
                    else if ((int) item2.EventMessage.AlarmData.AlarmState == _inactiveAcked)
                        _numAckedInactiveInList--;
                }
                else if (item2.EventMessage.EventType == EventType.Alert) _numAlertInList--;
                else if (item2.EventMessage.EventType != EventType.DiscardedMessage) _numEventInList--;
                _items.Remove(item2);
                removed = true;
            }
            return removed;
        }

        /// <summary>
        ///   This method takes a sorted list of event messages and merges them into _ListElements
        /// </summary>
        /// <param name="newEventListItems"> The sorted list to merge into _ListElements </param>
        /// <param name="newNumUnackedActive"> The number of UnackedActive messages in the sorted list </param>
        /// <param name="newNumUnackedInactive"> The number of UnackedInactive messages in the sorted list </param>
        /// <param name="newNumAckedActive"> The number of AckedActive messages in the sorted list </param>
        /// <param name="newNumAckedInactive"> The number of AckedInactive messages in the sorted list </param>
        /// <param name="newNumAlert"> The number of Alerts messages in the sorted list </param>
        /// <param name="newNumEvent"> The number of Events messages in the sorted list </param>
        protected void MergeNewElementsIntoList(List<XiEventListItem> newEventListItems, int newNumUnackedActive,
                                                int newNumUnackedInactive, int newNumAckedActive,
                                                int newNumAckedInactive, int newNumAlert,
                                                int newNumEvent)
        {
            int insertIdx = 0;
            int newElementListIdx = 0;
            MergeBlockOfElementsIntoList(insertIdx, ref _numUnackedActiveInList, newEventListItems,
                                            newElementListIdx, newNumUnackedActive);
            insertIdx = _numUnackedActiveInList;

            newElementListIdx = newNumUnackedActive;
            MergeBlockOfElementsIntoList(insertIdx, ref _numUnackedInactiveInList, newEventListItems,
                                            newElementListIdx, newNumUnackedInactive);
            insertIdx += _numUnackedInactiveInList;

            newElementListIdx += newNumUnackedInactive;
            MergeBlockOfElementsIntoList(insertIdx, ref _numAckedActiveInList, newEventListItems,
                                            newElementListIdx, newNumAckedActive);
            insertIdx += _numAckedActiveInList;

            newElementListIdx += newNumAckedActive;
            // The next two lines below would normally add the inactive/acked alarms back into the list, but they should really come out
            // so they are not added back in here.
            // TODO: Process the inactive/acked alarms differently here if desired
            //MergeBlockOfElementsIntoList(insertIdx, ref _numAckedInactiveInList, newEventListItems, newElementListIdx, newNumAckedInactive);
            //insertIdx += _numAckedInactiveInList;

            newElementListIdx += newNumAckedInactive;
            MergeBlockOfElementsIntoList(insertIdx, ref _numAlertInList, newEventListItems, newElementListIdx,
                                            newNumAlert);
            insertIdx += _numAlertInList;

            newElementListIdx += newNumAlert;
            MergeBlockOfElementsIntoList(insertIdx, ref _numEventInList, newEventListItems, newElementListIdx,
                                            newNumEvent);
        }

        /// <summary>
        ///   This method merges event messages into the event list (_ListElements).  The event messages 
        ///   to merge are contained as a contiguous subset of the event messages in the elementsToMerge 
        ///   list.  The elementStartIndex specifies the first event message in the list to merge and the 
        ///   numElementsInBlock specifies the number of event messages to merge.
        /// </summary>
        /// <param name="blockStartIdx"> The start of the block in _ListElements at which the merge will begin. </param>
        /// <param name="numInBlock"> The number of event messages in the block. This value is incremented for each event message merged into the block. </param>
        /// <param name="elementsToMerge"> The list containing the event messages to be merged into the block. </param>
        /// <param name="elementStartIndex"> The index of the first event message in the list of event messages to merge. </param>
        /// <param name="numElementsInBlock"> The mumber of event messages in elementsToMerge to be merged into the block. </param>
        protected void MergeBlockOfElementsIntoList(int blockStartIdx, ref int numInBlock,
                                                    List<XiEventListItem> elementsToMerge, int elementStartIndex,
                                                    int numElementsInBlock)
        {
            int insertIdx = blockStartIdx; // the index at which to insert the new element
            int blockEndIdx = blockStartIdx + numInBlock;
            int numInserted = 0; // for debugging
            int numDuplicates = 0; // for debugging
            int numInactiveAcked = 0; // for debugging
            
            // loop through this block of elementsToMerge and merge each element into _ListElements
            for (int i = 0; i < numElementsInBlock; i++)
            {
                // Loop through _ListElements in this block until one of the two is found:
                // 1) the _ListElements item's priority is less than the new event message's priority, or 
                // 2) the _ListElements item's priority is the same, and its occurrence time is the same as or earlier than the new event message's occurrence time.
                while ((insertIdx < blockEndIdx) &&
                        ((_items[insertIdx].EventMessage.Priority >
                            elementsToMerge[elementStartIndex].EventMessage.Priority) ||
                        ((_items[insertIdx].EventMessage.Priority ==
                            elementsToMerge[elementStartIndex].EventMessage.Priority) &&
                            (_items[insertIdx].EventMessage.OccurrenceTime >
                            elementsToMerge[elementStartIndex].EventMessage.
                                OccurrenceTime))))
                {
                    insertIdx++;
                }

                // When the while completes, the insert index will be at the end of this next block, or at a _ListElement in the block. The new 
                // element will be inserted just before this _ListElement.
                try
                {
                    XiEventListItem le = elementsToMerge[elementStartIndex];
                    if ((le.EventMessage.AlarmData != null) &&
                        (le.EventMessage.AlarmData.AlarmState == AlarmState.Initial))
                    {
                        // Don't include inactive/acked alarms
                        numInactiveAcked++; // for debugging
                    }
                    else
                    {
                        // see if the new event messages contain more than one event message for the 
                        // same alarm. If so, put the latest one in _ListElements
                        XiEventListItem existingItem = _items.SingleOrDefault(it => it.MessageKey == le.MessageKey);
                        
                        if (existingItem == null)
                        {
                            _items.Insert(insertIdx++, elementsToMerge[elementStartIndex]);
                            numInBlock++;
                            blockEndIdx++;
                            numInserted++; // for debugging
                        }
                        else if (existingItem.EventMessage.OccurrenceTime < le.EventMessage.OccurrenceTime)
                        {
                            // Replace an existing event in the list with the newer one if there are two messages 
                            // in the new events for the same event
                            _items.Remove(existingItem);
                            _items.Insert(insertIdx++, elementsToMerge[elementStartIndex]);
                            numInserted++; // for debugging
                        }
                        else numDuplicates++; // for debugging
                    }
                    _totalDeliveredEventMessages = (_totalDeliveredEventMessages == uint.MaxValue)
                                                        ? 1
                                                        : _totalDeliveredEventMessages + 1;
                }
                catch
                {
                    // ignore the failure to add the received message into the list - it will mean that a duplicate was received
                }
                elementStartIndex++;
            }
        }*/        

        /// <summary>
        ///     The inteval of time between Event List maintenance checks performed by the
        ///     EvemtNotification method.
        /// </summary>
        protected TimeSpan _listMaintenanceInterval = new TimeSpan(0, 2, 0);        

        #endregion

        #region private fields

        /// <summary>
        ///     This data member holds the last exception message encountered by the
        ///     InformationReport callback when calling valuesUpdateEvent().
        /// </summary>
        private static string? _lastEventNotificationExceptionMessage;

        // TODO: Update the methods in this region if the ordering of the Event List is to be changed

        /// <summary>
        ///     This data member is the private representation of CategorySpecificFieldCollection and
        ///     CategorySpecificFieldDictionary
        /// </summary>
        private readonly Dictionary<string, XiCategorySpecificFields> _CategorySpecificFieldDict =
            new Dictionary<string, XiCategorySpecificFields>();        

        /// <summary>
        ///     This data member is the private representation of MaxAnyEventAge
        /// </summary>
        private TimeSpan _maxAnyEventAge = new TimeSpan(1, 0, 0);

        /// <summary>
        ///     This data member is the private representation of MaxEventListItems
        /// </summary>
        private uint _maxEventListItems = 250;

        /// <summary>
        ///     This data member is the private representation of MaxKeepAllEventsAge
        /// </summary>
        private TimeSpan _maxKeepAllEventsAge = new TimeSpan(0, 0, 10);
       
        /// <summary>
        ///     This KeyedCollection holds the collection of XiEventListItems, each keyed by the
        ///     MessageKey.  This allows the list to be indexed by the MessageKey.
        /// </summary>
        private readonly List<XiEventListItem> _items = new List<XiEventListItem>(256);

        /// <summary>
        ///     Inactive, Acked integer value
        /// </summary>
        private const int _inactiveAcked = 0;

        #endregion
    }
}