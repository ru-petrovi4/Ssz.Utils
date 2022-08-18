using System;
using System.Collections.Generic;
using System.Linq;

namespace Ssz.Utils.DataAccess
{
    public enum EventSourceModelSubscriptionScope
    {
        Active,
        Unacked,
        ActiveOrUnacked,
        ActiveAndUnacked,
    }

    public class EventSourceModelSubscriptionInfo
    {
        public EventSourceModelSubscriptionInfo(int subscriptionType, EventSourceModelSubscriptionScope subscriptionScope, uint? alarmCategoryIdFilter = null)
        {
            EventSourceModelSubscriptionType = subscriptionType;
            EventSourceModelSubscriptionScope = subscriptionScope;
            AlarmCategoryIdFilter = alarmCategoryIdFilter;
        }

        public readonly int EventSourceModelSubscriptionType;        

        public readonly EventSourceModelSubscriptionScope EventSourceModelSubscriptionScope;
        
        public readonly uint? AlarmCategoryIdFilter;

        //public override int GetHashCode()
        //{
        //    return ((int)AlarmCategoryIdFilter << 24) + ((int)eventSourceModelSubscriptionScope << 16) + EventSourceModelSubscriptionType;
        //}

        //public override bool Equals(object obj)
        //{
        //    if (obj is EventSourceModelSubscriptionInfo that)
        //        return that.GetHashCode() == GetHashCode();
        //    else
        //        return false;
        //}
    }

    public class EventSourceModel : IEventSourceModel
    {
        #region public functions        

        public const int AlarmsCount_SubscriptionType = 0x1;
        public const int AlarmsAny_SubscriptionType = 0x2;
        public const int AlarmMaxCategoryId_SubscriptionType = 0x3;
        public const int AlarmConditionType_SubscriptionType = 0x4;        

        public bool IsInitialized { get; private set; }

        public CaseInsensitiveDictionary<EventSourceObject> EventSourceObjects { get; } =
            new();

        public CaseInsensitiveDictionary<EventSourceArea> EventSourceAreas { get; } =
            new();

        /// <summary>
        ///     Can be used after Initialize(...)
        /// </summary>
        public IDataAccessProvider DataAccessProvider { get; private set; } = null!;

        /// <summary>
        ///     Must be called after Close() or after creation.
        /// </summary>
        /// <param name="dataAccessProvider"></param>
        public virtual void Initialize(IDataAccessProvider dataAccessProvider)
        {
            DataAccessProvider = dataAccessProvider;

            DataAccessProvider.PropertyChanged += DataAccessProviderOnPropertyChanged;

            IsInitialized = true;
        }
        
        /// <summary>
        ///     Must be called after Initialize(...)
        /// </summary>
        public virtual void Close()
        {
            DataAccessProvider.PropertyChanged -= DataAccessProviderOnPropertyChanged;

            IsInitialized = false;
        }

        /// <summary>
        ///    Does not clear lists of EventSourceObjects and EventSourceAreas
        /// </summary>
        public virtual void Clear()
        {
            foreach (EventSourceObject eventSourceObject in EventSourceObjects.Values)
            {
                eventSourceObject.AlarmConditions.Clear();
                eventSourceObject.NotifySubscriptions();
            }
            foreach (EventSourceArea eventSourceArea in EventSourceAreas.Values)
            {
                eventSourceArea.AlarmCategoryInfos.Clear();
                eventSourceArea.NotifySubscriptions();                           
            }
        }

