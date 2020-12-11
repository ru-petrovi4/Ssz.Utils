using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Utils;
using Ssz.Xi.Client;
using TestWpfApp;

namespace Ssz.WpfHmi.Common.ModelData.Events
{
    /// <summary>
    ///     UI thread.
    /// </summary>
    internal class EventSourceModel
    {
        #region public functions

        public static EventSourceModel Instance = new EventSourceModel();        

        /// <summary>
        ///     condition != Normal
        ///     Returns true if active or unacked state of any condition changed.
        /// </summary>
        /// <returns>
        ///     true if active or unacked state of any condition changed
        ///     false if the alarm state remains the same
        /// </returns>
        public bool ProcessEventSourceObject(EventSourceObject eventSourceObject, AlarmConditionType alarmConditionType,
            uint categoryId, bool active, bool unacked, DateTime occurrenceTime)
        {
            Dictionary<AlarmConditionType, ConditionState> alarmConditions = eventSourceObject.AlarmConditions;
            
            ConditionState? conditionState;
            if (alarmConditions.TryGetValue(alarmConditionType, out conditionState))
            {
                if (conditionState.Active == active && conditionState.Unacked == unacked)
                {
                    //We found the existing condition, but have determined that nothing has changed
                    //in the condition.
                    return false;
                }
                
                //Something has changed in the condition.  Update the condition with the new alarm state
                conditionState.Active = active;
                conditionState.Unacked = unacked;
                if (active) conditionState.ActiveOccurrenceTime = occurrenceTime;

                if (!active && !unacked) //Moved into the Inactive-Acknowledged state
                {
                    //Remove this condition from the list since our alarm is no longer an alarm
                    alarmConditions.Remove(alarmConditionType);
                }
            }
            else
            {
                if (!active) 
                {
                    //A new alarm that is already inactive - weird?
                    return false;    //An odd state - we didn't find anything so return false.
                }

                //This condition doesn't already exist.  This means we are a new alarm
                //Normally this will be a newly active alarm.  Occasionally it will be an
                //inactive but unacknowledged alarm.  This second situation is a bit odd but
                //can occur if we have newly connected to an AE server that has 
                //unacknowledged-inactive alarms.  In both cases it is a valid alarm that we 
                //want to continue to track.  Add it to our list.
                var newConditionState = new ConditionState { Active = active, Unacked = unacked, CategoryId = categoryId };
                if (active) newConditionState.ActiveOccurrenceTime = occurrenceTime;
                alarmConditions.Add(alarmConditionType, newConditionState);
            }

            if (!active)
            {
                eventSourceObject.NormalCondition.Active = !alarmConditions.Any(c => c.Value.Active);
            }
            else
            {
                eventSourceObject.NormalCondition.Active = false;
            }

            eventSourceObject.NormalCondition.Unacked = unacked;

            eventSourceObject.NotifyAlarmUnackedSubscribers();
            eventSourceObject.NotifyAlarmCategorySubscribers();
            eventSourceObject.NotifyAlarmBrushSubscribers();
            eventSourceObject.NotifyAlarmConditionTypeSubscribers();

            return true;
        }

        public void OnBeforeStateLoad()
        {
            foreach (EventSourceObject eventSourceObject in _eventSourceObjectsDictionary.Values)
            {
                eventSourceObject.AlarmConditions.Clear();
                eventSourceObject.NotifyAlarmUnackedSubscribers();
                eventSourceObject.NotifyAlarmCategorySubscribers();
                eventSourceObject.NotifyAlarmBrushSubscribers();
                eventSourceObject.NotifyAlarmConditionTypeSubscribers();
            }
            foreach (EventSourceArea eventSourceArea in _eventSourceAreasDictionary.Values)
            {
                eventSourceArea.UnackedAlarmsCount = 0;
                eventSourceArea.ActiveAlarmsCategories.Clear();
                eventSourceArea.NotifyAlarmUnackedSubscribers();
                eventSourceArea.NotifyAlarmCategorySubscribers();
                eventSourceArea.NotifyAlarmBrushSubscribers();
            }
        }

