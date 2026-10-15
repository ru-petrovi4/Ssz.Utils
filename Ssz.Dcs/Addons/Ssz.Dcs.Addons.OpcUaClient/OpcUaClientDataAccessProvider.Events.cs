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
using SszAlarmState = Ssz.Utils.DataAccess.AlarmState;
using SszEventId = Ssz.Utils.DataAccess.EventId;
using SszEventType = Ssz.Utils.DataAccess.EventType;
using SszTypeId = Ssz.Utils.DataAccess.TypeId;

namespace Ssz.Dcs.Addons.OpcUaClient
{
    /// <summary>
    ///     Events and alarms: OPC UA events are mapped to Ssz event messages, condition events to alarm messages;
    ///     acknowledgement calls the standard Acknowledge method of the condition.
    /// </summary>
    public partial class OpcUaClientDataAccessProvider
    {
        #region public functions

        public override void AckAlarms(string operatorName, string comment, SszEventId[] eventIdsToAck)
        {
            WorkingThreadSafeDispatcher.BeginInvoke(async ct =>
            {
                ISession? session = _session;
                if (session is null)
                    return;

                var requests = new CallMethodRequestCollection();
                foreach (SszEventId eventId in eventIdsToAck)
                {
                    if (!TryParseOccurrenceId(eventId.OccurrenceId, out byte[]? opcUaEventId, out NodeId? conditionId))
                    {
                        LoggersSet.Logger.LogWarning("Cannot acknowledge alarm, unknown OccurrenceId: {OccurrenceId}", eventId.OccurrenceId);
                        continue;
                    }

                    requests.Add(new CallMethodRequest
                    {
                        ObjectId = conditionId,
                        MethodId = MethodIds.AcknowledgeableConditionType_Acknowledge,
                        InputArguments = new VariantCollection
                        {
                            new Variant(opcUaEventId),
                            new Variant(new LocalizedText(operatorName == @"" ? comment : operatorName + @": " + comment))
                        }
                    });
                }

                if (requests.Count == 0)
                    return;

                try
                {
                    CallResponse response = await session.CallAsync(null, requests, ct);
                    for (int i = 0; i < response.Results.Count; i++)
                    {
                        if (StatusCode.IsBad(response.Results[i].StatusCode))
                            LoggersSet.Logger.LogWarning("OPC UA Acknowledge failed. ConditionId: {ConditionId}, status: {Status}", requests[i].ObjectId, response.Results[i].StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    LoggersSet.Logger.LogWarning(ex, "OPC UA Acknowledge error.");
                    MarkSessionFailedIfNeeded(ex);
                }
            });
        }

        #endregion

        #region private functions

        /// <summary>
        ///     Select clauses order is the field order in <see cref="ToEventMessage"/>.
        /// </summary>
        private static EventFilter CreateEventFilter()
        {
            var filter = new EventFilter();
            filter.AddSelectClause(ObjectTypeIds.BaseEventType, BrowseNames.EventId);
            filter.AddSelectClause(ObjectTypeIds.BaseEventType, BrowseNames.EventType);
            filter.AddSelectClause(ObjectTypeIds.BaseEventType, BrowseNames.SourceNode);
            filter.AddSelectClause(ObjectTypeIds.BaseEventType, BrowseNames.SourceName);
            filter.AddSelectClause(ObjectTypeIds.BaseEventType, BrowseNames.Time);
            filter.AddSelectClause(ObjectTypeIds.BaseEventType, BrowseNames.ReceiveTime);
            filter.AddSelectClause(ObjectTypeIds.BaseEventType, BrowseNames.Message);
            filter.AddSelectClause(ObjectTypeIds.BaseEventType, BrowseNames.Severity);
            filter.AddSelectClause(ObjectTypeIds.ConditionType, BrowseNames.ConditionName);
            filter.AddSelectClause(ObjectTypeIds.ConditionType, BrowseNames.Retain);
            filter.AddSelectClause(ObjectTypeIds.ConditionType, BrowseNames.BranchId);
            filter.AddSelectClause(ObjectTypeIds.ConditionType, BrowseNames.ConditionClassName);
            filter.AddSelectClause(ObjectTypeIds.ConditionType, @"EnabledState/Id", Attributes.Value);
            filter.AddSelectClause(ObjectTypeIds.ConditionType, @"", Attributes.NodeId);
            filter.AddSelectClause(ObjectTypeIds.AcknowledgeableConditionType, @"AckedState/Id", Attributes.Value);
            filter.AddSelectClause(ObjectTypeIds.AcknowledgeableConditionType, @"ConfirmedState/Id", Attributes.Value);
            filter.AddSelectClause(ObjectTypeIds.AlarmConditionType, @"ActiveState/Id", Attributes.Value);
            filter.AddSelectClause(ObjectTypeIds.AlarmConditionType, @"ActiveState/TransitionTime", Attributes.Value);
            filter.AddSelectClause(ObjectTypeIds.AlarmConditionType, @"SuppressedState/Id", Attributes.Value);
            return filter;
        }

        private const int Field_EventId = 0;
        private const int Field_EventType = 1;
        private const int Field_SourceNode = 2;
        private const int Field_SourceName = 3;
        private const int Field_Time = 4;
        private const int Field_ReceiveTime = 5;
        private const int Field_Message = 6;
        private const int Field_Severity = 7;
        private const int Field_ConditionName = 8;
        private const int Field_Retain = 9;
        private const int Field_BranchId = 10;
        private const int Field_ConditionClassName = 11;
        private const int Field_EnabledState = 12;
        private const int Field_ConditionId = 13;
        private const int Field_AckedState = 14;
        private const int Field_ConfirmedState = 15;
        private const int Field_ActiveState = 16;
        private const int Field_ActiveStateTransitionTime = 17;
        private const int Field_SuppressedState = 18;

        private static readonly string[] FieldNames =
        {
            @"EventId", @"EventType", @"SourceNode", @"SourceName", @"Time", @"ReceiveTime", @"Message", @"Severity",
            @"ConditionName", @"Retain", @"BranchId", @"ConditionClassName", @"EnabledState", @"ConditionId",
            @"AckedState", @"ConfirmedState", @"ActiveState", @"ActiveStateTransitionTime", @"SuppressedState"
        };

        /// <summary>
        ///     Stack thread.
        /// </summary>
        private void OnFastEvent(Subscription subscription, EventNotificationList notification, IList<string> stringTable)
        {
            if (notification.Events is null || notification.Events.Count == 0)
                return;

            ISession? session = _session;
            if (session is null)
                return;

            var eventMessages = new List<EventMessage>(notification.Events.Count);
            foreach (EventFieldList eventFieldList in notification.Events)
            {
                try
                {
                    EventMessage? eventMessage = ToEventMessage(session, eventFieldList.EventFields, true);
                    if (eventMessage is not null)
                        eventMessages.Add(eventMessage);
                }
                catch (Exception ex)
                {
                    LoggersSet.Logger.LogDebug(ex, "OPC UA event conversion error.");
                }
            }

            if (eventMessages.Count == 0)
                return;

            RaiseEventMessagesCallback(eventMessages);
        }

        private void RaiseEventMessagesCallback(List<EventMessage> eventMessages)
        {
            IDispatcher? callbackDispatcher = CallbackDispatcher;
            if (callbackDispatcher is null)
                return;

            try
            {
                callbackDispatcher.BeginInvoke(ct =>
                {
                    var eventMessagesCollection = new EventMessagesCollection
                    {
                        EventMessages = eventMessages
                    };
                    ElementIdsMap?.AddCommonFieldsToEventMessagesCollection(eventMessagesCollection);
                    EventMessagesCallback(this, new EventMessagesCallbackEventArgs
                    {
                        EventMessagesCollection = eventMessagesCollection
                    });
                });
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        ///     Maps OPC UA event fields (see <see cref="CreateEventFilter"/>) to an Ssz event message.
        ///     Condition events become alarms with AlarmMessageData, as the OPC DA/AE client does.
        /// </summary>
        private EventMessage? ToEventMessage(ISession session, VariantCollection fields, bool trackAlarmState)
        {
            if (fields is null || fields.Count < FieldNames.Length)
                return null;

            byte[]? opcUaEventId = fields[Field_EventId].Value as byte[];
            NodeId? eventTypeId = fields[Field_EventType].Value as NodeId;
            NodeId? sourceNodeId = fields[Field_SourceNode].Value as NodeId;
            string sourceName = fields[Field_SourceName].Value as string ?? @"";
            DateTime time = ToUtc(fields[Field_Time].Value as DateTime?) ?? DateTime.UtcNow;
            string message = ToText(fields[Field_Message].Value);
            ushort severity = fields[Field_Severity].Value is ushort s ? s : (ushort)0;
            string? conditionName = fields[Field_ConditionName].Value as string;
            NodeId? conditionId = fields[Field_ConditionId].Value as NodeId;
            bool? enabled = fields[Field_EnabledState].Value as bool?;
            bool? acked = fields[Field_AckedState].Value as bool?;
            bool? active = fields[Field_ActiveState].Value as bool?;
            DateTime? activeTransitionTime = ToUtc(fields[Field_ActiveStateTransitionTime].Value as DateTime?);
            bool? suppressed = fields[Field_SuppressedState].Value as bool?;

            if (eventTypeId is not null && (session.TypeTree.IsTypeOf(eventTypeId, ObjectTypeIds.RefreshStartEventType) ||
                    session.TypeTree.IsTypeOf(eventTypeId, ObjectTypeIds.RefreshEndEventType)))
                return null;

            bool isCondition = conditionId is not null && !NodeId.IsNull(conditionId) ||
                (eventTypeId is not null && session.TypeTree.IsTypeOf(eventTypeId, ObjectTypeIds.ConditionType));

            string sourceElementId = sourceName != @"" ? sourceName : sourceNodeId?.ToString() ?? @"";

            var eventId = new SszEventId
            {
                SourceElementId = sourceElementId,
                OccurrenceId = MakeOccurrenceId(opcUaEventId, conditionId)
            };

            var eventMessage = new EventMessage(eventId)
            {
                OccurrenceTimeUtc = time,
                TextMessage = message,
                Priority = severity,
                CategoryId = 0,
                OperatorName = @""
            };

            if (isCondition)
            {
                string condition = String.IsNullOrEmpty(conditionName) ? @"Alarm" : conditionName;
                eventId.Conditions = new List<SszTypeId> { new SszTypeId { LocalId = condition } };
                eventMessage.EventType = SszEventType.SimpleAlarm;

                bool isActive = active ?? false;
                bool isUnacked = !(acked ?? true);
                SszAlarmState alarmState = SszAlarmState.Initial;
                if (isActive)
                    alarmState |= SszAlarmState.Active;
                if (isUnacked)
                    alarmState |= SszAlarmState.Unacked;
                if (enabled == false)
                    alarmState |= SszAlarmState.Disabled;
                if (suppressed == true)
                    alarmState |= SszAlarmState.Suppressed;

                var alarmMessageData = new AlarmMessageData
                {
                    AlarmState = alarmState
                };

                string key = sourceElementId + @"/" + condition;
                lock (_alarmsSyncRoot)
                {
                    _alarmStates.TryGetValue(key, out AlarmStateInfo? previous);

                    DateTime? timeLastActive = activeTransitionTime;
                    if (isActive)
                        timeLastActive ??= previous?.TimeLastActive ?? time;
                    else
                        timeLastActive ??= previous?.TimeLastActive;
                    alarmMessageData.TimeLastActive = timeLastActive ?? time;

                    uint alarmStateChange = 0;
                    if (previous is null || previous.Active != isActive)
                        alarmStateChange |= AlarmStateChangeCodes.Active;
                    if (previous is null || previous.Unacked != isUnacked)
                        alarmStateChange |= AlarmStateChangeCodes.Acknowledge;
                    if (previous is not null && previous.Message != message)
                        alarmStateChange |= AlarmStateChangeCodes.Message;
                    alarmMessageData.AlarmStateChange = alarmStateChange;

                    if (trackAlarmState)
                    {
                        _alarmStates[key] = new AlarmStateInfo
                        {
                            Active = isActive,
                            Unacked = isUnacked,
                            TimeLastActive = alarmMessageData.TimeLastActive,
                            Message = message
                        };
                    }
                }

                eventMessage.AlarmMessageData = alarmMessageData;
            }
            else
            {
                eventMessage.EventType = eventTypeId is not null && session.TypeTree.IsTypeOf(eventTypeId, ObjectTypeIds.AuditEventType)
                    ? SszEventType.OperatorActionEvent
                    : SszEventType.SystemEvent;
            }

            var eventMessageFields = new CaseInsensitiveOrderedDictionary<string?>();
            for (int i = 0; i < FieldNames.Length; i++)
            {
                eventMessageFields[FieldNames[i]] = ToText(fields[i].Value);
            }
            eventMessage.Fields = eventMessageFields;

            return eventMessage;
        }

        private static string MakeOccurrenceId(byte[]? opcUaEventId, NodeId? conditionId)
        {
            string eventIdPart = opcUaEventId is null ? @"" : Convert.ToHexString(opcUaEventId);
            string conditionPart = conditionId is null || NodeId.IsNull(conditionId) ? @"" : conditionId.ToString();
            return eventIdPart + @"|" + conditionPart;
        }

        private static bool TryParseOccurrenceId(string? occurrenceId, out byte[]? opcUaEventId, out NodeId? conditionId)
        {
            opcUaEventId = null;
            conditionId = null;
            if (String.IsNullOrEmpty(occurrenceId))
                return false;
            int i = occurrenceId.IndexOf('|');
            if (i < 0)
                return false;
            try
            {
                string eventIdPart = occurrenceId.Substring(0, i);
                string conditionPart = occurrenceId.Substring(i + 1);
                if (eventIdPart == @"" || conditionPart == @"")
                    return false;
                opcUaEventId = Convert.FromHexString(eventIdPart);
                conditionId = NodeId.Parse(conditionPart);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static DateTime? ToUtc(DateTime? dateTime)
        {
            if (dateTime is null || dateTime.Value == DateTime.MinValue)
                return null;
            DateTime value = dateTime.Value;
            return value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();
        }

        private static string ToText(object? value)
        {
            switch (value)
            {
                case null:
                    return @"";
                case LocalizedText localizedText:
                    return localizedText.Text ?? @"";
                case byte[] bytes:
                    return Convert.ToHexString(bytes);
                case DateTime dateTime:
                    return dateTime.ToString(@"O");
                default:
                    return value.ToString() ?? @"";
            }
        }

        #endregion

        #region private fields

        private readonly object _alarmsSyncRoot = new();

        /// <summary>
        ///     [SourceElementId/Condition, last known state], to fill AlarmStateChange and TimeLastActive.
        /// </summary>
        private readonly Dictionary<string, AlarmStateInfo> _alarmStates = new();

        #endregion

        private sealed class AlarmStateInfo
        {
            public bool Active;

            public bool Unacked;

            public DateTime? TimeLastActive;

            public string Message = @"";
        }
    }
}
