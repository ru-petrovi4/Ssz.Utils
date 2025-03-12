using System.ComponentModel;
using Avalonia.Markup;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
//using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.Commands.DsCommandOptions
{
    [TypeConverter(typeof(NameValueCollectionTypeConverter<ApplyDsCommandOptions>))]
    //[ValueSerializer(typeof(NameValueCollectionValueSerializer<ApplyDsCommandOptions>))]
    public class ApplyDsCommandOptions : WindowDsCommandOptionsBase
    {
        #region construction and destruction

        public ApplyDsCommandOptions()
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

        public override void DeserializeOwnedDataAsync(SerializationReader reader, object? context)
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