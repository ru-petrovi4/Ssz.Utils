using System;
using System.ComponentModel;
using Avalonia.Media;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;

//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils.Serialization;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Ssz.Operator.Core.DsShapes
{
    public class GeometryDsShape : DsShapeBase
    {
        #region construction and destruction

        public GeometryDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public GeometryDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            WidthInitial = 60;
            HeightInitial = 60;

            FillInfo = new BrushDataBinding(visualDesignMode, loadXamlContent)
            {
                ConstValue = new SolidDsBrush
                {
                    Color = Colors.Red
                }
            };
            StrokeThickness = 2.0;
            StrokeInfo = new BrushDataBinding(visualDesignMode, loadXamlContent)
            {
                ConstValue = new SolidDsBrush
                {
                    Color = Colors.Blue
                }
            };
            GeometryInfo = new DsUIElementProperty(visualDesignMode, loadXamlContent);
            GeometryInfo.TypeString = GeometryInfoSupplier.RectangleTypeString;
            StrokeLineJoin = PenLineJoin.Miter;
            StrokeStartLineCap = PenLineCap.Round;            
        }

        #endregion

        #region public functions

        public const string DsShapeTypeNameToDisplay = "Geometry";
        public static readonly Guid DsShapeTypeGuid = new(@"E59790B5-6905-4162-B20C-6818A6430FF4");

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.GeometryDsShapeFillInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(BrushTypeEditor), typeof(BrushTypeEditor))]
        //[PropertyOrder(1)]
        public BrushDataBinding FillInfo
        {
            get => _fillInfo;
            set => SetValue(ref _fillInfo, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.GeometryDsShapeStrokeInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(BrushTypeEditor), typeof(BrushTypeEditor))]
        //[PropertyOrder(2)]
        public BrushDataBinding StrokeInfo
        {
            get => _strokeInfo;
            set => SetValue(ref _strokeInfo, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.GeometryDsShapeStrokeThickness)]
        //[PropertyOrder(3)]
        public double StrokeThickness
        {
            get => _strokeThickness;
            set => SetValue(ref _strokeThickness, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.GeometryDsShapeStrokeDashLength)]
        [DefaultValue(null)] // For XAML serialization
        //[PropertyOrder(4)]
        public double? StrokeDashLength
        {
            get => _strokeDashLength;
            set => SetValue(ref _strokeDashLength, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.GeometryDsShapeStrokeGapLength)]
        [DefaultValue(null)] // For XAML serialization
        //[PropertyOrder(5)]
        public double? StrokeGapLength
        {
            get => _strokeGapLength;
            set => SetValue(ref _strokeGapLength, value);
        }


        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.GeometryDsShapeStrokeDashArray)]
        [LocalizedDescription(ResourceStrings.GeometryDsShapeStrokeDashArrayDescription)]
        [DefaultValue(null)] // For XAML serialization
        //[PropertyOrder(6)]
        public string StrokeDashArray
        {
            get => _strokeDashArray;
            set => SetValue(ref _strokeDashArray, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.GeometryDsShapeStrokeLineJoin)]
        //[PropertyOrder(7)]
        public PenLineJoin StrokeLineJoin
        {
            get => _strokeLineJoin;
            set => SetValue(ref _strokeLineJoin, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.GeometryDsShapeStrokeStartLineCap)]
        //[PropertyOrder(8)]
        public PenLineCap StrokeStartLineCap
        {
            get => _strokeStartLineCap;
            set => SetValue(ref _strokeStartLineCap, value);
        }        

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.GeometryDsShapeGeometryInfo)]
        //[Editor(//typeof(DsUIElementPropertyTypeEditor<GeometryInfoSupplier>),
            //typeof(DsUIElementPropertyTypeEditor<GeometryInfoSupplier>))]
        //[PropertyOrder(10)]
        public DsUIElementProperty GeometryInfo
        {
            get => _geometryInfo;
            set => SetValue(ref _geometryInfo, value);
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

            using (writer.EnterBlock(7))
            {
                writer.Write(FillInfo, context);
                writer.Write(StrokeInfo, context);
                writer.Write(StrokeThickness);
                writer.WriteNullable(StrokeDashLength);
                writer.WriteNullable(StrokeGapLength);
                writer.Write(StrokeDashArray);
                writer.Write((int) StrokeLineJoin);
                writer.Write((int) StrokeStartLineCap);                
                writer.Write(GeometryInfo, context);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            base.DeserializeOwnedData(reader, context);

            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 6:
                        reader.ReadOwnedData(FillInfo, context);
                        reader.ReadOwnedData(StrokeInfo, context);
                        StrokeThickness = reader.ReadDouble();
                        StrokeDashLength = reader.ReadNullableDouble();
                        StrokeGapLength = reader.ReadNullableDouble();
                        StrokeDashArray = reader.ReadString();
                        StrokeLineJoin = TreeHelper.GetPenLineJoin_FromWpf(reader.ReadInt32());
                        StrokeStartLineCap = TreeHelper.GetPenLineCap_FromWpf(reader.ReadInt32());
                        TreeHelper.GetPenLineCap_FromWpf(reader.ReadInt32());
                        reader.ReadOwnedData(GeometryInfo, context);

                        //// Fix for WPF 
                        //var left = LeftNotTransformed + StrokeThickness / 2;
                        //var top = TopNotTransformed + StrokeThickness / 2;

                        //var width = WidthInitial - StrokeThickness;
                        //if (width >= DsShapeBase.MinWidth)
                        //{
                        //    WidthInitial = width;
                        //    LeftNotTransformed = left;
                        //}
                        //var height = HeightInitial - StrokeThickness;
                        //if (height >= DsShapeBase.MinHeight)
                        //{
                        //    HeightInitial = height;
                        //    TopNotTransformed = top;
                        //}
                        break;
                    case 7:
                        reader.ReadOwnedData(FillInfo, context);
                        reader.ReadOwnedData(StrokeInfo, context);
                        StrokeThickness = reader.ReadDouble();
                        StrokeDashLength = reader.ReadNullableDouble();
                        StrokeGapLength = reader.ReadNullableDouble();
                        StrokeDashArray = reader.ReadString();
                        StrokeLineJoin = (PenLineJoin)reader.ReadInt32();
                        StrokeStartLineCap = (PenLineCap)reader.ReadInt32();                        
                        reader.ReadOwnedData(GeometryInfo, context);
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public override void RefreshForPropertyGrid(IDsContainer? container)
        {
            base.RefreshForPropertyGrid(container);

            if (GeometryInfo.TypeString == DsUIElementPropertySupplier.CustomTypeString &&
                    ConstantsHelper.ContainsQuery(GeometryInfo.CustomXamlString))
                OnPropertyChanged(nameof(GeometryInfo));
        }

        #endregion

        #region private functions

        private void PrepateAfterOldVersioneserializeOwnedData()
        {
            if (GeometryInfo.TypeString == GeometryInfoSupplier.LLineTypeString) FillInfo.ConstValue = null;
        }

        #endregion

        #region private fields

        private BrushDataBinding _fillInfo = null!;
        private double _strokeThickness;
        private double? _strokeDashLength;
        private double? _strokeGapLength;
        private string _strokeDashArray = @"";
        private BrushDataBinding _strokeInfo = null!;
        private DsUIElementProperty _geometryInfo = null!;
        private PenLineJoin _strokeLineJoin;
        private PenLineCap _strokeStartLineCap;
        private PenLineCap _strokeEndLineCap;

        #endregion
    }
}