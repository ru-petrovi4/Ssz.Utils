using Google.Protobuf;
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

            if (nextElementValuesJournal.ObjectValues.Memory.Length > 0)
            {
                ObjectStatusCodes.Add(nextElementValuesJournal.ObjectStatusCodes);
                ObjectTimestamps.Add(nextElementValuesJournal.ObjectTimestamps);                
                var result = new Byte[ObjectValues.Memory.Length + nextElementValuesJournal.ObjectValues.Memory.Length];
                ObjectValues.Memory.CopyTo(new Memory<byte>(result, 0, ObjectValues.Memory.Length));
                nextElementValuesJournal.ObjectValues.Memory.CopyTo(new Memory<byte>(result, ObjectValues.Memory.Length, nextElementValuesJournal.ObjectValues.Memory.Length));                
                ObjectValues = UnsafeByteOperations.UnsafeWrap(result);
            }
        }

        #endregion
    }
}
