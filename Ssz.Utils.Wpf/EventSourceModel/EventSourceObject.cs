//using Ssz.Utils.DataAccess;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Windows.Media;

//namespace Ssz.Utils.Wpf.EventSourceModel
//{
//    public class EventSourceObject
//    {
//        #region construction and destruction

//        public EventSourceObject(string tag)
//        {
//            Tag = tag;
//        }

//        #endregion

//        #region private functions

//        private Brush? GetAlarmBrush()
//        {
//            //if (AlarmTagTypeBrushes == null)
//            //    AlarmTagTypeBrushes = DsSolution.Instance.GetDataEngine().GetAlarmTagTypeInfo(Tag)
//            //        .GetAlarmTagTypeBrushes();

//            if (AnyUnacked())
//                switch (GetActiveAlarmsMaxCategory())
//                {
//                    case 0:
//                        return AlarmTagTypeBrushes.AlarmCategory0BlinkingBrush;
//                    case 1:
//                        return AlarmTagTypeBrushes.AlarmCategory1BlinkingBrush;
//                    case 2:
//                        return AlarmTagTypeBrushes.AlarmCategory2BlinkingBrush;
//                    default:
//                        return AlarmTagTypeBrushes.AlarmCategory1BlinkingBrush;
//                }

//            switch (GetActiveAlarmsMaxCategory())
//            {
//                case 0:
//                    return AlarmTagTypeBrushes.AlarmCategory0Brush;
//                case 1:
//                    return AlarmTagTypeBrushes.AlarmCategory1Brush;
//                case 2:
//                    return AlarmTagTypeBrushes.AlarmCategory2Brush;
//                default:
//                    return AlarmTagTypeBrushes.AlarmCategory1Brush;
//            }
//        }

//        #endregion

//        #region public functions

//        public event Action<ValueStatusTimestamp>? AlarmUnackedSubscribers;
//        public event Action<ValueStatusTimestamp>? AlarmCategorySubscribers;
//        public event Action<ValueStatusTimestamp>? AlarmBrushSubscribers;
//        public event Action<ValueStatusTimestamp>? AlarmConditionTypeSubscribers;
//        public readonly Dictionary<AlarmConditionType, ConditionState> AlarmConditions = new();
//        public readonly ConditionState NormalCondition = new();
//        public readonly CaseInsensitiveDictionary<EventSourceArea> EventSourceAreas = new();


//        public string Tag { get; }


//        public AlarmTagTypeBrushes? AlarmTagTypeBrushes { get; set; }


//        public bool AnyUnacked()
//        {
//            foreach (var kvp in AlarmConditions)
//                if (kvp.Value.Unacked)
//                    //We found one guy who is unacknowledged, so we can return true;
//                    return true;
//            return false;
//        }


//        public bool AnyActive()
//        {
//            foreach (var kvp in AlarmConditions)
//                if (kvp.Value.Active)
//                    //We found at least one guy who is active, so we can return true;
//                    return true;
//            return false;
//        }

//        public uint GetActiveAlarmsMaxCategory()
//        {
//            uint maxCategory = 0;
//            foreach (var kvp in AlarmConditions)
//                if (kvp.Value.Active && kvp.Value.CategoryId > maxCategory)
//                    maxCategory = kvp.Value.CategoryId;
//            return maxCategory;
//        }

//        public AlarmConditionType GetAlarmConditionType()
//        {
//            var activeConditions = AlarmConditions.Where(kvp => kvp.Value.Active).ToArray();
//            if (activeConditions.Length > 0)
//            {
//                var kvp = activeConditions.OrderByDescending(i => i.Value.CategoryId)
//                    .ThenByDescending(i => i.Value.ActiveOccurrenceTime).First();
//                return kvp.Key;
//            }

//            return AlarmConditionType.None;
//        }

//        public void NotifyAlarmUnackedSubscribers()
//        {
//            var alarmUnackedSubscribers = AlarmUnackedSubscribers;
//            if (alarmUnackedSubscribers != null)
//            {
//                if (DsDataAccessProvider.Instance.IsConnected)
//                {
//                    var anyUnacked = AnyUnacked();
//                    alarmUnackedSubscribers(new ValueStatusTimestamp(new Any(anyUnacked), StatusCodes.Good,
//                        DateTime.UtcNow));
//                }
//                else
//                {
//                    alarmUnackedSubscribers(new ValueStatusTimestamp());
//                }
//            }
//        }

