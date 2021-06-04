using Ssz.Utils.DataAccess;
using System;

namespace Ssz.Utils.EventSourceModel
{
    public class ConditionState
    {
        #region public functions

        public bool Active;
        public bool Unacked;
        public uint CategoryId;
        public DateTime ActiveOccurrenceTime;
        public AlarmInfoViewModelBase? LastAlarmInfoViewModel;

        #endregion
    }
}