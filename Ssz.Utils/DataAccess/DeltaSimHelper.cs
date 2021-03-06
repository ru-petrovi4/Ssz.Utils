﻿using Microsoft.Extensions.Logging;
using Ssz.Utils.EventSourceModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Ssz.Utils.DataAccess
{
    public static class DeltaSimHelper
    {
        #region public functions

        /// <summary>
        ///     Returns new AlarmInfoViewModels or null.
        /// </summary>
        /// <param name="eventSourceModel"></param>
        /// <param name="eventMessage"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static Task<IEnumerable<AlarmInfoViewModelBase>?> ProcessEventMessage(Ssz.Utils.EventSourceModel.EventSourceModel eventSourceModel, 
            EventMessage eventMessage, ILogger? logger = null)
        {
            if (eventMessage.EventId.Conditions != null &&
                eventMessage.EventId.Conditions.Any(c => c.LocalId == "BeforeStateLoad"))
            {
                return Task.FromResult((IEnumerable<AlarmInfoViewModelBase>?)null);
            }

            try
            {
                if (eventMessage.EventType != EventType.EclipsedAlarm && eventMessage.EventType != EventType.SimpleAlarm)
                    return Task.FromResult((IEnumerable<AlarmInfoViewModelBase>?)null);

                if (eventMessage.EventId.Conditions == null ||
                    eventMessage.EventId.Conditions.Count == 0 ||
                    eventMessage.EventId.SourceElementId == @"" ||
                    eventMessage.AlarmMessageData == null ||
                    !eventMessage.AlarmMessageData.TimeLastActive.HasValue ||
                    eventMessage.EventId.SourceElementId == @"")
                {
                    if (logger != null && logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug("Invalid message ignored: VarName=" + eventMessage.EventId.SourceElementId +                                       
                                       ";OccurrenceTime=" + eventMessage.OccurrenceTime +                                       
                                       ";TextMessage=" + eventMessage.TextMessage +
                                       ";OccurrenceId=" + eventMessage.EventId.OccurrenceId);
                    }
                    return Task.FromResult((IEnumerable<AlarmInfoViewModelBase>?)null);
                }
                
                if (logger != null && logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("Valid message received: VarName=" + eventMessage.EventId.SourceElementId +
                                   ";Condition=" + eventMessage.EventId.Conditions[0].LocalId +
                                   ";Active=" + eventMessage.AlarmMessageData.AlarmState.HasFlag(AlarmState.Active) +
                                   ";Unacked=" + eventMessage.AlarmMessageData.AlarmState.HasFlag(AlarmState.Unacked) +
                                   ";OccurrenceTime=" + eventMessage.OccurrenceTime +
                                   ";TimeLastActive=" + eventMessage.AlarmMessageData.TimeLastActive.Value +
                                   ";TextMessage=" + eventMessage.TextMessage +
                                   ";OccurrenceId=" + eventMessage.EventId.OccurrenceId);
                }

                string textMessage = eventMessage.TextMessage;

                string tag = "";
                string desc = "";
                string areas = "";
                string level = "";
                string eu = "";
                AlarmCondition condition;
                bool isDigital = false;
                string sourceElementId = eventMessage.EventId?.SourceElementId ?? "";
                bool active = eventMessage.AlarmMessageData.AlarmState.HasFlag(AlarmState.Active);
                bool unacked = eventMessage.AlarmMessageData.AlarmState.HasFlag(AlarmState.Unacked);
                uint categoryId = eventMessage.CategoryId;
                uint priority = eventMessage.Priority;
                bool alarmConditionChanged = false;
                bool unackedChanged = false;

                switch (eventMessage.EventId?.Conditions[0].LocalId)
                {
                    case "LoLo":
                        condition = AlarmCondition.LowLow;
                        break;
                    case "Lo":
                        condition = AlarmCondition.Low;
                        break;
                    case "None":
                        condition = AlarmCondition.None;
                        break;
                    case "Hi":
                        condition = AlarmCondition.High;
                        break;
                    case "HiHi":
                        condition = AlarmCondition.HighHigh;
                        break;
                    case "PositiveRate":
                        condition = AlarmCondition.PositiveRate;
                        break;
                    case "NegativeRate":
                        condition = AlarmCondition.NegativeRate;
                        break;
                    case "AlarmByChngPosLo":
                        condition = AlarmCondition.ChangeOfState;
                        isDigital = true;
                        break;
                    case "AlarmByChngPosLoLo":
                        condition = AlarmCondition.ChangeOfState;
                        isDigital = true;
                        break;
                    case "AlarmByChngPosHi":
                        condition = AlarmCondition.ChangeOfState;
                        isDigital = true;
                        break;
                    case "AlarmByChngPosHiHi":
                        condition = AlarmCondition.ChangeOfState;
                        isDigital = true;
                        break;
                    case "AlarmByPos_LoLo":
                        condition = AlarmCondition.OffNormal;
                        isDigital = true;
                        break;
                    case "AlarmByPos_Low":
                        condition = AlarmCondition.OffNormal;
                        isDigital = true;
                        break;
                    case "AlarmByPos_High":
                        condition = AlarmCondition.OffNormal;
                        isDigital = true;
                        break;
                    case "AlarmByPos_HiHi":
                        condition = AlarmCondition.OffNormal;
                        isDigital = true;
                        break;
                    default:
                        condition = AlarmCondition.Other;
                        break;
                }

                if (textMessage.StartsWith("<"))
                {
                    using (XmlReader xmlReader = new XmlTextReader(textMessage, XmlNodeType.Element, null))
                    {
                        xmlReader.MoveToContent();
                        tag = xmlReader.GetAttribute("Tag") ?? "";
                        desc = xmlReader.GetAttribute("Desc") ?? "";
                        areas = xmlReader.GetAttribute("Area") ?? "";
                        level = xmlReader.GetAttribute("Level") ?? "";
                        eu = xmlReader.GetAttribute("EU") ?? "";
                    }
                }
                else
                {
                    tag = sourceElementId;
                }

                EventSourceObject eventSourceObject = eventSourceModel.GetOrCreateEventSourceObject(tag, areas);

                if (condition != AlarmCondition.None)
                {
                    bool changed = eventSourceModel.ProcessEventSourceObject(eventSourceObject, condition, categoryId,
                            active, unacked, eventMessage.OccurrenceTime, out alarmConditionChanged, out unackedChanged);
                    if (!changed) return Task.FromResult((IEnumerable<AlarmInfoViewModelBase>?)null);
                }
                else
                {
                    alarmConditionChanged = (eventMessage.AlarmMessageData.AlarmStateChange & AlarmStateChangeCodes.Active) != 0;
                    unackedChanged = (eventMessage.AlarmMessageData.AlarmStateChange & AlarmStateChangeCodes.Acknowledge) != 0;
                }

                ConditionState? conditionState;
                if (condition != AlarmCondition.None)
                {
                    eventSourceObject.AlarmConditions.TryGetValue(condition, out conditionState);
                }
                else
                {
                    conditionState = eventSourceObject.NormalCondition;
                }

                double tripValue = new Any(level).ValueAsDouble(false);
                string tripValueText = level + @" " + eu;
                var alarmInfoViewModel = new AlarmInfoViewModelBase
                {
                    AlarmIsActive = active,
                    AlarmIsUnacked = unacked,
                    OccurrenceTime = eventMessage.OccurrenceTime,
                    TimeLastActive = eventMessage.AlarmMessageData.TimeLastActive.Value,
                    Tag = tag,
                    Desc = desc,
                    TripValue = tripValue,
                    TripValueText = tripValueText,
                    Area = areas,
                    CurrentAlarmCondition = condition,
                    IsDigital = isDigital,
                    CategoryId = categoryId,
                    Priority = priority,
                    EventId = eventMessage.EventId,
                    TextMessage = desc, // eventMessage.TextMessage contains XML
                    OriginalEventMessage = eventMessage,
                    AlarmConditionChanged = alarmConditionChanged,
                    UnackedChanged = unackedChanged
                };

                if (conditionState != null)
                    conditionState.LastAlarmInfoViewModel = alarmInfoViewModel;

                return Task.FromResult((IEnumerable<AlarmInfoViewModelBase>?)new[] { alarmInfoViewModel });
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError(ex, "DeltaSimHelper::ProcessEventMessage method error.");
            }

            return Task.FromResult((IEnumerable<AlarmInfoViewModelBase>?)null);
        }

        #endregion        
    }
}
