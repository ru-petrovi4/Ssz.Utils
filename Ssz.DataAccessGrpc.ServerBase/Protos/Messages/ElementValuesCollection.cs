using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Ssz.Utils.DataAccess;
using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.ServerBase
{    
    public sealed partial class ElementValuesCollection
    {
        #region public functions

        public void CombineWith(ElementValuesCollection nextElementValuesCollection)
        {
            Guid = nextElementValuesCollection.Guid;
            NextCollectionGuid = nextElementValuesCollection.NextCollectionGuid;

            DoubleAliases.Add(nextElementValuesCollection.DoubleAliases);
            DoubleValues.Add(nextElementValuesCollection.DoubleValues);
            DoubleValueStatusCodes.Add(nextElementValuesCollection.DoubleValueStatusCodes);
            DoubleTimestamps.Add(nextElementValuesCollection.DoubleTimestamps);            

            UintAliases.Add(nextElementValuesCollection.UintAliases);
            UintValues.Add(nextElementValuesCollection.UintValues);
            UintValueStatusCodes.Add(nextElementValuesCollection.UintValueStatusCodes);
            UintTimestamps.Add(nextElementValuesCollection.UintTimestamps);            

            ObjectAliases.Add(nextElementValuesCollection.ObjectAliases);
            ObjectValues = UnsafeByteOperations.UnsafeWrap(
                ObjectValues.Concat(nextElementValuesCollection.ObjectValues).ToArray()
                );
            ObjectValueStatusCodes.Add(nextElementValuesCollection.ObjectValueStatusCodes);
            ObjectTimestamps.Add(nextElementValuesCollection.ObjectTimestamps);            
        }

        /// <summary>
        ///     Result count >= 1
        /// </summary>
        /// <returns></returns>
        public List<ElementValuesCollection> SplitForCorrectGrpcMessageSize()
        {
            if (GetSize() < Constants.MaxReplyObjectSize)
            {
                return new List<ElementValuesCollection> { this };
            }

            var result = new List<ElementValuesCollection>();
                        
            ElementValuesCollection? prevResultElementValuesCollection = null;
            int index = 0;
            int count = GetCount();
            while (index < count)
            {
                var resultElementValuesCollection = new ElementValuesCollection();
                if (prevResultElementValuesCollection is not null)
                {
                    string guid = System.Guid.NewGuid().ToString();
                    prevResultElementValuesCollection.NextCollectionGuid = guid;
                    resultElementValuesCollection.Guid = guid;
                }

                int replyObjectSize = 0;
                while (index < count && replyObjectSize < Constants.MaxReplyObjectSize)
                {
                    int localIndex = index;                    
                    if (localIndex < DoubleTimestamps.Count)
                    {
                        resultElementValuesCollection.DoubleAliases.Add(DoubleAliases[localIndex]);
                        resultElementValuesCollection.DoubleValues.Add(DoubleValues[localIndex]);
                        resultElementValuesCollection.DoubleValueStatusCodes.Add(DoubleValueStatusCodes[localIndex]);
                        resultElementValuesCollection.DoubleTimestamps.Add(DoubleTimestamps[localIndex]);
                        replyObjectSize += sizeof(uint) + sizeof(double) + sizeof(uint) + 8;
                        index += 1;
                        continue;
                    }

                    localIndex = index - DoubleTimestamps.Count;
                    if (localIndex < UintTimestamps.Count)
                    {
                        resultElementValuesCollection.UintAliases.Add(UintAliases[localIndex]);
                        resultElementValuesCollection.UintValues.Add(UintValues[localIndex]);
                        resultElementValuesCollection.UintValueStatusCodes.Add(UintValueStatusCodes[localIndex]);
                        resultElementValuesCollection.UintTimestamps.Add(UintTimestamps[localIndex]);
                        replyObjectSize += sizeof(uint) + sizeof(uint) + sizeof(uint) + 8;
                        index += 1;
                        continue;
                    }

                    localIndex = index - DoubleTimestamps.Count - UintTimestamps.Count;
                    if (localIndex < ObjectTimestamps.Count)
                    {
                        resultElementValuesCollection.ObjectAliases.Add(ObjectAliases[localIndex]);                        
                        resultElementValuesCollection.ObjectValueStatusCodes.Add(ObjectValueStatusCodes[localIndex]);
                        resultElementValuesCollection.ObjectTimestamps.Add(ObjectTimestamps[localIndex]);
                        replyObjectSize += sizeof(uint) + sizeof(uint) + 8;
                        index += 1;
                        continue;
                    }

                    localIndex = index - DoubleTimestamps.Count - UintTimestamps.Count - ObjectTimestamps.Count;
                    int bytesCount = Constants.MaxReplyObjectSize - replyObjectSize;
                    resultElementValuesCollection.ObjectValues = UnsafeByteOperations.UnsafeWrap(
                                ObjectValues.Skip(localIndex).Take(bytesCount).ToArray()
                            );
                    replyObjectSize += resultElementValuesCollection.ObjectValues.Length;
                    index += resultElementValuesCollection.ObjectValues.Length;
                }

                result.Add(resultElementValuesCollection);
                prevResultElementValuesCollection = resultElementValuesCollection;
            }            

            return result;
        }

        #endregion

        #region private functions

        private int GetSize()
        {
            int size = 0;

            size += DoubleTimestamps.Count * (sizeof(uint) + sizeof(double) + sizeof(uint) + 8);

            size += UintTimestamps.Count * (sizeof(uint) + sizeof(uint) + sizeof(uint) + 8);

            size += ObjectTimestamps.Count * (sizeof(uint) + sizeof(uint) + 8);

            size += ObjectValues.Length;

            return size;
        }

        private int GetCount()
        {
            int count = 0;

            count += DoubleTimestamps.Count;

            count += UintTimestamps.Count;

            count += ObjectTimestamps.Count;

            count += ObjectValues.Length;

            return count;
        }

        #endregion
    }
}
