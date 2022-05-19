using System;
using System.Collections.Generic;
using System.Text;

namespace Ssz.Utils.DataAccess
{
    public static class EventSourceModelHelper
    {
        #region protected functions        

        public static Func<AlarmConditionState, bool> GetPredicate(EventSourceModelSubscriptionScope subscriptionScope)
        {
            switch (subscriptionScope)
            {
                case EventSourceModelSubscriptionScope.Active:
                    return Active;
                case EventSourceModelSubscriptionScope.Unacked:
                    return Unacked;
                case EventSourceModelSubscriptionScope.ActiveOrUnacked:
                    return ActiveOrUnacked;
                case EventSourceModelSubscriptionScope.ActiveAndUnacked:
                    return ActiveAndUnacked;
                default:
                    throw new ArgumentException(nameof(subscriptionScope));
            }
        }

        #endregion

        #region private functions

        private static bool Active(AlarmConditionState alarmConditionState)
        {
            return alarmConditionState.Active;
        }

        private static bool Unacked(AlarmConditionState alarmConditionState)
        {
            return alarmConditionState.Unacked;
        }

        private static bool ActiveOrUnacked(AlarmConditionState alarmConditionState)
        {
            return alarmConditionState.Active || alarmConditionState.Unacked;
        }

        private static bool ActiveAndUnacked(AlarmConditionState alarmConditionState)
        {
            return alarmConditionState.Active && alarmConditionState.Unacked;
        }

        #endregion
    }
}
