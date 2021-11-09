using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataGrpc.Server
{
    public sealed partial class PassthroughData
    {
        #region public functions        

        public List<PassthroughData> SplitForCorrectGrpcMessageSize()
        {
            if (Data.Length < MaxByteStringLength)
            {
                return new List<PassthroughData> { this };
            }

            var result = new List<PassthroughData>();

            int index = 0;
            PassthroughData? prevPassthroughData = null;
            while (index < Data.Length)
            {
                var passthroughData = new PassthroughData();
                if (prevPassthroughData is not null)
                {
                    string guid = System.Guid.NewGuid().ToString();
                    prevPassthroughData.NextGuid = guid;
                    passthroughData.Guid = guid;
                }
                
                passthroughData.Data = Google.Protobuf.ByteString.CopyFrom(
                    Data.Skip(index).Take(MaxByteStringLength).ToArray());

                result.Add(passthroughData);                
                index += MaxByteStringLength;
                prevPassthroughData = passthroughData;
            }

            return result;
        }

        #endregion

        #region private fields

        private const int MaxByteStringLength = 1024 * 1024;

        #endregion 
    }
}


//public void CombineWith(PassthroughData nextPassthroughData)
//{
//    Guid = nextPassthroughData.Guid;
//    NextGuid = nextPassthroughData.NextGuid;

//    Data = Google.Protobuf.ByteString.CopyFrom(
//        Data.Concat(nextPassthroughData.Data).ToArray()
//        );
//}