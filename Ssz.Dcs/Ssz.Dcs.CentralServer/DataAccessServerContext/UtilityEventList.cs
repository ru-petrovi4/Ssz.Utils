using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer
{
    public class UtilityEventList : EventListBase
    {
        #region construction and destruction
        
        public UtilityEventList(DataAccessServerWorkerBase serverWorker, ServerContext serverContext, uint listClientAlias, CaseInsensitiveOrderedDictionary<string?> listParams)
            : base(serverWorker, serverContext, listClientAlias, listParams)
        {
            ((ServerWorker)ServerContext.ServerWorker).UtilityEventMessageNotification += OnUtilityEventMessageNotification;
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                ((ServerWorker)ServerContext.ServerWorker).UtilityEventMessageNotification -= OnUtilityEventMessageNotification;
            }

            base.Dispose(disposing);
        }

        #endregion


        #region private functions

        private void OnUtilityEventMessageNotification(string? targetWorkstationName, Ssz.Utils.DataAccess.EventMessage eventMessage)
        {
            if (Disposed) return;

            bool send = false;
            if (String.IsNullOrEmpty(targetWorkstationName))
            {
                send = true;
            }
            else if (String.Equals(targetWorkstationName, ServerContext.ClientWorkstationName, StringComparison.InvariantCultureIgnoreCase))
            {
                send = true;
            }
            else if (String.Equals(targetWorkstationName, @"localhost", StringComparison.InvariantCultureIgnoreCase) &&
                        String.Equals(ServerContext.ClientWorkstationName, Environment.MachineName, StringComparison.InvariantCultureIgnoreCase))                
            {
                send = true;
            }

            if (send)
                EventMessagesCollections.Add(
                    new Ssz.Utils.DataAccess.EventMessagesCollection
                    {
                        EventMessages = new List<Ssz.Utils.DataAccess.EventMessage>() { eventMessage }
                    });
        }

        #endregion
    }
}
