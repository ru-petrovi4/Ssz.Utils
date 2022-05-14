using System;
using System.Collections.Generic;
using System.Linq;

namespace Ssz.Utils.DataAccess
{
    public class EventSourceObject
    {
        #region construction and destruction

        public EventSourceObject(string tag, IDataAccessProvider dataAccessProvider)
        {
            Tag = tag;
            DataAccessProvider = dataAccessProvider;
        }

        #endregion

        #region public functions        

        public string Tag { get; }

        /// <summary>
        ///     You can use this property as temp storage.
        /// </summary>
        public object? Obj { get; set; }

        public Dictionary<IValueSubscription, AlarmConditionSubscriptionInfo> Subscriptions { get; } = new(ReferenceEqualityComparer<IValueSubscription>.Default);

        /// <summary>
        ///     Not includes normal AlarmConditionState.
        /// </summary>
        public Dictionary<AlarmConditionType, AlarmConditionState> AlarmConditions { get; } = new();

        public AlarmConditionState NormalConditionState { get; } = new AlarmConditionState(AlarmConditionType.None);

        public CaseInsensitiveDictionary<EventSourceArea> EventSourceAreas { get; } = new();

        public bool GetAlarmAny(AlarmConditionSubscriptionScope subscriptionScope, uint? alarmCategoryIdFilter)
        {
            if (alarmCategoryIdFilter is null)
            {
                var predicate = EventSourceModelHelper.GetPredicate(subscriptionScope);
                foreach (var kvp in AlarmConditions)
                {
                    if (predicate(kvp.Value))
                        return true;
                }
                return false;
            }
            else
            {
                var predicate = EventSourceModelHelper.GetPredicate(subscriptionScope);
                foreach (var kvp in AlarmConditions)
                {
                    if (kvp.Value.CategoryId == alarmCategoryIdFilter.Value && predicate(kvp.Value))
                        return true;
                }
                return false;
            }            
        }

        public uint GetAlarmMaxCategoryId(AlarmConditionSubscriptionScope subscriptionScope)
        {
            var predicate = EventSourceModelHelper.GetPredicate(subscriptionScope);
            uint maxCategoryId = 0;
            foreach (var kvp in AlarmConditions)
                if (predicate(kvp.Value) && kvp.Value.CategoryId > maxCategoryId)
                    maxCategoryId = kvp.Value.CategoryId;
            return maxCategoryId;
        }

        public AlarmConditionType GetAlarmConditionType(AlarmConditionSubscriptionScope subscriptionScope)
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

        public uint GetAlarmsCount(AlarmConditionSubscriptionScope alarmConditionSubscriptionScope, uint? alarmCategoryIdFilter)
        {
            // TODO
            return 0;
        }

        public void NotifySubscriptions()
        {
            foreach (var kvp in Subscriptions)
            {
                NotifySubscription(kvp.Key, kvp.Value);
            }
        }

        public virtual void NotifySubscription(IValueSubscription subscription, AlarmConditionSubscriptionInfo alarmConditionSubscriptionInfo)
        {
            if (!DataAccessProvider.IsConnected)
            {                
                subscription.Update(new ValueStatusTimestamp());
                return;
            }            

            switch (alarmConditionSubscriptionInfo.AlarmConditionSubscriptionType)
            {
                case EventSourceModel.AlarmAny_SubscriptionType:
                    bool alarmAny = GetAlarmAny(alarmConditionSubscriptionInfo.AlarmConditionSubscriptionScope, alarmConditionSubscriptionInfo.AlarmCategoryIdFilter);
                    subscription.Update(new ValueStatusTimestamp(new Any(alarmAny)));
                    break;
                case EventSourceModel.AlarmMaxCategoryId_SubscriptionType:
                    uint alarmMaxCategoryId = GetAlarmMaxCategoryId(alarmConditionSubscriptionInfo.AlarmConditionSubscriptionScope);
                    subscription.Update(new ValueStatusTimestamp(new Any(alarmMaxCategoryId)));
                    break;
                case EventSourceModel.AlarmConditionType_SubscriptionType:
                    AlarmConditionType alarmConditionType = GetAlarmConditionType(alarmConditionSubscriptionInfo.AlarmConditionSubscriptionScope);
                    subscription.Update(new ValueStatusTimestamp(new Any(alarmConditionType)));
                    break;
                case EventSourceModel.AlarmsCount_SubscriptionType:
                    uint count = GetAlarmsCount(alarmConditionSubscriptionInfo.AlarmConditionSubscriptionScope, alarmConditionSubscriptionInfo.AlarmCategoryIdFilter);
                    subscription.Update(new ValueStatusTimestamp(new Any(count)));
                    break;
            }
        }        

        #endregion

        #region protected functions

        protected IDataAccessProvider DataAccessProvider { get; }

        #endregion
    }
}