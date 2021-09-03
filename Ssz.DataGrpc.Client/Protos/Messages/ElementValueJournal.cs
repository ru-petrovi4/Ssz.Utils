using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataGrpc.Server
{
    public sealed partial class ElementValueJournal
    {
        #region public functions

        public void CombineWith(ElementValueJournal nextElementValueJournal)
        {
            DoubleValueStatusCodes.Add(nextElementValueJournal.DoubleValueStatusCodes);
            DoubleTimestamps.Add(nextElementValueJournal.DoubleTimestamps);
            DoubleValues.Add(nextElementValueJournal.DoubleValues);

            UintValueStatusCodes.Add(nextElementValueJournal.UintValueStatusCodes);
            UintTimestamps.Add(nextElementValueJournal.UintTimestamps);
            UintValues.Add(nextElementValueJournal.UintValues);
        }

        #endregion
    }
}
