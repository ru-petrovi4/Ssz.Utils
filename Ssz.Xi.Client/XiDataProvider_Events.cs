using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Utils;
using Ssz.Xi.Client.Api;
using Ssz.Xi.Client.Api.Lists;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client
{
    public partial class XiDataProvider
    {
        #region public functions

        /// <summary>
        ///     Is called using сallbackDoer, see Initialize(..).        
        /// </summary>
        public event Action<IEnumerable<EventMessage>> EventNotification
        {
            add { BeginInvoke(ct => _xiEventListItemsManager.EventNotification += value); }
            remove { BeginInvoke(ct => _xiEventListItemsManager.EventNotification -= value); }
        }

        /// <summary>
        ///     events != null
        /// </summary>
        /// <param name="events"></param>
        public void AckAlarms(IEnumerable<EventId> events)
        {
            if (events == null)
                throw new ArgumentNullException("events");

            BeginInvoke(ct =>
            {
                if (!_onEventNotificationSubscribed)
                {
                    _onEventNotificationSubscribed = true;
                    _xiEventListItemsManager.EventNotification += OnEventNotification;
                }

                IXiEventListProxy? eventListProxy =
                    _xiEventListItemsManager.GetRelatedXiEventList(OnEventNotification);

                if (eventListProxy == null) return;

                try
                {
                    if (eventListProxy.Disposed) return;

                    eventListProxy.AcknowledgeAlarms(null, null, events.ToList());
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            });
        }

        #endregion

        #region private functions

        private void OnEventNotification(IEnumerable<EventMessage> obj)
        {
        }

        #endregion

        #region private fields

        private readonly XiEventListItemsManager _xiEventListItemsManager = new XiEventListItemsManager();
        private bool _onEventNotificationSubscribed;

        #endregion
    }
}