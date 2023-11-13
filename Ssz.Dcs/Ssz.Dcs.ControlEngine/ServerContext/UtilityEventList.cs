using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.ControlEngine
{
    public class UtilityEventList : EventListBase
    {
        #region construction and destruction
        
        public UtilityEventList(ServerContext serverContext, uint listClientAlias, CaseInsensitiveDictionary<string?> listParams)
            : base(serverContext, listClientAlias, listParams)
        {
            //((ServerWorker)ServerContext.ServerWorker).UtilityEventMessageNotification += OnUtilityEventMessageNotification;
        }

        //protected override void Dispose(bool disposing)
        //{
        //    if (Disposed) return;

        //    if (disposing)
        //    {
        //        ((ServerWorker)ServerContext.ServerWorker).UtilityEventMessageNotification -= OnUtilityEventMessageNotification;
        //    }

        //    base.Dispose(disposing);
        //}

        #endregion


        #region private functions

        //private void OnUtilityEventMessageNotification(string? targetWorkstationName, Ssz.Utils.DataAccess.EventMessage eventMessage)
        //{
        //    if (Disposed) return;

        //    if (!String.IsNullOrEmpty(targetWorkstationName) && 
        //        !String.Equals(targetWorkstationName, ServerContext.ClientWorkstationName, StringComparison.InvariantCultureIgnoreCase)) return;

        //    EventMessagesCollection.Enqueue(
        //        new Ssz.Utils.DataAccess.EventMessagesCollection
        //        {
        //            EventMessages = new List<Ssz.Utils.DataAccess.EventMessage>() { eventMessage }
        //        });
        //}

        #endregion
    }
}
