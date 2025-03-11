using System;
using System.Collections.Generic;
using System.ComponentModel;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.CustomAttributes;

using Ssz.Operator.Core.Properties;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Ssz.Operator.Core.DataEngines
{
    public abstract partial class DataEngineBase : OwnedDataSerializableAndCloneable,
        IDsItem,
        IUsedAddonsInfo
    {
        #region construction and destruction

        public DataEngineBase()
        {
            TagNameToDisplayInfo = new TextDataBinding();
            TagDescInfo = new TextDataBinding();
            ModelTagPropertyInfosCollection = new List<ProcessModelPropertyInfo>();
            TagAlarmsInfosCollection = new List<TagAlarmsInfo>();
            TagAlarmsInfosCollection.Add(new TagAlarmsInfo());
        }

        #endregion

        #region public functions

        [Browsable(false)] public abstract Guid Guid { get; }

        [Browsable(false)] public abstract string NameToDisplay { get; }

        [DsCategory(ResourceStrings.SystemCategory)]
        [DsDisplayName(ResourceStrings.DataEngineBaseDescription)]
        //[PropertyOrder(1)]
        public abstract string Description { get; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        [field: Searchable(false)]
        public IDsItem? ParentItem { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization        
        public bool RefreshForPropertyGridIsDisabled { get; set; }

        public const string TagConstant = @"%(TAG)";

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DataEngineBase_Constant)]
        //[PropertyOrder(1)]
        public string TagConstant_ => TagConstant;        

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DataEngineBase_TagNameToDisplayInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        //[PropertyOrder(2)]
        public TextDataBinding TagNameToDisplayInfo { get; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DataEngineBase_TagDescriptionInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        //[PropertyOrder(3)]
        public TextDataBinding TagDescInfo { get; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DataEngineBase_ModelTagPropertyInfosCollection)]
        //[Editor(//typeof(MiscTypeCloneableObjectsCollectionTypeEditor),
            //typeof(MiscTypeCloneableObjectsCollectionTypeEditor))]
        //[PropertyOrder(4)]
        public List<ProcessModelPropertyInfo> ModelTagPropertyInfosCollection { get; private set; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DataEngineBase_TagsFileName)]
        [LocalizedDescription(ResourceStrings.DataEngineBase_TagsFileName_Description)]
        //[PropertyOrder(5)]
        public string TagsFileName => @"Tags.csv";

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DataEngineBase_TagTypesFileName)]
        [LocalizedDescription(ResourceStrings.DataEngineBase_TagTypesFileName_Description)]
        //[PropertyOrder(6)]
        public string TagTypesFileName => @"TagTypes.csv";        

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DataEngineBase_ElementIdsMapFileName)]
        [LocalizedDescription(ResourceStrings.DataEngineBase_ElementIdsMapFileName_Description)]
        //[PropertyOrder(7)]
        public string ElementIdsMapFileName => @"Map.csv";

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DataEngineBase_ReverseElementIdsMapFileName)]
        [LocalizedDescription(ResourceStrings.DataEngineBase_ReverseElementIdsMapFileName_Description)]
        //[PropertyOrder(8)]
        public string ReverseElementIdsMapFileName => @"ReverseMap.csv";

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DataEngineBase_GlobalVariablesFileName)]
        [LocalizedDescription(ResourceStrings.DataEngineBase_GlobalVariablesFileName_Description)]
        //[PropertyOrder(9)]
        public string GlobalVariablesFileName => @"GlobalVariables.csv";

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DataEngineBase_FontsMapFileName)]
        [LocalizedDescription(ResourceStrings.DataEngineBase_FontsMapFileName_Description)]
        //[PropertyOrder(10)]
        public string FontsMapFileName => @"FontsMap.csv";

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DataEngineBase_TagAndPropertySeparator)]
        [LocalizedDescription(ResourceStrings.DataEngineBase_TagAndPropertySeparator_Description)]
        //[PropertyOrder(11)]
        public string TagAndPropSeparator
        {
            get => DsProject.Instance.ElementIdsMap.TagAndPropSeparator;
        }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DataEngineBase_TagTypeSeparator)]
        [LocalizedDescription(ResourceStrings.DataEngineBase_TagTypeSeparator_Description)]
        //[PropertyOrder(12)]
        public string TagTypeSeparator
        {
            get => DsProject.Instance.ElementIdsMap.TagTypeSeparator;
        }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DataEngineBase_TagAlarmsInfosCollection)]
        //[Editor(//typeof(MiscTypeCloneableObjectsCollectionTypeEditor),
            //typeof(MiscTypeCloneableObjectsCollectionTypeEditor))]
        //[PropertyOrder(13)]
        public List<TagAlarmsInfo> TagAlarmsInfosCollection { get; set; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DataEngineBase_SymbolsToRemoveFromTagInAlarmMessage)]
        [LocalizedDescription(ResourceStrings.DataEngineBase_SymbolsToRemoveFromTagInAlarmMessage_Description)]
        //[PropertyOrder(14)]
        public string SymbolsToRemoveFromTagInAlarmMessage
        {
            get { return _symbolsToRemoveFromTagInAlarmMessage; }
            set
            {
                _symbolsToRemoveFromTagInAlarmMessage = value;
                if (!string.IsNullOrEmpty(_symbolsToRemoveFromTagInAlarmMessage))
                    SymbolsToRemoveFromTagInAlarmMessageArray =
                        CsvHelper.ParseCsvLine(@",", _symbolsToRemoveFromTagInAlarmMessage);
                else
                    SymbolsToRemoveFromTagInAlarmMessageArray = null;
            }
        }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DataEngineBase_AlarmMessages_ElementIdsMapFileName)]
        [LocalizedDescription(ResourceStrings.DataEngineBase_AlarmMessages_ElementIdsMapFileNameDescription)]
        //[PropertyOrder(15)]
        public string AlarmMessages_ElementIdsMapFileName => @"AlarmMessagesMap.csv";        

        public void RefreshForPropertyGrid()
        {
            RefreshForPropertyGrid(ParentItem.Find<IDsContainer>());
        }

        public void EndEditInPropertyGrid()
        {
        }

        public IEnumerable<Guid> GetUsedAddonGuids()
        {
            var additionalAddon = AddonsHelper.GetAdditionalAddon(Guid);
            if (additionalAddon is not null) yield return additionalAddon.Guid;
        }

        public override string ToString()
        {
            return NameToDisplay + @"..";
        }

        public virtual void FindConstants(HashSet<string> constants)
        {
        }

        public virtual void ReplaceConstants(IDsContainer? container)
        {
        }

        public virtual void RefreshForPropertyGrid(IDsContainer? container)
        {            
            ItemHelper.RefreshForPropertyGrid(TagNameToDisplayInfo, container);
            ItemHelper.RefreshForPropertyGrid(TagDescInfo, container);
            foreach (ProcessModelPropertyInfo modelTagPropertyInfo in ModelTagPropertyInfosCollection)
                ItemHelper.RefreshForPropertyGrid(modelTagPropertyInfo, container);
            foreach (TagAlarmsInfo alarmTypeInfo in TagAlarmsInfosCollection)
                alarmTypeInfo.RefreshForPropertyGrid(container);
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(5))
            {
                writer.Write(TagNameToDisplayInfo, context);
                writer.Write(TagDescInfo, context);
                writer.Write(ModelTagPropertyInfosCollection);
                writer.Write(TagAlarmsInfosCollection);
                writer.Write(SymbolsToRemoveFromTagInAlarmMessage); 
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 4:
                        try
                        {
                            reader.ReadOwnedData(TagNameToDisplayInfo, context);
                            reader.ReadOwnedData(TagDescInfo, context);
                            ModelTagPropertyInfosCollection = reader.ReadList<ProcessModelPropertyInfo>();
                            TagAlarmsInfosCollection = reader.ReadList<TagAlarmsInfo>();
                            SymbolsToRemoveFromTagInAlarmMessage = reader.ReadString();                            
                            reader.ReadString();
                        }
                        catch (BlockEndingException)
                        {
                        }
                        break;
                    case 5:
                        try
                        {
                            reader.ReadOwnedData(TagNameToDisplayInfo, context);
                            reader.ReadOwnedData(TagDescInfo, context);
                            ModelTagPropertyInfosCollection = reader.ReadList<ProcessModelPropertyInfo>();
                            TagAlarmsInfosCollection = reader.ReadList<TagAlarmsInfo>();
                            SymbolsToRemoveFromTagInAlarmMessage = reader.ReadString();                            
                        }
                        catch (BlockEndingException)
                        {
                        }
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        #endregion

        #region private fields

        private string _symbolsToRemoveFromTagInAlarmMessage = @"";

        #endregion
    }
}