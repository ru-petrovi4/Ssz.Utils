using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.ServerBase
{    
    public partial class ServerContext        
    {
        #region internal functions
        
        internal async Task PollElementValuesChangesAsync(uint listServerAlias, IServerStreamWriter<ElementValuesCallback> responseStream)
        {
            ServerListRoot? serverList;

            if (!_listsManager.TryGetValue(listServerAlias, out serverList))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Incorrect listServerAlias."));
            }

            ServerContext.ElementValuesCallbackMessage? elementValuesCallbackMessage = serverList.GetElementValuesCallbackMessage();

            if (elementValuesCallbackMessage is not null)
            {
                foreach (var elementValuesCallback in elementValuesCallbackMessage.SplitForCorrectGrpcMessageSize())
                {
                    await responseStream.WriteAsync(elementValuesCallback);
                };
            }
        }
        
        internal EventMessagesCollection PollEventsChanges(uint listServerAlias)
        {
            ServerListRoot? serverList;

            if (!_listsManager.TryGetValue(listServerAlias, out serverList))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Incorrect listServerAlias."));
            }

            if (_pollEventMessagesCallbacksQueue.Count == 0)
            {
                while (true)
                {
                    ServerContext.EventMessagesCallbackMessage? eventMessagesCallbackMessage = serverList.GetNextEventMessagesCallbackMessage();
                    if (eventMessagesCallbackMessage is null)
                        break;

                    if (_pollEventMessagesCallbacksQueue.Count == 0)
                        _pollEventMessagesCallbacksQueue = eventMessagesCallbackMessage.SplitForCorrectGrpcMessageSize();                    
                    else
                        foreach (var emc in eventMessagesCallbackMessage.SplitForCorrectGrpcMessageSize())
                            _pollEventMessagesCallbacksQueue.Enqueue(emc);                    
                }
            }

            if (_pollEventMessagesCallbacksQueue.Count > 0)
            {
                return _pollEventMessagesCallbacksQueue.Dequeue().EventMessagesCollection;
            }
            else
            {
                return new EventMessagesCollection();
            }
        }

        #endregion

        #region private fields        

        private Queue<EventMessagesCallback> _pollEventMessagesCallbacksQueue = new Queue<EventMessagesCallback>();

        #endregion
    }
}