        public void OnAlarmsListChanged()
        {
            foreach (EventSourceArea eventSourceArea in _eventSourceAreasDictionary.Values)
            {
                eventSourceArea.UnackedAlarmsCount = 0;
                eventSourceArea.ActiveAlarmsCategories.Clear();
            }

            foreach (EventSourceObject eventSourceObject in _eventSourceObjectsDictionary.Values)
            {
                if (eventSourceObject.AnyUnacked())
                {
                    foreach (EventSourceArea eventSourceArea in eventSourceObject.EventSourceAreas.Values)
                    {
                        eventSourceArea.UnackedAlarmsCount += 1;
                    }
                }

                uint maxCategory = eventSourceObject.GetActiveAlarmsMaxCategory();
                if (maxCategory > 0)
                {
                    foreach (EventSourceArea eventSourceArea in eventSourceObject.EventSourceAreas.Values)
                    {
                        int activeAlarmsCount;
                        if (!eventSourceArea.ActiveAlarmsCategories.TryGetValue(maxCategory, out activeAlarmsCount))
                        {
                            eventSourceArea.ActiveAlarmsCategories[maxCategory] = 1;
                        }
                        else
                        {
                            eventSourceArea.ActiveAlarmsCategories[maxCategory] = activeAlarmsCount + 1;
                        }
                    }
                }
            }

            foreach (EventSourceArea eventSourceArea in _eventSourceAreasDictionary.Values)
            {
                eventSourceArea.NotifyAlarmUnackedSubscribers();
                eventSourceArea.NotifyAlarmCategorySubscribers();
            }
        }

        /// <summary>
        ///     null or Empty area is for root Area.
        ///     result != null
        /// </summary>
        /// <remarks>
        /// Retrieves the EventSourceArea object for the specified area name. If the area is not already being requested, 
        /// a new EventSourceArea is created, associated with that area name, and returned to the caller.
        /// </remarks>
        /// <param name="area"></param>
        /// <returns>
        /// An EventSourceArea. Either the existing one, or a newly created one
        /// </returns>
        public EventSourceArea GetEventSourceArea(string area)
        {
            if (String.IsNullOrEmpty(area)) area = @"";

            EventSourceArea? eventSourceArea;
            if (!_eventSourceAreasDictionary.TryGetValue(area, out eventSourceArea))
            {
                eventSourceArea = new EventSourceArea();
                _eventSourceAreasDictionary[area] = eventSourceArea;
            }
            return eventSourceArea;
        }

        /// <summary>        
        /// </summary>
        /// <remarks>
        /// Retrieves the EventSourceObject for the specified tag.  If the tag is not already being requested, 
        /// a new EventSourceObject is created, associated with the tag, and returned to the caller.
        /// </remarks>
        /// <returns>
        /// An EventSourceObject.  Either the existing one, or a newly created one
        /// </returns>
        public EventSourceObject GetEventSourceObject(string tag)
        {
            EventSourceObject? existingEventSourceObject;
            if (_eventSourceObjectsDictionary.TryGetValue(tag, out existingEventSourceObject))
            {
                //We already have this tag in our list.  Just return the existing object.
                return existingEventSourceObject;
            }

            //The tag doesn't already exist.  Create a new EventSourceObject and add it to our dictionary
            var newEventSourceObject = new EventSourceObject(tag);
            _eventSourceObjectsDictionary[tag] = newEventSourceObject;

            EventSourceArea overviewEventSourceArea = GetEventSourceArea(@"");
            newEventSourceObject.EventSourceAreas[@""] = overviewEventSourceArea;

            return newEventSourceObject;
        }

