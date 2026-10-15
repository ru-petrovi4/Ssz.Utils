using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Server;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AlarmConditionState = Opc.Ua.AlarmConditionState;
using SszEventId = Ssz.Utils.DataAccess.EventId;
using UaStatusCodes = Opc.Ua.StatusCodes;

namespace Ssz.Dcs.Addons.OpcUaServer.OpcUa
{
    /// <summary>
    ///     History: raw values history of tags and events history of the root folder are read from the data access provider journals.
    /// </summary>
    internal sealed partial class DcsNodeManager
    {
        #region protected functions

        protected override void HistoryReadRawModified(
            ServerSystemContext context,
            ReadRawModifiedDetails details,
            TimestampsToReturn timestampsToReturn,
            IList<HistoryReadValueId> nodesToRead,
            IList<HistoryReadResult> results,
            IList<ServiceResult> errors,
            List<NodeHandle> nodesToProcess,
            IDictionary<NodeId, NodeState> cache)
        {
            var requests = new List<(NodeHandle Handle, TagNode TagNode)>();

            foreach (NodeHandle handle in nodesToProcess)
            {
                if (handle.Node?.Handle is not TagNode tagNode || details.IsReadModified)
                {
                    errors[handle.Index] = UaStatusCodes.BadHistoryOperationUnsupported;
                    continue;
                }

                requests.Add((handle, tagNode));
            }

            if (requests.Count == 0)
                return;

            if (!TryGetTimeRange(details.StartTime, details.EndTime, out DateTime firstTimestampUtc, out DateTime secondTimestampUtc, out bool reverseOrder))
            {
                foreach (var request in requests)
                {
                    errors[request.Handle.Index] = UaStatusCodes.BadInvalidTimestampArgument;
                }
                return;
            }

            uint numValuesPerNode = details.NumValuesPerNode == 0 ? UInt32.MaxValue : details.NumValuesPerNode;

            bool newJournalSubscriptions = EnsureJournalSubscribed(requests.Select(r => r.TagNode));

            ValuesJournalSubscription?[] subscriptions;
            lock (Lock)
            {
                subscriptions = requests.Select(r => r.TagNode.ValuesJournalSubscription).ToArray();
            }
            object[] subscriptionsToRead = subscriptions.Where(s => s is not null).Cast<object>().ToArray();

            ElementValuesJournal[]? journals = null;
            if (subscriptionsToRead.Length > 0)
            {
                // A just created journal subscription is registered in the data access provider asynchronously,
                // so the first read may find nothing; retry a few times.
                for (int attempt = 0; attempt < 4; attempt++)
                {
                    if (newJournalSubscriptions)
                        Thread.Sleep(JournalSubscriptionSettleTime);

                    journals = ReadElementValuesJournals(firstTimestampUtc, secondTimestampUtc, numValuesPerNode, subscriptionsToRead);

                    if (!newJournalSubscriptions || journals is null || journals.Any(j => !j.IsEmpty()))
                        break;
                }
            }

            int journalIndex = 0;
            foreach (var request in requests)
            {
                ValuesJournalSubscription? subscription = subscriptions[requests.IndexOf(request)];
                if (subscription is null || journals is null)
                {
                    errors[request.Handle.Index] = UaStatusCodes.BadNoCommunication;
                    if (subscription is not null)
                        journalIndex += 1;
                    continue;
                }

                ElementValuesJournal journal = journals[journalIndex];
                journalIndex += 1;

                IEnumerable<ValueStatusTimestamp> valueStatusTimestamps = journal.ToValueStatusTimestams().OrderBy(v => v.TimestampUtc);
                if (reverseOrder)
                    valueStatusTimestamps = valueStatusTimestamps.Reverse();

                var historyData = new HistoryData();
                foreach (ValueStatusTimestamp vst in valueStatusTimestamps)
                {
                    historyData.DataValues.Add(OpcUaValueHelper.ToDataValue(vst));
                }

                HistoryReadResult result = results[request.Handle.Index];
                result.HistoryData = new ExtensionObject(historyData);
                result.StatusCode = historyData.DataValues.Count == 0 ? UaStatusCodes.GoodNoData : UaStatusCodes.Good;
                errors[request.Handle.Index] = ServiceResult.Good;
            }
        }

