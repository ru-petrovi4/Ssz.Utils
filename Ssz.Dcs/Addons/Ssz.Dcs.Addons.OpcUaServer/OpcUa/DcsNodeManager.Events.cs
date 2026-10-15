using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Server;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using AlarmConditionState = Opc.Ua.AlarmConditionState;
using SszAlarmState = Ssz.Utils.DataAccess.AlarmState;
using SszEventId = Ssz.Utils.DataAccess.EventId;
using SszEventType = Ssz.Utils.DataAccess.EventType;
using UaStatusCodes = Opc.Ua.StatusCodes;

namespace Ssz.Dcs.Addons.OpcUaServer.OpcUa
{
    /// <summary>
    ///     Events: Ssz event messages are reported on the root folder (and through it on the Server object).
    ///     Alarms are represented by AlarmConditionType instances under the Alarms folder, one per (source, condition),
    ///     and can be acknowledged by OPC UA clients.
    /// </summary>
    internal sealed partial class DcsNodeManager
    {
        #region public functions

        /// <summary>
        ///     Returns retained alarm conditions to clients that call ConditionRefresh.
        /// </summary>
        public override ServiceResult ConditionRefresh(OperationContext context, IList<IEventMonitoredItem> monitoredItems)
        {
            lock (Lock)
            {
                if (_root is null)
                    return ServiceResult.Good;

                List<AlarmConditionState> retainedAlarms = _alarms.Values.Where(a => a.Retain.Value).ToList();

                foreach (IEventMonitoredItem monitoredItem in monitoredItems)
                {
                    if (!monitoredItem.MonitoringAllEvents && monitoredItem.NodeId != _root.NodeId)
                        continue;

                    foreach (AlarmConditionState alarm in retainedAlarms)
                    {
                        monitoredItem.QueueEvent(alarm);
                    }
                }
            }

            return ServiceResult.Good;
        }

        #endregion

        #region private functions

        /// <summary>
        ///     Called in the addon dispatcher.
        /// </summary>
        private void OnDataAccessProvider_EventMessagesCallback(object? sender, EventMessagesCallbackEventArgs args)
        {
            List<EventMessage>? eventMessages = args.EventMessagesCollection?.EventMessages;
            if (eventMessages is null || eventMessages.Count == 0)
                return;

            lock (Lock)
            {
                if (_root is null)
                    return;

                foreach (EventMessage eventMessage in eventMessages)
                {
                    try
                    {
                        if (IsAlarmMessage(eventMessage))
                            ReportAlarm(eventMessage);
                        else
                            _root.ReportEvent(SystemContext, CreateSimpleEvent(eventMessage));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Event message reporting error.");
                    }
                }
            }
        }

        private static bool IsAlarmMessage(EventMessage eventMessage)
        {
            return eventMessage.AlarmMessageData is not null &&
                eventMessage.EventId is not null &&
                eventMessage.EventType is SszEventType.SimpleAlarm or SszEventType.EclipsedAlarm or SszEventType.GroupedAlarm or SszEventType.Alert;
        }

        private static DateTime ToUtc(DateTime dateTime)
        {
            if (dateTime == default)
                return DateTime.UtcNow;
            return dateTime.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc) : dateTime.ToUniversalTime();
        }

        private static string GetConditionName(SszEventId eventId)
        {
            string? localId = eventId.Conditions?.FirstOrDefault()?.LocalId;
            if (!String.IsNullOrEmpty(localId))
                return localId;
            localId = eventId.MultiplexedAlarmContainer?.LocalId;
            if (!String.IsNullOrEmpty(localId))
                return localId;
            return @"Alarm";
        }

        private NodeId GetSourceNodeId(string sourceElementId)
        {
            return sourceElementId == @"" ? _root!.NodeId : new NodeId(sourceElementId, NamespaceIndex);
        }

        private string GetSourceName(string sourceElementId)
        {
            return sourceElementId == @"" ? _options.RootFolderName : sourceElementId;
        }

