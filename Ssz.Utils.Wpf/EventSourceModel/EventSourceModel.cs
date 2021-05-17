//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace Ssz.Utils.Wpf.EventSourceModel
//{
//    public class EventSourceModel
//    {
//        #region private functions

//        private void OnConnectedOrDisconnected()
//        {
//            foreach (EventSourceObject eventSourceObject in _eventSourceObjectsDictionary.Values)
//            {
//                eventSourceObject.AlarmConditions.Clear();
//                eventSourceObject.NotifyAlarmUnackedSubscribers();
//                eventSourceObject.NotifyAlarmCategorySubscribers();
//                eventSourceObject.NotifyAlarmBrushSubscribers();
//                eventSourceObject.NotifyAlarmConditionTypeSubscribers();
//            }

//            foreach (EventSourceArea eventSourceArea in _eventSourceAreasDictionary.Values)
//            {
//                eventSourceArea.UnackedAlarmsCount = 0;
//                eventSourceArea.ActiveAlarmsCategories.Clear();
//                eventSourceArea.NotifyAlarmUnackedSubscribers();
//                eventSourceArea.NotifyAlarmCategorySubscribers();
//                eventSourceArea.NotifyAlarmBrushSubscribers();
//            }
//        }

//        #endregion

//        #region public functions

//        public event Action? BeforeStateLoad;

//        public void Initialize()
//        {
//            DsDataAccessProvider.Instance.Connected += OnConnectedOrDisconnected;
//            DsDataAccessProvider.Instance.Disconnected += OnConnectedOrDisconnected;
//            OnConnectedOrDisconnected();
//        }

//        public void Close()
//        {
//            DsDataAccessProvider.Instance.Connected -= OnConnectedOrDisconnected;
//            DsDataAccessProvider.Instance.Disconnected -= OnConnectedOrDisconnected;
//        }


//        public bool ProcessEventSourceObject(EventSourceObject eventSourceObject, AlarmConditionType alarmConditionType,
//            uint categoryId, bool active, bool unacked, DateTime occurrenceTime)
//        {
//            Dictionary<AlarmConditionType, ConditionState> alarmConditions = eventSourceObject.AlarmConditions;

//            ConditionState? conditionState;
//            if (alarmConditions.TryGetValue(alarmConditionType, out conditionState))
//            {
//                if (conditionState.Active == active && conditionState.Unacked == unacked)
//                    //We found the existing condition, but have determined that nothing has changed
//                    //in the condition.
//                    return false;

//                //Something has changed in the condition.  Update the condition with the new alarm state
//                conditionState.Active = active;
//                conditionState.Unacked = unacked;
//                if (active) conditionState.ActiveOccurrenceTime = occurrenceTime;

//                if (!active && !unacked) //Moved into the Inactive-Acknowledged state
//                    //Remove this condition from the list since our alarm is no longer an alarm
//                    alarmConditions.Remove(alarmConditionType);
//            }
//            else
//            {
//                if (!active)
//                    //A new alarm that is already inactive - weird?
//                    return false; //An odd state - we didn't find anything so return false.

//                //This condition doesn't already exist.  This means we are a new alarm
//                //Normally this will be a newly active alarm.  Occasionally it will be an
//                //inactive but unacknowledged alarm.  This second situation is a bit odd but
//                //can occur if we have newly connected to an AE server that has 
//                //unacknowledged-inactive alarms.  In both cases it is a valid alarm that we 
//                //want to continue to track.  Add it to our list.
//                var newConditionState = new ConditionState
//                    {Active = active, Unacked = unacked, CategoryId = categoryId};
//                if (active) newConditionState.ActiveOccurrenceTime = occurrenceTime;
//                alarmConditions.Add(alarmConditionType, newConditionState);
//            }

//            if (!active)
//                eventSourceObject.NormalCondition.Active = !alarmConditions.Any(c => c.Value.Active);
//            else
//                eventSourceObject.NormalCondition.Active = false;

