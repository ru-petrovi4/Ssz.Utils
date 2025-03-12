using System;
using Ssz.Operator.Core.Utils.Serialization;

namespace Ssz.Operator.Core.DsShapes
{
    public class WindowDragDsShape : DsShapeBase
    {
        #region construction and destruction

        public WindowDragDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public WindowDragDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            WidthInitial = 100;
            HeightInitial = 100;
        }

        #endregion

        #region public functions

        public const string DsShapeTypeNameToDisplay = "Window Drag Area";
        public static readonly Guid DsShapeTypeGuid = new(@"F3FFC437-B313-4806-B2D9-A5BD52E82EE8");

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
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        #endregion
    }
}