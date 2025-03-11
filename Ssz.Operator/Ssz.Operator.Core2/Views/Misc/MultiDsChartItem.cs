using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Media;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Properties;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;
using Ssz.Utils;

namespace Ssz.Operator.Core
{
    //[DsCategoryOrder(ResourceStrings.MainCategory, 3)]
    //[DsCategoryOrder(ResourceStrings.DataCategory, 4)]
    public class MultiDsChartItem :
        OwnedDataSerializableAndCloneable,
        IDsItem
    {
        public enum AppearanceType
        {
            Line = 0,
            Bezier = 1,
            QuadraticBezier = 2
        }

        #region construction and destruction

        public MultiDsChartItem() // For XAML serialization
            : this(true)
        {
        }

        public MultiDsChartItem(bool visualDesignMode)
        {
            TitleInfo = new TextDataBinding(visualDesignMode, true);
            Type = AppearanceType.Line;
            PointsIsVisible = true;
            LineIsVisible = true;
            FillIsVisible = true;
            PointGeometryInfo = new DsUIElementProperty(visualDesignMode, true);
            PointGeometryInfo.TypeString = ChartItemPointGeometryInfoSupplier.CrossTypeString;
            PointDsBrush = new BrushDataBinding(visualDesignMode, true)
            {
                ConstValue = new SolidDsBrush
                {
                    Color = Colors.Blue
                }
            };
            PointStrokeThickness = 1.0;
            LineDsBrush = new BrushDataBinding(visualDesignMode, true)
            {
                ConstValue = new SolidDsBrush
                {
                    Color = Colors.Black
                }
            };
            LineStrokeThickness = 1.0;
            FillDsBrush = new BrushDataBinding(visualDesignMode, true)
            {
                ConstValue = new SolidDsBrush
                {
                    Color = Colors.Red
                }
            };

            PointsStartNumber = 1;
            PointNumberFormat = @"";
            PointsCountInfo = new Int32DataBinding {ConstValue = 0};
            PointValueXInfo = new DoubleDataBinding(visualDesignMode, true) {ConstValue = double.NaN};
            PointValueYInfo = new DoubleDataBinding(visualDesignMode, true) {ConstValue = double.NaN};
            PointValueYInfo.DataBindingItemsCollection.Add(
                new DataBindingItem(@"SS.CELLVALUE.A%(N)", DataSourceType.OpcVariable, @"NaN"));
        }

        #endregion

        #region public functions

        public const string PointNumberConstantConst = @"%(N)";

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.MultiDsChartItem_TitleInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        //[PropertyOrder(1)]
        public TextDataBinding TitleInfo { get; set; }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.MultiDsChartItem_AppearanceType)]
        //[PropertyOrder(2)]
        public AppearanceType Type { get; set; }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.MultiDsChartItem_PointsIsVisible)]
        //[PropertyOrder(3)]
        public bool PointsIsVisible { get; set; }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.MultiDsChartItem_LineIsVisible)]
        //[PropertyOrder(4)]
        public bool LineIsVisible { get; set; }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.MultiDsChartItem_FillIsVisible)]
        //[PropertyOrder(5)]
        public bool FillIsVisible { get; set; }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.MultiDsChartItem_PointGeometryInfo)]
        //[Editor(//typeof(DsUIElementPropertyTypeEditor<ChartItemPointGeometryInfoSupplier>),
            //typeof(DsUIElementPropertyTypeEditor<ChartItemPointGeometryInfoSupplier>))]
        //[PropertyOrder(6)]
        public DsUIElementProperty PointGeometryInfo { get; set; }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.MultiDsChartItem_PointDsBrush)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(BrushTypeEditor), typeof(BrushTypeEditor))]
        //[PropertyOrder(7)]
        public BrushDataBinding PointDsBrush { get; set; }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.MultiDsChartItem_PointStrokeThickness)]
        //[PropertyOrder(8)]
        public double PointStrokeThickness { get; set; }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.MultiDsChartItem_LineDsBrush)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(BrushTypeEditor), typeof(BrushTypeEditor))]
        //[PropertyOrder(9)]
        public BrushDataBinding LineDsBrush { get; set; }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.MultiDsChartItem_LineStrokeThickness)]
        //[PropertyOrder(10)]
        public double LineStrokeThickness { get; set; }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.MultiDsChartItem_LineStrokeDashLength)]
        [DefaultValue(null)] // For XAML serialization
        //[PropertyOrder(11)]
        public double? LineStrokeDashLength { get; set; }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.MultiDsChartItem_LineStrokeGapLength)]
        [DefaultValue(null)] // For XAML serialization
        //[PropertyOrder(12)]
        public double? LineStrokeGapLength { get; set; }


        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.MultiDsChartItem_LineStrokeDashArray)]
        [LocalizedDescription(ResourceStrings.MultiDsChartItem_LineStrokeDashArrayDescription)]
        [DefaultValue(null)] // For XAML serialization
        //[PropertyOrder(13)]
        public string LineStrokeDashArray { get; set; } = @"";

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.MultiDsChartItem_FillDsBrush)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(BrushTypeEditor), typeof(BrushTypeEditor))]
        //[PropertyOrder(14)]
        public BrushDataBinding FillDsBrush { get; set; }

        [DsCategory(ResourceStrings.DataCategory)]
        [DsDisplayName(ResourceStrings.MultiDsChartItem_PointNumberConstant)]
        [LocalizedDescription(ResourceStrings.MultiDsChartItem_PointNumberConstantDescription)]
        //[PropertyOrder(0)]
        public string PointNumberConstant => PointNumberConstantConst;

        [DsCategory(ResourceStrings.DataCategory)]
        [DsDisplayName(ResourceStrings.MultiDsChartItem_PointsStartNumber)]
        //[PropertyOrder(1)]
        public int PointsStartNumber { get; set; }

        [DsCategory(ResourceStrings.DataCategory)]
        [DsDisplayName(ResourceStrings.MultiDsChartItem_PointNumberFormat)]
        //[PropertyOrder(2)]
        public string PointNumberFormat { get; set; }

        [DsCategory(ResourceStrings.DataCategory)]
        [DsDisplayName(ResourceStrings.MultiDsChartItem_PointsCountInfo)]
        //[PropertyOrder(3)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        public Int32DataBinding PointsCountInfo { get; set; }

        [DsCategory(ResourceStrings.DataCategory)]
        [DsDisplayName(ResourceStrings.MultiDsChartItem_PointValueXInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        //[PropertyOrder(4)]
        public DoubleDataBinding PointValueXInfo { get; set; }

        [DsCategory(ResourceStrings.DataCategory)]
        [DsDisplayName(ResourceStrings.MultiDsChartItem_PointValueYInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        //[PropertyOrder(5)]
        public DoubleDataBinding PointValueYInfo { get; set; }

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

        public override string ToString()
        {
            return TitleInfo.ConstValue ?? @"<No Title>";
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(2))
            {
                writer.Write(TitleInfo, context);
                writer.Write(PointsIsVisible);
                writer.Write(LineIsVisible);
                writer.Write(FillIsVisible);
                writer.Write(PointGeometryInfo, context);
                writer.Write(PointDsBrush, context);
                writer.Write(PointStrokeThickness);
                writer.Write(LineDsBrush, context);
                writer.Write(LineStrokeThickness);
                writer.WriteNullable(LineStrokeDashLength);
                writer.WriteNullable(LineStrokeGapLength);
                writer.Write(LineStrokeDashArray);
                writer.Write(FillDsBrush, context);
                writer.Write(PointsStartNumber);
                writer.Write(PointNumberFormat);
                writer.Write(PointsCountInfo, context);
                writer.Write(PointValueXInfo, context);
                writer.Write(PointValueYInfo, context);
                writer.Write((int) Type);
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
                            reader.ReadOwnedData(TitleInfo, context);
                            PointsIsVisible = reader.ReadBoolean();
                            LineIsVisible = reader.ReadBoolean();
                            FillIsVisible = reader.ReadBoolean();
                            reader.ReadOwnedData(PointGeometryInfo, context);
                            reader.ReadOwnedData(PointDsBrush, context);
                            PointStrokeThickness = reader.ReadDouble();
                            reader.ReadOwnedData(LineDsBrush, context);
                            LineStrokeThickness = reader.ReadDouble();
                            LineStrokeDashLength = reader.ReadNullableDouble();
                            LineStrokeGapLength = reader.ReadNullableDouble();
                            LineStrokeDashArray = reader.ReadString();
                            reader.ReadOwnedData(FillDsBrush, context);
                            reader.ReadOwnedData(PointsCountInfo, context);
                            reader.ReadOwnedData(PointValueXInfo, context);
                            reader.ReadOwnedData(PointValueYInfo, context);
                            Type = (AppearanceType) reader.ReadInt32();
                        }
                        catch (BlockEndingException)
                        {
                        }

                        break;
                    case 2:
                        try
                        {
                            reader.ReadOwnedData(TitleInfo, context);
                            PointsIsVisible = reader.ReadBoolean();
                            LineIsVisible = reader.ReadBoolean();
                            FillIsVisible = reader.ReadBoolean();
                            reader.ReadOwnedData(PointGeometryInfo, context);
                            reader.ReadOwnedData(PointDsBrush, context);
                            PointStrokeThickness = reader.ReadDouble();
                            reader.ReadOwnedData(LineDsBrush, context);
                            LineStrokeThickness = reader.ReadDouble();
                            LineStrokeDashLength = reader.ReadNullableDouble();
                            LineStrokeGapLength = reader.ReadNullableDouble();
                            LineStrokeDashArray = reader.ReadString();
                            reader.ReadOwnedData(FillDsBrush, context);
                            PointsStartNumber = reader.ReadInt32();
                            PointNumberFormat = reader.ReadString();
                            reader.ReadOwnedData(PointsCountInfo, context);
                            reader.ReadOwnedData(PointValueXInfo, context);
                            reader.ReadOwnedData(PointValueYInfo, context);
                            Type = (AppearanceType) reader.ReadInt32();
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
            TitleInfo.FindConstants(constants);
            PointDsBrush.FindConstants(constants);
            LineDsBrush.FindConstants(constants);
            FillDsBrush.FindConstants(constants);
            ConstantsHelper.FindConstants(PointNumberFormat, constants);
            PointsCountInfo.FindConstants(constants);
            PointValueXInfo.FindConstants(constants);
            PointValueYInfo.FindConstants(constants);
        }

        public void ReplaceConstants(IDsContainer? container)
        {
            TitleInfo.ReplaceConstants(container);
            PointDsBrush.ReplaceConstants(container);
            LineDsBrush.ReplaceConstants(container);
            FillDsBrush.ReplaceConstants(container);
            PointNumberFormat = ConstantsHelper.ComputeValue(container, PointNumberFormat)!;
            PointsCountInfo.ReplaceConstants(container);
            PointValueXInfo.ReplaceConstants(container);
            PointValueYInfo.ReplaceConstants(container);
        }

        public void RefreshForPropertyGrid(IDsContainer? container)
        {
            ItemHelper.RefreshForPropertyGrid(TitleInfo, container);
            ItemHelper.RefreshForPropertyGrid(PointDsBrush, container);
            ItemHelper.RefreshForPropertyGrid(LineDsBrush, container);
            ItemHelper.RefreshForPropertyGrid(FillDsBrush, container);
            ItemHelper.RefreshForPropertyGrid(PointsCountInfo, container);
            ItemHelper.RefreshForPropertyGrid(PointValueXInfo, container);
            ItemHelper.RefreshForPropertyGrid(PointValueYInfo, container);
        }

        #endregion
    }
}