//            eventSourceObject.NormalCondition.Unacked = unacked;

//            eventSourceObject.NotifyAlarmUnackedSubscribers();
//            eventSourceObject.NotifyAlarmCategorySubscribers();
//            eventSourceObject.NotifyAlarmBrushSubscribers();
//            eventSourceObject.NotifyAlarmConditionTypeSubscribers();

//            return true;
//        }

//        public void OnBeforeStateLoad()
//        {
//            var beforeStateLoad = BeforeStateLoad;
//            if (beforeStateLoad != null) beforeStateLoad();

//            foreach (EventSourceObject eventSourceObject in _eventSourceObjectsDictionary.Values)
//            {
//                eventSourceObject.AlarmConditions.Clear();
//                eventSourceObject.NotifyAlarmUnackedSubscribers();
//                eventSourceObject.NotifyAlarmCategorySubscribers();
//                eventSourceObject.NotifyAlarmBrushSubscribers();
//                eventSourceObject.NotifyAlarmConditionTypeSubscribers();
//            }

//            foreach (EventSourceArea eventSourceArea in _eventSourceAreasDictionary.Values)
//            {
//                eventSourceArea.UnackedAlarmsCount = 0;
//                eventSourceArea.ActiveAlarmsCategories.Clear();
//                eventSourceArea.NotifyAlarmUnackedSubscribers();
//                eventSourceArea.NotifyAlarmCategorySubscribers();
//                eventSourceArea.NotifyAlarmBrushSubscribers();
//            }
//        }

//        public void OnAlarmsListChanged()
//        {
//            foreach (EventSourceArea eventSourceArea in _eventSourceAreasDictionary.Values)
//            {
//                eventSourceArea.UnackedAlarmsCount = 0;
//                eventSourceArea.ActiveAlarmsCategories.Clear();
//            }

//            foreach (EventSourceObject eventSourceObject in _eventSourceObjectsDictionary.Values)
//            {
//                if (eventSourceObject.AnyUnacked())
//                    foreach (EventSourceArea eventSourceArea in eventSourceObject.EventSourceAreas.Values)
//                        eventSourceArea.UnackedAlarmsCount += 1;

//                var maxCategory = eventSourceObject.GetActiveAlarmsMaxCategory();
//                if (maxCategory > 0)
//                    foreach (EventSourceArea eventSourceArea in eventSourceObject.EventSourceAreas.Values)
//                    {
//                        int activeAlarmsCount;
//                        if (!eventSourceArea.ActiveAlarmsCategories.TryGetValue(maxCategory, out activeAlarmsCount))
//                            eventSourceArea.ActiveAlarmsCategories[maxCategory] = 1;
//                        else
//                            eventSourceArea.ActiveAlarmsCategories[maxCategory] = activeAlarmsCount + 1;
//                    }
//            }

//            foreach (EventSourceArea eventSourceArea in _eventSourceAreasDictionary.Values)
//            {
//                eventSourceArea.NotifyAlarmUnackedSubscribers();
//                eventSourceArea.NotifyAlarmCategorySubscribers();
//            }
//        }


//        public EventSourceArea GetEventSourceArea(string area)
//        {
//            if (string.IsNullOrEmpty(area)) area = @"";

//            EventSourceArea? eventSourceArea;
//            if (!_eventSourceAreasDictionary.TryGetValue(area, out eventSourceArea))
//            {
//                eventSourceArea = new EventSourceArea();
//                _eventSourceAreasDictionary[area] = eventSourceArea;
//            }

//            return eventSourceArea;
//        }


//        public EventSourceObject GetEventSourceObject(string tag)
//        {
//            EventSourceObject? existingEventSourceObject;
//            if (_eventSourceObjectsDictionary.TryGetValue(tag, out existingEventSourceObject))
//                //We already have this tag in our list.  Just return the existing object.
//                return existingEventSourceObject;

