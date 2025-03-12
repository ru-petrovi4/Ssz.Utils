using System;
using Ssz.Operator.Core.CustomAttributes;

using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils.Serialization;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Ssz.Operator.Core.DsShapes
{
    public class ConnectorDsShape : GeometryDsShape
    {
        #region construction and destruction

        public ConnectorDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public ConnectorDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            WidthInitial = 60;
            HeightInitial = 60;

            FillInfo.ConstValue = null;

            GeometryInfo.TypeString = DsUIElementPropertySupplier.CustomTypeString;
            GeometryInfo.CustomXamlString = @"M0,0 L1,0 L1,1 L2,1";
        }

        #endregion

        #region public functions

        public new const string DsShapeTypeNameToDisplay = "Connector";
        public new static readonly Guid DsShapeTypeGuid = new(@"028762C8-8590-46EC-A359-8C3999D51313");

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.ConnectorDsShape_Type)]
        //[PropertyOrder(1)]
        public string Type
        {
            get => _type;
            set => SetValue(ref _type, value);
        }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.ConnectorDsShape_Begin)]
        //[ReadOnlyInEditor]
        //[PropertyOrder(2)]
        public string Begin
        {
            get => _begin;
            set => SetValue(ref _begin, value);
        }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.ConnectorDsShape_End)]
        //[ReadOnlyInEditor]
        //[PropertyOrder(3)]
        public string End
        {
            get => _end;
            set => SetValue(ref _end, value);
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
            using (writer.EnterBlock(1))
            {
                base.SerializeOwnedData(writer, context);

                writer.Write(Type);
                writer.Write(Begin);
                writer.Write(End);
            }
        }

        public override void DeserializeOwnedDataAsync(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        base.DeserializeOwnedDataAsync(reader, context);

                        Type = reader.ReadString();
                        Begin = reader.ReadString();
                        End = reader.ReadString();
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        #endregion

        #region private fields

        private string _type = @"";
        private string _begin = @"";
        private string _end = @"";

        #endregion
    }
}