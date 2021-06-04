using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Ssz.Utils.DataAccess;

namespace Ssz.Utils.Wpf.EventSourceModel
{
    /// <summary>
    /// A class to handle Alarm and Event notifications
    /// </summary>
    public class EventSourceObject
    {
        #region construction and destruction

        public EventSourceObject(string tag, IDataAccessProvider dataAccessProvider, AlarmTypeBrushesBase alarmTypeBrushes)
        {
            _tag = tag;
            _dataAccessProvider = dataAccessProvider;
            _alarmTypeBrushes = alarmTypeBrushes;
        }

        #endregion

        #region public functions        

        public event Action<Any>? AlarmUnackedSubscribers;
        public event Action<Any>? AlarmCategorySubscribers;
        public event Action<Any>? AlarmBrushSubscribers;
        public event Action<Any>? AlarmConditionTypeSubscribers;

        public Dictionary<AlarmCondition, ConditionState> AlarmConditions { get; } = new Dictionary<AlarmCondition, ConditionState>();

        public ConditionState NormalCondition { get; } = new ConditionState();

        public CaseInsensitiveDictionary<EventSourceArea> EventSourceAreas { get; } = new CaseInsensitiveDictionary<EventSourceArea>();
        
        /// <summary>
        /// Indicates if any alarms on this EventSource are unacknowledged.
        /// </summary>
        /// <returns>
        /// true if any alarms are in the Unack state
        /// false if all the alarms have been acknowledged
        /// </returns>
        public bool AnyUnacked()
        {
            foreach (var kvp in AlarmConditions)
            {
                if (kvp.Value.Unacked)
                {
                    //We found one guy who is unacknowledged, so we can return true;
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Indicates if any alarms on this EventSource are active.
        /// </summary>
        /// <returns>
        /// true if any alarms are in the Active state
        /// false if all the alarms are not in the Active state
        /// </returns>
        public bool AnyActive()
        {
            foreach (var kvp in AlarmConditions)
            {
                if (kvp.Value.Active)
                {
                    //We found at least one guy who is active, so we can return true;
                    return true;
                }
            }
            return false;
        }

        public uint GetActiveAlarmsMaxCategory()
        {
            uint maxCategory = 0;
            foreach (var kvp in AlarmConditions)
            {
                if (kvp.Value.Active && kvp.Value.CategoryId > maxCategory)
                {
                    maxCategory = kvp.Value.CategoryId;
                }
            }
            return maxCategory;
        }

        public AlarmCondition GetAlarmConditionType()
        {
            var activeConditions = AlarmConditions.Where(kvp => kvp.Value.Active).ToArray();
            if (activeConditions.Length > 0)
            {
                var kvp = activeConditions.OrderByDescending(i => i.Value.CategoryId).ThenByDescending(i => i.Value.ActiveOccurrenceTime).First();
                return kvp.Key;
            }
            else
            {
                return AlarmCondition.None;
            }            
        }

        public void NotifyAlarmUnackedSubscribers()
        {
            var alarmUnackedSubscribers = AlarmUnackedSubscribers;
            if (alarmUnackedSubscribers != null)
            {
                if (_dataAccessProvider.IsConnected)
                {
                    bool anyUnacked = AnyUnacked();
                    alarmUnackedSubscribers(new Any(anyUnacked));
                }
                else
                {
                    alarmUnackedSubscribers(new Any(null));
                }
            }
        }

        public void NotifyAlarmUnackedSubscriber(IValueSubscription subscriber)
        {
            if (_dataAccessProvider.IsConnected)
            {
                bool anyUnacked = AnyUnacked();
                subscriber.Update(new ValueStatusTimestamp(new Any(anyUnacked), StatusCodes.Good, DateTime.UtcNow));
            }
            else
            {
                subscriber.Update(new ValueStatusTimestamp());
            }
        }

        public void NotifyAlarmCategorySubscribers()
        {
            var alarmCategorySubscribers = AlarmCategorySubscribers;
            if (alarmCategorySubscribers != null)
            {
                if (_dataAccessProvider.IsConnected)
                {
                    uint maxCategory = GetActiveAlarmsMaxCategory();
                    alarmCategorySubscribers(new Any(maxCategory));
                }
                else
                {
                    alarmCategorySubscribers(new Any(null));
                }
            }
        }

        public void NotifyAlarmCategorySubscriber(IValueSubscription subscriber)
        {
            if (_dataAccessProvider.IsConnected)
            {
                uint maxCategory = GetActiveAlarmsMaxCategory();
                subscriber.Update(new ValueStatusTimestamp(new Any(maxCategory), StatusCodes.Good, DateTime.UtcNow));
            }
            else
            {
                subscriber.Update(new ValueStatusTimestamp());
            }
        }

        public void NotifyAlarmBrushSubscribers()
        {
            var alarmBrushSubscribers = AlarmBrushSubscribers;
            if (alarmBrushSubscribers != null)
            {
                if (_dataAccessProvider.IsConnected)
                {
                    Brush alarmBrush = GetAlarmBrush();
                    alarmBrushSubscribers(new Any(alarmBrush));
                }
                else
                {
                    alarmBrushSubscribers(new Any(null));
                }
            }
        }

        public void NotifyAlarmBrushSubscriber(IValueSubscription subscriber)
        {
            if (_dataAccessProvider.IsConnected)
            {
                Brush alarmBrush = GetAlarmBrush();
                subscriber.Update(new ValueStatusTimestamp(new Any(alarmBrush), StatusCodes.Good, DateTime.UtcNow));
            }
            else
            {
                subscriber.Update(new ValueStatusTimestamp());
            }
        }

        public void NotifyAlarmConditionTypeSubscribers()
        {
            var alarmConditionTypeSubscribers = AlarmConditionTypeSubscribers;
            if (alarmConditionTypeSubscribers != null)
            {
                if (_dataAccessProvider.IsConnected)
                {
                    AlarmCondition alarmConditionType = GetAlarmConditionType();
                    alarmConditionTypeSubscribers(new Any(alarmConditionType));
                }
                else
                {
                    alarmConditionTypeSubscribers(new Any(null));
                }
            }
        }

        public void NotifyAlarmConditionTypeSubscriber(IValueSubscription subscriber)
        {
            if (_dataAccessProvider.IsConnected)
            {
                AlarmCondition alarmConditionType = GetAlarmConditionType();
                subscriber.Update(new ValueStatusTimestamp(new Any(alarmConditionType), StatusCodes.Good, DateTime.UtcNow));
            }
            else
            {
                subscriber.Update(new ValueStatusTimestamp());
            }
        }

        #endregion

        #region private functions

        private Brush GetAlarmBrush()
        {
            if (AnyUnacked())
            {
                switch (GetActiveAlarmsMaxCategory())
                {
                    case 0:
                        return _alarmTypeBrushes.AlarmCategory0BlinkingBrush;                        
                    case 1:
                        return _alarmTypeBrushes.AlarmCategory1BlinkingBrush;                        
                    case 2:
                        return _alarmTypeBrushes.AlarmCategory2BlinkingBrush;                        
                    default:
                        return _alarmTypeBrushes.AlarmCategory1BlinkingBrush;                        
                }
            }
            else // Acked
            {
                switch (GetActiveAlarmsMaxCategory())
                {
                    case 0:
                        return _alarmTypeBrushes.AlarmCategory0Brush;                        
                    case 1:
                        return _alarmTypeBrushes.AlarmCategory1Brush;                        
                    case 2:
                        return _alarmTypeBrushes.AlarmCategory2Brush;                        
                    default:
                        return _alarmTypeBrushes.AlarmCategory1Brush;                        
                }
            }
        }

        #endregion

        #region private fields
        
        private readonly string _tag;
        public readonly IDataAccessProvider _dataAccessProvider;
        private readonly AlarmTypeBrushesBase _alarmTypeBrushes;

        #endregion
    }
}