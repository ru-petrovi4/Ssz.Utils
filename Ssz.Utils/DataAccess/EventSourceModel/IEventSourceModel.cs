using System;

namespace Ssz.Utils.DataAccess
{
    public interface IEventSourceModel
    {
        /// <summary>
        ///     
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="area">Can be compound area in format 'ROOT_AREA/CHILD_AREA'</param>
        /// <returns></returns>
        EventSourceObject GetOrCreateEventSourceObject(string tagName, string? area = null);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="area">Can be compound area in format 'ROOT_AREA/CHILD_AREA'</param>
        /// <returns></returns>
        EventSourceArea GetOrCreateEventSourceArea(string area);

        /// <summary>
        ///     alarmConditionType != Normal
        ///     Returns true if active or unacked state of any condition changed.
        /// </summary>        
        /// <param name="eventSourceObject"></param>
        /// <param name="alarmCondition"></param>
        /// <param name="categoryId"></param>
        /// <param name="active"></param>
        /// <param name="unacked"></param>
        /// <param name="occurrenceTimeUtc"></param>
        /// <param name="alarmConditionTypeChanged"></param>
        /// <param name="unackedChanged"></param>
        /// <returns>
        ///     true if active or unacked state of any condition changed
        ///     false if the alarm state remains the same
        /// </returns>
        bool ProcessEventSourceObject(EventSourceObject eventSourceObject, AlarmConditionType alarmCondition,
            uint categoryId, bool active, bool unacked, DateTime occurrenceTimeUtc, out bool alarmConditionTypeChanged,
            out bool unackedChanged);

        void Clear();
    }
}