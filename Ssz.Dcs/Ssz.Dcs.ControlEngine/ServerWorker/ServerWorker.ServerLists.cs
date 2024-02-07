using System.Collections.Generic;
using System.Linq;
using Ssz.DataAccessGrpc.ServerBase;
using Grpc.Core;
using Ssz.Utils.DataAccess;
using Ssz.Utils;
using System;

namespace Ssz.Dcs.ControlEngine
{    
    public partial class ServerWorker : ServerWorkerBase
    {
        #region public functions

        public override ServerListRoot NewServerList(ServerContext serverContext, uint listClientAlias, uint listType, CaseInsensitiveDictionary<string?> listParams)
        {
            switch (listType)
            {
                case (uint)StandardListType.ElementValueList:
                    return NewElementValueList(serverContext, listClientAlias, listParams);
                case (uint)StandardListType.ElementValuesJournalList:
                    return NewElementValuesJournalList(serverContext, listClientAlias, listParams);
                case (uint)StandardListType.EventList:
                    return NewEventList(serverContext, listClientAlias, listParams);
                default:
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "This server does not support the type of list specified."));
            }
        }
        
        public ServerListRoot NewElementValueList(ServerContext serverContext, uint listClientAlias, CaseInsensitiveDictionary<string?> listParams)
        {
            string systemNameToConnect = serverContext.SystemNameToConnect;
            if (systemNameToConnect == @"")
            {
                return new UtilityElementValueList(this, serverContext, listClientAlias, listParams);
            }
            else
            {
                return new ProcessElementValueList(this, serverContext, listClientAlias, listParams);
            }
        }
        
        public ServerListRoot NewElementValuesJournalList(ServerContext serverContext, uint listClientAlias, CaseInsensitiveDictionary<string?> listParams)
        {
            string systemNameToConnect = serverContext.SystemNameToConnect;
            if (systemNameToConnect == @"")
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "This context does not support the type of list specified."));                
            }
            else
            {
                return new ProcessElementValuesJournalList(this, serverContext, listClientAlias, listParams);
            }
        }
        
        public ServerListRoot NewEventList(ServerContext serverContext, uint listClientAlias, CaseInsensitiveDictionary<string?> listParams)
        {
            string systemNameToConnect = serverContext.SystemNameToConnect;
            if (systemNameToConnect == @"")
            {
                return new UtilityEventList(this, serverContext, listClientAlias, listParams);
            }
            else
            {
                return new ProcessEventList(this, Logger, serverContext, listClientAlias, listParams);
            }
        }
        
        #endregion
    }
}