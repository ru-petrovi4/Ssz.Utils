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

        public uint GetAlarmsCount(AlarmConditionSubscriptionScope alarmConditionSubscriptionScope, uint? alarmCategoryIdFilter)
        {            
            uint count = 0;
            if (alarmCategoryIdFilter is null)
            {
                switch (alarmConditionSubscriptionScope)
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
                    switch (alarmConditionSubscriptionScope)
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
                //case EventSourceModel.AlarmAny_AlarmConditionSubscriptionType:
                //    bool alarmAny = GetAlarmAny(alarmConditionSubscriptionInfo.AlarmConditionSubscriptionScope);
                //    subscription.Update(new ValueStatusTimestamp(new Any(alarmAny)));
                //    break;
                //case EventSourceModel.AlarmMaxCategoryId_AlarmConditionSubscriptionType:
                //    uint alarmMaxCategoryId = GetAlarmMaxCategoryId(alarmConditionSubscriptionInfo.AlarmConditionSubscriptionScope);
                //    subscription.Update(new ValueStatusTimestamp(new Any(alarmMaxCategoryId)));
                //    break;
                //case EventSourceModel.AlarmCondition_AlarmConditionSubscriptionType:
                //    AlarmConditionType alarmConditionType = GetAlarmConditionType(alarmConditionSubscriptionInfo.AlarmConditionSubscriptionScope);
                //    subscription.Update(new ValueStatusTimestamp(new Any(alarmConditionType)));
                //    break;
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