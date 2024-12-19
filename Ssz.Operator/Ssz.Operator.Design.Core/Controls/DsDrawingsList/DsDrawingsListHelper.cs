using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ssz.Operator.Core.Utils; using Ssz.Utils; 
using Ssz.Operator.Core;
using Ssz.Operator.Core.ControlsDesign;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.VisualEditors;
using Ssz.Operator.Core.Addons;

namespace Ssz.Operator.Design.Core.Controls
{
    internal static class DsDrawingsListHelper
    {
        #region public functions

        public static void FillGroupViewModelWithDsPages(GroupViewModel rootGroupViewModel,
            IEnumerable<DsPageDrawingInfo> dsPageDrawingInfos,
            DsPagesGroupingFilter dsPagesGroupingFilter)
        {
            foreach (DsPageDrawingInfo dsPageDrawingInfo in dsPageDrawingInfos.OrderBy(di => di.Name))
            {
                rootGroupViewModel.Entities.Add(new DsPageDrawingInfoViewModel(dsPageDrawingInfo));
            }

            if (dsPagesGroupingFilter.GroupByStyle)
            {
                GroupDsPagesByStyle(rootGroupViewModel);
            }

            if (dsPagesGroupingFilter.GroupByGroup)
            {
                GroupDsPagesByGroup(rootGroupViewModel);
            }

            if (dsPagesGroupingFilter.GroupByMark)
            {
                GroupDsPagesByMark(rootGroupViewModel);
            }

            int number = 1;
            NuberDsPages(rootGroupViewModel, ref number);
        }