        protected override void HistoryReadEvents(
            ServerSystemContext context,
            ReadEventDetails details,
            TimestampsToReturn timestampsToReturn,
            IList<HistoryReadValueId> nodesToRead,
            IList<HistoryReadResult> results,
            IList<ServiceResult> errors,
            List<NodeHandle> nodesToProcess,
            IDictionary<NodeId, NodeState> cache)
        {
            foreach (NodeHandle handle in nodesToProcess)
            {
                if (_root is null || !ReferenceEquals(handle.Node, _root))
                {
                    errors[handle.Index] = UaStatusCodes.BadHistoryOperationUnsupported;
                    continue;
                }

                if (details.Filter is null)
                {
                    errors[handle.Index] = UaStatusCodes.BadEventFilterInvalid;
                    continue;
                }

                if (!TryGetTimeRange(details.StartTime, details.EndTime, out DateTime firstTimestampUtc, out DateTime secondTimestampUtc, out bool reverseOrder))
                {
                    errors[handle.Index] = UaStatusCodes.BadInvalidTimestampArgument;
                    continue;
                }

                List<EventMessagesCollection>? eventMessagesCollections = ReadEventMessagesJournal(firstTimestampUtc, secondTimestampUtc);
                if (eventMessagesCollections is null)
                {
                    errors[handle.Index] = UaStatusCodes.BadNoCommunication;
                    continue;
                }

                IEnumerable<EventMessage> eventMessages = eventMessagesCollections
                    .SelectMany(c => c.EventMessages)
                    .OrderBy(em => em.OccurrenceTimeUtc);
                if (reverseOrder)
                    eventMessages = eventMessages.Reverse();
                if (details.NumValuesPerNode > 0)
                    eventMessages = eventMessages.Take((int)Math.Min(details.NumValuesPerNode, Int32.MaxValue));

                var filterContext = new FilterContext(Server.NamespaceUris, Server.TypeTree, context, Server.Telemetry);
                var historyEvent = new HistoryEvent();

                lock (Lock)
                {
                    foreach (EventMessage eventMessage in eventMessages)
                    {
                        BaseEventState e = CreateHistoricalEvent(eventMessage);

                        var fields = new HistoryEventFieldList();
                        foreach (SimpleAttributeOperand clause in details.Filter.SelectClauses)
                        {
                            object? value = e.GetAttributeValue(filterContext, clause.TypeDefinitionId, clause.BrowsePath, clause.AttributeId, clause.ParsedIndexRange);
                            fields.EventFields.Add(new Variant(value));
                        }
                        historyEvent.Events.Add(fields);
                    }
                }

                HistoryReadResult result = results[handle.Index];
                result.HistoryData = new ExtensionObject(historyEvent);
                result.StatusCode = historyEvent.Events.Count == 0 ? UaStatusCodes.GoodNoData : UaStatusCodes.Good;
                errors[handle.Index] = ServiceResult.Good;
            }
        }

        #endregion

        #region private functions

        /// <summary>
        ///     OPC UA allows an unspecified start or end time. Unspecified end means now, unspecified start means one day before the end.
        /// </summary>
        private static bool TryGetTimeRange(DateTime startTime, DateTime endTime, out DateTime firstTimestampUtc, out DateTime secondTimestampUtc, out bool reverseOrder)
        {
            reverseOrder = false;
            firstTimestampUtc = default;
            secondTimestampUtc = default;

            if (startTime == DateTime.MinValue && endTime == DateTime.MinValue)
                return false;

            if (endTime == DateTime.MinValue)
                endTime = DateTime.UtcNow;
            if (startTime == DateTime.MinValue)
                startTime = endTime - TimeSpan.FromDays(1);

            firstTimestampUtc = ToUtc(startTime);
            secondTimestampUtc = ToUtc(endTime);

            if (firstTimestampUtc > secondTimestampUtc)
            {
                (firstTimestampUtc, secondTimestampUtc) = (secondTimestampUtc, firstTimestampUtc);
                reverseOrder = true;
            }

            return true;
        }

