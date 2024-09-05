using Ssz.Utils.DataAccess;
using System;

namespace Ssz.Utils.DataAccess
{
    public class AlarmConditionState
    {
        #region construction and destruction

        public AlarmConditionState(AlarmConditionType alarmCondition)
        {
            AlarmCondition = alarmCondition;
        }

        #endregion

        #region public functions

        public readonly AlarmConditionType AlarmCondition;

        public bool Active;
        public bool Unacked;
        public uint CategoryId;
        public uint Priority;
        public DateTime ActiveOccurrenceTimeUtc;
        public AlarmInfoViewModelBase? LastAlarmInfoViewModel;

        /// <summary>
        ///     You can use this property as temp storage.
        /// </summary>
        public object? Obj;

        #endregion
    }
}