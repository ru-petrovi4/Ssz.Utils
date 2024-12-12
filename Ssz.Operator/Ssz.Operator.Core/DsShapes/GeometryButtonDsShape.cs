using System;
using System.ComponentModel;
using System.Windows;
using Ssz.Operator.Core.CustomAttributes;


using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils.Serialization;

namespace Ssz.Operator.Core.DsShapes
{
    public class GeometryButtonDsShape : ContentButtonDsShape
    {
        #region construction and destruction

        public GeometryButtonDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public GeometryButtonDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            WidthInitial = 60;
            HeightInitial = 60;

            Padding = new Thickness(2);
            StrokeThickness = 2;

            GeometryInfo = new DsUIElementProperty(visualDesignMode, loadXamlContent);
            GeometryInfo.TypeString = GeometryInfoSupplier.RectangleTypeString;
        }

        #endregion

        #region public functions

        public new const string DsShapeTypeNameToDisplay = "GeometryButton";
        public new static readonly Guid DsShapeTypeGuid = new(@"235D8247-923E-4753-B3DE-D31F376FFD9B");

        public override Guid GetDsShapeTypeGuid()
        {
            return DsShapeTypeGuid;
        }

        public override string GetDsShapeTypeNameToDisplay()
        {
            return DsShapeTypeNameToDisplay;
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ControlDsShapeStyleInfo)]
        [Editor(typeof(DsUIElementPropertyTypeEditor<GeometryButtonStyleInfoSupplier>),
            typeof(DsUIElementPropertyTypeEditor<GeometryButtonStyleInfoSupplier>))]
        // For XAML serialization
        public override DsUIElementProperty StyleInfo
        {
            get => base.StyleInfo;
            set => base.StyleInfo = value;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public override Thickness BorderThickness
        {
            get => base.BorderThickness;
            set => base.BorderThickness = value;
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.GeometryDsShapeStrokeThickness)]
        public double StrokeThickness
        {
            get => _strokeThickness;
            set => SetValue(ref _strokeThickness, value);
        }

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.GeometryDsShapeGeometryInfo)]
        [Editor(typeof(DsUIElementPropertyTypeEditor<GeometryInfoSupplier>),
            typeof(DsUIElementPropertyTypeEditor<GeometryInfoSupplier>))]
        public DsUIElementProperty GeometryInfo
        {
            get => _geometryInfo;
            set => SetValue(ref _geometryInfo, value);
        }

        public override string? GetStyleXamlString(IDsContainer? container)
        {
            return new GeometryButtonStyleInfoSupplier().GetPropertyXamlString(base.StyleInfo, container);
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                base.SerializeOwnedData(writer, context);

                writer.Write(StrokeThickness);
                writer.Write(GeometryInfo, context);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        base.DeserializeOwnedData(reader, context);

                        try
                        {
                            StrokeThickness = reader.ReadDouble();
                            reader.ReadOwnedData(GeometryInfo, context);
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
        private DsUIElementProperty _geometryInfo = null!;

        #endregion
    }
}