//        public void NotifyAlarmUnackedSubscriber(IValueSubscription subscriber)
//        {
//            if (DsDataAccessProvider.Instance.IsConnected)
//            {
//                var anyUnacked = AnyUnacked();
//                subscriber.Update(new ValueStatusTimestamp(new Any(anyUnacked), StatusCodes.Good, DateTime.UtcNow));
//            }
//            else
//            {
//                subscriber.Update(new ValueStatusTimestamp());
//            }
//        }

//        public void NotifyAlarmCategorySubscribers()
//        {
//            var alarmCategorySubscribers = AlarmCategorySubscribers;
//            if (alarmCategorySubscribers != null)
//            {
//                if (DsDataAccessProvider.Instance.IsConnected)
//                {
//                    var maxCategory = GetActiveAlarmsMaxCategory();
//                    alarmCategorySubscribers(new ValueStatusTimestamp(new Any(maxCategory), StatusCodes.Good,
//                        DateTime.UtcNow));
//                }
//                else
//                {
//                    alarmCategorySubscribers(new ValueStatusTimestamp());
//                }
//            }
//        }

//        public void NotifyAlarmCategorySubscriber(IValueSubscription subscriber)
//        {
//            if (DsDataAccessProvider.Instance.IsConnected)
//            {
//                var maxCategory = GetActiveAlarmsMaxCategory();
//                subscriber.Update(new ValueStatusTimestamp(new Any(maxCategory), StatusCodes.Good, DateTime.UtcNow));
//            }
//            else
//            {
//                subscriber.Update(new ValueStatusTimestamp());
//            }
//        }

//        public void NotifyAlarmBrushSubscribers()
//        {
//            var alarmBrushSubscribers = AlarmBrushSubscribers;
//            if (alarmBrushSubscribers != null)
//            {
//                if (DsDataAccessProvider.Instance.IsConnected)
//                {
//                    var alarmBrush = GetAlarmBrush();
//                    alarmBrushSubscribers(new ValueStatusTimestamp(new Any(alarmBrush), StatusCodes.Good,
//                        DateTime.UtcNow));
//                }
//                else
//                {
//                    alarmBrushSubscribers(new ValueStatusTimestamp());
//                }
//            }
//        }

//        public void NotifyAlarmBrushSubscriber(IValueSubscription subscriber)
//        {
//            if (DsDataAccessProvider.Instance.IsConnected)
//            {
//                var alarmBrush = GetAlarmBrush();
//                subscriber.Update(new ValueStatusTimestamp(new Any(alarmBrush), StatusCodes.Good, DateTime.UtcNow));
//            }
//            else
//            {
//                subscriber.Update(new ValueStatusTimestamp());
//            }
//        }

//        public void NotifyAlarmConditionTypeSubscribers()
//        {
//            var alarmConditionTypeSubscribers = AlarmConditionTypeSubscribers;
//            if (alarmConditionTypeSubscribers != null)
//            {
//                if (DsDataAccessProvider.Instance.IsConnected)
//                {
//                    var alarmConditionType = GetAlarmConditionType();
//                    alarmConditionTypeSubscribers(new ValueStatusTimestamp(new Any(alarmConditionType),
//                        StatusCodes.Good, DateTime.UtcNow));
//                }
//                else
//                {
//                    alarmConditionTypeSubscribers(new ValueStatusTimestamp());
//                }
//            }
//        }

//        public void NotifyAlarmConditionTypeSubscriber(IValueSubscription subscriber)
//        {
//            if (DsDataAccessProvider.Instance.IsConnected)
//            {
//                var alarmConditionType = GetAlarmConditionType();
//                subscriber.Update(new ValueStatusTimestamp(new Any(alarmConditionType), StatusCodes.Good,
//                    DateTime.UtcNow));
//            }
//            else
//            {
//                subscriber.Update(new ValueStatusTimestamp());
//            }
//        }

//        #endregion
//    }
//}