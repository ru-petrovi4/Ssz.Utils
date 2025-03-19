using System.ComponentModel;
using Avalonia.Markup;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
//using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.Commands.DsCommandOptions
{
    [TypeConverter(typeof(NameValueCollectionTypeConverter<JumpForwardDsCommandOptions>))]
    //[ValueSerializer(typeof(NameValueCollectionValueSerializer<JumpForwardDsCommandOptions>))]
    public class JumpForwardDsCommandOptions : WindowDsCommandOptionsBase
    {
        #region construction and destruction

        public JumpForwardDsCommandOptions()
        {
            base.TargetWindow = TargetWindow.CurrentWindow;
        }

        #endregion

        #region public functions

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(2))
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
                        try
                        {
                            TargetWindow = (TargetWindow) reader.ReadInt32();
                        }
                        catch (BlockEndingException)
                        {
                        }

                        break;
                    case 2:
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