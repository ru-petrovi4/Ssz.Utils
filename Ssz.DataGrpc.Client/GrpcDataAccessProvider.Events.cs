using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Utils;
using Ssz.DataGrpc.Client.Managers;
using Ssz.DataGrpc.Server;
using Ssz.DataGrpc.Client.ClientLists;
using Ssz.Utils.DataAccess;
using Microsoft.Extensions.Logging;

namespace Ssz.DataGrpc.Client
{
    public partial class GrpcDataAccessProvider
    {
        #region public functions

        /// <summary>
        ///     Is called using сallbackDispatcher, see Initialize(..).        
        /// </summary>
        public event Action<Utils.DataAccess.EventMessage[]> EventMessagesCallback
        {
            add { BeginInvoke(ct => ClientEventListManager.EventMessagesCallback += value); }
            remove { BeginInvoke(ct => ClientEventListManager.EventMessagesCallback -= value); }
        }        
        
        public void AckAlarms(string operatorName, string comment, Ssz.Utils.DataAccess.EventId[] eventIdsToAck)
        {
            BeginInvoke(ct =>
            {
                ClientEventList? dataGrpcEventList =
                    ClientEventListManager.GetRelatedClientEventList(OnEventMessagesCallbackInternal);

                if (dataGrpcEventList is null) return;

                try
                {
                    if (dataGrpcEventList.Disposed) return;

                    dataGrpcEventList.AckAlarms(operatorName, comment, eventIdsToAck);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Exception");
                }
            }
            );
        }

        #endregion

        #region protected functions

        protected ClientEventListManager ClientEventListManager { get; }

        protected virtual void OnEventMessagesCallback(Utils.DataAccess.EventMessage[] newEventMessages)
        {
        }

        #endregion

        #region private functions

        private void OnEventMessagesCallbackInternal(Utils.DataAccess.EventMessage[] newEventMessages)
        {
            OnEventMessagesCallback(newEventMessages);
        }

        #endregion
    }
}

///// <summary>
/////     Is called using сallbackDispatcher, see Initialize(..).        
///// </summary>
//public event Action<Utils.DataAccess.LongrunningPassthroughCallback> LongrunningPassthroughCallback
//{
//    add { BeginInvoke(ct => ClientEventListManager.LongrunningPassthroughCallback += value); }
//    remove { BeginInvoke(ct => ClientEventListManager.LongrunningPassthroughCallback -= value); }
//}