using System;
using Ssz.Operator.Core.Utils.Serialization;

namespace Ssz.Operator.Core.DsShapes
{
    public class ComplexDsShapeCenterPointDsShape : DsShapeBase
    {
        #region construction and destruction

        public ComplexDsShapeCenterPointDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public ComplexDsShapeCenterPointDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            WidthInitial = 30;
            HeightInitial = 30;
            ResizeMode = DsShapeResizeMode.NoResize;
        }

        #endregion

        #region public functions

        public const string DsShapeTypeNameToDisplay = "Control Center Point";
        public static readonly Guid DsShapeTypeGuid = new(@"EDA4DAB8-6C1C-47DB-A56F-038B41D6ABF1");

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
            using (writer.EnterBlock(1))
            {
                base.SerializeOwnedData(writer, context);
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
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        #endregion
    }
}