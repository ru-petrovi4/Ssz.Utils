using System;
using System.ComponentModel;
using Ssz.Operator.Core.CustomAttributes;


using Ssz.Operator.Core.Properties;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils.Serialization;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Ssz.Operator.Core.DsShapes
{
    public class SliderDsShape : ControlDsShape
    {
        #region construction and destruction

        public SliderDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public SliderDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            WidthInitial = 120;
            HeightInitial = 30;

            MaximumInfo = new DoubleDataBinding(visualDesignMode, loadXamlContent) {ConstValue = 100.0};
            MinimumInfo = new DoubleDataBinding(visualDesignMode, loadXamlContent) {ConstValue = 0.0};
            ValueInfo = new DoubleDataBinding(visualDesignMode, loadXamlContent) {ConstValue = 0.0};
            LargeChangePercent = 1.0;
            SmallChangePercent = 0.1;
            Interval = 0;
            XamlInfo = new XamlDataBinding(visualDesignMode, loadXamlContent);
        }

        #endregion

        #region public functions

        public const string DsShapeTypeNameToDisplay = "Slider";
        public static readonly Guid DsShapeTypeGuid = new(@"A43635B3-C000-4D33-9682-7E33D59D06E6");

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ControlDsShapeStyleInfo)]
        //[Editor(//typeof(DsUIElementPropertyTypeEditor<SliderStyleInfoSupplier>),
            //typeof(DsUIElementPropertyTypeEditor<SliderStyleInfoSupplier>))]
        // For XAML serialization
        public override DsUIElementProperty StyleInfo
        {
            get => base.StyleInfo;
            set => base.StyleInfo = value;
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.SliderDsShapeXamlInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(XamlTypeEditor), typeof(XamlTypeEditor))]
        [DefaultValue(typeof(XamlDataBinding), @"")] // For XAML serialization
        public XamlDataBinding XamlInfo
        {
            get => _xamlInfo;
            set => SetValue(ref _xamlInfo, value);
        }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.SliderDsShapeMaximumInfo)]
        //[PropertyOrder(0)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        public DoubleDataBinding MaximumInfo
        {
            get => _maximumInfo;
            set => SetValue(ref _maximumInfo, value);
        }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.SliderDsShapeMinimumInfo)]
        //[PropertyOrder(1)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        public DoubleDataBinding MinimumInfo
        {
            get => _minimumInfo;
            set => SetValue(ref _minimumInfo, value);
        }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.SliderDsShapeValueInfo)]
        //[PropertyOrder(2)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        public DoubleDataBinding ValueInfo
        {
            get => _valueInfo;
            set => SetValue(ref _valueInfo, value);
        }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.SliderDsShapeLargeChangePercent)]
        //[PropertyOrder(3)]
        public double LargeChangePercent
        {
            get => _largeChangePercent;
            set => SetValue(ref _largeChangePercent, value);
        }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.SliderDsShapeSmallChangePercent)]
        //[PropertyOrder(4)]
        public double SmallChangePercent
        {
            get => _smallChangePercent;
            set => SetValue(ref _smallChangePercent, value);
        }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.SliderDsShapeInterval)]
        //[PropertyOrder(5)]
        public int Interval
        {
            get => _interval;
            set => SetValue(ref _interval, value);
        }

        public override string? GetStyleXamlString(IDsContainer? container)
        {
            return new SliderStyleInfoSupplier().GetPropertyXamlString(base.StyleInfo, container);
        }

        public override Guid GetDsShapeTypeGuid()
        {
            return DsShapeTypeGuid;
        }

        public override string GetDsShapeTypeNameToDisplay()
        {
            return DsShapeTypeNameToDisplay;
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            base.SerializeOwnedData(writer, context);

            using (writer.EnterBlock(1))
            {
                writer.Write(MaximumInfo, context);
                writer.Write(MinimumInfo, context);
                writer.Write(ValueInfo, context);
                writer.Write(LargeChangePercent);
                writer.Write(SmallChangePercent);
                writer.Write(Interval);
                writer.Write(XamlInfo, context);
            }
        }

        public override void DeserializeOwnedDataAsync(SerializationReader reader, object? context)
        {
            base.DeserializeOwnedDataAsync(reader, context);

            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        try
                        {
                            reader.ReadOwnedData(MaximumInfo, context);
                            reader.ReadOwnedData(MinimumInfo, context);
                            reader.ReadOwnedData(ValueInfo, context);
                            LargeChangePercent = reader.ReadDouble();
                            SmallChangePercent = reader.ReadDouble();
                            Interval = reader.ReadInt32();
                            reader.ReadOwnedData(XamlInfo, context);
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

        private DoubleDataBinding _maximumInfo = null!;
        private DoubleDataBinding _minimumInfo = null!;
        private DoubleDataBinding _valueInfo = null!;
        private double _largeChangePercent;
        private double _smallChangePercent;
        private int _interval;
        private XamlDataBinding _xamlInfo = null!;

        #endregion
    }
}