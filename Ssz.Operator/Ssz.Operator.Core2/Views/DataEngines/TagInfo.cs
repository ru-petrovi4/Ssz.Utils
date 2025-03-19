using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;

namespace Ssz.Operator.Core.DataEngines
{
    public class TagInfo : OwnedDataSerializableAndCloneable
    {
        #region public functions

        [DsDisplayName(ResourceStrings.TagInfo_TagName)]
        public string TagName { get; set; } = @"";


        [DsDisplayName(ResourceStrings.TagInfo_TagType)]
        public string TagType { get; set; } = @"";


        [DsDisplayName(ResourceStrings.TagInfo_DsPageFileRelativePath)]
        public string DsPageFileRelativePath { get; set; } = @"";

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                writer.Write(TagName);
                writer.Write(TagType);
                writer.Write(DsPageFileRelativePath);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        TagName = reader.ReadString();
                        TagType = reader.ReadString();
                        DsPageFileRelativePath = reader.ReadString();
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        #endregion
    }
}