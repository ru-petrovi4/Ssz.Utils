using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Utils;
using Ssz.DataGrpc.Client.Managers;
using Ssz.DataGrpc.Server;
using Ssz.DataGrpc.Client.ClientLists;
using Ssz.Utils.DataAccess;

namespace Ssz.DataGrpc.Client
{
    public partial class DataGrpcProvider
    {
        #region public functions

        /// <summary>
        ///     Is called using сallbackDispatcher, see Initialize(..).        
        /// </summary>
        public event Action<Utils.DataAccess.EventMessage[]> EventMessagesCallback
        {
            add { BeginInvoke(ct => _clientEventListManager.EventMessagesCallback += value); }
            remove { BeginInvoke(ct => _clientEventListManager.EventMessagesCallback -= value); }
        }

        /// <summary>
        ///     
        /// </summary>
        /// <param name="events"></param>
        public void AckAlarms(Ssz.Utils.DataAccess.EventId[] events)
        {
            BeginInvoke(ct =>
            {
                if (!_onEventMessagesCallbackSubscribed)
                {
                    _onEventMessagesCallbackSubscribed = true;
                    _clientEventListManager.EventMessagesCallback += OnEventMessagesCallback;
                }

                ClientEventList? dataGrpcEventList =
                    _clientEventListManager.GetRelatedClientEventList(OnEventMessagesCallback);

                if (dataGrpcEventList == null) return;

                try
                {
                    if (dataGrpcEventList.Disposed) return;

                    dataGrpcEventList.AcknowledgeAlarms(@"", @"", events);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
            );
        }

        #endregion

        #region private functions

        private void OnEventMessagesCallback(IEnumerable<Utils.DataAccess.EventMessage> obj)
        {
        }

        #endregion

        #region private fields

        private readonly ClientEventListManager _clientEventListManager;
        private bool _onEventMessagesCallbackSubscribed;

        #endregion
    }
}