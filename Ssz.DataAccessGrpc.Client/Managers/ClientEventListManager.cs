using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Ssz.Utils;
using Ssz.DataAccessGrpc.Common;
using Microsoft.Extensions.Logging;
using Ssz.DataAccessGrpc.Client.ClientLists;
using Ssz.DataAccessGrpc.Client.ClientListItems;
using Ssz.Utils.DataAccess;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Ssz.DataAccessGrpc.Client.Managers
{
    internal class ClientEventListManager
    {
        #region construction and destruction

        public ClientEventListManager(ILogger<GrpcDataAccessProvider> logger, IDataAccessProvider dataAccessProvider)
        {
            Logger = logger;
            DataAccessProvider = dataAccessProvider;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     No throw.
        /// </summary>
        /// <param name="clientContextManager"></param>
        /// <param name="сallbackDispatcher"></param>        
        /// <param name="callbackIsEnabled"></param>
        /// <param name="cancellationToken"></param>
        public async Task SubscribeAsync(ClientContextManager clientContextManager, IDispatcher? сallbackDispatcher, bool callbackIsEnabled, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!clientContextManager.ContextIsOperational) 
                return;
            if (!_dataGrpcEventItemsMustBeAdded) 
                return;

            bool allOk = true;

            foreach (var kvp in _eventMessagesCallbackEventHandlers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (kvp.Value.P is not null) continue;

                ClientEventList dataGrpcEventList;

                try
                {
                    dataGrpcEventList = await clientContextManager.NewEventListAsync(null);
                }
                catch (OperationCanceledException)
                {
                    throw;
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
                        EventHandler<EventMessagesCallbackEventArgs> eventMessagesCallbackEventHandler = kvp.Key;

                        dataGrpcEventList.EventMessagesCallback +=
                            (sender, args) =>
                            {                                
                                if (сallbackDispatcher is not null)
                                {
                                    try
                                    {
                                        сallbackDispatcher.BeginInvoke(ct =>
                                        {
                                            DataAccessProvider.ElementIdsMap?.AddCommonFieldsToEventMessagesCollection(args.EventMessagesCollection);
                                            eventMessagesCallbackEventHandler(DataAccessProvider, args);                                            
                                        });
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                            };

                        if (callbackIsEnabled)
                        {
                            await dataGrpcEventList.EnableListCallbackAsync(true);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception)
                    {
                        allOk = false;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "EventMessagesManager.Subscribe exception.");
                }

                kvp.Value.P = dataGrpcEventList;

                if (allOk) _dataGrpcEventItemsMustBeAdded = false;
            }
        }        

        /// <summary>
        ///     If not Pollable, does nothing.
        ///     No throw.
        /// </summary>
        public async Task PollChangesAsync()
        {
            foreach (var kvp in _eventMessagesCallbackEventHandlers)
            {
                ClientEventList? dataGrpcEventList = _eventMessagesCallbackEventHandlers[kvp.Key].P;

                if (dataGrpcEventList is null || dataGrpcEventList.Disposed) continue;
                try
                {
                    await dataGrpcEventList.PollEventsChangesAsync();
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

                if (dataGrpcEventList is not null)
                    dataGrpcEventList.Dispose();

                _eventMessagesCallbackEventHandlers[kvp.Key].P = null;
            }

            _dataGrpcEventItemsMustBeAdded = true;
        }

        public ClientEventList? GetRelatedClientEventList(EventHandler<EventMessagesCallbackEventArgs> eventHandler)
        {
            ClientEventListPointer? dataGrpcEventListPointer;
            if (!_eventMessagesCallbackEventHandlers.TryGetValue(eventHandler, out dataGrpcEventListPointer)) return null;
            return dataGrpcEventListPointer.P;
        }

        public event EventHandler<EventMessagesCallbackEventArgs> EventMessagesCallback
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
                if (dataGrpcEventListPointer.P is not null)
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

        protected IDataAccessProvider DataAccessProvider { get; }

        #endregion

        #region private fields

        private volatile bool _dataGrpcEventItemsMustBeAdded;

        private readonly Dictionary<EventHandler<EventMessagesCallbackEventArgs>, ClientEventListPointer> _eventMessagesCallbackEventHandlers =
            new ();

        #endregion

        private class ClientEventListPointer
        {
            #region public functions

            public ClientEventList? P;

            #endregion
        }
    }
}


//public event Action<Utils.DataAccess.LongrunningPassthroughCallback> LongrunningPassthroughCallback
//{
//    add
//    {
//        _longrunningPassthroughCallbackEventHandlers.Add(value, new ClientEventListPointer());
//        _dataGrpcEventItemsMustBeAdded = true;
//    }
//    remove
//    {
//        ClientEventListPointer? dataGrpcEventListPointer;
//        if (!_longrunningPassthroughCallbackEventHandlers.TryGetValue(value, out dataGrpcEventListPointer)) return;
//        _longrunningPassthroughCallbackEventHandlers.Remove(value);
//        if (dataGrpcEventListPointer.P is not null)
//        {
//            try
//            {
//                dataGrpcEventListPointer.P.Dispose();
//            }
//            catch (Exception ex)
//            {
//                Logger.LogWarning(ex, @"dataGrpcEventListPointer.P.Dispose() exception");
//            }
//        }
//    }
//}



//private readonly Dictionary<Action<Utils.DataAccess.LongrunningPassthroughCallback>, ClientEventListPointer> _longrunningPassthroughCallbackEventHandlers =
//    new();