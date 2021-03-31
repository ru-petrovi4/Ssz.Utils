using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Utils;
using Ssz.Xi.Client;
using TestWpfApp;

namespace Ssz.WpfHmi.Common.ModelData.Events
{
    public class EventSourceArea
    {
        #region public functions

        public event Action<Any>? AlarmUnackedSubscribers;
        public event Action<Any>? AlarmCategorySubscribers;
        public event Action<Any>? AlarmBrushSubscribers;
        public int UnackedAlarmsCount;
        public bool IsPage;
        /// <summary>
        ///     [AlarmCategory, Count]
        /// </summary>
        public readonly Dictionary<uint, int> ActiveAlarmsCategories = new Dictionary<uint, int>();

        public void NotifyAlarmUnackedSubscribers()
        {
            var alarmUnackedSubscribers = AlarmUnackedSubscribers;
            if (alarmUnackedSubscribers != null)
            {
                if (App.DataProvider.IsConnected)
                {
                    bool anyUnacked = UnackedAlarmsCount > 0;
                    alarmUnackedSubscribers(new Any(anyUnacked));
                }
                else
                {
                    alarmUnackedSubscribers(new Any(null));
                }
            }
        }

        public void NotifyAlarmCategorySubscribers()
        {
            var alarmCategorySubscribers = AlarmCategorySubscribers;
            if (alarmCategorySubscribers != null)
            {
                if (App.DataProvider.IsConnected)
                {
                    uint maxCategory = 0;
                    if (ActiveAlarmsCategories.Count > 0)
                        maxCategory = ActiveAlarmsCategories.Keys.Max();
                    alarmCategorySubscribers(new Any(maxCategory));
                }
                else
                {
                    alarmCategorySubscribers(new Any(null));
                }
            }
        }

        public void NotifyAlarmBrushSubscribers()
        {
            var alarmBrushSubscribers = AlarmBrushSubscribers;
            if (alarmBrushSubscribers != null)
            {
                alarmBrushSubscribers(new Any(null));
            }
            /* TODO
            */
        }

        #endregion
    }
}