//            //The tag doesn't already exist.  Create a new EventSourceObject and add it to our dictionary
//            var newEventSourceObject = new EventSourceObject(tag);
//            _eventSourceObjectsDictionary[tag] = newEventSourceObject;

//            EventSourceArea overviewEventSourceArea = GetEventSourceArea(@"");
//            newEventSourceObject.EventSourceAreas[@""] = overviewEventSourceArea;

//            var dsConstants = DsSolution.Instance.AllGraphicsCacheGetAllConstantsValues().TryGetValue(tag);
//            if (dsConstants != null)
//            {
//                IEnumerable<DsGraphicDrawing> dsGraphicDrawings = dsConstants
//                    .Select(i =>
//                        (DsGraphicDrawing) (i.CompoundDsControl.GetParentDrawing() ??
//                                            throw new InvalidOperationException()))
//                    .Distinct(ReferenceEqualityComparer<DsGraphicDrawing>.Default);

//                foreach (DsGraphicDrawing dsGraphicDrawing in dsGraphicDrawings)
//                {
//                    EventSourceArea eventSourceArea = GetEventSourceArea(dsGraphicDrawing.Name);
//                    eventSourceArea.IsGraphic = true;
//                    newEventSourceObject.EventSourceAreas[dsGraphicDrawing.Name] = eventSourceArea;
//                }
//            }

//            return newEventSourceObject;
//        }


//        public void AlarmUnackedAddItem(string? tagOrGraphicName, IValueSubscription valueSubscription)
//        {
//            if (string.IsNullOrEmpty(tagOrGraphicName))
//            {
//                EventSourceArea eventSourceArea = GetEventSourceArea(@"");
//                eventSourceArea.AlarmUnackedSubscribers += valueSubscription.Update;
//                eventSourceArea.NotifyAlarmUnackedSubscribers();
//                return;
//            }

//            CaseInsensitiveDictionary<DsGraphicDrawing> dsGraphicDrawings =
//                DsSolution.Instance.AllGraphicsCache;
//            if (dsGraphicDrawings.ContainsKey(tagOrGraphicName))
//            {
//                EventSourceArea eventSourceArea = GetEventSourceArea(tagOrGraphicName);
//                eventSourceArea.IsGraphic = true;
//                eventSourceArea.AlarmUnackedSubscribers += valueSubscription.Update;
//                eventSourceArea.NotifyAlarmUnackedSubscribers();
//                return;
//            }

//            EventSourceObject eventSourceObject = GetEventSourceObject(tagOrGraphicName);
//            eventSourceObject.AlarmUnackedSubscribers += valueSubscription.Update;
//            eventSourceObject.NotifyAlarmUnackedSubscriber(valueSubscription);
//        }


//        public void AlarmUnackedRemoveItem(string tagOrGraphicName, IValueSubscription valueSubscription)
//        {
//            if (string.IsNullOrEmpty(tagOrGraphicName))
//            {
//                EventSourceArea eventSourceArea = GetEventSourceArea(@"");
//                eventSourceArea.AlarmUnackedSubscribers -= valueSubscription.Update;
//                return;
//            }

//            CaseInsensitiveDictionary<DsGraphicDrawing> dsGraphicDrawings =
//                DsSolution.Instance.AllGraphicsCache;
//            if (dsGraphicDrawings.ContainsKey(tagOrGraphicName))
//            {
//                EventSourceArea eventSourceArea = GetEventSourceArea(tagOrGraphicName);
//                eventSourceArea.IsGraphic = true;
//                eventSourceArea.AlarmUnackedSubscribers -= valueSubscription.Update;
//                return;
//            }

//            EventSourceObject eventSourceObject = GetEventSourceObject(tagOrGraphicName);
//            eventSourceObject.AlarmUnackedSubscribers -= valueSubscription.Update;
//        }


