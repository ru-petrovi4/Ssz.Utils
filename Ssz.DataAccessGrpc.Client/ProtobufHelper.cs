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
        /// <summary>
        ///     Result list count >= 1
        /// </summary>
        /// <returns></returns>
        public static List<ByteString> SplitForCorrectGrpcMessageSize(byte[]? bytes)
        {
            if (bytes is null || bytes.Length == 0)
                return new List<ByteString> { ByteString.Empty };

            var result = new List<ByteString>();
            
            int position = 0;
            while (position < bytes.Length)
            {
                int length = Math.Min(bytes.Length - position, Constants.MaxReplyObjectSize);
                ByteString byteString = UnsafeByteOperations.UnsafeWrap(new ReadOnlyMemory<byte>(bytes, position, length));
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