        public void GetExistingAlarmInfoViewModels(Action<IEnumerable<AlarmInfoViewModelBase>> alarmNotification)
        {
            var alarmInfoViewModels = new List<AlarmInfoViewModelBase>();
            foreach (var kvp in _eventSourceObjectsDictionary)
            {
                EventSourceObject eventSourceObject = kvp.Value;
                var alarmInfoViewModelsForObject = new List<AlarmInfoViewModelBase>();
                foreach (var condition in eventSourceObject.AlarmConditions.Values.OrderByDescending(cs => cs.CategoryId))
                {
                    if (!condition.Active && condition.Unacked &&
                        condition.LastAlarmInfoViewModel != null)
                        alarmInfoViewModelsForObject.Add(condition.LastAlarmInfoViewModel);
                }
                foreach (var condition in eventSourceObject.AlarmConditions.Values.OrderBy(cs => cs.CategoryId))
                {
                    if (condition.Active &&
                        condition.LastAlarmInfoViewModel != null)
                        alarmInfoViewModelsForObject.Add(condition.LastAlarmInfoViewModel);
                }
                if (eventSourceObject.NormalCondition.Active && eventSourceObject.NormalCondition.Unacked &&
                    eventSourceObject.NormalCondition.LastAlarmInfoViewModel != null)
                        alarmInfoViewModelsForObject.Add(eventSourceObject.NormalCondition.LastAlarmInfoViewModel);
                if (alarmInfoViewModelsForObject.Count > 0)
                    alarmInfoViewModels.AddRange(alarmInfoViewModelsForObject);
            }

            if (alarmInfoViewModels.Count > 0) alarmNotification(alarmInfoViewModels);
        }

        public void Clear()
        {
            foreach (EventSourceObject eventSourceObject in _eventSourceObjectsDictionary.Values)
            {
                eventSourceObject.AlarmConditions.Clear();
                eventSourceObject.NotifyAlarmUnackedSubscribers();
                eventSourceObject.NotifyAlarmCategorySubscribers();
                eventSourceObject.NotifyAlarmBrushSubscribers();
                eventSourceObject.NotifyAlarmConditionTypeSubscribers();
            }
            foreach (EventSourceArea eventSourceArea in _eventSourceAreasDictionary.Values)
            {
                eventSourceArea.UnackedAlarmsCount = 0;
                eventSourceArea.ActiveAlarmsCategories.Clear();
                eventSourceArea.NotifyAlarmUnackedSubscribers();
                eventSourceArea.NotifyAlarmCategorySubscribers();
                eventSourceArea.NotifyAlarmBrushSubscribers();
            }
        }

        #endregion

        #region private fields

        private readonly CaseInsensitiveDictionary<EventSourceObject> _eventSourceObjectsDictionary =
            new CaseInsensitiveDictionary<EventSourceObject>();

        private readonly CaseInsensitiveDictionary<EventSourceArea> _eventSourceAreasDictionary =
            new CaseInsensitiveDictionary<EventSourceArea>();

        #endregion
    }
}




///// <summary>
/////     Empty pageName is for root Area.
///// </summary>
///// <param name="tagOrPageName"></param>
///// <param name="valueSubscription"></param>
//public void AlarmUnackedAddItem(string tagOrPageName, IValueSubscription valueSubscription)
//{
//    if (String.IsNullOrEmpty(tagOrPageName))
//    {
//        EventSourceArea eventSourceArea = GetEventSourceArea(@"");
//        eventSourceArea.AlarmUnackedSubscribers += valueSubscription.Update;
//        eventSourceArea.NotifyAlarmUnackedSubscribers();
//        return;
//    }

//    //CaseInsensitiveDictionary<PageDrawing> pageDrawings =
//    //    Project.Instance.AllPagesCache;
//    //if (pageDrawings.ContainsKey(tagOrPageName)) // IsArea
//    //{
//    //    EventSourceArea eventSourceArea = GetEventSourceArea(tagOrPageName);
//    //    eventSourceArea.IsPage = true;
//    //    eventSourceArea.AlarmUnackedSubscribers += valueSubscription.Update;
//    //    eventSourceArea.NotifyAlarmUnackedSubscribers();
//    //    return;
//    //}

