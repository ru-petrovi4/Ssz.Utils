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

            ObjectAliases.Add(nextElementValuesCollection.ObjectAliases);
            var result = new Byte[ObjectValues.Memory.Length + nextElementValuesCollection.ObjectValues.Memory.Length];
            ObjectValues.Memory.CopyTo(new Memory<byte>(result, 0, ObjectValues.Memory.Length));
            nextElementValuesCollection.ObjectValues.Memory.CopyTo(new Memory<byte>(result, ObjectValues.Memory.Length, nextElementValuesCollection.ObjectValues.Memory.Length));
            ObjectValues = UnsafeByteOperations.UnsafeWrap(result);
            ObjectStatusCodes.Add(nextElementValuesCollection.ObjectStatusCodes);
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
                    if (localIndex < ObjectTimestamps.Count)
                    {
                        resultElementValuesCollection.ObjectAliases.Add(ObjectAliases[localIndex]);                        
                        resultElementValuesCollection.ObjectStatusCodes.Add(ObjectStatusCodes[localIndex]);
                        resultElementValuesCollection.ObjectTimestamps.Add(ObjectTimestamps[localIndex]);
                        replyObjectSize += sizeof(uint) + sizeof(uint) + 8;
                        index += 1;
                        continue;
                    }

                    localIndex = index - ObjectTimestamps.Count;
                    int bytesCount = Constants.MaxReplyObjectSize - replyObjectSize;
                    var span = ObjectValues.Memory.Span;
                    int length = Math.Min(span.Length - localIndex, bytesCount);
                    resultElementValuesCollection.ObjectValues = Google.Protobuf.ByteString.CopyFrom(
                        span.Slice(localIndex, length));
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

            size += ObjectTimestamps.Count * (sizeof(uint) + sizeof(uint) + 8);

            size += ObjectValues.Length;

            return size;
        }

        private int GetCount()
        {
            int count = 0;            

            count += ObjectTimestamps.Count;

            count += ObjectValues.Length;

            return count;
        }

        #endregion
    }
}
