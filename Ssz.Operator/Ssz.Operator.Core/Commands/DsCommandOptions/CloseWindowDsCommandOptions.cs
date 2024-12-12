using System.ComponentModel;
using System.Windows.Markup;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.Commands.DsCommandOptions
{
    [TypeConverter(typeof(NameValueCollectionTypeConverter<CloseWindowDsCommandOptions>))]
    [ValueSerializer(typeof(NameValueCollectionValueSerializer<CloseWindowDsCommandOptions>))]
    public class CloseWindowDsCommandOptions : WindowDsCommandOptionsBase
    {
        #region construction and destruction

        public CloseWindowDsCommandOptions()
        {
            base.TargetWindow = TargetWindow.CurrentWindow;
        }

        #endregion

        #region public functions

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization 
        public override bool CurrentFrame { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization 
        public override string FrameName { get; set; } = "";

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization 
        public override string RootWindowNum { get; set; } = @"";

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                writer.Write((int) TargetWindow);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                if (block.Version == 1)
                    try
                    {
                        TargetWindow = (TargetWindow) reader.ReadInt32();
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
            return TargetWindow.ToString();
        }

        #endregion
    }
}