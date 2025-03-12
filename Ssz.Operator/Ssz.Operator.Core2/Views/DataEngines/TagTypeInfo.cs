using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;

namespace Ssz.Operator.Core.DataEngines
{
    public class TagTypeInfo : OwnedDataSerializableAndCloneable
    {
        #region construction and destruction

        public TagTypeInfo()
        {
            Constant = DataEngineBase.TagConstant;
        }

        #endregion

        #region public functions

        [DsDisplayName(ResourceStrings.TagTypeInfoTagType)]
        public string? TagType { get; set; }


        [DsDisplayName(ResourceStrings.TagTypeInfoDsPageFileRelativePaths)]
        public string? DsPageFileRelativePaths { get; set; }


        [DsDisplayName(ResourceStrings.TagTypeInfoConstant)]
        public string? Constant { get; set; }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                writer.Write(TagType);
                writer.Write(DsPageFileRelativePaths);
                writer.Write(Constant);
            }
        }

        public override void DeserializeOwnedDataAsync(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                if (block.Version == 1)
                    try
                    {
                        TagType = reader.ReadString();
                        DsPageFileRelativePaths = reader.ReadString();
                        Constant = reader.ReadString();
                    }
                    catch (BlockEndingException)
                    {
                    }
                else
                    throw new BlockUnsupportedVersionException();
            }
        }

        #endregion
    }
}