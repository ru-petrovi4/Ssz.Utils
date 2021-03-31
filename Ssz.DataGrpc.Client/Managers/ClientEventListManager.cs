using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Ssz.Utils;
using Ssz.DataGrpc.Server;
using Microsoft.Extensions.Logging;
using Ssz.DataGrpc.Client.ClientLists;
using Ssz.DataGrpc.Client.ClientListItems;
using Ssz.Utils.DataSource;

namespace Ssz.DataGrpc.Client.Managers
{
    public class ClientEventListManager
    {
        #region construction and destruction

        public ClientEventListManager(ILogger<DataGrpcProvider> logger)
        {
            Logger = logger;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     No throw.
        /// </summary>
        /// <param name="clientConnectionManager"></param>
        /// <param name="сallbackDispatcher"></param>        
        /// <param name="callbackIsEnabled"></param>
        /// <param name="ct"></param>
        public void Subscribe(ClientConnectionManager clientConnectionManager, IDispatcher? сallbackDispatcher, bool callbackIsEnabled, CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return;
            if (!clientConnectionManager.ConnectionExists) return;
            if (!_dataGrpcEventItemsMustBeAdded) return;

            bool allOk = true;

            foreach (
                var kvp in _eventNotificationEventHandlers)
            {
                if (ct.IsCancellationRequested) return;
                if (kvp.Value.P != null) continue;

                ClientEventList dataGrpcEventList;

                try
                {
                    dataGrpcEventList = clientConnectionManager.NewEventList(null);
                }
                catch (Exception)
                {
                    return;
                }

                try
                {
                    if (dataGrpcEventList.Disposed)
                    {
                        return;
                    }

                    try
                    {
                        Action<Utils.DataSource.EventMessage[]> eventNotificationEventHandler = kvp.Key;

                        dataGrpcEventList.EventNotificationEvent +=
                            (ClientEventList eventList, ClientEventListItem[] newListItems) =>
                            {
                                if (ct.IsCancellationRequested) return;
                                if (сallbackDispatcher != null)
                                {
                                    try
                                    {
                                        сallbackDispatcher.BeginInvoke(ct => eventNotificationEventHandler(
                                            newListItems.Select(li => li.EventMessage.ToEventMessage()).ToArray()));
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                            };

                        if (callbackIsEnabled)
                        {
                            dataGrpcEventList.EnableListCallback(true);
                        }
                    }
                    catch (Exception)
                    {
                        allOk = false;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "ClientEventListItemsManager.Subscribe exception.");
                }

                kvp.Value.P = dataGrpcEventList;

                if (allOk) _dataGrpcEventItemsMustBeAdded = false;
            }
        }

        /// <summary>
        ///     If not Pollable, does nothing.
        ///     No throw.
        /// </summary>
        public void PollChanges()
        {
            foreach (var kvp in _eventNotificationEventHandlers)
            {
                ClientEventList? dataGrpcEventList = _eventNotificationEventHandlers[kvp.Key].P;

                if (dataGrpcEventList == null || dataGrpcEventList.Disposed) continue;
                try
                {
                    dataGrpcEventList.PollEventsChanges();
                }
                catch
                {
                }
            }            
        }

        public void Unsubscribe()
        {
            foreach (
                var kvp in _eventNotificationEventHandlers)
            {
                ClientEventList? dataGrpcEventList = _eventNotificationEventHandlers[kvp.Key].P;

                if (dataGrpcEventList != null)
                    dataGrpcEventList.Dispose();

                _eventNotificationEventHandlers[kvp.Key].P = null;
            }

            _dataGrpcEventItemsMustBeAdded = true;
        }

        public ClientEventList? GetRelatedClientEventList(Action<IEnumerable<Utils.DataSource.EventMessage>> eventHandler)
        {
            ClientEventListPointer? dataGrpcEventListPointer;
            if (!_eventNotificationEventHandlers.TryGetValue(eventHandler, out dataGrpcEventListPointer)) return null;
            return dataGrpcEventListPointer.P;
        }

        public event Action<Utils.DataSource.EventMessage[]> EventNotification
        {
            add
            {
                _eventNotificationEventHandlers.Add(value, new ClientEventListPointer());
                _dataGrpcEventItemsMustBeAdded = true;
            }
            remove
            {
                ClientEventListPointer? dataGrpcEventListPointer;
                if (!_eventNotificationEventHandlers.TryGetValue(value, out dataGrpcEventListPointer)) return;
                _eventNotificationEventHandlers.Remove(value);
                if (dataGrpcEventListPointer.P != null)
                {
                    try
                    {
                        dataGrpcEventListPointer.P.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, @"dataGrpcEventListPointer.P.Dispose() exception");
                    }
                }
            }
        }

        #endregion

        #region protected functions

        protected ILogger<DataGrpcProvider> Logger { get; }

        #endregion

        #region private fields

        private volatile bool _dataGrpcEventItemsMustBeAdded;

        private readonly Dictionary<Action<Utils.DataSource.EventMessage[]>, ClientEventListPointer> _eventNotificationEventHandlers =
            new Dictionary<Action<Utils.DataSource.EventMessage[]>, ClientEventListPointer>();

        #endregion

        private class ClientEventListPointer
        {
            #region public functions

            public ClientEventList? P;

            #endregion
        }
    }
}