//        public void AlarmCategoryAddItem(string tagOrGraphicName, IValueSubscription valueSubscription)
//        {
//            if (string.IsNullOrEmpty(tagOrGraphicName))
//            {
//                EventSourceArea eventSourceArea = GetEventSourceArea(@"");
//                eventSourceArea.AlarmCategorySubscribers += valueSubscription.Update;
//                eventSourceArea.NotifyAlarmCategorySubscribers();
//                return;
//            }

//            CaseInsensitiveDictionary<DsGraphicDrawing> dsGraphicDrawings =
//                DsSolution.Instance.AllGraphicsCache;
//            if (dsGraphicDrawings.ContainsKey(tagOrGraphicName))
//            {
//                EventSourceArea eventSourceArea = GetEventSourceArea(tagOrGraphicName);
//                eventSourceArea.IsGraphic = true;
//                eventSourceArea.AlarmCategorySubscribers += valueSubscription.Update;
//                eventSourceArea.NotifyAlarmCategorySubscribers();
//                return;
//            }

//            EventSourceObject eventSourceObject = GetEventSourceObject(tagOrGraphicName);
//            eventSourceObject.AlarmCategorySubscribers += valueSubscription.Update;
//            eventSourceObject.NotifyAlarmCategorySubscriber(valueSubscription);
//        }


//        public void AlarmCategoryRemoveItem(string tagOrGraphicName, IValueSubscription valueSubscription)
//        {
//            if (string.IsNullOrEmpty(tagOrGraphicName))
//            {
//                EventSourceArea eventSourceArea = GetEventSourceArea(@"");
//                eventSourceArea.AlarmCategorySubscribers -= valueSubscription.Update;
//                return;
//            }

//            CaseInsensitiveDictionary<DsGraphicDrawing> dsGraphicDrawings =
//                DsSolution.Instance.AllGraphicsCache;
//            if (dsGraphicDrawings.ContainsKey(tagOrGraphicName))
//            {
//                EventSourceArea eventSourceArea = GetEventSourceArea(tagOrGraphicName);
//                eventSourceArea.IsGraphic = true;
//                eventSourceArea.AlarmCategorySubscribers -= valueSubscription.Update;
//                return;
//            }

//            EventSourceObject eventSourceObject = GetEventSourceObject(tagOrGraphicName);
//            eventSourceObject.AlarmCategorySubscribers -= valueSubscription.Update;
//        }


//        public void AlarmBrushAddItem(string tagOrGraphicName, IValueSubscription valueSubscription)
//        {
//            if (string.IsNullOrEmpty(tagOrGraphicName))
//            {
//                EventSourceArea eventSourceArea = GetEventSourceArea(@"");
//                eventSourceArea.AlarmBrushSubscribers += valueSubscription.Update;
//                eventSourceArea.NotifyAlarmBrushSubscribers();
//                return;
//            }

//            CaseInsensitiveDictionary<DsGraphicDrawing> dsGraphicDrawings =
//                DsSolution.Instance.AllGraphicsCache;
//            if (dsGraphicDrawings.ContainsKey(tagOrGraphicName))
//            {
//                EventSourceArea eventSourceArea = GetEventSourceArea(tagOrGraphicName);
//                eventSourceArea.IsGraphic = true;
//                eventSourceArea.AlarmBrushSubscribers += valueSubscription.Update;
//                eventSourceArea.NotifyAlarmBrushSubscribers();
//                return;
//            }

//            EventSourceObject eventSourceObject = GetEventSourceObject(tagOrGraphicName);
//            eventSourceObject.AlarmBrushSubscribers += valueSubscription.Update;
//            eventSourceObject.NotifyAlarmBrushSubscriber(valueSubscription);
//        }


//        public void AlarmBrushRemoveItem(string tagOrGraphicName, IValueSubscription valueSubscription)
//        {
//            if (string.IsNullOrEmpty(tagOrGraphicName))
//            {
//                EventSourceArea eventSourceArea = GetEventSourceArea(@"");
//                eventSourceArea.AlarmBrushSubscribers -= valueSubscription.Update;
//                return;
//            }

