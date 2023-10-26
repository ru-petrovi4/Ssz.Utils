using System;
using System.Collections.Generic;
using System.Linq;

namespace Ssz.Utils.DataAccess
{
    public class AlarmCategoryInfo
    {
        public uint ActiveCount;

        public uint UnackedCount;

        public uint ActiveOrUnackedCount;

        public uint ActiveAndUnackedCount;        
    }

    public class EventSourceArea
    {
        #region construction and destruction

        public EventSourceArea(string area, IDataAccessProvider dataAccessProvider)
        {
            Area = area;
            DataAccessProvider = dataAccessProvider;
        }

        #endregion

        #region public functions

        public string Area { get; }

        /// <summary>
        ///     You can use this property as temp storage.
        /// </summary>
        public object? Obj { get; set; }

        public Dictionary<IValueSubscription, EventSourceModelSubscriptionInfo> Subscriptions { get; } = new(ReferenceEqualityComparer<IValueSubscription>.Default);

        public readonly Dictionary<uint, AlarmCategoryInfo> AlarmCategoryInfos = new();

        public uint GetAlarmsCount(EventSourceModelSubscriptionScope subscriptionScope, uint? alarmCategoryIdFilter)
        {            
            uint count = 0;
            if (alarmCategoryIdFilter is null)
            {
                switch (subscriptionScope)
                {
                    case EventSourceModelSubscriptionScope.Active:
                        foreach (var alarmCategoryInfo in AlarmCategoryInfos)
                        {
                            count += alarmCategoryInfo.Value.ActiveCount;
                        }
                        break;
                    case EventSourceModelSubscriptionScope.Unacked:
                        foreach (var alarmCategoryInfo in AlarmCategoryInfos)
                        {
                            count += alarmCategoryInfo.Value.UnackedCount;
                        }
                        break;
                    case EventSourceModelSubscriptionScope.ActiveOrUnacked:
                        foreach (var alarmCategoryInfo in AlarmCategoryInfos)
                        {
                            count += alarmCategoryInfo.Value.ActiveOrUnackedCount;
                        }
                        break;
                    case EventSourceModelSubscriptionScope.ActiveAndUnacked:
                        foreach (var alarmCategoryInfo in AlarmCategoryInfos)
                        {
                            count += alarmCategoryInfo.Value.ActiveAndUnackedCount;
                        }
                        break;                    
                }
            }
            else
            {
                AlarmCategoryInfos.TryGetValue(alarmCategoryIdFilter.Value, out AlarmCategoryInfo? alarmCategoryInfo);
                if (alarmCategoryInfo is not null)
                {
                    switch (subscriptionScope)
                    {
                        case EventSourceModelSubscriptionScope.Active:
                            count = alarmCategoryInfo.ActiveCount;
                            break;
                        case EventSourceModelSubscriptionScope.Unacked:
                            count = alarmCategoryInfo.UnackedCount;
                            break;
                        case EventSourceModelSubscriptionScope.ActiveOrUnacked:
                            count = alarmCategoryInfo.ActiveOrUnackedCount;
                            break;
                        case EventSourceModelSubscriptionScope.ActiveAndUnacked:
                            count = alarmCategoryInfo.ActiveAndUnackedCount;
                            break;                        
                    }
                }
            }
            return count;
        }

        public uint GetAlarmMaxCategoryId(EventSourceModelSubscriptionScope subscriptionScope)
        {            
            switch (subscriptionScope)
            {
                case EventSourceModelSubscriptionScope.Active:
                    return AlarmCategoryInfos.Where(kvp => kvp.Value.ActiveCount > 0).DefaultIfEmpty().Max(kvp => kvp.Key);
                case EventSourceModelSubscriptionScope.Unacked:
                    return AlarmCategoryInfos.Where(kvp => kvp.Value.UnackedCount > 0).DefaultIfEmpty().Max(kvp => kvp.Key);
                case EventSourceModelSubscriptionScope.ActiveOrUnacked:
                    return AlarmCategoryInfos.Where(kvp => kvp.Value.ActiveOrUnackedCount > 0).DefaultIfEmpty().Max(kvp => kvp.Key);
                case EventSourceModelSubscriptionScope.ActiveAndUnacked:
                    return AlarmCategoryInfos.Where(kvp => kvp.Value.ActiveAndUnackedCount > 0).DefaultIfEmpty().Max(kvp => kvp.Key);
                default:
                    throw new ArgumentException(nameof(subscriptionScope));
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
                subscription.Update(new ValueStatusTimestamp { ValueStatusCode = ValueStatusCodes.Unknown });
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
                //case EventSourceModel.AlarmCondition_EventSourceModelSubscriptionType:
                //    AlarmConditionType alarmConditionType = GetAlarmConditionType(eventSourceModelSubscriptionInfo.eventSourceModelSubscriptionScope);
                //    subscription.Update(new ValueStatusTimestamp(new Any(alarmConditionType)));
                //    break;
            }
        }        

        #endregion

        #region protected functions

        protected IDataAccessProvider DataAccessProvider { get; }        

        #endregion     
    }
}