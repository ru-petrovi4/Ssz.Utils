using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Utils.DataAccess;

namespace Ssz.Utils.EventSourceModel
{    
    public class EventSourceModel : IDisposable
    {
        #region construction and destruction

        public EventSourceModel(IDataAccessProvider dataAccessProvider)
        {
            _dataAccessProvider = dataAccessProvider;

            _dataAccessProvider.PropertyChanged += DataAccessProviderOnPropertyChanged;
        }        

        /// <summary>
        ///     This is the implementation of the IDisposable.Dispose method.  The client
        ///     application should invoke this method when this instance is no longer needed.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     This method is invoked when the IDisposable.Dispose or Finalize actions are
        ///     requested.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                _dataAccessProvider.PropertyChanged -= DataAccessProviderOnPropertyChanged;
            }

            Disposed = true;
        }

        /// <summary>
        ///     Invoked by the .NET Framework while doing heap managment (Finalize).
        /// </summary>
        ~EventSourceModel()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public bool Disposed { get; private set; }

        public CaseInsensitiveDictionary<EventSourceObject> EventSourceObjects { get; } =
            new();

        public CaseInsensitiveDictionary<EventSourceArea> EventSourceAreas { get; } =
            new();

        public void Clear()
        {
            foreach (EventSourceObject eventSourceObject in EventSourceObjects.Values)
            {
                eventSourceObject.AlarmConditions.Clear();
                eventSourceObject.NotifyAlarmUnackedSubscribers();
                eventSourceObject.NotifyAlarmCategorySubscribers();                
                eventSourceObject.NotifyAlarmConditionTypeSubscribers();
            }
            foreach (EventSourceArea eventSourceArea in EventSourceAreas.Values)
            {
                eventSourceArea.UnackedAlarmsCount = 0;
                eventSourceArea.ActiveAlarmsCategories.Clear();
                eventSourceArea.NotifyAlarmUnackedSubscribers();
                eventSourceArea.NotifyAlarmCategorySubscribers();                
            }
        }

        /// <summary>
        ///     condition != Normal
        ///     Returns true if active or unacked state of any condition changed.
        /// </summary>
        /// <returns>
        ///     true if active or unacked state of any condition changed
        ///     false if the alarm state remains the same
        /// </returns>
        public bool ProcessEventSourceObject(EventSourceObject eventSourceObject, AlarmCondition alarmCondition,
            uint categoryId, bool active, bool unacked, DateTime occurrenceTime, 
            out bool alarmConditionChanged,
            out bool unackedChanged)
        {
            alarmConditionChanged = false;
            unackedChanged = false;

            Dictionary<AlarmCondition, ConditionState> alarmConditions = eventSourceObject.AlarmConditions;
            
            ConditionState? conditionState;
            if (alarmConditions.TryGetValue(alarmCondition, out conditionState))
            {
                if (conditionState.Active == active && conditionState.Unacked == unacked)
                {
                    //We found the existing condition, but have determined that nothing has changed
                    //in the condition.
                    return false;
                }

                //Something has changed in the condition.  Update the condition with the new alarm state
                if (conditionState.Active != active)
                {
                    conditionState.Active = active;
                    alarmConditionChanged = true;
                }
                
                if (conditionState.Unacked != unacked)
                {
                    conditionState.Unacked = unacked;
                    unackedChanged = true;
                }
                
                if (active) conditionState.ActiveOccurrenceTime = occurrenceTime;

                if (!active && !unacked) //Moved into the Inactive-Acknowledged state
                {
                    //Remove this condition from the list since our alarm is no longer an alarm
                    alarmConditions.Remove(alarmCondition);
                }
            }
            else
            {
                if (!active) 
                {
                    //A new alarm that is already inactive - weird?
                    return false;    //An odd state - we didn't find anything so return false.
                }

                alarmConditionChanged = true;
                if (unacked)
                    unackedChanged = true;

                //This condition doesn't already exist.  This means we are a new alarm
                //Normally this will be a newly active alarm.  Occasionally it will be an
                //inactive but unacknowledged alarm.  This second situation is a bit odd but
                //can occur if we have newly connected to an AE server that has 
                //unacknowledged-inactive alarms.  In both cases it is a valid alarm that we 
                //want to continue to track.  Add it to our list.
                var newConditionState = new ConditionState { Active = active, Unacked = unacked, CategoryId = categoryId };
                if (active) newConditionState.ActiveOccurrenceTime = occurrenceTime;
                alarmConditions.Add(alarmCondition, newConditionState);
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
            eventSourceObject.NotifyAlarmConditionTypeSubscribers();

            return true;
        }
        
        public void OnAlarmsListChanged()
        {
            foreach (EventSourceArea eventSourceArea in EventSourceAreas.Values)
            {
                eventSourceArea.UnackedAlarmsCount = 0;
                eventSourceArea.ActiveAlarmsCategories.Clear();
            }

            foreach (EventSourceObject eventSourceObject in EventSourceObjects.Values)
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

            foreach (EventSourceArea eventSourceArea in EventSourceAreas.Values)
            {
                eventSourceArea.NotifyAlarmUnackedSubscribers();
                eventSourceArea.NotifyAlarmCategorySubscribers();
            }
        }

        /// <summary>        
        ///     Empty area is for root Area.
        /// </summary>
        /// <param name="area"></param>
        /// <returns></returns>        
        public EventSourceArea GetOrCreateEventSourceArea(string area)
        {
            EventSourceArea? eventSourceArea;
            if (!EventSourceAreas.TryGetValue(area, out eventSourceArea))
            {
                eventSourceArea = new EventSourceArea(area, _dataAccessProvider);
                EventSourceAreas[area] = eventSourceArea;
            }
            return eventSourceArea;
        }

        /// <summary>        
        ///     area can contain '/' chars. 
        ///     Adds all necessary areas to a new object: 
        ///     area=String.Empty - root area, 
        ///     all parent areas,
        ///     leaf area.        
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="area"></param>
        /// <returns></returns>
        public EventSourceObject GetOrCreateEventSourceObject(string tag, string area)
        {
            EventSourceObject? existingEventSourceObject;
            if (EventSourceObjects.TryGetValue(tag, out existingEventSourceObject))
            {
                //We already have this tag in our list.  Just return the existing object.
                return existingEventSourceObject;
            }

            //The tag doesn't already exist.  Create a new EventSourceObject and add it to our dictionary
            var newEventSourceObject = new EventSourceObject(tag, _dataAccessProvider);
            EventSourceObjects[tag] = newEventSourceObject;

            EventSourceArea overviewEventSourceArea = GetOrCreateEventSourceArea(@"");
            newEventSourceObject.EventSourceAreas[@""] = overviewEventSourceArea;
            if (area != @"")
            {
                string parentArea = @"";
                foreach (string areaPart in area.Split('\\', '/'))
                {
                    if (parentArea == @"") parentArea = areaPart;
                    else parentArea += "/" + areaPart;
                    newEventSourceObject.EventSourceAreas[parentArea] = GetOrCreateEventSourceArea(parentArea);
                }
            }

            return newEventSourceObject;
        }

        public void GetExistingAlarmInfoViewModels(Action<IEnumerable<AlarmInfoViewModelBase>> alarmNotification)
        {
            var alarmInfoViewModels = new List<AlarmInfoViewModelBase>();
            foreach (var kvp in EventSourceObjects)
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

        #endregion

        #region private fields

        private void DataAccessProviderOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            switch (args.PropertyName)
            {
                case @"IsConnected":
                    Clear();
                    break;
            }            
        }

        #endregion        

        #region private fields

        private readonly IDataAccessProvider _dataAccessProvider;        

        #endregion
    }
}