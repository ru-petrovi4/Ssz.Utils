using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.ServerBase
{
    public static class ProtobufHelper
    {
        public static ReadOnlyMemory<byte> Combine(List<ByteString> requestByteStrings)
        {
            if (requestByteStrings.Count == 0)
                return ReadOnlyMemory<byte>.Empty;
            if (requestByteStrings.Count == 1)
                return requestByteStrings[0].Memory;
            var bytes = new byte[requestByteStrings.Sum(bs => bs.Length)];
            int position = 0;
            foreach (var byteString in requestByteStrings)
            {
                byteString.CopyTo(bytes, position);
                position += byteString.Length;
            }
            return bytes;
        }

        /// <summary>
        ///     Result list count >= 1
        /// </summary>
        /// <returns></returns>
        public static List<DataChunk> SplitForCorrectGrpcMessageSize(ReadOnlyMemory<byte> bytes)
        {
            if (bytes.Length == 0)
                return new List<DataChunk> { new DataChunk { Bytes = ByteString.Empty } };

            var result = new List<DataChunk>();
            
            int position = 0;
            while (position < bytes.Length)
            {
                DataChunk dataChunk = new();
                int length;
                if (bytes.Length - position <= Constants.MaxReplyObjectSize)
                {
                    length = bytes.Length - position;
                }
                else
                {
                    length = Constants.MaxReplyObjectSize;
                    dataChunk.IsIncomplete = true;
                }
                dataChunk.Bytes = UnsafeByteOperations.UnsafeWrap(bytes.Slice(position, length));
                position += length;
                result.Add(dataChunk);                
            }

            return result;
        }

        public static List<EventMessagesCollection> SplitForCorrectGrpcMessageSize(List<EventMessage> eventMessages, CaseInsensitiveDictionary<string?>? commonFields)
        {
            var result = new List<EventMessagesCollection>();

            int index = 0;            
            while (index < eventMessages.Count)
            {
                var eventMessagesCollection = new EventMessagesCollection();
                if (commonFields is not null)
                {
                    foreach (var kvp in commonFields)
                        eventMessagesCollection.CommonFields.Add(kvp.Key,
                            kvp.Value is not null ? new NullableString { Data = kvp.Value } : new NullableString { Null = NullValue.NullValue });
                }
                int length;
                if (eventMessages.Count - index <= Constants.MaxEventMessagesCount)
                {
                    length = eventMessages.Count - index;                    
                }
                else
                {
                    length = Constants.MaxEventMessagesCount;
                    eventMessagesCollection.IsIncomplete = true;
                }
                eventMessagesCollection.EventMessages.AddRange(eventMessages.Skip(index).Take(length));
                index += length;
                result.Add(eventMessagesCollection);                
            }

            return result;
        }

        public static Timestamp ConvertToTimestamp(DateTime dateTimeUtc)
        {
            if (dateTimeUtc == default(DateTime))
                return new Timestamp();
            try
            {
                return Timestamp.FromDateTime(dateTimeUtc);
            }
            catch
            {
                return new Timestamp();
            }
        }
    }
}
