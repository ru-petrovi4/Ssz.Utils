using System;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Ssz.Operator.Core.DsShapes
{
    public class ConnectionPointDsShape : DsShapeBase
    {
        #region private fields

        private ConnectionPointDsShapeType _type;

        #endregion

        #region construction and destruction

        public ConnectionPointDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public ConnectionPointDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            WidthInitial = 5;
            HeightInitial = 5;
            ResizeMode = DsShapeResizeMode.NoResize;
            Type = ConnectionPointDsShapeType.InOut;
        }

        #endregion

        #region public functions

        public const string DsShapeTypeNameToDisplay = "ConnectionPoint";
        public static readonly Guid DsShapeTypeGuid = new(@"B544822B-DC17-4588-BF6D-D03C0803FF69");

        public override Guid GetDsShapeTypeGuid()
        {
            return DsShapeTypeGuid;
        }

        public override string GetDsShapeTypeNameToDisplay()
        {
            return DsShapeTypeNameToDisplay;
        }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.ConnectionPointDsShape_Type)]
        [PropertyOrder(1)]
        public ConnectionPointDsShapeType Type
        {
            get => _type;
            set => SetValue(ref _type, value);
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                base.SerializeOwnedData(writer, context);

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
                        base.DeserializeOwnedData(reader, context);

                        try
                        {
                            Type = (ConnectionPointDsShapeType) reader.ReadInt32();
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
    }

    public enum ConnectionPointDsShapeType
    {
        InOut = 0,
        In,
        Out
    }
}