        public static void FillGroupViewModelWithStandardSimpleDsShapes(GroupViewModel rootGroupViewModel)
        {
            var entities = new List<EntityInfoViewModel>();

            entities.Add(
                new EntityInfoViewModel(new EntityInfo(TextBlockDsShape.DsShapeTypeNameToDisplay,
                    TextBlockDsShape.DsShapeTypeGuid, "", "")));

            entities.Add(
                new EntityInfoViewModel(new EntityInfo(UpDownDsShape.DsShapeTypeNameToDisplay,
                    UpDownDsShape.DsShapeTypeGuid, "", "")));

            entities.Add(
                new EntityInfoViewModel(new EntityInfo(ContentDsShape.DsShapeTypeNameToDisplay,
                    ContentDsShape.DsShapeTypeGuid, "", "")));

            entities.Add(
                new EntityInfoViewModel(new EntityInfo(ButtonDsShape.DsShapeTypeNameToDisplay,
                    ButtonDsShape.DsShapeTypeGuid, "", "")));

            entities.Add(
                new EntityInfoViewModel(new EntityInfo(GeometryDsShape.DsShapeTypeNameToDisplay,
                    GeometryDsShape.DsShapeTypeGuid, "", "")));

            entities.Add(
                new EntityInfoViewModel(new EntityInfo(GeometryButtonDsShape.DsShapeTypeNameToDisplay,
                    GeometryButtonDsShape.DsShapeTypeGuid, "", "")));

            entities.Add(
                new EntityInfoViewModel(new EntityInfo(TextBoxDsShape.DsShapeTypeNameToDisplay,
                    TextBoxDsShape.DsShapeTypeGuid, "", "")));            

            entities.Add(
                new EntityInfoViewModel(new EntityInfo(ToggleButtonDsShape.DsShapeTypeNameToDisplay,
                    ToggleButtonDsShape.DsShapeTypeGuid, "", "")));

            entities.Add(
                new EntityInfoViewModel(new EntityInfo(SliderDsShape.DsShapeTypeNameToDisplay,
                    SliderDsShape.DsShapeTypeGuid, "", "")));

            entities.Add(
                new EntityInfoViewModel(new EntityInfo(CommandListenerDsShape.DsShapeTypeNameToDisplay,
                    CommandListenerDsShape.DsShapeTypeGuid, "", "")));

            entities.Add(
                new EntityInfoViewModel(new EntityInfo(ContextMenuDsShape.DsShapeTypeNameToDisplay,
                    ContextMenuDsShape.DsShapeTypeGuid, "", "")));

            entities.Add(
                new EntityInfoViewModel(new EntityInfo(ComboBoxDsShape.DsShapeTypeNameToDisplay,
                    ComboBoxDsShape.DsShapeTypeGuid, "", "")));

            entities.Add(
                new EntityInfoViewModel(new EntityInfo(VarComboBoxDsShape.DsShapeTypeNameToDisplay,
                    VarComboBoxDsShape.DsShapeTypeGuid, "", "")));

            entities.Add(
                new EntityInfoViewModel(new EntityInfo(EditableComboBoxDsShape.DsShapeTypeNameToDisplay,
                    EditableComboBoxDsShape.DsShapeTypeGuid, "", "")));

            entities.Add(
                new EntityInfoViewModel(new EntityInfo(TabDsShape.DsShapeTypeNameToDisplay,
                    TabDsShape.DsShapeTypeGuid, "", "")));

            entities.Add(
                new EntityInfoViewModel(new EntityInfo(TrendGroupDsShape.DsShapeTypeNameToDisplay,
                    TrendGroupDsShape.DsShapeTypeGuid, "", "")));

            entities.Add(
                new EntityInfoViewModel(new EntityInfo(ChartDsShape.DsShapeTypeNameToDisplay,
                    ChartDsShape.DsShapeTypeGuid, "", "")));

            entities.Add(
                new EntityInfoViewModel(new EntityInfo(MultiChartDsShape.DsShapeTypeNameToDisplay,
                    MultiChartDsShape.DsShapeTypeGuid, "", "")));

            entities.Add(
                new EntityInfoViewModel(new EntityInfo(FrameDsShape.DsShapeTypeNameToDisplay,
                   FrameDsShape.DsShapeTypeGuid, "", "")));

            entities.Add(
                new EntityInfoViewModel(new EntityInfo(WindowDragDsShape.DsShapeTypeNameToDisplay,
                   WindowDragDsShape.DsShapeTypeGuid, "", "")));

            entities.Add(
                new EntityInfoViewModel(new EntityInfo(BrowserDsShape.DsShapeTypeNameToDisplay,
                    BrowserDsShape.DsShapeTypeGuid, "", "")));

            entities.Add(
                new EntityInfoViewModel(new EntityInfo(AlarmListDsShape.DsShapeTypeNameToDisplay,
                    AlarmListDsShape.DsShapeTypeGuid, "", "")));

            entities.Add(
                new EntityInfoViewModel(new EntityInfo(Top3AlarmListDsShape.DsShapeTypeNameToDisplay,
                    Top3AlarmListDsShape.DsShapeTypeGuid, "", "")));

            rootGroupViewModel.Entities = entities.OrderBy(e => e.Header).ToList();

        }

        public static void FillGroupViewModelWithDsShapes(GroupViewModel rootGroupViewModel,
            IEnumerable<EntityInfo> entityInfos, Func<EntityInfo, EntityInfoViewModel> dsShapeViewModelFactory)
        {
            if (entityInfos is null) return;

            Dictionary<string, List<EntityInfo>> entityInfosDictionary = DsProject.GetEntityInfosDictionary(entityInfos);

            List<EntityInfo>? entityInfosList;
            if (entityInfosDictionary.TryGetValue("", out entityInfosList))
            {
                foreach (EntityInfo entityInfo in entityInfosList)
                {
                    EntityInfoViewModel dsShapeViewModel = dsShapeViewModelFactory(entityInfo);

                    rootGroupViewModel.Entities.Add(dsShapeViewModel);
                }
            }

            foreach (
                string group in
                    entityInfosDictionary.Keys.OrderBy(
                        key => key))
            {
                if (group == "") continue;

                Guid? typeGuid = null;

                var rootDsPageGroupViewModel = rootGroupViewModel as DsPageDrawingInfosGroupViewModel;
                if (rootDsPageGroupViewModel is not null)
                {
                    typeGuid = rootDsPageGroupViewModel.DrawingTypeGuid;
                }

                var groupViewModel = new DsPageDrawingInfosGroupViewModel(group, typeGuid)
                {
                    Header = group
                };

                foreach (EntityInfo entityInfo in entityInfosDictionary[group])
                {
                    EntityInfoViewModel dsShapeViewModel = dsShapeViewModelFactory(entityInfo);

                    groupViewModel.Entities.Add(dsShapeViewModel);
                }

                rootGroupViewModel.ChildGroups.Add(groupViewModel);
            }
        }

