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

        public event Action<ValueStatusTimestamp>? AlarmUnackedChanged;
        public event Action<ValueStatusTimestamp>? AlarmCategoryChanged;       

        public string Area { get; }

        public int UnackedAlarmsCount;
        
        /// <summary>
        ///     [AlarmCategory, Count]
        /// </summary>
        public Dictionary<uint, int> ActiveAlarmsCategories { get; } = new();        

        public void NotifyAlarmUnackedSubscribers()
        {
            var alarmUnackedChanged = AlarmUnackedChanged;
            if (alarmUnackedChanged != null)
            {
                if (_dataAccessProvider.IsConnected)
                {
                    bool anyUnacked = UnackedAlarmsCount > 0;
                    alarmUnackedChanged(new ValueStatusTimestamp(new Any(anyUnacked), StatusCodes.Good, DateTime.UtcNow));
                }
                else
                {
                    alarmUnackedChanged(new ValueStatusTimestamp());
                }
            }
        }

        public void NotifyAlarmCategorySubscribers()
        {
            var alarmCategoryChanged = AlarmCategoryChanged;
            if (alarmCategoryChanged != null)
            {
                if (_dataAccessProvider.IsConnected)
                {
                    uint maxCategory = 0;
                    if (ActiveAlarmsCategories.Count > 0)
                        maxCategory = ActiveAlarmsCategories.Keys.Max();
                    alarmCategoryChanged(new ValueStatusTimestamp(new Any(maxCategory), StatusCodes.Good, DateTime.UtcNow));
                }
                else
                {
                    alarmCategoryChanged(new ValueStatusTimestamp());
                }
            }
        }

        #endregion

        #region private fields
        
        private readonly IDataAccessProvider _dataAccessProvider;

        #endregion        
    }
}