using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Ssz.Utils;
using Ssz.DataGrpc.Server;
using Microsoft.Extensions.Logging;
using Ssz.DataGrpc.Client.Core.Lists;
using Ssz.DataGrpc.Client.Core.ListItems;

namespace Ssz.DataGrpc.Client.Managers
{
    public class DataGrpcEventListItemsManager
    {
        #region construction and destruction

        public DataGrpcEventListItemsManager(ILogger<DataGrpcProvider> logger)
        {
            Logger = logger;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     No throw.
        /// </summary>
        /// <param name="dataGrpcServerProxy"></param>
        /// <param name="сallbackDoer"></param>        
        /// <param name="callbackIsEnabled"></param>
        /// <param name="ct"></param>
        public void Subscribe(DataGrpcServerManager dataGrpcServerProxy, ICallbackDoer? сallbackDoer, bool callbackIsEnabled, CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return;
            if (!dataGrpcServerProxy.ConnectionExists) return;
            if (!_dataGrpcEventItemsMustBeAdded) return;

            bool allOk = true;

            foreach (
                var kvp in _eventNotificationEventHandlers)
            {
                if (ct.IsCancellationRequested) return;
                if (kvp.Value.P != null) continue;

                DataGrpcEventList dataGrpcEventList;

                try
                {
                    dataGrpcEventList = dataGrpcServerProxy.NewEventList(null);
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
                        Action<IEnumerable<EventMessage>> eventNotificationEventHandler = kvp.Key;

                        dataGrpcEventList.EventNotificationEvent +=
                            (DataGrpcEventList eventList, DataGrpcEventListItem[] newListItems) =>
                            {
                                if (ct.IsCancellationRequested) return;
                                if (сallbackDoer != null)
                                {
                                    try
                                    {
                                        сallbackDoer.BeginInvoke(ct => eventNotificationEventHandler(
                                            newListItems.Select(li => li.EventMessage)));
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
                    Logger.LogWarning(ex, "DataGrpcEventListItemsManager.Subscribe exception.");
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
                DataGrpcEventList? dataGrpcEventList = _eventNotificationEventHandlers[kvp.Key].P;

                if (dataGrpcEventList == null || dataGrpcEventList.Disposed) continue;
                try
                {
                    dataGrpcEventList.PollEventChanges();
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
                DataGrpcEventList? dataGrpcEventList = _eventNotificationEventHandlers[kvp.Key].P;

                if (dataGrpcEventList != null)
                    dataGrpcEventList.Dispose();

                _eventNotificationEventHandlers[kvp.Key].P = null;
            }

            _dataGrpcEventItemsMustBeAdded = true;
        }

        public DataGrpcEventList? GetRelatedDataGrpcEventList(Action<IEnumerable<EventMessage>> eventHandler)
        {
            DataGrpcEventListPointer? dataGrpcEventListPointer;
            if (!_eventNotificationEventHandlers.TryGetValue(eventHandler, out dataGrpcEventListPointer)) return null;
            return dataGrpcEventListPointer.P;
        }

        public event Action<IEnumerable<EventMessage>> EventNotification
        {
            add
            {
                _eventNotificationEventHandlers.Add(value, new DataGrpcEventListPointer());
                _dataGrpcEventItemsMustBeAdded = true;
            }
            remove
            {
                DataGrpcEventListPointer? dataGrpcEventListPointer;
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

        private readonly Dictionary<Action<IEnumerable<EventMessage>>, DataGrpcEventListPointer> _eventNotificationEventHandlers =
            new Dictionary<Action<IEnumerable<EventMessage>>, DataGrpcEventListPointer>();

        #endregion

        private class DataGrpcEventListPointer
        {
            #region public functions

            public DataGrpcEventList? P;

            #endregion
        }
    }
}