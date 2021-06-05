using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Ssz.Utils.EventSourceModel;
using Ssz.Xi.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;


namespace TestWpfApp.Alarms
{
    public static class CtcmModelEngine
    {
        /// <summary>        
        ///     Returns new AlarmInfoViewModels or Null.
        /// </summary>
        public static async Task<IEnumerable<AlarmInfoViewModelBase>?> ProcessEventMessage(EventMessage eventMessage)
        {
            if (eventMessage.EventId != null && eventMessage.EventId.Conditions != null &&
                eventMessage.EventId.Conditions.Any(c => c.LocalId == "BeforeStateLoad"))
            {                
                return null;
            }

            try
            {
                if (eventMessage.EventType != EventType.EclipsedAlarm && eventMessage.EventType != EventType.SimpleAlarm)
                    return null;

                if (eventMessage.EventId == null ||
                    eventMessage.EventId.Conditions == null ||
                    eventMessage.EventId.SourceElementId == null ||
                    eventMessage.AlarmMessageData == null ||
                    !eventMessage.AlarmMessageData.TimeLastActive.HasValue ||
                    eventMessage.EventId.SourceElementId == "") return null;

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
                AlarmCondition condition;
                bool isDigital = false;
                string varName = eventMessage.EventId?.SourceElementId ?? "";
                bool active = eventMessage.AlarmMessageData.AlarmState.HasFlag(AlarmState.Active);
                bool unacked = eventMessage.AlarmMessageData.AlarmState.HasFlag(AlarmState.Unacked);
                uint categoryId = eventMessage.CategoryId;
                uint priority = eventMessage.Priority;

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
                        area = xmlReader.GetAttribute("Area") ?? "";
                    }
                }
                else
                {
                    var tagCompleted = new TaskCompletionSource<string>();
                    var descCompleted = new TaskCompletionSource<string>();
                    new ReadOnceValueSubscription(App.DataAccessProvider, varName + ".propTag",
                               vst => tagCompleted.SetResult(vst.Value.ValueAsString(false)));
                    new ReadOnceValueSubscription(App.DataAccessProvider, varName + ".propDescription",
                               vst => descCompleted.SetResult(vst.Value.ValueAsString(false)));
                    tag = await tagCompleted.Task ?? @"";
                    desc = await descCompleted.Task ?? @"";
                    area = @"";
                }
                

                EventSourceObject eventSourceObject = App.EventSourceModel.GetEventSourceObject(tag);                

                if (condition != AlarmCondition.None)
                {
                    bool changed = App.EventSourceModel.ProcessEventSourceObject(eventSourceObject, condition, categoryId,
                            active, unacked, eventMessage.OccurrenceTime, out bool alarmConditionChanged, out bool unackedChanged);                    
                    if (!changed) return null;
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

                AlarmInfoViewModelBase alarmInfoViewModel;
                if (conditionState != null && conditionState.LastAlarmInfoViewModel != null)
                {
                    alarmInfoViewModel = conditionState.LastAlarmInfoViewModel;
                    alarmInfoViewModel.AlarmIsActive = active;
                    alarmInfoViewModel.AlarmIsUnacked = unacked;
                    alarmInfoViewModel.OccurrenceTime = eventMessage.OccurrenceTime;
                    alarmInfoViewModel.TimeLastActive = eventMessage.AlarmMessageData.TimeLastActive.Value;
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
                        AlarmIsActive = active,
                        AlarmIsUnacked = unacked,
                        OccurrenceTime = eventMessage.OccurrenceTime,
                        TimeLastActive = eventMessage.AlarmMessageData.TimeLastActive.Value,
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
                        var eventSourceArea = App.EventSourceModel.GetEventSourceArea(area);
                        eventSourceObject.EventSourceAreas[area] = eventSourceArea;                        
                    }

                    if (condition == AlarmCondition.None)
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
            catch
            {
                //Logger.Error(ex, "DataAccessProviderOnEventNotificationEvent method error.");
            }

            return null;
        }
    }
}
