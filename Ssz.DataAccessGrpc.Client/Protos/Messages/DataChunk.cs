using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.ServerBase
{
    internal sealed partial class DataChunk
    {
        #region public functions

        public void CombineWith(DataChunk nextDataChunk)
        {
            Guid = nextDataChunk.Guid;
            NextDataChunkGuid = nextDataChunk.NextDataChunkGuid;

            var bytes = new Byte[Bytes.Memory.Length + nextDataChunk.Bytes.Memory.Length];
            Bytes.Memory.CopyTo(new Memory<byte>(bytes, 0, Bytes.Memory.Length));
            nextDataChunk.Bytes.Memory.CopyTo(new Memory<byte>(bytes, Bytes.Memory.Length, nextDataChunk.Bytes.Memory.Length));
            Bytes = UnsafeByteOperations.UnsafeWrap(bytes);
        }

        #endregion
    }
}