//    EventSourceObject eventSourceObject = GetEventSourceObject(tagOrPageName);
//    eventSourceObject.AlarmUnackedSubscribers += valueSubscription.Update;
//    eventSourceObject.NotifyAlarmUnackedSubscriber(valueSubscription);
//}

///// <summary>
/////     null or Empty pageName is for root Area.
///// </summary>
///// <param name="tagOrPageName"></param>
///// <param name="valueSubscription"></param>
//public void AlarmUnackedRemoveItem(string tagOrPageName, IValueSubscription valueSubscription)
//{
//    if (String.IsNullOrEmpty(tagOrPageName))
//    {
//        EventSourceArea eventSourceArea = GetEventSourceArea(@"");
//        eventSourceArea.AlarmUnackedSubscribers -= valueSubscription.Update;
//        return;
//    }

//    //CaseInsensitiveDictionary<PageDrawing> pageDrawings =
//    //    Project.Instance.AllPagesCache;
//    //if (pageDrawings.ContainsKey(tagOrPageName))
//    //{
//    //    EventSourceArea eventSourceArea = GetEventSourceArea(tagOrPageName);
//    //    eventSourceArea.IsPage = true;
//    //    eventSourceArea.AlarmUnackedSubscribers -= valueSubscription.Update;
//    //    return;
//    //}

//    EventSourceObject eventSourceObject = GetEventSourceObject(tagOrPageName);
//    eventSourceObject.AlarmUnackedSubscribers -= valueSubscription.Update;
//}

///// <summary>
/////     Empty pageName is for root Area.
///// </summary>
///// <param name="tagOrPageName"></param>
///// <param name="valueSubscription"></param>
//public void AlarmCategoryAddItem(string tagOrPageName, IValueSubscription valueSubscription)
//{
//    if (String.IsNullOrEmpty(tagOrPageName))
//    {
//        EventSourceArea eventSourceArea = GetEventSourceArea(@"");
//        eventSourceArea.AlarmCategorySubscribers += valueSubscription.Update;
//        eventSourceArea.NotifyAlarmCategorySubscribers();
//        return;
//    }

//    //CaseInsensitiveDictionary<PageDrawing> pageDrawings =
//    //    Project.Instance.AllPagesCache;
//    //if (pageDrawings.ContainsKey(tagOrPageName))
//    //{
//    //    EventSourceArea eventSourceArea = GetEventSourceArea(tagOrPageName);
//    //    eventSourceArea.IsPage = true;
//    //    eventSourceArea.AlarmCategorySubscribers += valueSubscription.Update;
//    //    eventSourceArea.NotifyAlarmCategorySubscribers();
//    //    return;
//    //}

//    EventSourceObject eventSourceObject = GetEventSourceObject(tagOrPageName);
//    eventSourceObject.AlarmCategorySubscribers += valueSubscription.Update;
//    eventSourceObject.NotifyAlarmCategorySubscriber(valueSubscription);
//}

///// <summary>
/////     null or Empty pageName is for root Area.
///// </summary>
///// <param name="tagOrPageName"></param>
///// <param name="valueSubscription"></param>
//public void AlarmCategoryRemoveItem(string tagOrPageName, IValueSubscription valueSubscription)
//{
//    if (String.IsNullOrEmpty(tagOrPageName))
//    {
//        EventSourceArea eventSourceArea = GetEventSourceArea(@"");
//        eventSourceArea.AlarmCategorySubscribers -= valueSubscription.Update;
//        return;
//    }

//    //CaseInsensitiveDictionary<PageDrawing> pageDrawings =
//    //    Project.Instance.AllPagesCache;
//    //if (pageDrawings.ContainsKey(tagOrPageName))
//    //{
//    //    EventSourceArea eventSourceArea = GetEventSourceArea(tagOrPageName);
//    //    eventSourceArea.IsPage = true;
//    //    eventSourceArea.AlarmCategorySubscribers -= valueSubscription.Update;
//    //    return;
//    //}

