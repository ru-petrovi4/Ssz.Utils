using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ssz.Utils.EventSourceModel
{
    public class EventSourceArea
    {
        #region construction and destruction

        public EventSourceArea(string area, IDataAccessProvider dataAccessProvider)
        {
            Area = area;
            _dataAccessProvider = dataAccessProvider;
        }

        #endregion

        #region public functions        

        public event Action<Any>? AlarmUnackedSubscribers;
        public event Action<Any>? AlarmCategorySubscribers;       

        public string Area { get; }

        public int UnackedAlarmsCount;
        
        /// <summary>
        ///     [AlarmCategory, Count]
        /// </summary>
        public Dictionary<uint, int> ActiveAlarmsCategories { get; } = new();        

        public void NotifyAlarmUnackedSubscribers()
        {
            var alarmUnackedSubscribers = AlarmUnackedSubscribers;
            if (alarmUnackedSubscribers != null)
            {
                if (_dataAccessProvider.IsConnected)
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
                if (_dataAccessProvider.IsConnected)
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

        #endregion

        #region private fields
        
        private readonly IDataAccessProvider _dataAccessProvider;

        #endregion        
    }
}