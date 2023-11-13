using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils;
using Ssz.Utils.DataAccess;

namespace Ssz.Dcs.CentralServer
{
    
    public class EventListBase : ServerListRoot
    {
        #region construction and destruction

        public EventListBase(ServerContext serverContext, uint listClientAlias, CaseInsensitiveDictionary<string?> listParams)
            : base(serverContext, listClientAlias, listParams)
        {
            string? updateRate = listParams.TryGetValue("UpdateRateMs");
            if (updateRate is not null)
            {
                UpdateRateMs = (uint)(new Any(updateRate).ValueAsInt32(false));
            }
        }

        #endregion

        #region public functions

        public uint UpdateRateMs { get; set; }

        public override void EnableListCallback(bool enable)
        {
            if (ListCallbackIsEnabled == enable) return;

            ListCallbackIsEnabled = enable;                        
        }

        public override ServerContext.EventMessagesCallbackMessage? GetNextEventMessagesCallbackMessage()
        {
            if (EventMessagesCollection.Count == 0) return null;

            var emc = EventMessagesCollection.Dequeue();

            ServerContext.EventMessagesCallbackMessage result = new ServerContext.EventMessagesCallbackMessage();

            result.ListClientAlias = ListClientAlias;
            result.EventMessages = emc.EventMessages.Select(em => new Ssz.DataAccessGrpc.ServerBase.EventMessage(em)).ToList();
            result.CommonFields = emc.CommonFields;

            return result;
        }

        public override void DoWork(DateTime nowUtc, CancellationToken token)
        {
            if (Disposed) return;

            if (!ListCallbackIsEnabled) return; // Callback is not Enabled.            

            if (nowUtc >= LastCallbackTime.AddMilliseconds(UpdateRateMs))
            {
                LastCallbackTime = nowUtc;

                while (true)
                {
                    ServerContext.EventMessagesCallbackMessage? eventMessagesCallbackMessage = GetNextEventMessagesCallbackMessage();
                    if (eventMessagesCallbackMessage is null)
                        break;
                    
                    ServerContext.AddCallbackMessage(eventMessagesCallbackMessage);                    
                }
            }
        }

        #endregion

        #region protected functions

        protected Queue<Ssz.Utils.DataAccess.EventMessagesCollection> EventMessagesCollection { get; set; } = new();        

        #endregion        
    }
}