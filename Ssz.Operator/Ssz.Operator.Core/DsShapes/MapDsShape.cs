using System;
using System.ComponentModel;
using System.Windows.Media;
using Ssz.Operator.Core.CustomAttributes;


using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Ssz.Operator.Core.DsShapes
{
    public class MapDsShape : DsShapeBase
    {
        #region construction and destruction

        public MapDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public MapDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            WidthInitial = 400;
            HeightInitial = 300;

            StrokeThickness = 4.0;
            StrokeInfo = new BrushDataBinding(visualDesignMode, loadXamlContent)
            {
                ConstValue = new SolidDsBrush
                {
                    Color = Colors.Red
                }
            };
            StrokeDashLength = 4.0;
            StrokeGapLength = 4.0;
        }

        #endregion

        #region public functions

        public const string DsShapeTypeNameToDisplay = "Map";
        public static readonly Guid DsShapeTypeGuid = new(@"6B9F6F77-28DD-4029-986F-E43B1E876039");

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.MapDsShapeStrokeThickness)]
        public double StrokeThickness
        {
            get => _strokeThickness;
            set => SetValue(ref _strokeThickness, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.MapDsShapeStrokeDashLength)]
        public double? StrokeDashLength
        {
            get => _strokeDashLength;
            set => SetValue(ref _strokeDashLength, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.MapDsShapeStrokeGapLength)]
        public double? StrokeGapLength
        {
            get => _strokeGapLength;
            set => SetValue(ref _strokeGapLength, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.MapDsShapeStrokeInfo)]
        [ExpandableObject]
        [ValuePropertyPath(@"ConstValue")]
        [IsValueEditorEnabledPropertyPath(@"IsConst")]
        [Editor(typeof(BrushTypeEditor), typeof(BrushTypeEditor))]
        public BrushDataBinding StrokeInfo
        {
            get => _strokeInfo;
            set => SetValue(ref _strokeInfo, value);
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
                writer.Write(StrokeThickness);
                writer.WriteNullable(StrokeDashLength);
                writer.WriteNullable(StrokeGapLength);
                writer.Write(StrokeInfo, context);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            base.DeserializeOwnedData(reader, context);

            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        try
                        {
                            StrokeThickness = reader.ReadDouble();
                            StrokeDashLength = reader.ReadNullableDouble();
                            StrokeGapLength = reader.ReadNullableDouble();
                            reader.ReadOwnedData(StrokeInfo, context);
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

        private double _strokeThickness;
        private double? _strokeDashLength;
        private double? _strokeGapLength;
        private BrushDataBinding _strokeInfo = null!;

        #endregion
    }
}