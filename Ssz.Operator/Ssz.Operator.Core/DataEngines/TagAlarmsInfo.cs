using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils.DataAccess;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Ssz.Utils;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core;

namespace Ssz.Operator.Core.DataEngines
{
    public class TagAlarmsInfo :
        OwnedDataSerializableAndCloneable, IDsItem
    {
        #region construction and destruction

        public TagAlarmsInfo() // For XAML serialization
        {
            AlarmCategory0DsBrush = new SolidDsBrush(Colors.Lime);
            AlarmCategory1DsBrush = new SolidDsBrush(Colors.Yellow);
            AlarmCategory2DsBrush = new SolidDsBrush(Colors.Red);
            AlarmCategory3DsBrush = new SolidDsBrush(Colors.Red);
            AlarmConditionInfosList = new List<AlarmConditionInfo>();
            AlarmPriorityInfosList = new List<AlarmPriorityInfo>();
            AlarmsIsVisible = true;
        }

        #endregion

        #region public functions

        [Browsable(false)] 
        public static TagAlarmsInfo Default = new();

        [Browsable(false)]
        public static TagAlarmsInfo DefaultInvisible = new()
        {
            AlarmsIsVisible = false
        };

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.TagAlarmsInfo_CsvDbFileName)]
        [LocalizedDescription(ResourceStrings.TagAlarmsInfo_CsvDbFileName_Description)]
        [PropertyOrder(1)]
        public string CsvDbFileName { get; set; } = @"";

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.TagAlarmsInfo_AlarmsIsVisible)]
        [PropertyOrder(2)]
        public bool AlarmsIsVisible { get; set; }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.TagAlarmsInfo_AlarmConditionInfosList)]
        [PropertyOrder(3)]
        [Editor(typeof(SameTypeCloneableObjectsListTypeEditor),
            typeof(SameTypeCloneableObjectsListTypeEditor))]
        public List<AlarmConditionInfo> AlarmConditionInfosList { get; set; }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.TagAlarmsInfo_AlarmCategory0DsBrush)]
        [PropertyOrder(4)]
        [Editor(typeof(SolidBrushTypeEditor), typeof(SolidBrushTypeEditor))]
        public SolidDsBrush AlarmCategory0DsBrush { get; set; }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.TagAlarmsInfo_AlarmCategory1DsBrush)]
        [PropertyOrder(5)]
        [Editor(typeof(SolidBrushTypeEditor), typeof(SolidBrushTypeEditor))]
        public SolidDsBrush AlarmCategory1DsBrush { get; set; }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.TagAlarmsInfo_AlarmCategory2DsBrush)]
        [PropertyOrder(6)]
        [Editor(typeof(SolidBrushTypeEditor), typeof(SolidBrushTypeEditor))]
        public SolidDsBrush AlarmCategory2DsBrush { get; set; }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.TagAlarmsInfo_AlarmCategory3DsBrush)]
        [PropertyOrder(7)]
        [Editor(typeof(SolidBrushTypeEditor), typeof(SolidBrushTypeEditor))]
        public SolidDsBrush AlarmCategory3DsBrush { get; set; }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.TagAlarmsInfo_AlarmPriorityInfosList)]
        [PropertyOrder(8)]
        [Editor(typeof(SameTypeCloneableObjectsListTypeEditor),
            typeof(SameTypeCloneableObjectsListTypeEditor))]
        public List<AlarmPriorityInfo> AlarmPriorityInfosList { get; set; }

        [Browsable(false)]
        public IDsItem? ParentItem { get; set; }

        [Browsable(false)]
        public bool RefreshForPropertyGridIsDisabled { get; set; }

        /// <summary>
        ///     Cached.
        /// </summary>
        /// <returns></returns>
        public TagAlarmsBrushes GetTagAlarmsBrushes()
        {
            if (_tagAlarmsBrushes is null)
            {
                _tagAlarmsBrushes = new TagAlarmsBrushes();

                var brush = (AlarmCategory0DsBrush.GetBrush(DsProject.Instance) as SolidColorBrush) ?? new SolidColorBrush();
                _tagAlarmsBrushes.AlarmCategory0Brushes = new AlarmBrushes()
                {
                    Brush = brush,
                    BlinkingBrush = BlinkingDsBrush.GetBrush(brush.Color, Colors.Transparent),
                    TextBrush = new SolidColorBrush(Colors.Black)
                };

                brush = (AlarmCategory1DsBrush.GetBrush(DsProject.Instance) as SolidColorBrush) ?? new SolidColorBrush();
                _tagAlarmsBrushes.AlarmCategory1Brushes = new AlarmBrushes()
                {
                    Brush = brush,
                    BlinkingBrush = BlinkingDsBrush.GetBrush(brush.Color, Colors.Transparent),
                    TextBrush = new SolidColorBrush(Colors.Black)
                };

                brush = (AlarmCategory2DsBrush.GetBrush(DsProject.Instance) as SolidColorBrush) ?? new SolidColorBrush();
                _tagAlarmsBrushes.AlarmCategory2Brushes = new AlarmBrushes()
                {
                    Brush = brush,
                    BlinkingBrush = BlinkingDsBrush.GetBrush(brush.Color, Colors.Transparent),
                    TextBrush = new SolidColorBrush(Colors.Black)
                };

                brush = (AlarmCategory3DsBrush.GetBrush(DsProject.Instance) as SolidColorBrush) ?? new SolidColorBrush();
                _tagAlarmsBrushes.AlarmCategory3Brushes = new AlarmBrushes()
                {
                    Brush = brush,
                    BlinkingBrush = BlinkingDsBrush.GetBrush(brush.Color, Colors.Transparent),
                    TextBrush = new SolidColorBrush(Colors.Black)
                };
                
                if (AlarmPriorityInfosList.Count > 0)
                {
                    _tagAlarmsBrushes.PriorityBrushes = new();
                    foreach (var alarmPriorityInfo in AlarmPriorityInfosList)
                    {
                        brush = new SolidColorBrush(new Any(alarmPriorityInfo.ColorString).ValueAs<Color>(false));
                        var textColor = new Any(alarmPriorityInfo.TextColorString).ValueAs<Color>(false);
                        if (textColor == default)
                            textColor = Colors.Black;
                        _tagAlarmsBrushes.PriorityBrushes[alarmPriorityInfo.Priority] = new AlarmBrushes()
                        {
                            Brush = brush,
                            BlinkingBrush = BlinkingDsBrush.GetBrush(brush.Color, Colors.Transparent),
                            TextBrush = new SolidColorBrush(textColor)
                        };
                    }
                }                
            }

            return _tagAlarmsBrushes;
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(4))
            {
                writer.Write(CsvDbFileName);
                writer.Write(AlarmsIsVisible);
                writer.Write(AlarmConditionInfosList);
                writer.Write(AlarmCategory0DsBrush, context);
                writer.Write(AlarmCategory1DsBrush, context);
                writer.Write(AlarmCategory2DsBrush, context);
                writer.Write(AlarmCategory3DsBrush, context);
                writer.Write(AlarmPriorityInfosList);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 2:
                        try
                        {
                            CsvDbFileName = reader.ReadString();
                            reader.ReadOwnedData(AlarmCategory0DsBrush, context);
                            reader.ReadOwnedData(AlarmCategory1DsBrush, context);
                            reader.ReadOwnedData(AlarmCategory2DsBrush, context);
                            reader.ReadBoolean();
                            AlarmConditionInfosList = reader.ReadList<AlarmConditionInfo>();
                            reader.ReadNullableUInt32();
                            AlarmsIsVisible = reader.ReadBoolean();
                        }
                        catch (BlockEndingException)
                        {
                        }
                        break;
                    case 3:
                        CsvDbFileName = reader.ReadString();
                        reader.ReadOwnedData(AlarmCategory0DsBrush, context);
                        reader.ReadOwnedData(AlarmCategory1DsBrush, context);
                        reader.ReadOwnedData(AlarmCategory2DsBrush, context);
                        reader.ReadOwnedData(AlarmCategory3DsBrush, context);
                        AlarmConditionInfosList = reader.ReadList<AlarmConditionInfo>();
                        AlarmsIsVisible = reader.ReadBoolean();
                        break;
                    case 4:
                        CsvDbFileName = reader.ReadString();
                        AlarmsIsVisible = reader.ReadBoolean();
                        AlarmConditionInfosList = reader.ReadList<AlarmConditionInfo>();
                        reader.ReadOwnedData(AlarmCategory0DsBrush, context);
                        reader.ReadOwnedData(AlarmCategory1DsBrush, context);
                        reader.ReadOwnedData(AlarmCategory2DsBrush, context);
                        reader.ReadOwnedData(AlarmCategory3DsBrush, context);
                        AlarmPriorityInfosList = reader.ReadList<AlarmPriorityInfo>();
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public void FindConstants(HashSet<string> constants)
        {
            ConstantsHelper.FindConstants(AlarmConditionInfosList, constants);
            AlarmCategory0DsBrush.FindConstants(constants);
            AlarmCategory1DsBrush.FindConstants(constants);
            AlarmCategory2DsBrush.FindConstants(constants);
            AlarmCategory3DsBrush.FindConstants(constants);
            ConstantsHelper.FindConstants(AlarmPriorityInfosList, constants);
        }

        public void ReplaceConstants(IDsContainer? container)
        {
            ItemHelper.ReplaceConstants(AlarmConditionInfosList, container);
            ItemHelper.ReplaceConstants(AlarmCategory0DsBrush, container);
            ItemHelper.ReplaceConstants(AlarmCategory1DsBrush, container);
            ItemHelper.ReplaceConstants(AlarmCategory2DsBrush, container);
            ItemHelper.ReplaceConstants(AlarmCategory3DsBrush, container);
            ItemHelper.ReplaceConstants(AlarmPriorityInfosList, container);
        }

        public void RefreshForPropertyGrid(IDsContainer? container)
        {
            ItemHelper.RefreshForPropertyGrid(AlarmCategory0DsBrush, container);
            ItemHelper.RefreshForPropertyGrid(AlarmCategory1DsBrush, container);
            ItemHelper.RefreshForPropertyGrid(AlarmCategory2DsBrush, container);
            ItemHelper.RefreshForPropertyGrid(AlarmCategory3DsBrush, container);
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(CsvDbFileName))
                return CsvDbFileName;
            return "Default Alarm Type";
        }

        public void RefreshForPropertyGrid()
        {
        }

        public void EndEditInPropertyGrid()
        {
        }

        #endregion

        #region private fields

        private TagAlarmsBrushes? _tagAlarmsBrushes;

        #endregion
    }

    public struct AlarmBrushes
    {        
        public SolidColorBrush Brush { get; set; }

        public Brush BlinkingBrush { get; set; }

        public SolidColorBrush TextBrush { get; set; }
    }

    public class TagAlarmsBrushes
    {        
        public AlarmBrushes AlarmCategory0Brushes { get; set; }
        public AlarmBrushes AlarmCategory1Brushes { get; set; }
        public AlarmBrushes AlarmCategory2Brushes { get; set; }
        public AlarmBrushes AlarmCategory3Brushes { get; set; }
        public Dictionary<uint, AlarmBrushes>? PriorityBrushes { get; set; }
    }    

    public class AlarmConditionInfo : OwnedDataSerializableAndCloneable, IDsItem
    {
        #region public functions

        public AlarmConditionType AlarmConditionType { get; set; }

        public string AlarmConditionTypeToDisplay { get; set; } = @"";

        public string Priority { get; set; } = @"";

        public string CategoryId { get; set; } = @"";

        public string ActivateBuzzer { get; set; } = @"";

        [Browsable(false)]
        public IDsItem? ParentItem { get; set; }

        [Browsable(false)]
        public bool RefreshForPropertyGridIsDisabled { get; set; }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(2))
            {
                writer.Write((int) AlarmConditionType);
                writer.Write(AlarmConditionTypeToDisplay);
                writer.Write(Priority);
                writer.Write(CategoryId);
                writer.Write(ActivateBuzzer);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        try
                        {
                            AlarmConditionType = (AlarmConditionType)reader.ReadInt32();
                            AlarmConditionTypeToDisplay = reader.ReadString();
                            Priority = new Any(reader.ReadNullableUInt32()).ValueAsString(false);
                            CategoryId = new Any(reader.ReadNullableUInt32()).ValueAsString(false);
                            ActivateBuzzer = new Any(reader.ReadBoolean()).ValueAsString(false);
                        }
                        catch (BlockEndingException)
                        {
                        }                        
                        break;
                    case 2:
                        try
                        {
                            AlarmConditionType = (AlarmConditionType)reader.ReadInt32();
                            AlarmConditionTypeToDisplay = reader.ReadString();
                            Priority = reader.ReadString();
                            CategoryId = reader.ReadString();
                            ActivateBuzzer = reader.ReadString();
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

        public override object Clone()
        {
            return new AlarmConditionInfo
            {
                AlarmConditionType = this.AlarmConditionType,
                AlarmConditionTypeToDisplay = this.AlarmConditionTypeToDisplay,
                Priority = this.Priority,
                CategoryId = this.CategoryId,
                ActivateBuzzer = this.ActivateBuzzer,
            };
        }

        public void ReplaceConstants(IDsContainer? container)
        {
            AlarmConditionTypeToDisplay = ConstantsHelper.ComputeValue(container, AlarmConditionTypeToDisplay)!;
            Priority = ConstantsHelper.ComputeValue(container, Priority)!;
            CategoryId = ConstantsHelper.ComputeValue(container, CategoryId)!;
            ActivateBuzzer = ConstantsHelper.ComputeValue(container, ActivateBuzzer)!;
        }

        public void RefreshForPropertyGrid(IDsContainer? container)
        {            
        }

        public void FindConstants(HashSet<string> constants)
        {
            ConstantsHelper.FindConstants(AlarmConditionTypeToDisplay, constants);
            ConstantsHelper.FindConstants(Priority, constants);
            ConstantsHelper.FindConstants(CategoryId, constants);
            ConstantsHelper.FindConstants(ActivateBuzzer, constants);
        }

        public void RefreshForPropertyGrid()
        {            
        }

        public void EndEditInPropertyGrid()
        {            
        }

        #endregion
    }

    public class AlarmPriorityInfo : OwnedDataSerializableAndCloneable, IDsItem
    {
        #region public functions        

        public uint Priority { get; set; }

        public string ColorString { get; set; } = @"";

        public string TextColorString { get; set; } = @"";

        [Browsable(false)]
        public IDsItem? ParentItem { get; set; }

        [Browsable(false)]
        public bool RefreshForPropertyGridIsDisabled { get; set; }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                writer.Write(Priority);
                writer.Write(ColorString);
                writer.Write(TextColorString);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        try
                        {
                            Priority = reader.ReadUInt32();
                            ColorString = reader.ReadString();
                            TextColorString = reader.ReadString();
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

        public override object Clone()
        {
            return new AlarmPriorityInfo
            {                
                Priority = this.Priority,
                ColorString = this.ColorString,
                TextColorString = this.TextColorString,
            };
        }

        public void ReplaceConstants(IDsContainer? container)
        {
            ColorString = ConstantsHelper.ComputeValue(container, ColorString)!;
            TextColorString = ConstantsHelper.ComputeValue(container, TextColorString)!;
        }

        public void RefreshForPropertyGrid(IDsContainer? container)
        {            
        }

        public void FindConstants(HashSet<string> constants)
        {
            ConstantsHelper.FindConstants(ColorString, constants);
            ConstantsHelper.FindConstants(TextColorString, constants);
        }

        public void RefreshForPropertyGrid()
        {            
        }

        public void EndEditInPropertyGrid()
        {            
        }

        #endregion
    }
}