        /// <summary>
        ///     Returns true if at least one journal subscription was created by this call.
        /// </summary>
        private bool EnsureJournalSubscribed(IEnumerable<TagNode> tagNodes)
        {
            bool result = false;

            lock (Lock)
            {
                foreach (TagNode tagNode in tagNodes)
                {
                    tagNode.LastAccessUtc = DateTime.UtcNow;

                    if (tagNode.IsDeleted || tagNode.ValuesJournalSubscription is not null)
                        continue;

                    // Journal subscriptions are thread-safe in the data access provider, they can be created on any thread.
                    tagNode.ValuesJournalSubscription = new ValuesJournalSubscription(_dataAccessProvider, tagNode.ElementId);
                    result = true;
                }
            }

            return result;
        }

        private ElementValuesJournal[]? ReadElementValuesJournals(DateTime firstTimestampUtc, DateTime secondTimestampUtc, uint numValuesPerNode, object[] subscriptions)
        {
            try
            {
                Task<ElementValuesJournal[]?> task = _dataAccessProvider.ReadElementValuesJournalsAsync(firstTimestampUtc, secondTimestampUtc, numValuesPerNode, null, null, subscriptions);
                if (!task.Wait(HistoryReadTimeout))
                {
                    _logger.LogWarning("OPC UA history read timeout.");
                    return null;
                }
                return task.Result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OPC UA history read error.");
                return null;
            }
        }

        private List<EventMessagesCollection>? ReadEventMessagesJournal(DateTime firstTimestampUtc, DateTime secondTimestampUtc)
        {
            try
            {
                Task<List<EventMessagesCollection>?> task = _dataAccessProvider.ReadEventMessagesJournalAsync(firstTimestampUtc, secondTimestampUtc, null);
                if (!task.Wait(HistoryReadTimeout))
                {
                    _logger.LogWarning("OPC UA events history read timeout.");
                    return null;
                }
                return task.Result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OPC UA events history read error.");
                return null;
            }
        }

        /// <summary>
        ///     Must be called under Lock. Creates a detached event instance (not part of the address space) for history results.
        /// </summary>
        private BaseEventState CreateHistoricalEvent(EventMessage eventMessage)
        {
            if (IsAlarmMessage(eventMessage))
            {
                try
                {
                    SszEventId sszEventId = eventMessage.EventId!;
                    string sourceElementId = sszEventId.SourceElementId ?? @"";
                    string conditionName = GetConditionName(sszEventId);
                    string displayName = sourceElementId == @"" ? conditionName : sourceElementId + @" " + conditionName;

                    var alarm = new AlarmConditionState(null);
                    alarm.Create(SystemContext, null, new QualifiedName(displayName, NamespaceIndex), new LocalizedText(displayName), false);
                    alarm.AutoReportStateChanges = false;
                    alarm.ConditionClassId.Value = ObjectTypeIds.ProcessConditionClassType;
                    alarm.ConditionClassName.Value = new LocalizedText(@"ProcessConditionClass");
                    alarm.ConditionName.Value = conditionName;
                    alarm.SourceNode.Value = GetSourceNodeId(sourceElementId);
                    alarm.SourceName.Value = GetSourceName(sourceElementId);
                    alarm.BranchId.Value = NodeId.Null;
                    UpdateAlarmState(alarm, eventMessage, Guid.NewGuid().ToByteArray());
                    return alarm;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Historical alarm event creation error, reporting as simple event.");
                }
            }

            return CreateSimpleEvent(eventMessage);
        }

        #endregion

        #region private fields

        private static readonly TimeSpan HistoryReadTimeout = TimeSpan.FromSeconds(120);

        private static readonly TimeSpan JournalSubscriptionSettleTime = TimeSpan.FromMilliseconds(400);

        #endregion
    }
}
