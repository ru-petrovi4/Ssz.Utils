using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Client;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Dcs.Addons.OpcUaClient
{
    /// <summary>
    ///     Historical access: raw values history of variables and events history of the event notifier object.
    /// </summary>
    public partial class OpcUaClientDataAccessProvider
    {
        #region public functions

        public override void JournalAddItem(string elementId, object valueJournalSubscription)
        {
            string id = GetMappedId(elementId);
            WorkingThreadSafeDispatcher.BeginInvoke(ct =>
            {
                _journalIds[valueJournalSubscription] = id;
            });
        }

        public override void JournalRemoveItem(object valueJournalSubscription)
        {
            WorkingThreadSafeDispatcher.BeginInvoke(ct =>
            {
                _journalIds.Remove(valueJournalSubscription);
            });
        }

        public override async Task<ElementValuesJournal[]?> ReadElementValuesJournalsAsync(DateTime firstTimestampUtc, DateTime secondTimestampUtc, uint numValuesPerSubscription, Ssz.Utils.DataAccess.TypeId? calculation, CaseInsensitiveOrderedDictionary<string?>? params_, object[] valueJournalSubscriptions)
        {
            var taskCompletionSource = new TaskCompletionSource<ElementValuesJournal[]?>(TaskCreationOptions.RunContinuationsAsynchronously);

            WorkingThreadSafeDispatcher.BeginInvoke(async ct =>
            {
                try
                {
                    taskCompletionSource.TrySetResult(await ReadElementValuesJournalsInternalAsync(firstTimestampUtc, secondTimestampUtc, numValuesPerSubscription, valueJournalSubscriptions, ct));
                }
                catch (Exception ex)
                {
                    LoggersSet.Logger.LogWarning(ex, "OPC UA history read error.");
                    MarkSessionFailedIfNeeded(ex);
                    taskCompletionSource.TrySetResult(null);
                }
            });

            return await taskCompletionSource.Task;
        }

        public override async Task<List<EventMessagesCollection>?> ReadEventMessagesJournalAsync(DateTime firstTimestampUtc, DateTime secondTimestampUtc, CaseInsensitiveOrderedDictionary<string?>? params_)
        {
            var taskCompletionSource = new TaskCompletionSource<List<EventMessagesCollection>?>(TaskCreationOptions.RunContinuationsAsynchronously);

            WorkingThreadSafeDispatcher.BeginInvoke(async ct =>
            {
                try
                {
                    taskCompletionSource.TrySetResult(await ReadEventMessagesJournalInternalAsync(firstTimestampUtc, secondTimestampUtc, ct));
                }
                catch (Exception ex)
                {
                    LoggersSet.Logger.LogWarning(ex, "OPC UA events history read error.");
                    MarkSessionFailedIfNeeded(ex);
                    taskCompletionSource.TrySetResult(null);
                }
            });

            return await taskCompletionSource.Task;
        }

        #endregion

        #region private functions

        /// <summary>
        ///     Working task.
        /// </summary>
        private async Task<ElementValuesJournal[]?> ReadElementValuesJournalsInternalAsync(DateTime firstTimestampUtc, DateTime secondTimestampUtc, uint numValuesPerSubscription, object[] valueJournalSubscriptions, CancellationToken cancellationToken)
        {
            ISession? session = _session;
            if (session is null)
                return null;

            var result = new ElementValuesJournal[valueJournalSubscriptions.Length];
            var valuesPerSubscription = new List<ValueStatusTimestamp>[valueJournalSubscriptions.Length];
            var nodesToRead = new HistoryReadValueIdCollection();
            var nodesToReadIndexes = new List<int>();

            for (int i = 0; i < valueJournalSubscriptions.Length; i++)
            {
                result[i] = new ElementValuesJournal();
                valuesPerSubscription[i] = new List<ValueStatusTimestamp>();
                if (!_journalIds.TryGetValue(valueJournalSubscriptions[i], out string? id))
                    continue;
                NodeId? nodeId = ResolveNodeId(id);
                if (nodeId is null)
                    continue;
                nodesToRead.Add(new HistoryReadValueId { NodeId = nodeId });
                nodesToReadIndexes.Add(i);
            }

            if (nodesToRead.Count == 0)
                return result;

            var details = new ReadRawModifiedDetails
            {
                StartTime = firstTimestampUtc,
                EndTime = secondTimestampUtc,
                NumValuesPerNode = numValuesPerSubscription,
                IsReadModified = false,
                ReturnBounds = false
            };
            var detailsExtension = new ExtensionObject(details);

            for (int round = 0; round < MaxHistoryReadRounds && nodesToRead.Count > 0; round++)
            {
                HistoryReadResponse response = await session.HistoryReadAsync(null, detailsExtension, TimestampsToReturn.Source, false, nodesToRead, cancellationToken);

                var nextNodesToRead = new HistoryReadValueIdCollection();
                var nextNodesToReadIndexes = new List<int>();

                for (int i = 0; i < nodesToRead.Count && i < response.Results.Count; i++)
                {
                    HistoryReadResult historyReadResult = response.Results[i];
                    int subscriptionIndex = nodesToReadIndexes[i];

                    if (StatusCode.IsBad(historyReadResult.StatusCode))
                    {
                        LoggersSet.Logger.LogDebug("OPC UA history read failed. NodeId: {NodeId}, status: {Status}", nodesToRead[i].NodeId, historyReadResult.StatusCode);
                        continue;
                    }

                    if (ExtensionObject.ToEncodeable(historyReadResult.HistoryData) is HistoryData historyData)
                    {
                        foreach (DataValue dataValue in historyData.DataValues)
                        {
                            valuesPerSubscription[subscriptionIndex].Add(OpcUaValueHelper.ToValueStatusTimestamp(dataValue));
                        }
                    }

                    if (historyReadResult.ContinuationPoint is { Length: > 0 } &&
                        (numValuesPerSubscription == 0 || valuesPerSubscription[subscriptionIndex].Count < numValuesPerSubscription))
                    {
                        nextNodesToRead.Add(new HistoryReadValueId { NodeId = nodesToRead[i].NodeId, ContinuationPoint = historyReadResult.ContinuationPoint });
                        nextNodesToReadIndexes.Add(subscriptionIndex);
                    }
                    else if (historyReadResult.ContinuationPoint is { Length: > 0 })
                    {
                        await ReleaseContinuationPointAsync(session, detailsExtension, nodesToRead[i].NodeId, historyReadResult.ContinuationPoint);
                    }
                }

                nodesToRead = nextNodesToRead;
                nodesToReadIndexes = nextNodesToReadIndexes;
            }

            for (int i = 0; i < valueJournalSubscriptions.Length; i++)
            {
                if (valuesPerSubscription[i].Count > 0)
                    result[i] = ElementValuesJournal.From(valuesPerSubscription[i].OrderBy(v => v.TimestampUtc).ToArray());
            }

            return result;
        }

        private async Task ReleaseContinuationPointAsync(ISession session, ExtensionObject detailsExtension, NodeId nodeId, byte[] continuationPoint)
        {
            try
            {
                await session.HistoryReadAsync(null, detailsExtension, TimestampsToReturn.Source, true,
                    new HistoryReadValueIdCollection { new HistoryReadValueId { NodeId = nodeId, ContinuationPoint = continuationPoint } }, CancellationToken.None);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        ///     Working task.
        /// </summary>
        private async Task<List<EventMessagesCollection>?> ReadEventMessagesJournalInternalAsync(DateTime firstTimestampUtc, DateTime secondTimestampUtc, CancellationToken cancellationToken)
        {
            ISession? session = _session;
            OpcUaClientOptions? options = _options;
            if (session is null || options is null)
                return null;

            NodeId? eventNotifierNodeId = ResolveNodeId(options.EventNotifierNodeId);
            if (eventNotifierNodeId is null)
                return null;

            var details = new ReadEventDetails
            {
                StartTime = firstTimestampUtc,
                EndTime = secondTimestampUtc,
                NumValuesPerNode = 0,
                Filter = CreateEventFilter()
            };
            var detailsExtension = new ExtensionObject(details);

            var eventMessages = new List<EventMessage>();
            var nodesToRead = new HistoryReadValueIdCollection { new HistoryReadValueId { NodeId = eventNotifierNodeId } };

            for (int round = 0; round < MaxHistoryReadRounds; round++)
            {
                HistoryReadResponse response = await session.HistoryReadAsync(null, detailsExtension, TimestampsToReturn.Source, false, nodesToRead, cancellationToken);
                if (response.Results.Count == 0)
                    break;

                HistoryReadResult historyReadResult = response.Results[0];
                if (StatusCode.IsBad(historyReadResult.StatusCode))
                {
                    LoggersSet.Logger.LogDebug("OPC UA events history read failed. Status: {Status}", historyReadResult.StatusCode);
                    return null;
                }

                if (ExtensionObject.ToEncodeable(historyReadResult.HistoryData) is HistoryEvent historyEvent)
                {
                    foreach (HistoryEventFieldList historyEventFieldList in historyEvent.Events)
                    {
                        try
                        {
                            EventMessage? eventMessage = ToEventMessage(session, historyEventFieldList.EventFields, false);
                            if (eventMessage is not null)
                                eventMessages.Add(eventMessage);
                        }
                        catch (Exception ex)
                        {
                            LoggersSet.Logger.LogDebug(ex, "OPC UA historical event conversion error.");
                        }
                    }
                }

                if (historyReadResult.ContinuationPoint is not { Length: > 0 })
                    break;
                nodesToRead = new HistoryReadValueIdCollection { new HistoryReadValueId { NodeId = eventNotifierNodeId, ContinuationPoint = historyReadResult.ContinuationPoint } };
            }

            var eventMessagesCollection = new EventMessagesCollection
            {
                EventMessages = eventMessages.OrderBy(em => em.OccurrenceTimeUtc).ToList()
            };
            ElementIdsMap?.AddCommonFieldsToEventMessagesCollection(eventMessagesCollection);

            return new List<EventMessagesCollection> { eventMessagesCollection };
        }

        #endregion

        #region private fields

        private const int MaxHistoryReadRounds = 1000;

        /// <summary>
        ///     [Journal subscription object, Id on the server]. Working task only.
        /// </summary>
        private readonly Dictionary<object, string> _journalIds = new(ReferenceEqualityComparer<object>.Default);

        #endregion
    }
}
