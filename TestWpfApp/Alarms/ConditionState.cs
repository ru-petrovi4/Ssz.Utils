using System;

namespace Ssz.WpfHmi.Common.ModelData.Events
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