using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.Common
{
    public static class ProtobufHelper
    {
        public static ReadOnlyMemory<byte> Combine(List<DataChunk> dataChunks)
        {
            if (dataChunks.Count == 0)
                return ReadOnlyMemory<byte>.Empty;
            if (String.Equals(dataChunks[0].Compression, @"DeflateStream", StringComparison.InvariantCultureIgnoreCase))
            {
                using var input = new MemoryStream(dataChunks.Sum(dc => dc.Bytes.Length));
                foreach (var dataChunk in dataChunks)
                {
#if NET6_0_OR_GREATER
                    input.Write(dataChunk.Bytes.Span);
#else
                    var bytesArray = dataChunk.Bytes.ToArray();
                    input.Write(bytesArray, 0, bytesArray.Length);
#endif                    
                }
                input.Position = 0;
                using var decompressionStream = new DeflateStream(input, CompressionMode.Decompress);
                using var result = new MemoryStream();
                decompressionStream.CopyTo(result);
                return result.ToArray();
            }
            else
            {                
                if (dataChunks.Count == 1)
                {
                    return dataChunks[0].Bytes.Memory;
                }
                else
                {
                    var bytesArray = new byte[dataChunks.Sum(dc => dc.Bytes.Length)];
                    int position = 0;
                    foreach (var dataChunk in dataChunks)
                    {
                        dataChunk.Bytes.CopyTo(bytesArray, position);
                        position += dataChunk.Bytes.Length;
                    }
                    return bytesArray;
                }
            }
        }

        public static EventMessagesCollection Combine(List<EventMessagesCollection> eventMessagesCollections)
        {
            if (eventMessagesCollections.Count == 0)
                return new EventMessagesCollection();
            if (eventMessagesCollections.Count == 1)
            {
                return eventMessagesCollections[0];
            }
            else
            {
                var eventMessagesCollection = new EventMessagesCollection();                
                foreach (var emc in eventMessagesCollections)
                {
                    eventMessagesCollection.EventMessages.Add(emc.EventMessages);
                    
                    eventMessagesCollection.CommonFieldsOrdered.Add(emc.CommonFieldsOrdered);

                    // Obsolete for compatibility only
                    foreach (var kvp in emc.CommonFields)
                    {
                        eventMessagesCollection.CommonFields[kvp.Key] = kvp.Value;
                    }
                }
                return eventMessagesCollection;
            }
        }

        /// <summary>
        ///     Result list count >= 1
        /// </summary>
        /// <returns></returns>
        public static List<DataChunk> SplitForCorrectGrpcMessageSize(ReadOnlyMemory<byte> bytes)
        {
            if (bytes.Length == 0)
                return new List<DataChunk> { new DataChunk { Bytes = ByteString.Empty } };

            string compression = @"";
            if (bytes.Length > 1024 * 1024) // 1M
            {
                using var output = new MemoryStream();
                using (var compressionStream = new DeflateStream(output, CompressionLevel.Fastest))
                {
#if NET6_0_OR_GREATER
                    compressionStream.Write(bytes.Span);
#else
                    var bytesArray = bytes.ToArray();
                    compressionStream.Write(bytesArray, 0, bytesArray.Length);
#endif
                }
                bytes = output.ToArray();
                compression = @"DeflateStream";
            }

            var result = new List<DataChunk>();
            
            int position = 0;
            while (position < bytes.Length)
            {
                DataChunk dataChunk = new()
                {
                    Compression = compression,
                };
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

        public static List<EventMessagesCollection> SplitForCorrectGrpcMessageSize(List<EventMessage> eventMessages, CaseInsensitiveOrderedDictionary<string?>? commonFields)
        {
            var result = new List<EventMessagesCollection>();

            int index = 0;            
            while (index < eventMessages.Count)
            {
                var eventMessagesCollection = new EventMessagesCollection();
                if (index == 0 && commonFields is not null)
                {
                    foreach (var kvp in commonFields)
                    {
                        eventMessagesCollection.CommonFieldsOrdered.Add(new Field()
                        {
                            Name = kvp.Key,
                            Value = kvp.Value is not null ? new NullableString { Data = kvp.Value } : new NullableString { Null = NullValue.NullValue }
                        });

                        // Obsolete for compatibility only
                        eventMessagesCollection.CommonFields.Add(kvp.Key,
                            kvp.Value is not null ? new NullableString { Data = kvp.Value } : new NullableString { Null = NullValue.NullValue });
                    }
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


//public static ReadOnlyMemory<byte> Combine(List<ByteString> requestByteStrings)
//{
//    if (requestByteStrings.Count == 0)
//        return ReadOnlyMemory<byte>.Empty;
//    if (requestByteStrings.Count == 1)
//        return requestByteStrings[0].Memory;
//    var bytes = new byte[requestByteStrings.Sum(bs => bs.Length)];
//    int position = 0;
//    foreach (var byteString in requestByteStrings)
//    {
//        byteString.CopyTo(bytes, position);
//        position += byteString.Length;
//    }
//    return bytes;
//}