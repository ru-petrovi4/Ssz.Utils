using System;
using System.Collections.Generic;
using System.Threading;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils.DataAccess;
using Ssz.Utils;
using System.Linq;

namespace Ssz.Dcs.ControlEngine
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
            if (ListCallbackIsEnabled == enable) 
                return;

            ListCallbackIsEnabled = enable;
        }

        public override List<ServerContext.EventMessagesCallbackMessage>? GetEventMessagesCallbackMessages()
        {
            if (EventMessagesCollections.Count == 0)
                return null;

            List<ServerContext.EventMessagesCallbackMessage> result = new();

            foreach (var eventMessagesCollection in EventMessagesCollections)
            {
                result.Add(new ServerContext.EventMessagesCallbackMessage
                {
                    ListClientAlias = this.ListClientAlias,
                    EventMessages = eventMessagesCollection.EventMessages.Select(em => new Ssz.DataAccessGrpc.ServerBase.EventMessage(em)).ToList(),
                    CommonFields = eventMessagesCollection.CommonFields
                });
            }

            EventMessagesCollections.Clear();

            return result;
        }

        public override void DoWork(DateTime nowUtc, CancellationToken token)
        {
            if (Disposed) 
                return;

            if (!ListCallbackIsEnabled) 
                return; // Callback is not Enabled.            

            if (nowUtc >= LastCallbackTime.AddMilliseconds(UpdateRateMs))
            {
                LastCallbackTime = nowUtc;

                List<ServerContext.EventMessagesCallbackMessage>? eventMessagesCallbackMessages = GetEventMessagesCallbackMessages();
                if (eventMessagesCallbackMessages is not null)
                {
                    foreach (var eventMessagesCallbackMessage in eventMessagesCallbackMessages)
                    {
                        ServerContext.AddCallbackMessage(eventMessagesCallbackMessage);
                    }
                }
            }
        }

        #endregion

        #region protected functions

        protected List<Ssz.Utils.DataAccess.EventMessagesCollection> EventMessagesCollections { get; } = new();

        #endregion
    }
}