        /// <summary>
        ///     alarmCondition != Normal
        ///     Returns true if active or unacked state of any condition changed.
        /// </summary>        
        /// <param name="eventSourceObject"></param>
        /// <param name="alarmConditionType"></param>
        /// <param name="categoryId"></param>
        /// <param name="active"></param>
        /// <param name="unacked"></param>
        /// <param name="occurrenceTimeUtc"></param>
        /// <param name="alarmConditionTypeChanged"></param>
        /// <param name="unackedChanged"></param>
        /// <returns>
        ///     true if active or unacked state of any condition changed
        ///     false if the alarm state remains the same
        /// </returns>
        public virtual bool ProcessEventSourceObject(EventSourceObject eventSourceObject, AlarmConditionType alarmConditionType,
            uint categoryId, bool active, bool unacked, DateTime occurrenceTimeUtc, out bool alarmConditionTypeChanged,
            out bool unackedChanged)
        {
            alarmConditionTypeChanged = false;
            unackedChanged = false;

            Dictionary<AlarmConditionType, AlarmConditionState> alarmConditions = eventSourceObject.AlarmConditions;
            
            AlarmConditionState? alarmConditionState;
            if (alarmConditions.TryGetValue(alarmConditionType, out alarmConditionState))
            {
                if (alarmConditionState.Active == active && alarmConditionState.Unacked == unacked)
                    return false;
                
                if (alarmConditionState.Active != active)
                {
                    alarmConditionState.Active = active;
                    alarmConditionTypeChanged = true;
                }
                
                if (alarmConditionState.Unacked != unacked)
                {
                    alarmConditionState.Unacked = unacked;
                    unackedChanged = true;
                }
                
                if (active) 
                    alarmConditionState.ActiveOccurrenceTimeUtc = occurrenceTimeUtc;

                if (!active && !unacked) // Moved into the Inactive-Acknowledged state
                    alarmConditions.Remove(alarmConditionType);
            }
            else
            {
                if (!active) 
                    return false;                

                alarmConditionTypeChanged = true;
                if (unacked)
                    unackedChanged = true;
                
                var newConditionState = new AlarmConditionState(alarmConditionType) { Active = active, Unacked = unacked, CategoryId = categoryId };
                if (active)
                    newConditionState.ActiveOccurrenceTimeUtc = occurrenceTimeUtc;
                alarmConditions.Add(alarmConditionType, newConditionState);
            }

            if (!active)
                eventSourceObject.NormalConditionState.Active = !alarmConditions.Any(c => c.Value.Active);
            else
                eventSourceObject.NormalConditionState.Active = false;

            eventSourceObject.NormalConditionState.Unacked = unacked;

            eventSourceObject.NotifySubscriptions();            
            return true;
        }
        
        public virtual void OnAlarmsListChanged()
        {
            foreach (EventSourceArea eventSourceArea in EventSourceAreas.Values)
            {
                foreach (var alarmCategoryInfo in eventSourceArea.AlarmCategoryInfos.Values)
                {
                    alarmCategoryInfo.ActiveCount = 0;
                    alarmCategoryInfo.UnackedCount = 0;                    
                    alarmCategoryInfo.ActiveOrUnackedCount = 0;
                    alarmCategoryInfo.ActiveAndUnackedCount = 0;
                }
            }

            foreach (EventSourceObject eventSourceObject in EventSourceObjects.Values)
            {
                var maxCategoryId = eventSourceObject.GetAlarmMaxCategoryId(EventSourceModelSubscriptionScope.Active);
                if (maxCategoryId > 0)
                    foreach (EventSourceArea eventSourceArea in eventSourceObject.EventSourceAreas.Values)
                    {
                        AlarmCategoryInfo? alarmCategoryInfo;
                        eventSourceArea.AlarmCategoryInfos.TryGetValue(maxCategoryId, out alarmCategoryInfo);
                        if (alarmCategoryInfo is null)
                        {
                            alarmCategoryInfo = new AlarmCategoryInfo();
                            eventSourceArea.AlarmCategoryInfos.Add(maxCategoryId, alarmCategoryInfo);
                        }
                        alarmCategoryInfo.ActiveCount += 1;
                    }

                maxCategoryId = eventSourceObject.GetAlarmMaxCategoryId(EventSourceModelSubscriptionScope.Unacked);
                if (maxCategoryId > 0)
                    foreach (EventSourceArea eventSourceArea in eventSourceObject.EventSourceAreas.Values)
                    {
                        AlarmCategoryInfo? alarmCategoryInfo;
                        eventSourceArea.AlarmCategoryInfos.TryGetValue(maxCategoryId, out alarmCategoryInfo);
                        if (alarmCategoryInfo is null)
                        {
                            alarmCategoryInfo = new AlarmCategoryInfo();
                            eventSourceArea.AlarmCategoryInfos.Add(maxCategoryId, alarmCategoryInfo);
                        }
                        alarmCategoryInfo.UnackedCount += 1;
                    }

                maxCategoryId = eventSourceObject.GetAlarmMaxCategoryId(EventSourceModelSubscriptionScope.ActiveOrUnacked);
                if (maxCategoryId > 0)
                    foreach (EventSourceArea eventSourceArea in eventSourceObject.EventSourceAreas.Values)
                    {
                        AlarmCategoryInfo? alarmCategoryInfo;
                        eventSourceArea.AlarmCategoryInfos.TryGetValue(maxCategoryId, out alarmCategoryInfo);
                        if (alarmCategoryInfo is null)
                        {
                            alarmCategoryInfo = new AlarmCategoryInfo();
                            eventSourceArea.AlarmCategoryInfos.Add(maxCategoryId, alarmCategoryInfo);
                        }
                        alarmCategoryInfo.ActiveOrUnackedCount += 1;
                    }

                maxCategoryId = eventSourceObject.GetAlarmMaxCategoryId(EventSourceModelSubscriptionScope.ActiveAndUnacked);
                if (maxCategoryId > 0)
                    foreach (EventSourceArea eventSourceArea in eventSourceObject.EventSourceAreas.Values)
                    {
                        AlarmCategoryInfo? alarmCategoryInfo;
                        eventSourceArea.AlarmCategoryInfos.TryGetValue(maxCategoryId, out alarmCategoryInfo);
                        if (alarmCategoryInfo is null)
                        {
                            alarmCategoryInfo = new AlarmCategoryInfo();
                            eventSourceArea.AlarmCategoryInfos.Add(maxCategoryId, alarmCategoryInfo);
                        }
                        alarmCategoryInfo.ActiveAndUnackedCount += 1;
                    }
            }            

            foreach (EventSourceArea eventSourceArea in EventSourceAreas.Values)
            {
                eventSourceArea.NotifySubscriptions();                
            }
        }

