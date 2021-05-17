using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils.Wpf.EventSourceModel
{
    public enum AlarmCondition
    {
        None,
        Low,
        LowLow,
        High,
        HighHigh,
        PVLevel,
        DVLow,
        DVHigh,
        DigitalHigh,
        DigitalLow,
        NegativeRate,
        PositiveRate,
        OffNormal,
        ChangeOfState,
        CommandDisagree,
        CommandFail,
        Uncommanded,
        Trip,
        Interlock,
        AnswerbackHigh,
        AnswerbackLow,
        Other,
    }
}