        /// <summary>
        ///     Must be called under Lock.
        /// </summary>
        private BaseEventState CreateSimpleEvent(EventMessage eventMessage)
        {
            var e = new BaseEventState(null);
            e.Initialize(SystemContext, _root, (EventSeverity)OpcUaValueHelper.ToSeverity(eventMessage.Priority), new LocalizedText(eventMessage.TextMessage ?? @""));

            string sourceElementId = eventMessage.EventId?.SourceElementId ?? @"";
            e.SetChildValue(SystemContext, BrowseNames.SourceNode, GetSourceNodeId(sourceElementId), false);
            e.SetChildValue(SystemContext, BrowseNames.SourceName, GetSourceName(sourceElementId), false);
            e.SetChildValue(SystemContext, BrowseNames.Time, ToUtc(eventMessage.OccurrenceTimeUtc), false);
            e.SetChildValue(SystemContext, BrowseNames.ReceiveTime, DateTime.UtcNow, false);

            return e;
        }

        /// <summary>
        ///     Must be called under Lock.
        /// </summary>
        private void ReportAlarm(EventMessage eventMessage)
        {
            SszEventId sszEventId = eventMessage.EventId!;
            string sourceElementId = sszEventId.SourceElementId ?? @"";
            string conditionName = GetConditionName(sszEventId);
            string key = sourceElementId + @"/" + conditionName;

            if (!_alarms.TryGetValue(key, out AlarmConditionState? alarm))
            {
                alarm = CreateAlarmCondition(sourceElementId, conditionName, key);
                _alarms[key] = alarm;
            }

            UpdateAlarmState(alarm, eventMessage, RegisterEventId(eventMessage));

            _root!.ReportEvent(SystemContext, alarm);
        }

        /// <summary>
        ///     Must be called under Lock.
        /// </summary>
        private AlarmConditionState CreateAlarmCondition(string sourceElementId, string conditionName, string key)
        {
            string displayName = sourceElementId == @"" ? conditionName : sourceElementId + @" " + conditionName;

            var alarm = new AlarmConditionState(_alarmsFolder)
            {
                SymbolicName = key,
                ReferenceTypeId = ReferenceTypeIds.HasComponent
            };
            alarm.Create(SystemContext, new NodeId(AlarmNodeIdPrefix + key, NamespaceIndex), new QualifiedName(displayName, NamespaceIndex), new LocalizedText(displayName), true);
            alarm.AutoReportStateChanges = false;

            alarm.ConditionClassId.Value = ObjectTypeIds.ProcessConditionClassType;
            alarm.ConditionClassName.Value = new LocalizedText(@"ProcessConditionClass");
            alarm.ConditionName.Value = conditionName;
            alarm.SourceNode.Value = GetSourceNodeId(sourceElementId);
            alarm.SourceName.Value = GetSourceName(sourceElementId);
            alarm.BranchId.Value = NodeId.Null;
            alarm.Retain.Value = false;
            alarm.SetEnableState(SystemContext, true);
            alarm.SetActiveState(SystemContext, false);
            alarm.SetAcknowledgedState(SystemContext, true);
            if (alarm.SuppressedState is not null)
                alarm.SetSuppressedState(SystemContext, false);
            alarm.OnAcknowledge = OnAlarmAcknowledge;

            _alarmsFolder!.AddChild(alarm);
            AddPredefinedNode(SystemContext, alarm);

            return alarm;
        }

