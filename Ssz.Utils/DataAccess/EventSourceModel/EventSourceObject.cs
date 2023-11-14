using System;
using System.Collections.Generic;
using System.Linq;

namespace Ssz.Utils.DataAccess
{
    public class EventSourceObject
    {
        #region construction and destruction

        public EventSourceObject(string tagName, IDataAccessProvider dataAccessProvider)
        {
            TagName = tagName;
            DataAccessProvider = dataAccessProvider;
        }

        #endregion

        #region public functions        

        public string TagName { get; }

        /// <summary>
        ///     You can use this property as temp storage.
        /// </summary>
        public object? Obj { get; set; }

        public Dictionary<IValueSubscription, EventSourceModelSubscriptionInfo> Subscriptions { get; } = new(ReferenceEqualityComparer<IValueSubscription>.Default);

        /// <summary>
        ///     Not includes normal AlarmConditionState.
        /// </summary>
        public Dictionary<AlarmConditionType, AlarmConditionState> AlarmConditions { get; } = new();

        public AlarmConditionState NormalConditionState { get; } = new AlarmConditionState(AlarmConditionType.None);

        public CaseInsensitiveDictionary<EventSourceArea> EventSourceAreas { get; } = new();

        public uint GetAlarmMaxCategoryId(EventSourceModelSubscriptionScope subscriptionScope)
        {
            var predicate = EventSourceModelHelper.GetPredicate(subscriptionScope);
            uint maxCategoryId = 0;
            foreach (var kvp in AlarmConditions)
                if (predicate(kvp.Value) && kvp.Value.CategoryId > maxCategoryId)
                    maxCategoryId = kvp.Value.CategoryId;
            return maxCategoryId;
        }

        public AlarmConditionType GetAlarmConditionType(EventSourceModelSubscriptionScope subscriptionScope)
        {
            var predicate = EventSourceModelHelper.GetPredicate(subscriptionScope);
            var conditions = AlarmConditions.Values.Where(predicate).ToArray();
            if (conditions.Length > 0)
            {
                AlarmConditionState alarmConditionState = conditions.OrderByDescending(ac => ac.CategoryId).ThenByDescending(ac => ac.ActiveOccurrenceTimeUtc).First();
                return alarmConditionState.AlarmCondition;
            }
            else
            {
                return AlarmConditionType.None;
            }
        }

        public uint GetAlarmsCount(EventSourceModelSubscriptionScope subscriptionScope, uint? alarmCategoryIdFilter)
        {            
            if (alarmCategoryIdFilter is null)
            {
                var predicate = EventSourceModelHelper.GetPredicate(subscriptionScope);
                foreach (var kvp in AlarmConditions)
                {
                    if (predicate(kvp.Value))
                        return 1;
                }
                return 0;
            }
            else
            {
                var predicate = EventSourceModelHelper.GetPredicate(subscriptionScope);
                foreach (var kvp in AlarmConditions)
                {
                    if (kvp.Value.CategoryId == alarmCategoryIdFilter.Value && predicate(kvp.Value))
                        return 1;
                }
                return 0;
            }            
        }

        public void NotifySubscriptions()
        {
            foreach (var kvp in Subscriptions)
            {
                NotifySubscription(kvp.Key, kvp.Value);
            }
        }

        public virtual void NotifySubscription(IValueSubscription subscription, EventSourceModelSubscriptionInfo eventSourceModelSubscriptionInfo)
        {
            if (!DataAccessProvider.IsConnected)
            {                
                subscription.Update(new ValueStatusTimestamp { ValueStatusCode = ValueStatusCodes.Uncertain });
                return;
            }            

            switch (eventSourceModelSubscriptionInfo.EventSourceModelSubscriptionType)
            {
                case EventSourceModel.AlarmsCount_SubscriptionType:
                    uint count = GetAlarmsCount(eventSourceModelSubscriptionInfo.EventSourceModelSubscriptionScope, eventSourceModelSubscriptionInfo.AlarmCategoryIdFilter);
                    subscription.Update(new ValueStatusTimestamp(new Any(count)));
                    break;
                case EventSourceModel.AlarmsAny_SubscriptionType:
                    bool alarmAny = GetAlarmsCount(eventSourceModelSubscriptionInfo.EventSourceModelSubscriptionScope, eventSourceModelSubscriptionInfo.AlarmCategoryIdFilter) > 0;
                    subscription.Update(new ValueStatusTimestamp(new Any(alarmAny)));
                    break;
                case EventSourceModel.AlarmMaxCategoryId_SubscriptionType:
                    uint alarmMaxCategoryId = GetAlarmMaxCategoryId(eventSourceModelSubscriptionInfo.EventSourceModelSubscriptionScope);
                    subscription.Update(new ValueStatusTimestamp(new Any(alarmMaxCategoryId)));
                    break;
                case EventSourceModel.AlarmConditionType_SubscriptionType:
                    AlarmConditionType alarmConditionType = GetAlarmConditionType(eventSourceModelSubscriptionInfo.EventSourceModelSubscriptionScope);
                    subscription.Update(new ValueStatusTimestamp(new Any(alarmConditionType)));
                    break;                
            }
        }        

        #endregion

        #region protected functions

        protected IDataAccessProvider DataAccessProvider { get; }

        #endregion
    }
}