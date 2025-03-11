using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;

namespace Ssz.Operator.Core.Commands.DsCommandOptions
{
    public class OnOffToggleDsCommandOptions : OwnedDataSerializableAndCloneable
    {
        #region construction and destruction

        public OnOffToggleDsCommandOptions()
        {
            EnableDisableBuzzer = OnOffToggle.Default;
        }

        #endregion

        #region public functions

        [DsDisplayName(ResourceStrings.EnableDisableBuzzer)]
        public OnOffToggle EnableDisableBuzzer { get; set; }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                writer.Write((int) EnableDisableBuzzer);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                if (block.Version == 1)
                    try
                    {
                        EnableDisableBuzzer = (OnOffToggle) reader.ReadInt32();
                    }
                    catch (BlockEndingException)
                    {
                    }
                else
                    throw new BlockUnsupportedVersionException();
            }
        }

        public override string ToString()
        {
            return @"Set Buzzer to " + EnableDisableBuzzer;
        }

        #endregion
    }


    public enum OnOffToggle
    {
        Default = 0,
        Off = 1,
        On = 2,
        Toggle = 3
    }
}