        /// <summary>
        ///     Must be called under Lock.
        /// </summary>
        private void UpdateAlarmState(AlarmConditionState alarm, EventMessage eventMessage, byte[] opcUaEventId)
        {
            AlarmMessageData alarmMessageData = eventMessage.AlarmMessageData!;
            bool disabled = (alarmMessageData.AlarmState & SszAlarmState.Disabled) != 0;
            bool active = (alarmMessageData.AlarmState & SszAlarmState.Active) != 0;
            bool unacked = (alarmMessageData.AlarmState & SszAlarmState.Unacked) != 0;
            bool suppressed = (alarmMessageData.AlarmState & SszAlarmState.Suppressed) != 0;

            alarm.SetEnableState(SystemContext, !disabled);
            alarm.SetActiveState(SystemContext, active);
            alarm.SetAcknowledgedState(SystemContext, !unacked);
            if (alarm.SuppressedState is not null)
                alarm.SetSuppressedState(SystemContext, suppressed);
            alarm.Retain.Value = !disabled && (active || unacked);

            alarm.Time.Value = ToUtc(eventMessage.OccurrenceTimeUtc);
            alarm.ReceiveTime.Value = DateTime.UtcNow;
            alarm.Message.Value = new LocalizedText(eventMessage.TextMessage ?? @"");
            if (alarm.LastSeverity is not null)
                alarm.LastSeverity.Value = alarm.Severity.Value;
            alarm.Severity.Value = OpcUaValueHelper.ToSeverity(eventMessage.Priority);
            if (alarm.ClientUserId is not null)
                alarm.ClientUserId.Value = eventMessage.OperatorName ?? @"";
            if (alarm.Quality is not null)
                alarm.Quality.Value = UaStatusCodes.Good;
            alarm.EventId.Value = opcUaEventId;

            alarm.ClearChangeMasks(SystemContext, true);
        }

        /// <summary>
        ///     Must be called under Lock. Returns OPC UA EventId that maps to the Ssz EventId for acknowledgement.
        /// </summary>
        private byte[] RegisterEventId(EventMessage eventMessage)
        {
            SszEventId sszEventId = eventMessage.EventId!;
            var ackEventId = new SszEventId
            {
                SourceElementId = sszEventId.SourceElementId,
                MultiplexedAlarmContainer = sszEventId.MultiplexedAlarmContainer,
                Conditions = sszEventId.Conditions,
                OccurrenceId = sszEventId.OccurrenceId,
                TimeLastActiveUtc = eventMessage.AlarmMessageData?.TimeLastActive ?? sszEventId.TimeLastActiveUtc
            };

            Guid guid = Guid.NewGuid();
            _eventIds[guid] = ackEventId;
            _eventIdsQueue.Enqueue(guid);
            while (_eventIdsQueue.Count > MaxRegisteredEventIds)
            {
                _eventIds.Remove(_eventIdsQueue.Dequeue());
            }

            return guid.ToByteArray();
        }

        /// <summary>
        ///     Called by the OPC UA stack when a client acknowledges an alarm condition.
        /// </summary>
        private ServiceResult OnAlarmAcknowledge(ISystemContext context, ConditionState condition, byte[] eventId, LocalizedText comment)
        {
            SszEventId? sszEventId = null;
            lock (Lock)
            {
                if (eventId is { Length: 16 })
                    _eventIds.TryGetValue(new Guid(eventId), out sszEventId);
            }

            if (sszEventId is null)
                return UaStatusCodes.BadEventIdUnknown;

            string operatorName = (context as ServerSystemContext)?.UserIdentity?.DisplayName ?? context?.UserId ?? @"";
            if (String.IsNullOrEmpty(operatorName))
                operatorName = @"OPC UA";

            _dataAccessProvider.AckAlarms(operatorName, comment?.Text ?? @"", new[] { sszEventId });

            return ServiceResult.Good;
        }

        #endregion

        #region private fields

        private const string AlarmNodeIdPrefix = @"/Alarms/";

        private const int MaxRegisteredEventIds = 20000;

        /// <summary>
        ///     [SourceElementId/ConditionName, Alarm condition]
        /// </summary>
        private readonly Dictionary<string, AlarmConditionState> _alarms = new();

        /// <summary>
        ///     [OPC UA EventId, Ssz EventId for acknowledgement]
        /// </summary>
        private readonly Dictionary<Guid, SszEventId> _eventIds = new();

        private readonly Queue<Guid> _eventIdsQueue = new();

        #endregion
    }
}
