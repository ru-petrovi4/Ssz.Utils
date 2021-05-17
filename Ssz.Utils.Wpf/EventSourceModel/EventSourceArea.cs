//using Ssz.Utils.DataAccess;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace Ssz.Utils.Wpf.EventSourceModel
//{
//    public class EventSourceArea
//    {
//        #region public functions

//        public event Action<ValueStatusTimestamp>? AlarmUnackedSubscribers;
//        public event Action<ValueStatusTimestamp>? AlarmCategorySubscribers;
//        public event Action<ValueStatusTimestamp>? AlarmBrushSubscribers;
//        public int UnackedAlarmsCount;
//        public bool IsGraphic;


//        public readonly Dictionary<uint, int> ActiveAlarmsCategories = new();

//        public void NotifyAlarmUnackedSubscribers()
//        {
//            var alarmUnackedSubscribers = AlarmUnackedSubscribers;
//            if (alarmUnackedSubscribers != null)
//            {
//                if (DsDataAccessProvider.Instance.IsConnected)
//                {
//                    var anyUnacked = UnackedAlarmsCount > 0;
//                    alarmUnackedSubscribers(new ValueStatusTimestamp(new Any(anyUnacked), StatusCodes.Good,
//                        DateTime.UtcNow));
//                }
//                else
//                {
//                    alarmUnackedSubscribers(new ValueStatusTimestamp());
//                }
//            }
//        }

//        public void NotifyAlarmCategorySubscribers()
//        {
//            var alarmCategorySubscribers = AlarmCategorySubscribers;
//            if (alarmCategorySubscribers != null)
//            {
//                if (DsDataAccessProvider.Instance.IsConnected)
//                {
//                    uint maxCategory = 0;
//                    if (ActiveAlarmsCategories.Count > 0)
//                        maxCategory = ActiveAlarmsCategories.Keys.Max();
//                    alarmCategorySubscribers(new ValueStatusTimestamp(new Any(maxCategory), StatusCodes.Good,
//                        DateTime.UtcNow));
//                }
//                else
//                {
//                    alarmCategorySubscribers(new ValueStatusTimestamp());
//                }
//            }
//        }

//        public void NotifyAlarmBrushSubscribers()
//        {
//            var alarmBrushSubscribers = AlarmBrushSubscribers;
//            if (alarmBrushSubscribers != null) alarmBrushSubscribers(new ValueStatusTimestamp());
//            /* TODO
//            */
//        }

//        #endregion
//    }
//}