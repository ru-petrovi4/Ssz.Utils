using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Ssz.Utils;
using Ssz.DataGrpc.Server;
using Microsoft.Extensions.Logging;
using Ssz.DataGrpc.Client.ClientLists;
using Ssz.DataGrpc.Client.ClientListItems;
using Ssz.Utils.DataAccess;

namespace Ssz.DataGrpc.Client.Managers
{
    public class ClientEventListManager
    {
        #region construction and destruction

        public ClientEventListManager(ILogger<GrpcDataAccessProvider> logger)
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
                var kvp in _eventMessagesCallbackEventHandlers)
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
                        Action<Utils.DataAccess.EventMessage[]> eventMessagesCallbackEventHandler = kvp.Key;

                        dataGrpcEventList.EventMessagesCallbackEvent +=
                            (ClientEventList eventList, ClientEventListItem[] newListItems) =>
                            {
                                if (ct.IsCancellationRequested) return;
                                if (сallbackDispatcher != null)
                                {
                                    try
                                    {
                                        сallbackDispatcher.BeginInvoke(ct => eventMessagesCallbackEventHandler(
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
            foreach (var kvp in _eventMessagesCallbackEventHandlers)
            {
                ClientEventList? dataGrpcEventList = _eventMessagesCallbackEventHandlers[kvp.Key].P;

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
                var kvp in _eventMessagesCallbackEventHandlers)
            {
                ClientEventList? dataGrpcEventList = _eventMessagesCallbackEventHandlers[kvp.Key].P;

                if (dataGrpcEventList != null)
                    dataGrpcEventList.Dispose();

                _eventMessagesCallbackEventHandlers[kvp.Key].P = null;
            }

            _dataGrpcEventItemsMustBeAdded = true;
        }

        public ClientEventList? GetRelatedClientEventList(Action<IEnumerable<Utils.DataAccess.EventMessage>> eventHandler)
        {
            ClientEventListPointer? dataGrpcEventListPointer;
            if (!_eventMessagesCallbackEventHandlers.TryGetValue(eventHandler, out dataGrpcEventListPointer)) return null;
            return dataGrpcEventListPointer.P;
        }

        public event Action<Utils.DataAccess.EventMessage[]> EventMessagesCallback
        {
            add
            {
                _eventMessagesCallbackEventHandlers.Add(value, new ClientEventListPointer());
                _dataGrpcEventItemsMustBeAdded = true;
            }
            remove
            {
                ClientEventListPointer? dataGrpcEventListPointer;
                if (!_eventMessagesCallbackEventHandlers.TryGetValue(value, out dataGrpcEventListPointer)) return;
                _eventMessagesCallbackEventHandlers.Remove(value);
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

        protected ILogger<GrpcDataAccessProvider> Logger { get; }

        #endregion

        #region private fields

        private volatile bool _dataGrpcEventItemsMustBeAdded;

        private readonly Dictionary<Action<Utils.DataAccess.EventMessage[]>, ClientEventListPointer> _eventMessagesCallbackEventHandlers =
            new Dictionary<Action<Utils.DataAccess.EventMessage[]>, ClientEventListPointer>();

        #endregion

        private class ClientEventListPointer
        {
            #region public functions

            public ClientEventList? P;

            #endregion
        }
    }
}