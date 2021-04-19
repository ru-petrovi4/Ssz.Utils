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

        public List<ElementValuesCollection> SplitForCorrectGrpcMessageSize()
        {
            if (DoubleValues.Count < MaxArrayLength && UintValues.Count < MaxArrayLength && ObjectValues.Length < MaxByteStringLength)
            {
                return new List<ElementValuesCollection> { this };
            }

            var result = new List<ElementValuesCollection>();

            int doubleIndex = 0;
            int uintIndex = 0;
            int objectIndex = 0;
            int objectByteIndex = 0;
            ElementValuesCollection? prevElementValuesCollection = null;
            while (doubleIndex < DoubleAliases.Count ||
                uintIndex < UintAliases.Count ||
                objectIndex < ObjectAliases.Count ||
                objectByteIndex < ObjectValues.Length)
            {
                var elementValuesCollection = new ElementValuesCollection();
                if (prevElementValuesCollection != null)
                {
                    string guid = System.Guid.NewGuid().ToString();
                    prevElementValuesCollection.NextCollectionGuid = guid;
                    elementValuesCollection.Guid = guid;
                }

                elementValuesCollection.DoubleAliases.AddRange(DoubleAliases.Skip(doubleIndex).Take(MaxArrayLength));
                elementValuesCollection.DoubleStatusCodes.AddRange(DoubleStatusCodes.Skip(doubleIndex).Take(MaxArrayLength));
                elementValuesCollection.DoubleTimestamps.AddRange(DoubleTimestamps.Skip(doubleIndex).Take(MaxArrayLength));
                elementValuesCollection.DoubleValues.AddRange(DoubleValues.Skip(doubleIndex).Take(MaxArrayLength));

                elementValuesCollection.UintAliases.AddRange(UintAliases.Skip(uintIndex).Take(MaxArrayLength));
                elementValuesCollection.UintStatusCodes.AddRange(UintStatusCodes.Skip(uintIndex).Take(MaxArrayLength));
                elementValuesCollection.UintTimestamps.AddRange(UintTimestamps.Skip(uintIndex).Take(MaxArrayLength));
                elementValuesCollection.UintValues.AddRange(UintValues.Skip(uintIndex).Take(MaxArrayLength));

                elementValuesCollection.ObjectAliases.AddRange(ObjectAliases.Skip(objectIndex).Take(MaxArrayLength));
                elementValuesCollection.ObjectStatusCodes.AddRange(ObjectStatusCodes.Skip(objectIndex).Take(MaxArrayLength));
                elementValuesCollection.ObjectTimestamps.AddRange(ObjectTimestamps.Skip(objectIndex).Take(MaxArrayLength));
                elementValuesCollection.ObjectValues = Google.Protobuf.ByteString.CopyFrom(
                    ObjectValues.Skip(objectByteIndex).Take(MaxByteStringLength).ToArray());

                result.Add(elementValuesCollection);
                doubleIndex += MaxArrayLength;
                uintIndex += MaxArrayLength;
                objectIndex += MaxArrayLength;
                objectByteIndex += MaxByteStringLength;
                prevElementValuesCollection = elementValuesCollection;
            }

            return result;
        }

        #endregion

        #region private fields

        private const int MaxArrayLength = 1024;

        private const int MaxByteStringLength = 1024 * 1024;

        #endregion   
    }
}
