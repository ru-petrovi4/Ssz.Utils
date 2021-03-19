using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataGrpc.Server
{
    public sealed partial class ElementValueArrays
    {
        #region public functions

        public void Add(ElementValueArrays elementValueArrays)
        {
            Guid = elementValueArrays.Guid;
            NextArraysGuid = elementValueArrays.NextArraysGuid;

            DoubleAliases.Add(elementValueArrays.DoubleAliases);
            DoubleStatusCodes.Add(elementValueArrays.DoubleStatusCodes);
            DoubleTimestamps.Add(elementValueArrays.DoubleTimestamps);
            DoubleValues.Add(elementValueArrays.DoubleValues);

            UintAliases.Add(elementValueArrays.UintAliases);
            UintStatusCodes.Add(elementValueArrays.UintStatusCodes);
            UintTimestamps.Add(elementValueArrays.UintTimestamps);
            UintValues.Add(elementValueArrays.UintValues);

            ObjectAliases.Add(elementValueArrays.ObjectAliases);
            ObjectStatusCodes.Add(elementValueArrays.ObjectStatusCodes);
            ObjectTimestamps.Add(elementValueArrays.ObjectTimestamps);
            ObjectValues = Google.Protobuf.ByteString.CopyFrom(
                ObjectValues.ToByteArray().Concat(elementValueArrays.ObjectValues.ToByteArray()).ToArray()
                );
        }

        #endregion        
    }
}
