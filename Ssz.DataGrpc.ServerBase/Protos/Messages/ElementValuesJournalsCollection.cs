using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataGrpc.ServerBase
{
    public sealed partial class ElementValuesJournalsCollection
    {
        #region public functions

        /// <summary>
        ///     Result count >= 1
        /// </summary>
        /// <returns></returns>
        public Queue<ElementValuesJournalsCollection> SplitForCorrectGrpcMessageSize()
        {
            var result = new Queue<ElementValuesJournalsCollection>();

            if (GetSize() < Constants.MaxReplyObjectSize)
            {
                result.Enqueue(this);
                return result;
            }            
            
            ElementValuesJournalsCollection? prevResultElementValuesJournalsCollection = null;
            int index = 0;
            int count = GetCount();
            int elementValuesJournal_Index = 0;
            int finishedCount = 0;
            while (index < count)
            {
                var resultElementValuesJournalsCollection = new ElementValuesJournalsCollection();
                if (prevResultElementValuesJournalsCollection is not null)
                {
                    string guid = System.Guid.NewGuid().ToString();
                    prevResultElementValuesJournalsCollection.NextCollectionGuid = guid;
                    resultElementValuesJournalsCollection.Guid = guid;
                }

                int replyObjectSize = 0;
                while (index < count && replyObjectSize < Constants.MaxReplyObjectSize)
                {
                    ElementValuesJournal elementValuesJournal = ElementValuesJournals[elementValuesJournal_Index];

                    ElementValuesJournal resultElementValuesJournal;
                    if (elementValuesJournal_Index >= resultElementValuesJournalsCollection.ElementValuesJournals.Count)
                    {
                        foreach (var i in Enumerable.Range(0, elementValuesJournal_Index - resultElementValuesJournalsCollection.ElementValuesJournals.Count + 1))
                        {
                            resultElementValuesJournalsCollection.ElementValuesJournals.Add(new ElementValuesJournal());
                        }                       
                    }
                    resultElementValuesJournal = resultElementValuesJournalsCollection.ElementValuesJournals[elementValuesJournal_Index];

                    int localIndex = index - finishedCount;
                    if (localIndex < elementValuesJournal.DoubleTimestamps.Count)
                    {
                        resultElementValuesJournal.DoubleValues.Add(elementValuesJournal.DoubleValues[localIndex]);
                        resultElementValuesJournal.DoubleValueStatusCodes.Add(elementValuesJournal.DoubleValueStatusCodes[localIndex]);
                        resultElementValuesJournal.DoubleTimestamps.Add(elementValuesJournal.DoubleTimestamps[localIndex]);
                        replyObjectSize += sizeof(double) + sizeof(uint) + 8;
                        index += 1;
                        continue;
                    }

                    localIndex = index - elementValuesJournal.DoubleTimestamps.Count - finishedCount;
                    if (localIndex < elementValuesJournal.UintTimestamps.Count)
                    {
                        resultElementValuesJournal.UintValues.Add(elementValuesJournal.UintValues[localIndex]);
                        resultElementValuesJournal.UintValueStatusCodes.Add(elementValuesJournal.UintValueStatusCodes[localIndex]);
                        resultElementValuesJournal.UintTimestamps.Add(elementValuesJournal.UintTimestamps[localIndex]);
                        replyObjectSize += sizeof(uint) + sizeof(uint) + 8;
                        index += 1;
                        continue;
                    }

                    localIndex = index - elementValuesJournal.DoubleTimestamps.Count - elementValuesJournal.UintTimestamps.Count - finishedCount;
                    if (localIndex < elementValuesJournal.ObjectTimestamps.Count)
                    {                        
                        resultElementValuesJournal.ObjectValueStatusCodes.Add(elementValuesJournal.ObjectValueStatusCodes[localIndex]);
                        resultElementValuesJournal.ObjectTimestamps.Add(elementValuesJournal.ObjectTimestamps[localIndex]);
                        replyObjectSize += sizeof(uint) + 8;
                        index += 1;
                        continue;
                    }

                    if (elementValuesJournal.ObjectValues.Length > 0)
                    {
                        localIndex = index - elementValuesJournal.DoubleTimestamps.Count - elementValuesJournal.UintTimestamps.Count - elementValuesJournal.ObjectTimestamps.Count - finishedCount;
                        int bytesCount = Constants.MaxReplyObjectSize - replyObjectSize;                        
                        resultElementValuesJournal.ObjectValues = UnsafeByteOperations.UnsafeWrap(
                                    elementValuesJournal.ObjectValues.Skip(localIndex).Take(bytesCount).ToArray()
                                );
                        replyObjectSize += resultElementValuesJournal.ObjectValues.Length;
                        index += resultElementValuesJournal.ObjectValues.Length;

                        int remainingBytesCount = elementValuesJournal.ObjectValues.Length - localIndex - bytesCount;
                        if (remainingBytesCount > 0)
                            continue;
                    }

                    elementValuesJournal_Index += 1;
                    finishedCount = index;                    
                }

                result.Enqueue(resultElementValuesJournalsCollection);
                prevResultElementValuesJournalsCollection = resultElementValuesJournalsCollection;
            }

            return result;
        }

        #endregion

        #region private functions

        private int GetSize()
        {
            int size = 0;

            foreach (var elementValuesJournal in ElementValuesJournals)
            {
                size += elementValuesJournal.DoubleTimestamps.Count * (sizeof(double) + sizeof(uint) + 8);

                size += elementValuesJournal.UintTimestamps.Count * (sizeof(uint) + sizeof(uint) + 8);

                size += elementValuesJournal.ObjectTimestamps.Count * (sizeof(uint) + 8);

                size += elementValuesJournal.ObjectValues.Length;
            }            

            return size;
        }

        private int GetCount()
        {
            int count = 0;

            foreach (var elementValuesJournal in ElementValuesJournals)
            {
                count += elementValuesJournal.DoubleTimestamps.Count;

                count += elementValuesJournal.UintTimestamps.Count;

                count += elementValuesJournal.ObjectTimestamps.Count;

                count += elementValuesJournal.ObjectValues.Length;
            }

            return count;
        }

        #endregion
    }
}
