using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.DataAccess;
using Ssz.Operator.Core.Properties;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Ssz.Operator.Core.DataEngines
{
    public abstract partial class DataEngineBase
    {
        #region public functions        

        /// <summary>
        ///     Returns TagAlarmsInfo with DsConstants substituted.
        /// </summary>
        /// <param name="tagName"></param>
        /// <returns></returns>
        public TagAlarmsInfo GetTagAlarmsInfo(string? tagName)
        {
            if (string.IsNullOrEmpty(tagName)) 
                return TagAlarmsInfo.Default;

            TagAlarmsInfo? result = null;

            foreach (var tagAlarmsInfo in TagAlarmsInfosCollection)
                if (!string.IsNullOrEmpty(tagAlarmsInfo.CsvDbFileName))
                {
                    var csvDbFileName = ConstantsHelper.ComputeValue(DsProject.Instance, tagAlarmsInfo.CsvDbFileName);
                    var existingTag = DsProject.Instance.CsvDb.GetValue(csvDbFileName, tagName!, 0);
                    if (!String.IsNullOrEmpty(existingTag))
                    {
                        result = tagAlarmsInfo;
                        break;
                    }
                }

            if (result is null)
            {
                var defaultTagAlarmsInfo = TagAlarmsInfosCollection.FirstOrDefault(
                    ai => string.IsNullOrEmpty(ai.CsvDbFileName));
                if (defaultTagAlarmsInfo is not null)
                    result = defaultTagAlarmsInfo;
            }

            if (result is not null)
            {
                result = (TagAlarmsInfo)result.Clone();
                var parentContainer = new GenericContainer();
                parentContainer.ParentItem = DsProject.Instance;
                parentContainer.DsConstantsCollection.Add(new DsConstant
                {
                    Name = @"%(TAG)",
                    Value = tagName!,
                });
                result.ReplaceConstants(parentContainer);
                return result;
            }
            else
            {
                return TagAlarmsInfo.DefaultInvisible;
            }                
        }

        public async Task<(List<DsAlarmInfoViewModelBase>, List<JournalRecordViewModel>)> ProcessEventMessages(EventMessagesCollection eventMessagesCollection)
        {
            List<DsAlarmInfoViewModelBase> newDsAlarmInfoViewModels = new();
            List<JournalRecordViewModel> newJournalRecordViewModels = new();

            foreach (EventMessage eventMessage in eventMessagesCollection.EventMessages.Where(em => em is not null)
                .OrderBy(em => em.OccurrenceTimeUtc))
            {
                IEnumerable<DsAlarmInfoViewModelBase>? dsAlarmInfoViewModels = null;
                IEnumerable<JournalRecordViewModel>? dsJournalRecordViewModels = null;
                try
                {
                    (dsAlarmInfoViewModels, dsJournalRecordViewModels) = await ProcessEventMessage(eventMessage, PlayDsProjectView.EventSourceModel);
                }
                catch
                {
                }
                if (dsAlarmInfoViewModels is not null)
                    newDsAlarmInfoViewModels.AddRange(dsAlarmInfoViewModels);
                if (dsJournalRecordViewModels is not null)
                    newJournalRecordViewModels.AddRange(dsJournalRecordViewModels);
            }

            if (newDsAlarmInfoViewModels.Count > 0)
            {
                foreach (var newAlarmInfoViewModel in newDsAlarmInfoViewModels)
                {
                    string tagName = newAlarmInfoViewModel.TagName;

                    var tagDesc_Control = _tagDesc_ControlsCollection.TryGetValue(tagName);
                    if (tagDesc_Control is null)
                    {
                        var genericContainer = new GenericContainer();
                        genericContainer.ParentItem = DsProject.Instance;
                        genericContainer.DsConstantsCollection.Add(new DsConstant(TagConstant, tagName));
                        tagDesc_Control = new DataValueStyledElement(genericContainer, null, TagDescInfo);
                        _tagDesc_ControlsCollection[tagName] = tagDesc_Control;
                    }
                    tagDesc_Control.ValueChanged +=
                        o =>
                        {
                            var s = new Any(o).ValueAsString(true);
                            if (!String.IsNullOrEmpty(s))
                                newAlarmInfoViewModel.Desc = s;
                        };

                    var tagNameToDisplay_Control = _tagNameToDisplay_ControlsCollection.TryGetValue(tagName);
                    if (tagNameToDisplay_Control is null)
                    {
                        var genericContainer = new GenericContainer();
                        genericContainer.ParentItem = DsProject.Instance;
                        genericContainer.DsConstantsCollection.Add(new DsConstant(TagConstant, tagName));
                        tagNameToDisplay_Control = new DataValueStyledElement(genericContainer, null, TagNameToDisplayInfo);
                        _tagNameToDisplay_ControlsCollection[tagName] = tagNameToDisplay_Control;
                    }
                    tagNameToDisplay_Control.ValueChanged +=
                        o =>
                        {
                            var s = new Any(o).ValueAsString(true);
                            if (!String.IsNullOrEmpty(s))
                                newAlarmInfoViewModel.TagNameToDisplay = s;                            
                        };
                    
                    if (!DsProject.Instance.AlarmMessages_ElementIdsMap.IsEmpty)                        
                    {
                        string textMessage = @"";

                        Func<string, IterationInfo, string> getConstantValue =
                            (constant, iIterationInfo) =>
                            {
                                if (String.Equals(constant, @"%(TripValue)", StringComparison.InvariantCultureIgnoreCase))
                                    return new Any(newAlarmInfoViewModel.TripValue).ValueAsString(true);
                                return @"";
                            };

                        var id = tagName + "." + newAlarmInfoViewModel.CurrentAlarmConditionType + "=" + new Any(newAlarmInfoViewModel.TripValue).ValueAsString(false);
                        var mappedId = DsProject.Instance.AlarmMessages_ElementIdsMap.GetFromMap(id, getConstantValue);                        
                        if (mappedId is not null)
                        {
                            textMessage = mappedId[1] ?? @"";
                        }
                        else
                        {
                            id = tagName + "." + newAlarmInfoViewModel.CurrentAlarmConditionType;
                            mappedId = DsProject.Instance.AlarmMessages_ElementIdsMap.GetFromMap(id, getConstantValue);
                            if (mappedId is not null)
                            {
                                textMessage = mappedId[1] ?? @"";
                            }                            
                        }

                        if (textMessage != @"")
                        {                            
                            newAlarmInfoViewModel.TextMessage = textMessage;
                        }
                    }
                }

                PlayDsProjectView.EventSourceModel.OnAlarmsListChanged();                
            }

            return (newDsAlarmInfoViewModels, newJournalRecordViewModels);
        }

        public void ClearCache()
        {            
            foreach (var fe in _tagDesc_ControlsCollection.Values)
                fe.Dispose();
            _tagDesc_ControlsCollection.Clear();

            foreach (var fe in _tagNameToDisplay_ControlsCollection.Values)
                fe.Dispose();
            _tagNameToDisplay_ControlsCollection.Clear();
        }

        #endregion

        #region protected functions

        protected string?[]? SymbolsToRemoveFromTagInAlarmMessageArray { get; private set; }

        protected virtual Task<(IEnumerable<DsAlarmInfoViewModelBase>?, IEnumerable<JournalRecordViewModel>?)> ProcessEventMessage(EventMessage eventMessage,
            IEventSourceModel eventSourceModel)
        {
            return Task.FromResult(((IEnumerable<DsAlarmInfoViewModelBase>?)null, (IEnumerable<JournalRecordViewModel>?)null));
        }

        #endregion

        #region private fields
        
        private readonly CaseInsensitiveDictionary<DataValueStyledElement> _tagDesc_ControlsCollection = new();
        private readonly CaseInsensitiveDictionary<DataValueStyledElement> _tagNameToDisplay_ControlsCollection = new();

        #endregion
    }
}