        public static void UpdateDsPageDrawingProps(DsPageDrawing dsPageDrawing, DsPageDrawingInfosGroupViewModel newDsPageGroupViewModel)
        {
            if (newDsPageGroupViewModel.DrawingTypeGuid.HasValue)
            {
                if (newDsPageGroupViewModel.DrawingTypeGuid.Value == dsPageDrawing.DsPageTypeGuid)
                {
                    dsPageDrawing.Group = newDsPageGroupViewModel.DrawingGroup ?? @"";
                }
                else
                {
                    dsPageDrawing.DsPageTypeGuid = newDsPageGroupViewModel.DrawingTypeGuid.Value;
                    if (newDsPageGroupViewModel.DrawingGroup is not null) dsPageDrawing.Group = newDsPageGroupViewModel.DrawingGroup;
                }
            }
            else
            {
                dsPageDrawing.Group = newDsPageGroupViewModel.DrawingGroup ?? @"";
            }
        }

        #endregion

        #region private functions

        private static void GroupDsPagesByStyle(GroupViewModel rootGroupViewModel)
        {
            if (rootGroupViewModel.ChildGroups.Count > 0)
            {
                foreach (GroupViewModel childGroupViewModel in rootGroupViewModel.ChildGroups)
                {
                    GroupDsPagesByStyle(childGroupViewModel);
                }
            }

            var groupDictionary = new Dictionary<Guid, GroupViewModel>();

            foreach (EntityInfoViewModel entityInfoViewModel in rootGroupViewModel.Entities)
            {
                var dsPageDrawingInfo = (DsPageDrawingInfo) entityInfoViewModel.EntityInfo;
                GroupViewModel? childGroupViewModel;
                if (!groupDictionary.TryGetValue(dsPageDrawingInfo.DsPageTypeInfo.Guid, out childGroupViewModel))
                {
                    childGroupViewModel = new DsPageDrawingInfosGroupViewModel(null, dsPageDrawingInfo.DsPageTypeInfo.Guid)
                    {
                        Header = dsPageDrawingInfo.DsPageTypeInfo.Name ?? @""
                    };
                    groupDictionary[dsPageDrawingInfo.DsPageTypeInfo.Guid] = childGroupViewModel;
                }
                childGroupViewModel.Entities.Add(entityInfoViewModel);
            }

            rootGroupViewModel.Entities.Clear();

            foreach (
                var keyValuePair in
                    groupDictionary.OrderBy(kvp => AddonsHelper.IsFaceplate(kvp.Key)).ThenBy(kvp => kvp.Value.Header))
            {
                rootGroupViewModel.ChildGroups.Add(keyValuePair.Value);
            }
        }

