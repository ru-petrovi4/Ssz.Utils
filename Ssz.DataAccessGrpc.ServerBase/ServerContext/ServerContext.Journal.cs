using Grpc.Core;
using Ssz.Utils;
using System;
using System.Collections.Generic;
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
        internal async Task<ElementValuesJournalsCollection> ReadElementValuesJournalsAsync(
            uint listServerAlias, 
            DateTime firstTimeStampUtc,
            DateTime secondTimeStampUtc,
            uint numValuesPerAlias,
            TypeId calculation,
            CaseInsensitiveDictionary<string?> params_,
            List<uint> serverAliases)
        {
            ServerListRoot? serverList;

            if (!_listsManager.TryGetValue(listServerAlias, out serverList))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Incorrect listServerAlias."));
            }

            if (_pendingReadElementValuesJournalsCollection.Count == 0)
            {
                var elementValuesJournalsCollection = await serverList.ReadElementValuesJournalsAsync(
                    firstTimeStampUtc,
                    secondTimeStampUtc,
                    numValuesPerAlias,
                    calculation,
                    params_,
                    serverAliases
                );

                _pendingReadElementValuesJournalsCollection = elementValuesJournalsCollection.SplitForCorrectGrpcMessageSize();
            }

            if (_pendingReadElementValuesJournalsCollection.Count > 0)
            {                
                return _pendingReadElementValuesJournalsCollection.Dequeue();
            }
            else
            {
                return new ElementValuesJournalsCollection();
            }            
        }

        internal async Task<EventMessagesCollection> ReadEventMessagesJournalAsync(
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

            if (_pendingReadEventMessagesCallbacksQueue.Count == 0)
            {
                ServerContext.EventMessagesCallbackMessage eventMessagesCallbackMessage = await serverList.ReadEventMessagesJournalAsync(
                    firstTimeStampUtc,
                    secondTimeStampUtc,
                    params_);

                _pendingReadEventMessagesCallbacksQueue = eventMessagesCallbackMessage.SplitForCorrectGrpcMessageSize();
            }        

            if (_pendingReadEventMessagesCallbacksQueue.Count > 0)
            {
                return _pendingReadEventMessagesCallbacksQueue.Dequeue().EventMessagesCollection;
            }
            else
            {
                return new EventMessagesCollection();
            }
        }

        #endregion

        #region private fields

        private Queue<ElementValuesJournalsCollection> _pendingReadElementValuesJournalsCollection = new();

        private Queue<EventMessagesCallback> _pendingReadEventMessagesCallbacksQueue = new Queue<EventMessagesCallback>();

        #endregion
    }
}