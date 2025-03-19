using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Ssz.Operator.Core.Panorama
{
    public class CreateMapToolkitOperationOptions : OwnedDataSerializableAndCloneable
    {
        #region construction and destruction

        public CreateMapToolkitOperationOptions()
        {
            MutualPointRefsMaxAngleDelta = 40;
            MaxPointsDelta = 2;
        }

        #endregion

        #region public functions

        [DsDisplayName(ResourceStrings.CreateMapToolkitOperationOptionsMutualPointRefsMaxAngleDelta)]
        //[PropertyOrder(1)]
        public double MutualPointRefsMaxAngleDelta { get; set; }


        [DsDisplayName(ResourceStrings.CreateMapToolkitOperationOptionsMaxPointsDelta)]
        //[PropertyOrder(2)]
        public double MaxPointsDelta { get; set; }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                writer.Write(MutualPointRefsMaxAngleDelta);
                writer.Write(MaxPointsDelta);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        MutualPointRefsMaxAngleDelta = reader.ReadDouble();
                        MaxPointsDelta = reader.ReadDouble();
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public override string ToString()
        {
            return "Create Map";
        }

        #endregion
    }
}