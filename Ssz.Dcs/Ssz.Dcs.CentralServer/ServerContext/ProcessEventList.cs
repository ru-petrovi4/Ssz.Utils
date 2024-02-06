using Grpc.Core;
using Microsoft.Extensions.Logging;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Ssz.Dcs.CentralServer
{
    /// <summary>
    /// </summary>
    public class ProcessEventList : EventListBase
    {
        #region construction and destruction
        
        public ProcessEventList(ILogger logger, ServerContext serverContext, uint listClientAlias, CaseInsensitiveDictionary<string?> listParams)
            : base(serverContext, listClientAlias, listParams)
        {
            _logger = logger;

            _engineSessions = ((ServerWorker)ServerContext.ServerWorker).GetEngineSessions(ServerContext);
            _engineSessions.CollectionChanged += OnEngineSessions_CollectionChanged;

            ((ServerWorker)ServerContext.ServerWorker).ProcessEventMessageNotification += OnProcessEventMessageNotification;

            foreach (EngineSession engineSession in _engineSessions)
            {
                engineSession.DataAccessProvider.EventMessagesCallback += OnDataAccessProvider_EventMessagesCallback;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                foreach (EngineSession engineSession in _engineSessions)
                {
                    engineSession.DataAccessProvider.EventMessagesCallback -= OnDataAccessProvider_EventMessagesCallback;
                }                

                ((ServerWorker)ServerContext.ServerWorker).ProcessEventMessageNotification -= OnProcessEventMessageNotification;

                _engineSessions.CollectionChanged -= OnEngineSessions_CollectionChanged;
            }

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public override List<EventIdResult> AckAlarms(string operatorName, string comment,
                                                               IEnumerable<Ssz.DataAccessGrpc.ServerBase.EventId> eventIdsToAck)
        {
            var eventIdsToAckArray = eventIdsToAck.Select(e => e.ToEventId()).ToArray();

            foreach (EngineSession engineSession in _engineSessions)
            {
                engineSession.DataAccessProvider.AckAlarms(operatorName, comment, eventIdsToAckArray);
            }
            return eventIdsToAck.Select(e => new EventIdResult { StatusCode = (uint)StatusCode.OK, EventId = e }).ToList();
        }

        #endregion

        #region private functions

        private void OnEngineSessions_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var engineSession in e.NewItems!.OfType<EngineSession>())
                    {
                        engineSession.DataAccessProvider.EventMessagesCallback += OnDataAccessProvider_EventMessagesCallback;
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (var engineSession in e.OldItems!.OfType<EngineSession>())
                    {
                        engineSession.DataAccessProvider.EventMessagesCallback -= OnDataAccessProvider_EventMessagesCallback;
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    break;

                case NotifyCollectionChangedAction.Reset:
                    break;
            }
        }

        private void OnDataAccessProvider_EventMessagesCallback(object? sender, EventMessagesCallbackEventArgs args)
        {
            if (Disposed) return;

            _logger.LogDebug("ModelDataEventList::DataAccessProviderOnEventMessagesCallback eventMessages.Length=" + args.EventMessagesCollection.EventMessages.Count);

            EventMessagesCollections.Add(args.EventMessagesCollection);
        }

        private void OnProcessEventMessageNotification(string? targetWorkstationName, Ssz.Utils.DataAccess.EventMessage eventMessage)
        {
            if (Disposed) return;

            if (!String.IsNullOrEmpty(targetWorkstationName) &&
                !String.Equals(targetWorkstationName, ServerContext.ClientWorkstationName, StringComparison.InvariantCultureIgnoreCase)) return;

            EventMessagesCollections.Add(
                new Ssz.Utils.DataAccess.EventMessagesCollection
                {
                    EventMessages = new List<Ssz.Utils.DataAccess.EventMessage>() { eventMessage }
                });
        }

        #endregion

        #region private fields

        private readonly ILogger _logger;

        private readonly ObservableCollection<EngineSession> _engineSessions;

        #endregion
    }
}