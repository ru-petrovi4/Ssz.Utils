using Ssz.Utils;
using Ssz.Utils.DataSource;
using Ssz.WpfHmi.Common.ModelData.Events;
using Ssz.Xi.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Xi.Contracts.Data;

namespace TestWpfApp.Alarms
{
    public static class CtcmModelEngine
    {
        /// <summary>        
        ///     Returns new AlarmInfoViewModels or Null.
        /// </summary>
        public static async Task<IEnumerable<AlarmInfoViewModelBase>?> ProcessEventMessage(Xi.Contracts.Data.EventMessage eventMessage)
        {
            if (eventMessage.EventId != null && eventMessage.EventId.Condition != null &&
                eventMessage.EventId.Condition.Any(c => c.LocalId == "BeforeStateLoad"))
            {                
                return null;
            }

            try
            {
                if (eventMessage.EventType != EventType.EclipsedAlarm && eventMessage.EventType != EventType.SimpleAlarm)
                    return null;

                if (eventMessage.EventId == null ||
                    eventMessage.EventId.Condition == null ||
                    eventMessage.EventId.SourceId == null ||
                    eventMessage.AlarmData == null ||
                    !eventMessage.AlarmData.TimeLastActive.HasValue ||
                    !eventMessage.EventId.SourceId.IsValid()) return null;

                /*
                if (Logger.ShouldTrace(TraceEventType.Verbose))
                {
                    Logger.Verbose("After check VarName=" + eventMessage.EventId.SourceId.LocalId +
                                   ";Condition=" + eventMessage.EventId.Condition[0].LocalId +
                                   ";Active=" + eventMessage.AlarmData.AlarmState.HasFlag(AlarmState.Active) +
                                   ";Unacked=" + eventMessage.AlarmData.AlarmState.HasFlag(AlarmState.Unacked) +
                                   ";OccurrenceTime=" + eventMessage.OccurrenceTime +
                                   ";TimeLastActive=" + eventMessage.AlarmData.TimeLastActive.Value +
                                   ";TextMessage=" + eventMessage.TextMessage +
                                   ";OccurrenceId=" + eventMessage.EventId.OccurrenceId);
                }*/

                string textMessage = eventMessage.TextMessage ?? "";

                string tag;
                string desc;
                string area;
                AlarmConditionType condition;
                bool isDigital = false;
                string varName = eventMessage.EventId?.SourceId?.LocalId ?? "";
                bool active = eventMessage.AlarmData.AlarmState.HasFlag(AlarmState.Active);
                bool unacked = eventMessage.AlarmData.AlarmState.HasFlag(AlarmState.Unacked);
                uint categoryId = eventMessage.CategoryId;
                uint priority = eventMessage.Priority;

                switch (eventMessage.EventId?.Condition[0].LocalId)
                {
                    case "LoLo":
                        condition = AlarmConditionType.LowLow;
                        break;
                    case "Lo":
                        condition = AlarmConditionType.Low;
                        break;
                    case "None":
                        condition = AlarmConditionType.None;
                        break;
                    case "Hi":
                        condition = AlarmConditionType.High;
                        break;
                    case "HiHi":
                        condition = AlarmConditionType.HighHigh;
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
                        break;
                }

                if (textMessage.StartsWith("<"))
                {
                    using (XmlReader xmlReader = new XmlTextReader(textMessage, XmlNodeType.Element, null))
                    {
                        xmlReader.MoveToContent();
                        tag = xmlReader.GetAttribute("Tag") ?? "";
                        desc = xmlReader.GetAttribute("Desc") ?? "";
                        area = xmlReader.GetAttribute("Area") ?? "";
                    }
                }
                else
                {
                    var tagCompleted = new TaskCompletionSource<string>();
                    var descCompleted = new TaskCompletionSource<string>();
                    new ReadOnceValueSubscription(App.XiDataProvider, varName + ".propTag",
                               any => tagCompleted.SetResult(any.ValueAsString(false)));
                    new ReadOnceValueSubscription(App.XiDataProvider, varName + ".propDescription",
                               any => descCompleted.SetResult(any.ValueAsString(false)));
                    tag = await tagCompleted.Task ?? @"";
                    desc = await descCompleted.Task ?? @"";
                    area = @"";
                }
                

                EventSourceObject eventSourceObject = EventSourceModel.Instance.GetEventSourceObject(tag);                

                if (condition != AlarmConditionType.None)
                {
                    bool changed = EventSourceModel.Instance.ProcessEventSourceObject(eventSourceObject, condition, categoryId,
                            active, unacked, eventMessage.OccurrenceTime);                    
                    if (!changed) return null;
                }

                ConditionState? conditionState;
                if (condition != AlarmConditionType.None)
                {
                    eventSourceObject.AlarmConditions.TryGetValue(condition, out conditionState);
                }
                else
                {
                    conditionState = eventSourceObject.NormalCondition;
                }

                AlarmInfoViewModelBase alarmInfoViewModel;
                if (conditionState != null && conditionState.LastAlarmInfoViewModel != null)
                {
                    alarmInfoViewModel = conditionState.LastAlarmInfoViewModel;
                    alarmInfoViewModel.Active = active;
                    alarmInfoViewModel.Unacked = unacked;
                    alarmInfoViewModel.OccurrenceTime = eventMessage.OccurrenceTime;
                    alarmInfoViewModel.TimeLastActive = eventMessage.AlarmData.TimeLastActive.Value;
                    alarmInfoViewModel.Tag = tag;
                    alarmInfoViewModel.Desc = desc;
                    alarmInfoViewModel.Area = area;
                    alarmInfoViewModel.CurrentAlarmCondition = condition;
                    alarmInfoViewModel.IsDigital = isDigital;
                    alarmInfoViewModel.CategoryId = categoryId;
                    alarmInfoViewModel.Priority = priority;
                    alarmInfoViewModel.EventId = eventMessage.EventId;
                    alarmInfoViewModel.TextMessage = desc; // eventMessage.TextMessage contains XML
                    alarmInfoViewModel.OriginalEventMessage = eventMessage;
                }
                else
                {
                    alarmInfoViewModel = new AlarmInfoViewModelBase
                    {
                        Active = active,
                        Unacked = unacked,
                        OccurrenceTime = eventMessage.OccurrenceTime,
                        TimeLastActive = eventMessage.AlarmData.TimeLastActive.Value,
                        Tag = tag,
                        Desc = desc,
                        Area = area,
                        CurrentAlarmCondition = condition,
                        IsDigital = isDigital,
                        CategoryId = categoryId,
                        Priority = priority,
                        EventId = eventMessage.EventId,
                        TextMessage = desc, // eventMessage.TextMessage contains XML
                        OriginalEventMessage = eventMessage,
                    };

                    if (!String.IsNullOrEmpty(area))
                    {
                        var eventSourceArea = EventSourceModel.Instance.GetEventSourceArea(area);
                        eventSourceObject.EventSourceAreas[area] = eventSourceArea;                        
                    }

                    if (condition == AlarmConditionType.None)
                    {
                        eventSourceObject.NormalCondition.LastAlarmInfoViewModel = alarmInfoViewModel;                        
                    }
                    else
                    {
                        if (conditionState != null) conditionState.LastAlarmInfoViewModel = alarmInfoViewModel;                        
                    }
                }

                return new[] { alarmInfoViewModel };
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "DataProviderOnEventNotificationEvent method error.");
            }

            return null;
        }
    }
}
