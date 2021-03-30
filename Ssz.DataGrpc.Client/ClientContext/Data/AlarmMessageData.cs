using Ssz.Utils.DataSource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataGrpc.Server
{
    public sealed partial class AlarmMessageData
    {
        #region public functions

        public Ssz.Utils.DataSource.AlarmMessageData ToAlarmMessageData()
        {
            var alarmMessageData = new Ssz.Utils.DataSource.AlarmMessageData();
            alarmMessageData.AlarmState = (AlarmState)AlarmState;
            alarmMessageData.AlarmStateChange = AlarmStateChange;
            if (OptionalTimeLastActiveCase == OptionalTimeLastActiveOneofCase.TimeLastActive)
            {
                alarmMessageData.TimeLastActive = TimeLastActive.ToDateTime();
            }            
            return alarmMessageData;
        }

        #endregion
    }
}
