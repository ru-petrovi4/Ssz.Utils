using System;
using System.Collections.Generic;
using System.ComponentModel;
using Ssz.Operator.Core.CustomAttributes;

using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Ssz.Operator.Core.DataEngines
{
    public class ProcessModelPropertyInfo : OwnedDataSerializableAndCloneable,
        IDsItem
    {
        #region construction and destruction

        public ProcessModelPropertyInfo()
        {
            TagType = "";
            PropertyPath = "";
            PropertyPathToDisplay = "";
            MaxScaleInfo = new DoubleDataBinding();
            MinScaleInfo = new DoubleDataBinding();
            FormatInfo = new TextDataBinding();
            EUInfo = new TextDataBinding();
            HiHiAlarmInfo = new DoubleDataBinding {ConstValue = double.NaN};
            HiAlarmInfo = new DoubleDataBinding {ConstValue = double.NaN};
            LoAlarmInfo = new DoubleDataBinding {ConstValue = double.NaN};
            LoLoAlarmInfo = new DoubleDataBinding {ConstValue = double.NaN};
            AddToTrendGroups = false;
        }

        #endregion

        #region public functions

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.ModelTagPropertyInfoTagType)]
        //[PropertyOrder(1)]
        public string TagType { get; set; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.ModelTagPropertyInfoPropertyPath)]
        //[PropertyOrder(2)]
        public string PropertyPath { get; set; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.ModelTagPropertyInfoPropertyPathToDisplay)]
        //[PropertyOrder(3)]
        public string PropertyPathToDisplay { get; set; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.ModelTagPropertyInfoMaxScaleInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        //[PropertyOrder(4)]
        public DoubleDataBinding MaxScaleInfo { get; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.ModelTagPropertyInfoMinScaleInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        //[PropertyOrder(5)]
        public DoubleDataBinding MinScaleInfo { get; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.ModelTagPropertyInfoEUInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        //[PropertyOrder(6)]
        public TextDataBinding EUInfo { get; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.ModelTagPropertyInfoFormatInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        //[PropertyOrder(7)]
        public TextDataBinding FormatInfo { get; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.ModelTagPropertyInfoHiHiAlarmInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        //[PropertyOrder(8)]
        public DoubleDataBinding HiHiAlarmInfo { get; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.ModelTagPropertyInfoHiAlarmInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        //[PropertyOrder(9)]
        public DoubleDataBinding HiAlarmInfo { get; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.ModelTagPropertyInfoLoAlarmInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        //[PropertyOrder(10)]
        public DoubleDataBinding LoAlarmInfo { get; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.ModelTagPropertyInfoLoLoAlarmInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        //[PropertyOrder(11)]
        public DoubleDataBinding LoLoAlarmInfo { get; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.ModelTagPropertyInfoAddToTrendGroups)]
        //[PropertyOrder(12)]
        public bool AddToTrendGroups { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        [field: Searchable(false)]
        public IDsItem? ParentItem { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization        
        public bool RefreshForPropertyGridIsDisabled { get; set; }

        public void RefreshForPropertyGrid()
        {
            RefreshForPropertyGrid(ParentItem.Find<IDsContainer>());
        }

        public void EndEditInPropertyGrid()
        {
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(4))
            {
                writer.Write(TagType);
                writer.Write(PropertyPath);
                writer.Write(PropertyPathToDisplay);
                writer.Write(MinScaleInfo, context);
                writer.Write(MaxScaleInfo, context);
                writer.Write(EUInfo, context);
                writer.Write(HiHiAlarmInfo, context);
                writer.Write(HiAlarmInfo, context);
                writer.Write(LoAlarmInfo, context);
                writer.Write(LoLoAlarmInfo, context);
                writer.Write(AddToTrendGroups);
                writer.Write(FormatInfo, context);
            }
        }

        public override void DeserializeOwnedDataAsync(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 4:
                        try
                        {
                            TagType = reader.ReadString();
                            PropertyPath = reader.ReadString();
                            PropertyPathToDisplay = reader.ReadString();
                            reader.ReadOwnedData(MinScaleInfo, context);
                            reader.ReadOwnedData(MaxScaleInfo, context);
                            reader.ReadOwnedData(EUInfo, context);
                            reader.ReadOwnedData(HiHiAlarmInfo, context);
                            reader.ReadOwnedData(HiAlarmInfo, context);
                            reader.ReadOwnedData(LoAlarmInfo, context);
                            reader.ReadOwnedData(LoLoAlarmInfo, context);
                            AddToTrendGroups = reader.ReadBoolean();
                            reader.ReadOwnedData(FormatInfo, context);
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

        public void FindConstants(HashSet<string> constants)
        {
            throw new NotImplementedException();
        }

        public void ReplaceConstants(IDsContainer? container)
        {
            throw new NotImplementedException();
        }

        public void RefreshForPropertyGrid(IDsContainer? container)
        {
            ItemHelper.RefreshForPropertyGrid(MinScaleInfo, container);
            ItemHelper.RefreshForPropertyGrid(MaxScaleInfo, container);
            ItemHelper.RefreshForPropertyGrid(FormatInfo, container);
            ItemHelper.RefreshForPropertyGrid(EUInfo, container);
            ItemHelper.RefreshForPropertyGrid(HiHiAlarmInfo, container);
            ItemHelper.RefreshForPropertyGrid(HiAlarmInfo, container);
            ItemHelper.RefreshForPropertyGrid(LoAlarmInfo, container);
            ItemHelper.RefreshForPropertyGrid(LoLoAlarmInfo, container);
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(TagType)) return PropertyPath;
            return TagType + @":" + PropertyPath;
        }

        #endregion
    }
}