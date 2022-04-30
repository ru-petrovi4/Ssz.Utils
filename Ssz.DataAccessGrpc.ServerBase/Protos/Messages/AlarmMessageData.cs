using Google.Protobuf.WellKnownTypes;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.ServerBase
{
    public sealed partial class AlarmMessageData
    {
        #region construction and destruction

        public AlarmMessageData(Ssz.Utils.DataAccess.AlarmMessageData alarmMessageData)
        {
            AlarmState = (uint)alarmMessageData.AlarmState;
            AlarmStateChange = alarmMessageData.AlarmStateChange;
            if (alarmMessageData.TimeLastActive is not null)
            {
                TimeLastActive = Timestamp.FromDateTime(alarmMessageData.TimeLastActive.Value);
            }
        }

        #endregion
    }
}