//            CaseInsensitiveDictionary<DsGraphicDrawing> dsGraphicDrawings =
//                DsSolution.Instance.AllGraphicsCache;
//            if (dsGraphicDrawings.ContainsKey(tagOrGraphicName))
//            {
//                EventSourceArea eventSourceArea = GetEventSourceArea(tagOrGraphicName);
//                eventSourceArea.IsGraphic = true;
//                eventSourceArea.AlarmBrushSubscribers -= valueSubscription.Update;
//                return;
//            }

//            EventSourceObject eventSourceObject = GetEventSourceObject(tagOrGraphicName);
//            eventSourceObject.AlarmBrushSubscribers -= valueSubscription.Update;
//        }


//        public void AlarmConditionTypeAddItem(string tag, IValueSubscription valueSubscription)
//        {
//            if (string.IsNullOrEmpty(tag)) return;

//            EventSourceObject eventSourceObject = GetEventSourceObject(tag);
//            eventSourceObject.AlarmConditionTypeSubscribers += valueSubscription.Update;
//            eventSourceObject.NotifyAlarmConditionTypeSubscriber(valueSubscription);
//        }


//        public void AlarmConditionTypeRemoveItem(string tag, IValueSubscription valueSubscription)
//        {
//            if (string.IsNullOrEmpty(tag)) return;

//            EventSourceObject eventSourceObject = GetEventSourceObject(tag);
//            eventSourceObject.AlarmConditionTypeSubscribers -= valueSubscription.Update;
//        }

//        public void GetExistingAlarmInfoViewModels(Action<IEnumerable<AlarmInfoViewModelBase>> alarmNotification)
//        {
//            var alarmInfoViewModels = new List<AlarmInfoViewModelBase>();
//            foreach (var kvp in _eventSourceObjectsDictionary)
//            {
//                string userTagsFileName = DsSolution.Instance.PlayInfo.UserTagsFileName;
//                if (!string.IsNullOrEmpty(userTagsFileName))
//                {
//                    var tag = DsSolution.Instance.CsvDbFileGetValue(userTagsFileName, kvp.Key, 0);
//                    if (tag == null) continue;
//                }

//                EventSourceObject eventSourceObject = kvp.Value;
//                var alarmInfoViewModelsForObject = new List<AlarmInfoViewModelBase>();
//                foreach (var condition in eventSourceObject.AlarmConditions.Values.OrderByDescending(
//                    cs => cs.CategoryId))
//                    if (!condition.Active && condition.Unacked &&
//                        condition.LastAlarmInfoViewModel != null)
//                        alarmInfoViewModelsForObject.Add(condition.LastAlarmInfoViewModel);
//                foreach (var condition in eventSourceObject.AlarmConditions.Values.OrderBy(cs => cs.CategoryId))
//                    if (condition.Active &&
//                        condition.LastAlarmInfoViewModel != null)
//                        alarmInfoViewModelsForObject.Add(condition.LastAlarmInfoViewModel);
//                if (eventSourceObject.NormalCondition.Active && eventSourceObject.NormalCondition.Unacked &&
//                    eventSourceObject.NormalCondition.LastAlarmInfoViewModel != null)
//                    alarmInfoViewModelsForObject.Add(eventSourceObject.NormalCondition.LastAlarmInfoViewModel);
//                if (alarmInfoViewModelsForObject.Count > 0)
//                    alarmInfoViewModels.AddRange(alarmInfoViewModelsForObject);
//            }

//            if (alarmInfoViewModels.Count > 0) alarmNotification(alarmInfoViewModels);
//        }

//        #endregion

//        #region private fields

//        private readonly CaseInsensitiveDictionary<EventSourceObject> _eventSourceObjectsDictionary =
//            new();

//        private readonly CaseInsensitiveDictionary<EventSourceArea> _eventSourceAreasDictionary =
//            new();

//        #endregion
//    }
//}