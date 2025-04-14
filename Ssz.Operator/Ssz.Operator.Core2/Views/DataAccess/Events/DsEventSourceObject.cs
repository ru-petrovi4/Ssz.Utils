using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;

using Ssz.Operator.Core.DataEngines;
using Ssz.Utils;
using Ssz.Utils.DataAccess;

namespace Ssz.Operator.Core.DataAccess
{
    internal class DsEventSourceObject : EventSourceObject
    {
        #region construction and destruction

        public DsEventSourceObject(string tagName, IDataAccessProvider dataAccessProvider) :
            base(tagName, dataAccessProvider)
        {            
        }

        #endregion

        #region public functions        

        public TagAlarmsBrushes? TagsAlarmsBrushes { get; set; }

        public bool AnyUnacked()
        {
            foreach (var kvp in AlarmConditions)
            {
                if (kvp.Value.Unacked)
                    return true;
            }
            return false;
        }

        public Brush? GetAlarmBrush(EventSourceModelSubscriptionScope subscriptionScope)
        {
            if (_tagAlarmsInfo is null)
                _tagAlarmsInfo = DsProject.Instance.DataEngine.GetTagAlarmsInfo(TagName);

            var tagAlarmsBrushes = _tagAlarmsInfo.GetTagAlarmsBrushes();

            var (categoryId, priority) = GetAlarmMaxCategoryId(subscriptionScope);

            if (tagAlarmsBrushes.PriorityBrushes is not null)
            {
                if (tagAlarmsBrushes.PriorityBrushes.TryGetValue(priority, out AlarmBrushes alarmBrushes))
                {
                    if (AnyUnacked())
                        return alarmBrushes.BlinkingBrush;
                    else
                        return alarmBrushes.Brush;
                }
            }

            if (AnyUnacked())
            {
                switch (categoryId)
                {
                    case 0:
                        return tagAlarmsBrushes.AlarmCategory0Brushes.BlinkingBrush;
                    case 1:
                        return tagAlarmsBrushes.AlarmCategory1Brushes.BlinkingBrush;
                    case 2:
                        return tagAlarmsBrushes.AlarmCategory2Brushes.BlinkingBrush;
                    default:
                        return tagAlarmsBrushes.AlarmCategory1Brushes.BlinkingBrush;
                }
            }
            else
            {
                switch (categoryId)
                {
                    case 0:
                        return tagAlarmsBrushes.AlarmCategory0Brushes.Brush;
                    case 1:
                        return tagAlarmsBrushes.AlarmCategory1Brushes.Brush;
                    case 2:
                        return tagAlarmsBrushes.AlarmCategory2Brushes.Brush;
                    default:
                        return tagAlarmsBrushes.AlarmCategory1Brushes.Brush;
                }
            }
        }

        public override void NotifySubscription(IValueSubscription subscription, EventSourceModelSubscriptionInfo eventSourceModelSubscriptionInfo)
        {
            switch (eventSourceModelSubscriptionInfo.EventSourceModelSubscriptionType)
            {
                case DsEventSourceModel.AlarmBrush_SubscriptionType:
                    if (!DataAccessProvider.IsConnected)
                    {
                        subscription.Update(new ValueStatusTimestamp { StatusCode = StatusCodes.Uncertain });
                    }
                    else
                    {
                        Brush? alarmBrush = GetAlarmBrush(eventSourceModelSubscriptionInfo.EventSourceModelSubscriptionScope);
                        subscription.Update(new ValueStatusTimestamp(new Any(alarmBrush)));
                    }
                    break;
                default:
                    base.NotifySubscription(subscription, eventSourceModelSubscriptionInfo);
                    break;
            }
        }

        #endregion

        #region private fields

        private TagAlarmsInfo? _tagAlarmsInfo;

        #endregion        
    }
}