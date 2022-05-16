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

        public Dictionary<IValueSubscription, AlarmConditionSubscriptionInfo> Subscriptions { get; } = new(ReferenceEqualityComparer<IValueSubscription>.Default);

        public readonly Dictionary<uint, AlarmCategoryInfo> AlarmCategoryInfos = new();

        public uint GetAlarmsCount(AlarmConditionSubscriptionScope subscriptionScope, uint? alarmCategoryIdFilter)
        {            
            uint count = 0;
            if (alarmCategoryIdFilter is null)
            {
                switch (subscriptionScope)
                {
                    case AlarmConditionSubscriptionScope.Active:
                        foreach (var alarmCategoryInfo in AlarmCategoryInfos)
                        {
                            count += alarmCategoryInfo.Value.ActiveCount;
                        }
                        break;
                    case AlarmConditionSubscriptionScope.Unacked:
                        foreach (var alarmCategoryInfo in AlarmCategoryInfos)
                        {
                            count += alarmCategoryInfo.Value.UnackedCount;
                        }
                        break;
                    case AlarmConditionSubscriptionScope.ActiveOrUnacked:
                        foreach (var alarmCategoryInfo in AlarmCategoryInfos)
                        {
                            count += alarmCategoryInfo.Value.ActiveOrUnackedCount;
                        }
                        break;
                    case AlarmConditionSubscriptionScope.ActiveAndUnacked:
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
                        case AlarmConditionSubscriptionScope.Active:
                            count = alarmCategoryInfo.ActiveCount;
                            break;
                        case AlarmConditionSubscriptionScope.Unacked:
                            count = alarmCategoryInfo.UnackedCount;
                            break;
                        case AlarmConditionSubscriptionScope.ActiveOrUnacked:
                            count = alarmCategoryInfo.ActiveOrUnackedCount;
                            break;
                        case AlarmConditionSubscriptionScope.ActiveAndUnacked:
                            count = alarmCategoryInfo.ActiveAndUnackedCount;
                            break;                        
                    }
                }
            }
            return count;
        }

        public uint GetAlarmMaxCategoryId(AlarmConditionSubscriptionScope subscriptionScope)
        {            
            switch (subscriptionScope)
            {
                case AlarmConditionSubscriptionScope.Active:
                    return AlarmCategoryInfos.Where(kvp => kvp.Value.ActiveCount > 0).Max(kvp => kvp.Key);
                case AlarmConditionSubscriptionScope.Unacked:
                    return AlarmCategoryInfos.Where(kvp => kvp.Value.UnackedCount > 0).Max(kvp => kvp.Key);
                case AlarmConditionSubscriptionScope.ActiveOrUnacked:
                    return AlarmCategoryInfos.Where(kvp => kvp.Value.ActiveOrUnackedCount > 0).Max(kvp => kvp.Key);
                case AlarmConditionSubscriptionScope.ActiveAndUnacked:
                    return AlarmCategoryInfos.Where(kvp => kvp.Value.ActiveAndUnackedCount > 0).Max(kvp => kvp.Key);
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

        public virtual void NotifySubscription(IValueSubscription subscription, AlarmConditionSubscriptionInfo alarmConditionSubscriptionInfo)
        {
            if (!DataAccessProvider.IsConnected)
            {
                subscription.Update(new ValueStatusTimestamp());
                return;
            }

            switch (alarmConditionSubscriptionInfo.AlarmConditionSubscriptionType)
            {
                case EventSourceModel.AlarmsCount_SubscriptionType:
                    uint count = GetAlarmsCount(alarmConditionSubscriptionInfo.AlarmConditionSubscriptionScope, alarmConditionSubscriptionInfo.AlarmCategoryIdFilter);
                    subscription.Update(new ValueStatusTimestamp(new Any(count)));
                    break;
                case EventSourceModel.AlarmsAny_SubscriptionType:
                    bool alarmAny = GetAlarmsCount(alarmConditionSubscriptionInfo.AlarmConditionSubscriptionScope, alarmConditionSubscriptionInfo.AlarmCategoryIdFilter) > 0;
                    subscription.Update(new ValueStatusTimestamp(new Any(alarmAny)));
                    break;
                case EventSourceModel.AlarmMaxCategoryId_SubscriptionType:
                    uint alarmMaxCategoryId = GetAlarmMaxCategoryId(alarmConditionSubscriptionInfo.AlarmConditionSubscriptionScope);
                    subscription.Update(new ValueStatusTimestamp(new Any(alarmMaxCategoryId)));
                    break;
                //case EventSourceModel.AlarmCondition_AlarmConditionSubscriptionType:
                //    AlarmConditionType alarmConditionType = GetAlarmConditionType(alarmConditionSubscriptionInfo.AlarmConditionSubscriptionScope);
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