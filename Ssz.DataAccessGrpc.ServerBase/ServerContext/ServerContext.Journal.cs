using Grpc.Core;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.ServerBase
{    
    public partial class ServerContext        
    {
        #region internal functions

        /// <summary>
        /// 
        /// </summary>
        /// <param name="listServerAlias"></param>
        /// <param name="firstTimeStampUtc"></param>
        /// <param name="secondTimeStampUtc"></param>
        /// <param name="numValuesPerAlias"></param>
        /// <param name="calculation"></param>
        /// <param name="params_"></param>
        /// <param name="serverAliases"></param>
        /// <returns></returns>
        /// <exception cref="RpcException"></exception>
        public async Task<byte[]> ReadElementValuesJournalsAsync(
            uint listServerAlias, 
            DateTime firstTimeStampUtc,
            DateTime secondTimeStampUtc,
            uint numValuesPerAlias,
            Ssz.Utils.DataAccess.TypeId calculation,
            CaseInsensitiveDictionary<string?> params_,
            List<uint> serverAliases)
        {
            ServerListRoot? serverList;

            if (!_listsManager.TryGetValue(listServerAlias, out serverList))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Incorrect listServerAlias."));
            }

            var reply = await serverList.ReadElementValuesJournalsAsync(
                    firstTimeStampUtc,
                    secondTimeStampUtc,
                    numValuesPerAlias,
                    calculation,
                    params_,
                    serverAliases
                );

            byte[] bytes;
            using (var memoryStream = new MemoryStream(1024))
            {
                using (var writer = new SerializationWriter(memoryStream))
                {
                    using (writer.EnterBlock(1))
                    {
                        writer.WriteArrayOfOwnedDataSerializable(reply, null);                        
                    }
                }
                bytes = memoryStream.ToArray();
            }
            return bytes;            
        }

        public async Task<EventMessagesCallbackMessage?> ReadEventMessagesJournalAsync(
            uint listServerAlias,
            DateTime firstTimeStampUtc,
            DateTime secondTimeStampUtc,            
            CaseInsensitiveDictionary<string?> params_)
        {
            ServerListRoot? serverList;

            if (!_listsManager.TryGetValue(listServerAlias, out serverList))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Incorrect listServerAlias."));
            }

            return await serverList.ReadEventMessagesJournalAsync(
                    firstTimeStampUtc,
                    secondTimeStampUtc,
                    params_);            
        }

        #endregion
    }
}