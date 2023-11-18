using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Utils;
using Ssz.DataAccessGrpc.Client.Managers;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.DataAccessGrpc.Client.ClientLists;
using Ssz.Utils.DataAccess;
using Microsoft.Extensions.Logging;

namespace Ssz.DataAccessGrpc.Client
{
    public partial class GrpcDataAccessProvider
    {
        #region public functions

        /// <summary>
        ///     Is called using сallbackDispatcher, see Initialize(..).        
        /// </summary>
        public override event EventHandler<EventMessagesCallbackEventArgs> EventMessagesCallback
        {
            add { WorkingThreadSafeDispatcher.BeginInvoke(ct => _clientEventListManager.EventMessagesCallback += value); }
            remove { WorkingThreadSafeDispatcher.BeginInvoke(ct => _clientEventListManager.EventMessagesCallback -= value); }
        }        
        
        public override void AckAlarms(string operatorName, string comment, Ssz.Utils.DataAccess.EventId[] eventIdsToAck)
        {
            WorkingThreadSafeDispatcher.BeginAsyncInvoke(async ct =>
            {
                ClientEventList? clientEventList =
                    _clientEventListManager.GetRelatedClientEventList(OnClientEventListManager_EventMessagesCallbackInternal);

                if (clientEventList is null) return;

                try
                {
                    if (clientEventList.Disposed) return;

                    await clientEventList.AckAlarmsAsync(operatorName, comment, eventIdsToAck);
                }
                catch (Exception ex)
                {
                    LoggersSet.Logger.LogError(ex, "Exception");
                }
            }
            );
        }

        #endregion

        #region protected functions

        protected virtual void OnClientEventListManager_EventMessagesCallback(Utils.DataAccess.EventMessagesCollection eventMessagesCollection)
        {
        }

        #endregion

        #region private functions

        private void OnClientEventListManager_EventMessagesCallbackInternal(object? sender, EventMessagesCallbackEventArgs args)
        {
            OnClientEventListManager_EventMessagesCallback(args.EventMessagesCollection);
        }

        #endregion

        #region private fields

        private ClientEventListManager _clientEventListManager { get; }

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