//    EventSourceObject eventSourceObject = GetEventSourceObject(tagOrPageName);
//    eventSourceObject.AlarmCategorySubscribers -= valueSubscription.Update;
//}

///// <summary>
/////     null or Empty pageName is for root Area.
///// </summary>
///// <param name="tagOrPageName"></param>
///// <param name="valueSubscription"></param>
//public void AlarmBrushAddItem(string tagOrPageName, IValueSubscription valueSubscription)
//{
//    if (String.IsNullOrEmpty(tagOrPageName))
//    {
//        EventSourceArea eventSourceArea = GetEventSourceArea(@"");
//        eventSourceArea.AlarmBrushSubscribers += valueSubscription.Update;
//        eventSourceArea.NotifyAlarmBrushSubscribers();
//        return;
//    }

//    //CaseInsensitiveDictionary<PageDrawing> pageDrawings =
//    //    Project.Instance.AllPagesCache;
//    //if (pageDrawings.ContainsKey(tagOrPageName))
//    //{
//    //    EventSourceArea eventSourceArea = GetEventSourceArea(tagOrPageName);
//    //    eventSourceArea.IsPage = true;
//    //    eventSourceArea.AlarmBrushSubscribers += valueSubscription.Update;
//    //    eventSourceArea.NotifyAlarmBrushSubscribers();
//    //    return;
//    //}

//    EventSourceObject eventSourceObject = GetEventSourceObject(tagOrPageName);
//    eventSourceObject.AlarmBrushSubscribers += valueSubscription.Update;
//    eventSourceObject.NotifyAlarmBrushSubscriber(valueSubscription);
//}

///// <summary>
/////     null or Empty pageName is for root Area.
///// </summary>
///// <param name="tagOrPageName"></param>
///// <param name="valueSubscription"></param>
//public void AlarmBrushRemoveItem(string tagOrPageName, IValueSubscription valueSubscription)
//{
//    if (String.IsNullOrEmpty(tagOrPageName))
//    {
//        EventSourceArea eventSourceArea = GetEventSourceArea(@"");
//        eventSourceArea.AlarmBrushSubscribers -= valueSubscription.Update;
//        return;
//    }

//    //CaseInsensitiveDictionary<PageDrawing> pageDrawings =
//    //    Project.Instance.AllPagesCache;
//    //if (pageDrawings.ContainsKey(tagOrPageName))
//    //{
//    //    EventSourceArea eventSourceArea = GetEventSourceArea(tagOrPageName);
//    //    eventSourceArea.IsPage = true;
//    //    eventSourceArea.AlarmBrushSubscribers -= valueSubscription.Update;
//    //    return;
//    //}

//    EventSourceObject eventSourceObject = GetEventSourceObject(tagOrPageName);
//    eventSourceObject.AlarmBrushSubscribers -= valueSubscription.Update;
//}

///// <summary>        
///// </summary>
///// <param name="tag"></param>
///// <param name="valueSubscription"></param>
//public void AlarmConditionTypeAddItem(string tag, IValueSubscription valueSubscription)
//{
//    if (String.IsNullOrEmpty(tag))
//    {
//        return;
//    }

//    EventSourceObject eventSourceObject = GetEventSourceObject(tag);
//    eventSourceObject.AlarmConditionTypeSubscribers += valueSubscription.Update;
//    eventSourceObject.NotifyAlarmConditionTypeSubscriber(valueSubscription);
//}

///// <summary>        
///// </summary>
///// <param name="tag"></param>
///// <param name="valueSubscription"></param>
//public void AlarmConditionTypeRemoveItem(string tag, IValueSubscription valueSubscription)
//{
//    if (String.IsNullOrEmpty(tag))
//    {
//        return;
//    }

//    EventSourceObject eventSourceObject = GetEventSourceObject(tag);
//    eventSourceObject.AlarmConditionTypeSubscribers -= valueSubscription.Update;
//}