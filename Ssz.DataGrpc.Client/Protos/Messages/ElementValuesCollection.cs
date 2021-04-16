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

        public void CombineWith(ElementValuesCollection nextElementValuesCollection)
        {
            Guid = nextElementValuesCollection.Guid;
            NextCollectionGuid = nextElementValuesCollection.NextCollectionGuid;

            DoubleAliases.Add(nextElementValuesCollection.DoubleAliases);
            DoubleStatusCodes.Add(nextElementValuesCollection.DoubleStatusCodes);
            DoubleTimestamps.Add(nextElementValuesCollection.DoubleTimestamps);
            DoubleValues.Add(nextElementValuesCollection.DoubleValues);

            UintAliases.Add(nextElementValuesCollection.UintAliases);
            UintStatusCodes.Add(nextElementValuesCollection.UintStatusCodes);
            UintTimestamps.Add(nextElementValuesCollection.UintTimestamps);
            UintValues.Add(nextElementValuesCollection.UintValues);

            ObjectAliases.Add(nextElementValuesCollection.ObjectAliases);
            ObjectStatusCodes.Add(nextElementValuesCollection.ObjectStatusCodes);
            ObjectTimestamps.Add(nextElementValuesCollection.ObjectTimestamps);
            ObjectValues = Google.Protobuf.ByteString.CopyFrom(
                ObjectValues.Concat(nextElementValuesCollection.ObjectValues).ToArray()
                );
        }

        #endregion        
    }
}