        private static void GroupDsPagesByGroup(GroupViewModel rootGroupViewModel)
        {
            if (rootGroupViewModel.ChildGroups.Count > 0)
            {
                foreach (GroupViewModel childGroupViewModel in rootGroupViewModel.ChildGroups)
                {
                    GroupDsPagesByGroup(childGroupViewModel);
                }
            }

            var groupDictionary = new CaseInsensitiveDictionary<GroupViewModel>();

            foreach (EntityInfoViewModel entityInfoViewModel in rootGroupViewModel.Entities)
            {
                var dsPageDrawingInfo = (DsPageDrawingInfo) entityInfoViewModel.EntityInfo;
                GroupViewModel? childGroupViewModel;

                string group;
                if (!String.IsNullOrWhiteSpace(dsPageDrawingInfo.Group)) group = dsPageDrawingInfo.Group;
                else group = "";

                if (!groupDictionary.TryGetValue(group, out childGroupViewModel))
                {
                    Guid? typeGuid = null;

                    var rootDsPageGroupViewModel = rootGroupViewModel as DsPageDrawingInfosGroupViewModel;
                    if (rootDsPageGroupViewModel is not null)
                    {
                        typeGuid = rootDsPageGroupViewModel.DrawingTypeGuid;
                    }

                    childGroupViewModel = new DsPageDrawingInfosGroupViewModel(group, typeGuid)
                    {
                        Header = group
                    };
                    groupDictionary[group] = childGroupViewModel;
                }
                childGroupViewModel.Entities.Add(entityInfoViewModel);
            }

            rootGroupViewModel.Entities.Clear();

            GroupViewModel? mainGroupViewModel;
            if (groupDictionary.TryGetValue("", out mainGroupViewModel))
            {
                foreach (EntityInfoViewModel entityInfoViewModel in mainGroupViewModel.Entities)
                {
                    rootGroupViewModel.Entities.Add(entityInfoViewModel);
                }
            }

            foreach (
                var keyValuePair in
                    groupDictionary.OrderBy(kvp => kvp.Value.Header))
            {
                if (keyValuePair.Key == "") continue;

                rootGroupViewModel.ChildGroups.Add(keyValuePair.Value);
            }
        }

        private static void GroupDsPagesByMark(GroupViewModel rootGroupViewModel)
        {
            if (rootGroupViewModel.ChildGroups.Count > 0)
            {
                foreach (GroupViewModel childGroupViewModel in rootGroupViewModel.ChildGroups)
                {
                    GroupDsPagesByMark(childGroupViewModel);
                }
            }

            var groupDictionary = new Dictionary<int, GroupViewModel>();

            foreach (EntityInfoViewModel entityInfoViewModel in rootGroupViewModel.Entities)
            {
                var dsPageDrawingInfo = (DsPageDrawingInfo) entityInfoViewModel.EntityInfo;
                GroupViewModel? childGroupViewModel;
                if (!groupDictionary.TryGetValue(dsPageDrawingInfo.Mark, out childGroupViewModel))
                {
                    childGroupViewModel = new GroupViewModel();
                    groupDictionary[dsPageDrawingInfo.Mark] = childGroupViewModel;
                }
                childGroupViewModel.Entities.Add(entityInfoViewModel);
            }

            rootGroupViewModel.Entities.Clear();

            /*
            GroupViewModel mainGroupViewModel;
            if (groupDictionary.TryGetValue(0, out mainGroupViewModel))
            {
                foreach (var entityInfoViewModel in mainGroupViewModel.Entities)
                {
                    rootGroupViewModel.Entities.Add(entityInfoViewModel);
                }
            }*/

            foreach (
                var keyValuePair in
                    groupDictionary.OrderByDescending(kvp => kvp.Key))
            {
                //if (keyValuePair.Key == 0) continue;

                foreach (EntityInfoViewModel entityInfoViewModel in keyValuePair.Value.Entities)
                {
                    rootGroupViewModel.Entities.Add(entityInfoViewModel);
                }
            }
        }

        private static void NuberDsPages(GroupViewModel rootGroupViewModel, ref int number)
        {
            foreach (GroupViewModel childGroupViewModel in rootGroupViewModel.ChildGroups)
            {
                NuberDsPages(childGroupViewModel, ref number);
            }

            foreach (EntityInfoViewModel entityInfoViewModel in rootGroupViewModel.Entities)
            {
                ((DsPageDrawingInfoViewModel)entityInfoViewModel).Number = number;
                number += 1;
            }            
        }

        #endregion
    }
    
    public class DsPagesGroupingFilter
    {
        public bool GroupByStyle { get; set; }
        public bool GroupByGroup { get; set; }
        public bool GroupByMark { get; set; }
    }
}