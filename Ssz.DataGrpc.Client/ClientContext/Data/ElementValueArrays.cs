using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataGrpc.Server
{
    public sealed partial class ElementValuesCollection
    {
        #region public functions

        public void Add(ElementValuesCollection elementValuesCollection)
        {
            Guid = elementValuesCollection.Guid;
            NextCollectionGuid = elementValuesCollection.NextCollectionGuid;

            DoubleAliases.Add(elementValuesCollection.DoubleAliases);
            DoubleStatusCodes.Add(elementValuesCollection.DoubleStatusCodes);
            DoubleTimestamps.Add(elementValuesCollection.DoubleTimestamps);
            DoubleValues.Add(elementValuesCollection.DoubleValues);

            UintAliases.Add(elementValuesCollection.UintAliases);
            UintStatusCodes.Add(elementValuesCollection.UintStatusCodes);
            UintTimestamps.Add(elementValuesCollection.UintTimestamps);
            UintValues.Add(elementValuesCollection.UintValues);

            ObjectAliases.Add(elementValuesCollection.ObjectAliases);
            ObjectStatusCodes.Add(elementValuesCollection.ObjectStatusCodes);
            ObjectTimestamps.Add(elementValuesCollection.ObjectTimestamps);
            ObjectValues = Google.Protobuf.ByteString.CopyFrom(
                ObjectValues.ToByteArray().Concat(elementValuesCollection.ObjectValues.ToByteArray()).ToArray()
                );
        }

        #endregion        
    }
}
