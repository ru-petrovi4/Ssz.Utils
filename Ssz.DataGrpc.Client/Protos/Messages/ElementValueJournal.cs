using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataGrpc.ServerBase
{
    public sealed partial class ElementValuesJournal
    {
        #region public functions

        public void CombineWith(ElementValuesJournal nextElementValuesJournal)
        {
            DoubleValueStatusCodes.Add(nextElementValuesJournal.DoubleValueStatusCodes);
            DoubleTimestamps.Add(nextElementValuesJournal.DoubleTimestamps);
            DoubleValues.Add(nextElementValuesJournal.DoubleValues);

            UintValueStatusCodes.Add(nextElementValuesJournal.UintValueStatusCodes);
            UintTimestamps.Add(nextElementValuesJournal.UintTimestamps);
            UintValues.Add(nextElementValuesJournal.UintValues);
        }

        #endregion
    }
}
