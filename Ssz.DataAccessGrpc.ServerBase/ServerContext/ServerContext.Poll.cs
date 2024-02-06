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
        
        internal async Task PollEventsChangesAsync(uint listServerAlias, IServerStreamWriter<EventMessagesCollection> responseStream)
        {
            ServerListRoot? serverList;

            if (!_listsManager.TryGetValue(listServerAlias, out serverList))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Incorrect listServerAlias."));
            }

            List<EventMessagesCallbackMessage>? eventMessagesCallbackMessages = serverList.GetEventMessagesCallbackMessages();
            if (eventMessagesCallbackMessages is not null)
            {
                foreach (var fullEventMessagesCallbackMessage in eventMessagesCallbackMessages)
                {
                    foreach (var eventMessagesCallbackMessage in fullEventMessagesCallbackMessage.SplitForCorrectGrpcMessageSize())
                    {
                        await responseStream.WriteAsync(eventMessagesCallbackMessage.EventMessagesCollection);
                    };                    
                }
            }      
        }

        #endregion        
    }
}