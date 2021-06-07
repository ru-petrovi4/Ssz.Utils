using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Utils.DataAccess;

namespace Ssz.Utils.EventSourceModel
{
    /// <summary>
    /// A class to handle Alarm and Event notifications
    /// </summary>
    public class EventSourceObject
    {
        #region construction and destruction

        public EventSourceObject(string tag, IDataAccessProvider dataAccessProvider)
        {
            Tag = tag;
            _dataAccessProvider = dataAccessProvider;            
        }

        #endregion

        #region public functions       
        
        public string Tag { get; }

        /// <summary>
        ///     You can use this property as temp storage.
        /// </summary>
        public object? Obj { get; set; }

        public event Action<ValueStatusTimestamp>? AlarmUnackedChanged;
        public event Action<ValueStatusTimestamp>? AlarmCategoryChanged;        
        public event Action<ValueStatusTimestamp>? AlarmConditionTypeChanged;

        public Dictionary<AlarmCondition, ConditionState> AlarmConditions { get; } = new();

        public ConditionState NormalCondition { get; } = new ConditionState();

        public CaseInsensitiveDictionary<EventSourceArea> EventSourceAreas { get; } = new();
        
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
            var alarmUnackedChanged = AlarmUnackedChanged;
            if (alarmUnackedChanged != null)
            {
                if (_dataAccessProvider.IsConnected)
                {
                    bool anyUnacked = AnyUnacked();
                    alarmUnackedChanged(new ValueStatusTimestamp(new Any(anyUnacked), StatusCodes.Good, DateTime.UtcNow));
                }
                else
                {
                    alarmUnackedChanged(new ValueStatusTimestamp());
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
            var alarmCategoryChanged = AlarmCategoryChanged;
            if (alarmCategoryChanged != null)
            {
                if (_dataAccessProvider.IsConnected)
                {
                    uint maxCategory = GetActiveAlarmsMaxCategory();
                    alarmCategoryChanged(new ValueStatusTimestamp(new Any(maxCategory), StatusCodes.Good, DateTime.UtcNow));
                }
                else
                {
                    alarmCategoryChanged(new ValueStatusTimestamp());
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

        public void NotifyAlarmConditionTypeSubscribers()
        {
            var alarmConditionTypeChanged = AlarmConditionTypeChanged;
            if (alarmConditionTypeChanged != null)
            {
                if (_dataAccessProvider.IsConnected)
                {
                    AlarmCondition alarmConditionType = GetAlarmConditionType();
                    alarmConditionTypeChanged(new ValueStatusTimestamp(new Any(alarmConditionType), StatusCodes.Good, DateTime.UtcNow));
                }
                else
                {
                    alarmConditionTypeChanged(new ValueStatusTimestamp());
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

        #region private fields        
        
        public readonly IDataAccessProvider _dataAccessProvider;        

        #endregion
    }
}