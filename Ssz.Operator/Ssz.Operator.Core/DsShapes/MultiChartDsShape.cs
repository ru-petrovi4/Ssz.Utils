using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Ssz.Operator.Core.CustomAttributes;

using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils.MonitoredUndo;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Ssz.Operator.Core.DsShapes
{
    public class MultiChartDsShape : ControlDsShape
    {
        #region construction and destruction

        public MultiChartDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public MultiChartDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            WidthInitial = 120;
            HeightInitial = 30;

            BackgroundInfo = new BrushDataBinding(visualDesignMode, true)
            {
                ConstValue = new SolidDsBrush
                {
                    Color = Colors.White
                }
            };

            ForegroundInfo = new BrushDataBinding(visualDesignMode, true)
            {
                ConstValue = new SolidDsBrush
                {
                    Color = Colors.Black
                }
            };

            Type = AppearanceType.Default;

            MaximumXInfo = new DoubleDataBinding(visualDesignMode, true) {ConstValue = 1.0};
            MinimumXInfo = new DoubleDataBinding(visualDesignMode, true) {ConstValue = 0.0};
            FormatXInfo = new TextDataBinding(visualDesignMode, true) {ConstValue = @"F02"};
            EngUnitXInfo = new TextDataBinding(visualDesignMode, true) {ConstValue = @""};
            TickFrequencyXInfo = new DoubleDataBinding(visualDesignMode, true) {ConstValue = 0.1};
            TextTickFrequencyXInfo = new DoubleDataBinding(visualDesignMode, true) {ConstValue = 0.2};

            MaximumYInfo = new DoubleDataBinding(visualDesignMode, true) {ConstValue = 1.0};
            MinimumYInfo = new DoubleDataBinding(visualDesignMode, true) {ConstValue = 0.0};
            FormatYInfo = new TextDataBinding(visualDesignMode, true) {ConstValue = @"F02"};
            EngUnitYInfo = new TextDataBinding(visualDesignMode, true) {ConstValue = @""};
            TickFrequencyYInfo = new DoubleDataBinding(visualDesignMode, true) {ConstValue = 0.1};
            TextTickFrequencyYInfo = new DoubleDataBinding(visualDesignMode, true) {ConstValue = 0.2};

            AxisYLeftIsVisible = true;
            AxisXTopIsVisible = true;
            AxisYRightIsVisible = true;
            AxisXBottomIsVisible = true;
            ChartGridIsVisible = true;
            ChartGridLabelsLeftIsVisible = false;
            ChartGridLabelsTopIsVisible = false;
            ChartGridLabelsRightIsVisible = false;
            ChartGridLabelsBottomIsVisible = false;
            ChartGridBrush = new SolidDsBrush {Color = Color.FromArgb(50, 0, 0, 0)};
            TickStrokeThickness = 1.0;
            TickLength = 10.0;

            if (visualDesignMode) MultiDsChartItemsCollection.CollectionChanged += MultiDsChartItemsCollectionOnChanged;
        }

        #endregion

        #region public functions

        public const string DsShapeTypeNameToDisplay = "ChartMultiPoint";
        public static readonly Guid DsShapeTypeGuid = new(@"B9515CB0-7CA0-4545-B81D-DAC7C68E09B4");

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public override HorizontalAlignment HorizontalContentAlignment
        {
            get => base.HorizontalContentAlignment;
            set => base.HorizontalContentAlignment = value;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public override VerticalAlignment VerticalContentAlignment
        {
            get => base.VerticalContentAlignment;
            set => base.VerticalContentAlignment = value;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public override DsUIElementProperty StyleInfo
        {
            get => base.StyleInfo;
            set => base.StyleInfo = value;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public override BrushDataBinding BorderBrushInfo
        {
            get => base.BorderBrushInfo;
            set => base.BorderBrushInfo = value;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public AppearanceType Type
        {
            get => _type;
            set => SetValue(ref _type, value);
        }

        [DsCategory(ResourceStrings.DataCategory)]
        [DsDisplayName(ResourceStrings.MultiChartDsShape_MaximumXInfo)]
        [ExpandableObject]
        [ValuePropertyPath(@"ConstValue")]
        [IsValueEditorEnabledPropertyPath(@"IsConst")]
        [Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        [PropertyOrder(2)]
        public DoubleDataBinding MaximumXInfo
        {
            get => _maximumXInfo;
            set => SetValue(ref _maximumXInfo, value);
        }

        [DsCategory(ResourceStrings.DataCategory)]
        [DsDisplayName(ResourceStrings.MultiChartDsShape_MinimumXInfo)]
        [ExpandableObject]
        [ValuePropertyPath(@"ConstValue")]
        [IsValueEditorEnabledPropertyPath(@"IsConst")]
        [Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        [PropertyOrder(3)]
        public DoubleDataBinding MinimumXInfo
        {
            get => _minimumXInfo;
            set => SetValue(ref _minimumXInfo, value);
        }

        [DsCategory(ResourceStrings.DataCategory)]
        [DsDisplayName(ResourceStrings.MultiChartDsShape_FormatXInfo)]
        [PropertyOrder(4)]
        [ExpandableObject]
        [ValuePropertyPath(@"ConstValue")]
        [IsValueEditorEnabledPropertyPath(@"IsConst")]
        [Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        public TextDataBinding FormatXInfo
        {
            get => _formatXInfo;
            set => SetValue(ref _formatXInfo, value);
        }

        [DsCategory(ResourceStrings.DataCategory)]
        [DsDisplayName(ResourceStrings.MultiChartDsShape_EngUnitXInfo)]
        [PropertyOrder(5)]
        [ExpandableObject]
        [ValuePropertyPath(@"ConstValue")]
        [IsValueEditorEnabledPropertyPath(@"IsConst")]
        [Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        public TextDataBinding EngUnitXInfo
        {
            get => _engUnitXInfo;
            set => SetValue(ref _engUnitXInfo, value);
        }

        [DsCategory(ResourceStrings.DataCategory)]
        [DsDisplayName(ResourceStrings.MultiChartDsShape_TickFrequencyXInfo)]
        [ExpandableObject]
        [ValuePropertyPath(@"ConstValue")]
        [IsValueEditorEnabledPropertyPath(@"IsConst")]
        [Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        [PropertyOrder(6)]
        public DoubleDataBinding TickFrequencyXInfo
        {
            get => _tickFrequencyXInfo;
            set => SetValue(ref _tickFrequencyXInfo, value);
        }

        [DsCategory(ResourceStrings.DataCategory)]
        [DsDisplayName(ResourceStrings.MultiChartDsShape_TextTickFrequencyXInfo)]
        [ExpandableObject]
        [ValuePropertyPath(@"ConstValue")]
        [IsValueEditorEnabledPropertyPath(@"IsConst")]
        [Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        [PropertyOrder(7)]
        public DoubleDataBinding TextTickFrequencyXInfo
        {
            get => _textTickFrequencyXInfo;
            set => SetValue(ref _textTickFrequencyXInfo, value);
        }

        [DsCategory(ResourceStrings.DataCategory)]
        [DsDisplayName(ResourceStrings.MultiChartDsShape_MaximumYInfo)]
        [ExpandableObject]
        [ValuePropertyPath(@"ConstValue")]
        [IsValueEditorEnabledPropertyPath(@"IsConst")]
        [Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        [PropertyOrder(8)]
        public DoubleDataBinding MaximumYInfo
        {
            get => _maximumYInfo;
            set => SetValue(ref _maximumYInfo, value);
        }

        [DsCategory(ResourceStrings.DataCategory)]
        [DsDisplayName(ResourceStrings.MultiChartDsShape_MinimumYInfo)]
        [ExpandableObject]
        [ValuePropertyPath(@"ConstValue")]
        [IsValueEditorEnabledPropertyPath(@"IsConst")]
        [Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        [PropertyOrder(9)]
        public DoubleDataBinding MinimumYInfo
        {
            get => _minimumYInfo;
            set => SetValue(ref _minimumYInfo, value);
        }

        [DsCategory(ResourceStrings.DataCategory)]
        [DsDisplayName(ResourceStrings.MultiChartDsShape_FormatYInfo)]
        [PropertyOrder(10)]
        [ExpandableObject]
        [ValuePropertyPath(@"ConstValue")]
        [IsValueEditorEnabledPropertyPath(@"IsConst")]
        [Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        public TextDataBinding FormatYInfo
        {
            get => _formatYInfo;
            set => SetValue(ref _formatYInfo, value);
        }

        [DsCategory(ResourceStrings.DataCategory)]
        [DsDisplayName(ResourceStrings.MultiChartDsShape_EngUnitYInfo)]
        [PropertyOrder(11)]
        [ExpandableObject]
        [ValuePropertyPath(@"ConstValue")]
        [IsValueEditorEnabledPropertyPath(@"IsConst")]
        [Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        public TextDataBinding EngUnitYInfo
        {
            get => _engUnitYInfo;
            set => SetValue(ref _engUnitYInfo, value);
        }

        [DsCategory(ResourceStrings.DataCategory)]
        [DsDisplayName(ResourceStrings.MultiChartDsShape_TickFrequencyYInfo)]
        [ExpandableObject]
        [ValuePropertyPath(@"ConstValue")]
        [IsValueEditorEnabledPropertyPath(@"IsConst")]
        [Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        [PropertyOrder(12)]
        public DoubleDataBinding TickFrequencyYInfo
        {
            get => _tickFrequencyYInfo;
            set => SetValue(ref _tickFrequencyYInfo, value);
        }

        [DsCategory(ResourceStrings.DataCategory)]
        [DsDisplayName(ResourceStrings.MultiChartDsShape_TextTickFrequencyYInfo)]
        [ExpandableObject]
        [ValuePropertyPath(@"ConstValue")]
        [IsValueEditorEnabledPropertyPath(@"IsConst")]
        [Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        [PropertyOrder(13)]
        public DoubleDataBinding TextTickFrequencyYInfo
        {
            get => _textTickFrequencyYInfo;
            set => SetValue(ref _textTickFrequencyYInfo, value);
        }

        [DsCategory(ResourceStrings.DataCategory)]
        [DsDisplayName(ResourceStrings.MultiChartDsShape_MultiDsChartItemsCollection)]
        [Editor(typeof(MiscTypeCloneableObjectsCollectionTypeEditor),
            typeof(MiscTypeCloneableObjectsCollectionTypeEditor))]
        [NewItemTypes(typeof(MultiDsChartItem))]
        [PropertyOrder(14)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        // For XAML serialization of collections
        public ObservableCollection<MultiDsChartItem> MultiDsChartItemsCollection { get; } = new();

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.MultiChartDsShape_AxisYLeftIsVisible)]
        [PropertyOrder(1)]
        public bool AxisYLeftIsVisible
        {
            get => _axisYLeftIsVisible;
            set => SetValue(ref _axisYLeftIsVisible, value);
        }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.MultiChartDsShape_AxisXTopIsVisible)]
        [PropertyOrder(2)]
        public bool AxisXTopIsVisible
        {
            get => _axisXTopIsVisible;
            set => SetValue(ref _axisXTopIsVisible, value);
        }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.MultiChartDsShape_AxisYRightIsVisible)]
        [PropertyOrder(3)]
        public bool AxisYRightIsVisible
        {
            get => _axisYRightIsVisible;
            set => SetValue(ref _axisYRightIsVisible, value);
        }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.MultiChartDsShape_AxisXBottomIsVisible)]
        [PropertyOrder(4)]
        public bool AxisXBottomIsVisible
        {
            get => _axisXBottomIsVisible;
            set => SetValue(ref _axisXBottomIsVisible, value);
        }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.MultiChartDsShape_ChartGridIsVisible)]
        [PropertyOrder(5)]
        public bool ChartGridIsVisible
        {
            get => _chartGridIsVisible;
            set => SetValue(ref _chartGridIsVisible, value);
        }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.MultiChartDsShape_ChartGridLabelsLeftIsVisible)]
        [PropertyOrder(6)]
        public bool ChartGridLabelsLeftIsVisible
        {
            get => _chartGridLabelsLeftIsVisible;
            set => SetValue(ref _chartGridLabelsLeftIsVisible, value);
        }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.MultiChartDsShape_ChartGridLabelsTopIsVisible)]
        [PropertyOrder(7)]
        public bool ChartGridLabelsTopIsVisible
        {
            get => _chartGridLabelsTopIsVisible;
            set => SetValue(ref _chartGridLabelsTopIsVisible, value);
        }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.MultiChartDsShape_ChartGridLabelsRightIsVisible)]
        [PropertyOrder(8)]
        public bool ChartGridLabelsRightIsVisible
        {
            get => _chartGridLabelsRightIsVisible;
            set => SetValue(ref _chartGridLabelsRightIsVisible, value);
        }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.MultiChartDsShape_ChartGridLabelsBottomIsVisible)]
        [PropertyOrder(9)]
        public bool ChartGridLabelsBottomIsVisible
        {
            get => _chartGridLabelsBottomIsVisible;
            set => SetValue(ref _chartGridLabelsBottomIsVisible, value);
        }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.MultiChartDsShape_ChartGridBrush)]
        [Editor(typeof(SolidBrushTypeEditor), typeof(SolidBrushTypeEditor))]
        [PropertyOrder(10)]
        public SolidDsBrush ChartGridBrush
        {
            get => _chartGridBrush;
            set => SetValue(ref _chartGridBrush, value);
        }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.MultiChartDsShape_TickStrokeThickness)]
        [PropertyOrder(11)]
        public double TickStrokeThickness
        {
            get => _tickStrokeThickness;
            set => SetValue(ref _tickStrokeThickness, value);
        }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.MultiChartDsShape_TickLength)]
        [PropertyOrder(12)]
        public double TickLength
        {
            get => _tickLength;
            set => SetValue(ref _tickLength, value);
        }

        public override Guid GetDsShapeTypeGuid()
        {
            return DsShapeTypeGuid;
        }

        public override string GetDsShapeTypeNameToDisplay()
        {
            return DsShapeTypeNameToDisplay;
        }

        public override void RefreshForPropertyGrid(IDsContainer? container)
        {
            base.RefreshForPropertyGrid(container);

            foreach (MultiDsChartItem multiDsChartItem in MultiDsChartItemsCollection)
                ItemHelper.RefreshForPropertyGrid(multiDsChartItem, container);

            OnPropertyChanged(nameof(MultiDsChartItemsCollection));
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            base.SerializeOwnedData(writer, context);

            using (writer.EnterBlock(3))
            {
                writer.Write((int) Type);
                writer.Write(MaximumXInfo, context);
                writer.Write(MinimumXInfo, context);
                writer.Write(FormatXInfo, context);
                writer.Write(EngUnitXInfo, context);
                writer.Write(TickFrequencyXInfo, context);
                writer.Write(TextTickFrequencyXInfo, context);
                writer.Write(MaximumYInfo, context);
                writer.Write(MinimumYInfo, context);
                writer.Write(FormatYInfo, context);
                writer.Write(EngUnitYInfo, context);
                writer.Write(TickFrequencyYInfo, context);
                writer.Write(TextTickFrequencyYInfo, context);
                writer.WriteListOfOwnedDataSerializable(MultiDsChartItemsCollection, context);
                writer.Write(AxisYLeftIsVisible);
                writer.Write(AxisXTopIsVisible);
                writer.Write(AxisYRightIsVisible);
                writer.Write(AxisXBottomIsVisible);
                writer.Write(ChartGridIsVisible);
                writer.Write(ChartGridLabelsLeftIsVisible);
                writer.Write(ChartGridLabelsTopIsVisible);
                writer.Write(ChartGridLabelsRightIsVisible);
                writer.Write(ChartGridLabelsBottomIsVisible);
                writer.WriteObject(ChartGridBrush);
                writer.Write(TickStrokeThickness);
                writer.Write(TickLength);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            base.DeserializeOwnedData(reader, context);

            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {                    
                    case 3:
                        try
                        {
                            Type = (AppearanceType) reader.ReadInt32();
                            reader.ReadOwnedData(MaximumXInfo, context);
                            reader.ReadOwnedData(MinimumXInfo, context);
                            reader.ReadOwnedData(FormatXInfo, context);
                            reader.ReadOwnedData(EngUnitXInfo, context);
                            reader.ReadOwnedData(TickFrequencyXInfo, context);
                            reader.ReadOwnedData(TextTickFrequencyXInfo, context);
                            reader.ReadOwnedData(MaximumYInfo, context);
                            reader.ReadOwnedData(MinimumYInfo, context);
                            reader.ReadOwnedData(FormatYInfo, context);
                            reader.ReadOwnedData(EngUnitYInfo, context);
                            reader.ReadOwnedData(TickFrequencyYInfo, context);
                            reader.ReadOwnedData(TextTickFrequencyYInfo, context);
                            List<MultiDsChartItem> multiDsChartItemsCollection =
                                reader.ReadListOfOwnedDataSerializable(() => new MultiDsChartItem(VisualDesignMode),
                                    context);
                            MultiDsChartItemsCollection.Clear();
                            foreach (MultiDsChartItem multiDsChartItem in multiDsChartItemsCollection)
                                MultiDsChartItemsCollection.Add(multiDsChartItem);
                            AxisYLeftIsVisible = reader.ReadBoolean();
                            AxisXTopIsVisible = reader.ReadBoolean();
                            AxisYRightIsVisible = reader.ReadBoolean();
                            AxisXBottomIsVisible = reader.ReadBoolean();
                            ChartGridIsVisible = reader.ReadBoolean();
                            ChartGridLabelsLeftIsVisible = reader.ReadBoolean();
                            ChartGridLabelsTopIsVisible = reader.ReadBoolean();
                            ChartGridLabelsRightIsVisible = reader.ReadBoolean();
                            ChartGridLabelsBottomIsVisible = reader.ReadBoolean();
                            ChartGridBrush = reader.ReadObject<SolidDsBrush>();
                            TickStrokeThickness = reader.ReadDouble();
                            TickLength = reader.ReadDouble();
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

        #region private functions

        private void MultiDsChartItemsCollectionOnChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            DefaultChangeFactory.Instance.OnCollectionChanged(this, nameof(MultiDsChartItemsCollection),
                MultiDsChartItemsCollection, e);

            OnPropertyChanged(nameof(MultiDsChartItemsCollection));
        }

        #endregion

        #region private fields

        private AppearanceType _type;

        private DoubleDataBinding _maximumXInfo = null!;

        private DoubleDataBinding _minimumXInfo = null!;

        private TextDataBinding _formatXInfo = null!;

        private TextDataBinding _engUnitXInfo = null!;

        private DoubleDataBinding _tickFrequencyXInfo = null!;

        private DoubleDataBinding _textTickFrequencyXInfo = null!;

        private DoubleDataBinding _maximumYInfo = null!;

        private DoubleDataBinding _minimumYInfo = null!;

        private TextDataBinding _formatYInfo = null!;

        private TextDataBinding _engUnitYInfo = null!;

        private DoubleDataBinding _tickFrequencyYInfo = null!;

        private DoubleDataBinding _textTickFrequencyYInfo = null!;

        private bool _axisYLeftIsVisible;

        private bool _axisXTopIsVisible;

        private bool _axisYRightIsVisible;

        private bool _axisXBottomIsVisible;

        private bool _chartGridIsVisible;

        private bool _chartGridLabelsLeftIsVisible;

        private bool _chartGridLabelsTopIsVisible;

        private bool _chartGridLabelsRightIsVisible;

        private bool _chartGridLabelsBottomIsVisible;

        private SolidDsBrush _chartGridBrush = null!;

        private double _tickStrokeThickness;

        private double _tickLength;

        #endregion

        public enum AppearanceType
        {
            Default = 0,
            //History = 1,
        }
    }
}