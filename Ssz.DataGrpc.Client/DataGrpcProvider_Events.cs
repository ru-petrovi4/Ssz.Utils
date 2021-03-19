using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Utils;
using Ssz.DataGrpc.Client.Managers;
using Ssz.DataGrpc.Server;
using Ssz.DataGrpc.Client.Core.Lists;

namespace Ssz.DataGrpc.Client
{
    public partial class DataGrpcProvider
    {
        #region public functions

        /// <summary>
        ///     Is called using сallbackDoer, see Initialize(..).        
        /// </summary>
        public event Action<IEnumerable<EventMessage>> EventNotification
        {
            add { BeginInvoke(ct => _dataGrpcEventListItemsManager.EventNotification += value); }
            remove { BeginInvoke(ct => _dataGrpcEventListItemsManager.EventNotification -= value); }
        }

        /// <summary>
        ///     
        /// </summary>
        /// <param name="events"></param>
        public void AckAlarms(EventId[] events)
        {
            BeginInvoke(ct =>
            {
                if (!_onEventNotificationSubscribed)
                {
                    _onEventNotificationSubscribed = true;
                    _dataGrpcEventListItemsManager.EventNotification += OnEventNotification;
                }

                DataGrpcEventList? dataGrpcEventList =
                    _dataGrpcEventListItemsManager.GetRelatedDataGrpcEventList(OnEventNotification);

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

        private void OnEventNotification(IEnumerable<EventMessage> obj)
        {
        }

        #endregion

        #region private fields

        private readonly DataGrpcEventListItemsManager _dataGrpcEventListItemsManager;
        private bool _onEventNotificationSubscribed;

        #endregion
    }
}