        /// <summary>        
        ///     Empty area is for root Area.
        /// </summary>
        /// <param name="area"></param>
        /// <returns></returns>        
        public virtual EventSourceArea GetOrCreateEventSourceArea(string area)
        {
            EventSourceArea? eventSourceArea;
            if (!EventSourceAreas.TryGetValue(area, out eventSourceArea))
            {
                eventSourceArea = new EventSourceArea(area, DataAccessProvider);
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
        /// <param name="tagName"></param>
        /// <param name="area"></param>
        /// <returns></returns>
        public virtual EventSourceObject GetOrCreateEventSourceObject(string tagName, string? area = null)
        {
            EventSourceObject? existingEventSourceObject;
            if (EventSourceObjects.TryGetValue(tagName, out existingEventSourceObject))
                return existingEventSourceObject;

            //The tag doesn't already exist.  Create a new EventSourceObject and add it to our dictionary
            var newEventSourceObject = new EventSourceObject(tagName, DataAccessProvider);
            EventSourceObjects[tagName] = newEventSourceObject;

            EventSourceArea overviewEventSourceArea = GetOrCreateEventSourceArea(@"");
            newEventSourceObject.EventSourceAreas[@""] = overviewEventSourceArea;

            if (area != null && area != @"")
            {
                string currentArea = @"";
                foreach (string areaPart in area.Split('/'))
                {
                    if (currentArea == @"") 
                        currentArea = areaPart;
                    else currentArea += "/" + areaPart;
                    newEventSourceObject.EventSourceAreas[currentArea] = GetOrCreateEventSourceArea(currentArea);
                }
            }

            return newEventSourceObject;
        }

        public virtual IEnumerable<AlarmInfoViewModelBase> GetExistingAlarmInfoViewModels()
        {
            var alarmInfoViewModels = new List<AlarmInfoViewModelBase>();

            foreach (var kvp in EventSourceObjects)
            {
                EventSourceObject eventSourceObject = kvp.Value;
                var alarmInfoViewModelsForObject = new List<AlarmInfoViewModelBase>();
                foreach (var condition in eventSourceObject.AlarmConditions.Values.OrderByDescending(cs => cs.CategoryId))
                {
                    if (!condition.Active && condition.Unacked &&
                        condition.LastAlarmInfoViewModel is not null)
                        alarmInfoViewModelsForObject.Add(condition.LastAlarmInfoViewModel);
                }
                foreach (var condition in eventSourceObject.AlarmConditions.Values.OrderBy(cs => cs.CategoryId))
                {
                    if (condition.Active &&
                        condition.LastAlarmInfoViewModel is not null)
                        alarmInfoViewModelsForObject.Add(condition.LastAlarmInfoViewModel);
                }
                if (eventSourceObject.NormalConditionState.Active && eventSourceObject.NormalConditionState.Unacked &&
                    eventSourceObject.NormalConditionState.LastAlarmInfoViewModel is not null)
                        alarmInfoViewModelsForObject.Add(eventSourceObject.NormalConditionState.LastAlarmInfoViewModel);
                alarmInfoViewModels.AddRange(alarmInfoViewModelsForObject);
            }

            return alarmInfoViewModels;
        }

        #endregion

        #region protected functions        

        protected virtual void DataAccessProviderOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            switch (args.PropertyName)
            {
                case nameof(IDataAccessProvider.IsConnected):
                    Clear();
                    break;
            }
        }

        #endregion
    }
}