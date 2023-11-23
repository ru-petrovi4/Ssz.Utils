using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.ServerBase
{
    internal sealed partial class ElementValuesJournal
    {
        #region public functions

        public void CombineWith(ElementValuesJournal nextElementValuesJournal)
        {
            DoubleStatusCodes.Add(nextElementValuesJournal.DoubleStatusCodes);
            DoubleTimestamps.Add(nextElementValuesJournal.DoubleTimestamps);
            DoubleValues.Add(nextElementValuesJournal.DoubleValues);

            UintStatusCodes.Add(nextElementValuesJournal.UintStatusCodes);
            UintTimestamps.Add(nextElementValuesJournal.UintTimestamps);
            UintValues.Add(nextElementValuesJournal.UintValues);
        }

        #endregion
    }
}
