using System;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils.Serialization;

namespace Ssz.Operator.Core.DsPageTypes
{
    public class ToolTipDsPageType : DsPageTypeBase
    {
        #region public functions

        public static readonly Guid TypeGuid = new(@"27296571-9233-473B-955B-D38D92C83452");

        public override Guid Guid => TypeGuid;

        public override string Name => @"ToolTip";

        public override string Desc => Resources.ToolTipDsPageType_Desc;

        public override bool IsFaceplate => false;

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                if (block.Version == 1)
                {
                }
                else
                {
                    throw new BlockUnsupportedVersionException();
                }
            }
        }

        #endregion
    }
}