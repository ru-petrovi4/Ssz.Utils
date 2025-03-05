using Grpc.Core;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.ServerBase
{    
    public partial class ServerContext        
    {
        #region internal functions
        
        public Task<ElementValuesCallbackMessage?> PollElementValuesChangesAsync(uint listServerAlias)
        {
            ServerListRoot? serverList;

            if (!_listsManager.TryGetValue(listServerAlias, out serverList))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Incorrect listServerAlias."));
            }

            return Task.FromResult(serverList.GetElementValuesCallbackMessage());
        }

        public Task<List<EventMessagesCallbackMessage>?> PollEventsChangesAsync(uint listServerAlias)
        {
            ServerListRoot? serverList;

            if (!_listsManager.TryGetValue(listServerAlias, out serverList))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Incorrect listServerAlias."));
            }

           return Task.FromResult(serverList.GetEventMessagesCallbackMessages());            
        }

        #endregion        
    }
}