using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Utils;
using Ssz.Xi.Client.Api;
using Ssz.Xi.Client.Api.Lists;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client
{
    public partial class XiDataAccessProvider
    {
        #region public functions

        /// <summary>
        ///     Is called using сallbackDoer, see Initialize(..).        
        /// </summary>
        public event Action<Ssz.Utils.DataAccess.EventMessage[]> EventMessagesCallback
        {
            add { BeginInvoke(ct => _xiEventListItemsManager.EventMessagesCallback += value); }
            remove { BeginInvoke(ct => _xiEventListItemsManager.EventMessagesCallback -= value); }
        }
        
        public void AckAlarms(string operatorName, string comment, Ssz.Utils.DataAccess.EventId[] eventIdsToAck)
        {
            BeginInvoke(ct =>
            {
                if (!_onEventMessagesCallbackSubscribed)
                {
                    _onEventMessagesCallbackSubscribed = true;
                    _xiEventListItemsManager.EventMessagesCallback += OnEventMessagesCallback;
                }

                IXiEventListProxy? eventListProxy =
                    _xiEventListItemsManager.GetRelatedXiEventList(OnEventMessagesCallback);

                if (eventListProxy == null) return;

                try
                {
                    if (eventListProxy.Disposed) return;

                    eventListProxy.AcknowledgeAlarms(operatorName, comment, eventIdsToAck.Select(e => new EventId(e)).ToList());
                }
                catch
                {
                    //Logger.Error(ex);
                }
            });
        }

        #endregion

        #region private functions

        private void OnEventMessagesCallback(Ssz.Utils.DataAccess.EventMessage[] obj)
        {
        }

        #endregion

        #region private fields

        private readonly XiEventListItemsManager _xiEventListItemsManager = new XiEventListItemsManager();
        private bool _onEventMessagesCallbackSubscribed;

        #endregion
    }
}