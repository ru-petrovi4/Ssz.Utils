using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.ServerBase
{
    public static class ProtobufHelper
    {
        public static byte[] Combine(List<ByteString> requestByteStrings)
        {
            // TODOP optimization needed.
            //if (requestByteStrings.Count == 1)
            //    return requestByteStrings[0].Memory;
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
        public static List<DataChunk> SplitForCorrectGrpcMessageSize(byte[] bytes)
        {
            if (bytes is null || bytes.Length == 0)
                return new List<DataChunk> { new DataChunk { Bytes = ByteString.Empty } };

            var result = new List<DataChunk>();

            DataChunk? prevDataChunk = null;
            int position = 0;
            while (position < bytes.Length)
            {
                DataChunk dataChunk = new();
                if (prevDataChunk is not null)
                    prevDataChunk.NextDataChunkGuid = dataChunk.Guid = System.Guid.NewGuid().ToString();

                int length = Math.Min(bytes.Length - position, Constants.MaxReplyObjectSize);
                dataChunk.Bytes = UnsafeByteOperations.UnsafeWrap(new ReadOnlyMemory<byte>(bytes, position, length));
                position += length;

                result.Add(dataChunk);
                prevDataChunk = dataChunk;
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
