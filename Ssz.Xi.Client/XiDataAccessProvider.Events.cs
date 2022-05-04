using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
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
        public event Action<IDataAccessProvider, Ssz.Utils.DataAccess.EventMessage[]> EventMessagesCallback
        {
            add { ThreadSafeDispatcher.BeginInvoke(ct => _xiEventListItemsManager.EventMessagesCallback += value); }
            remove { ThreadSafeDispatcher.BeginInvoke(ct => _xiEventListItemsManager.EventMessagesCallback -= value); }
        }
        
        public void AckAlarms(string operatorName, string comment, Ssz.Utils.DataAccess.EventId[] eventIdsToAck)
        {
            ThreadSafeDispatcher.BeginInvoke(ct =>
            {
                IXiEventListProxy? eventListProxy =
                    _xiEventListItemsManager.GetRelatedXiEventList(OnEventMessagesCallback);

                if (eventListProxy is null) return;

                try
                {
                    if (eventListProxy.Disposed) return;

                    eventListProxy.AcknowledgeAlarms(operatorName, comment, eventIdsToAck.Select(e => new global::Xi.Contracts.Data.EventId(e)).ToList());
                }
                catch
                {
                    //Logger.Error(ex);
                }
            });
        }

        #endregion

        #region private functions

        private void OnEventMessagesCallback(IDataAccessProvider dataAccessProvider, Ssz.Utils.DataAccess.EventMessage[] obj)
        {
        }

        #endregion

        #region private fields

        private readonly XiEventListItemsManager _xiEventListItemsManager;        

        #endregion
    }
}