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
        public override event Action<IDataAccessProvider, Utils.DataAccess.EventMessagesCollection> EventMessagesCallback
        {
            add { ThreadSafeDispatcher.BeginInvoke(ct => _clientEventListManager.EventMessagesCallback += value); }
            remove { ThreadSafeDispatcher.BeginInvoke(ct => _clientEventListManager.EventMessagesCallback -= value); }
        }        
        
        public override void AckAlarms(string operatorName, string comment, Ssz.Utils.DataAccess.EventId[] eventIdsToAck)
        {
            ThreadSafeDispatcher.BeginInvoke(ct =>
            {
                ClientEventList? clientEventList =
                    _clientEventListManager.GetRelatedClientEventList(OnEventMessagesCallbackInternal);

                if (clientEventList is null) return;

                try
                {
                    if (clientEventList.Disposed) return;

                    clientEventList.AckAlarms(operatorName, comment, eventIdsToAck);
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

        protected virtual void OnEventMessagesCallback(Utils.DataAccess.EventMessagesCollection eventMessagesCollection)
        {
        }

        #endregion

        #region private functions

        private void OnEventMessagesCallbackInternal(IDataAccessProvider dataAccessProvider, Utils.DataAccess.EventMessagesCollection eventMessagesCollection)
        {
            OnEventMessagesCallback(eventMessagesCollection);
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