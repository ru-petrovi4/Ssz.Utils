using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Ssz.Utils.DataAccess
{
    public static class ExperionHelper
    {
        #region public functions

        /// <summary>
        ///     Returns new AlarmInfoViewModels or null.
        /// </summary>
        /// <param name="eventSourceModel"></param>
        /// <param name="eventMessage"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static Task<IEnumerable<AlarmInfoViewModelBase>?> ProcessEventMessage(IEventSourceModel eventSourceModel, 
            EventMessage eventMessage, ILogger? logger = null)
        {
            try
            {
                if (eventMessage.EventType != EventType.EclipsedAlarm && eventMessage.EventType != EventType.SimpleAlarm)
                    return Task.FromResult((IEnumerable<AlarmInfoViewModelBase>?)null);

                if (eventMessage.EventId is null ||
                    eventMessage.EventId.Conditions is null ||
                    eventMessage.EventId.Conditions.Count == 0 ||
                    eventMessage.EventId.SourceElementId == @"" ||
                    eventMessage.AlarmMessageData is null ||
                    !eventMessage.AlarmMessageData.TimeLastActive.HasValue ||
                    eventMessage.EventId.SourceElementId == @"")
                {
                    if (logger is not null && logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug("Invalid message ignored: VarName=" + eventMessage.EventId?.SourceElementId +                                       
                                       ";OccurrenceTime=" + eventMessage.OccurrenceTimeUtc +                                       
                                       ";TextMessage=" + eventMessage.TextMessage +
                                       ";OccurrenceId=" + eventMessage.EventId?.OccurrenceId);
                    }
                    return Task.FromResult((IEnumerable<AlarmInfoViewModelBase>?)null);
                }
                
                if (logger is not null && logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("Valid message received: VarName=" + eventMessage.EventId.SourceElementId +
                                   ";Condition=" + eventMessage.EventId.Conditions[0].LocalId +
                                   ";Active=" + eventMessage.AlarmMessageData.AlarmState.HasFlag(AlarmState.Active) +
                                   ";Unacked=" + eventMessage.AlarmMessageData.AlarmState.HasFlag(AlarmState.Unacked) +
                                   ";OccurrenceTime=" + eventMessage.OccurrenceTimeUtc +
                                   ";TimeLastActive=" + eventMessage.AlarmMessageData.TimeLastActive.Value +
                                   ";TextMessage=" + eventMessage.TextMessage +
                                   ";OccurrenceId=" + eventMessage.EventId.OccurrenceId);
                }

                string textMessage = eventMessage.TextMessage;

                string tag = eventMessage.EventId?.SourceElementId ?? "";
                string desc = "";
                string area = "";
                string level = "";
                string eu = "";
                AlarmConditionType condition;
                bool isDigital = false;                
                bool active = eventMessage.AlarmMessageData.AlarmState.HasFlag(AlarmState.Active);
                bool unacked = eventMessage.AlarmMessageData.AlarmState.HasFlag(AlarmState.Unacked);
                uint categoryId = eventMessage.CategoryId;
                uint priority = eventMessage.Priority;
                bool alarmConditionChanged = false;
                bool unackedChanged = false;

                switch (eventMessage.EventId?.Conditions[0].LocalId)
                {
                    case "PVLL":
                    case "LL":                    
                        condition = AlarmConditionType.LowLow;
                        categoryId = 2;
                        break;
                    case "PVLO":
                    case "LO":
                    case "L":
                        condition = AlarmConditionType.Low;
                        categoryId = 1;
                        break;
                    case "None":
                        condition = AlarmConditionType.None;
                        break;
                    case "PVHI":
                    case "HI":
                    case "H":
                        condition = AlarmConditionType.High;
                        categoryId = 1;
                        break;
                    case "HH":
                    case "PVHH":
                        condition = AlarmConditionType.HighHigh;
                        categoryId = 2;
                        break;
                    case "PositiveRate":
                        condition = AlarmConditionType.PositiveRate;
                        break;
                    case "NegativeRate":
                        condition = AlarmConditionType.NegativeRate;
                        break;
                    case "AlarmByChngPosLo":
                        condition = AlarmConditionType.ChangeOfState;
                        isDigital = true;
                        break;
                    case "AlarmByChngPosLoLo":
                        condition = AlarmConditionType.ChangeOfState;
                        isDigital = true;
                        break;
                    case "AlarmByChngPosHi":
                        condition = AlarmConditionType.ChangeOfState;
                        isDigital = true;
                        break;
                    case "AlarmByChngPosHiHi":
                        condition = AlarmConditionType.ChangeOfState;
                        isDigital = true;
                        break;
                    case "AlarmByPos_LoLo":
                        condition = AlarmConditionType.OffNormal;
                        isDigital = true;
                        break;
                    case "AlarmByPos_Low":
                        condition = AlarmConditionType.OffNormal;
                        isDigital = true;
                        break;
                    case "AlarmByPos_High":
                        condition = AlarmConditionType.OffNormal;
                        isDigital = true;
                        break;
                    case "AlarmByPos_HiHi":
                        condition = AlarmConditionType.OffNormal;
                        isDigital = true;
                        break;
                    default:
                        condition = AlarmConditionType.Other;
                        categoryId = 1;
                        break;
                }

                EventSourceObject eventSourceObject = eventSourceModel.GetOrCreateEventSourceObject(tag, area);

                if (condition != AlarmConditionType.None)
                {
                    bool changed = eventSourceModel.ProcessEventSourceObject(eventSourceObject, condition, categoryId,
                            active, unacked, eventMessage.OccurrenceTimeUtc, out alarmConditionChanged, out unackedChanged);
                    if (!changed) return Task.FromResult((IEnumerable<AlarmInfoViewModelBase>?)null);
                }
                else
                {
                    alarmConditionChanged = (eventMessage.AlarmMessageData.AlarmStateChange & AlarmStateChangeCodes.Active) != 0;
                    unackedChanged = (eventMessage.AlarmMessageData.AlarmStateChange & AlarmStateChangeCodes.Acknowledge) != 0;
                }

                AlarmConditionState? conditionState;
                if (condition != AlarmConditionType.None)
                {
                    eventSourceObject.AlarmConditions.TryGetValue(condition, out conditionState);
                }
                else
                {
                    conditionState = eventSourceObject.NormalConditionState;
                }

                double tripValue = new Any(level).ValueAsDouble(false);
                string tripValueText = level + @" " + eu;
                var alarmInfoViewModel = new AlarmInfoViewModelBase
                {
                    AlarmIsActive = active,
                    AlarmIsUnacked = unacked,
                    OccurrenceTime = eventMessage.OccurrenceTimeUtc,
                    TimeLastActive = eventMessage.AlarmMessageData.TimeLastActive.Value,
                    Tag = tag,
                    Desc = desc,
                    TripValue = tripValue,
                    TripValueText = tripValueText,
                    Area = area,
                    CurrentAlarmConditionType = condition,
                    IsDigital = isDigital,
                    CategoryId = categoryId,
                    Priority = priority,
                    EventId = eventMessage.EventId,
                    TextMessage = desc, // eventMessage.TextMessage contains XML
                    OriginalEventMessage = eventMessage,
                    AlarmConditionChanged = alarmConditionChanged,
                    UnackedChanged = unackedChanged
                };

                if (conditionState is not null)
                    conditionState.LastAlarmInfoViewModel = alarmInfoViewModel;

                return Task.FromResult((IEnumerable<AlarmInfoViewModelBase>?)new[] { alarmInfoViewModel });
            }
            catch (Exception ex)
            {
                if (logger is not null)
                    logger.LogError(ex, "DeltaSimHelper::ProcessEventMessage method error.");
            }

            return Task.FromResult((IEnumerable<AlarmInfoViewModelBase>?)null);
        }

        #endregion        
    }
}
