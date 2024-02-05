using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.Client
{
    public static class ProtobufHelper
    {
        public static ReadOnlyMemory<byte> Combine(List<ByteString> requestByteStrings)
        {
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
        public static List<ByteString> SplitForCorrectGrpcMessageSize(ReadOnlyMemory<byte> bytes)
        {
            if (bytes.Length == 0)
                return new List<ByteString> { ByteString.Empty };

            var result = new List<ByteString>();
            
            int position = 0;
            while (position < bytes.Length)
            {
                int length = Math.Min(bytes.Length - position, Constants.MaxReplyObjectSize);
                ByteString byteString = UnsafeByteOperations.UnsafeWrap(bytes.Slice(position, length));
                position += length;
                result.Add(byteString);                
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
