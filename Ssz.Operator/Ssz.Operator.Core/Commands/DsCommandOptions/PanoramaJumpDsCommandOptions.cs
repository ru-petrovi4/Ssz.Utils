using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Markup;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils.Wpf;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Ssz.Operator.Core.Commands.DsCommandOptions
{
    [TypeConverter(typeof(NameValueCollectionTypeConverter<PanoramaJumpDsCommandOptions>))]
    [ValueSerializer(typeof(NameValueCollectionValueSerializer<PanoramaJumpDsCommandOptions>))]
    public class PanoramaJumpDsCommandOptions : JumpDsCommandOptions
    {
        #region construction and destruction

        public PanoramaJumpDsCommandOptions()
        {
            base.TargetWindow = TargetWindow.CurrentWindow;
            HorizontalLength = "";
            VerticalDelta = "0";
            HorizontalDeltaToButton = "0";
        }

        #endregion

        #region public functions

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public override TargetWindow TargetWindow
        {
            get => base.TargetWindow;
            set => base.TargetWindow = value;
        }

        [DsDisplayName(ResourceStrings.PanoramaJumpDsCommandOptionsHorizontalLength)]
        [PropertyOrder(100)]
        public string HorizontalLength { get; set; }

        [DsDisplayName(ResourceStrings.PanoramaJumpDsCommandOptionsVerticalDelta)]
        [PropertyOrder(101)]
        public string VerticalDelta { get; set; }

        [DsDisplayName(ResourceStrings.PanoramaJumpDsCommandOptionsHorizontalDeltaToButton)]
        [LocalizedDescription(ResourceStrings.PanoramaJumpDsCommandOptionsHorizontalDeltaToButtonDescription)]
        [PropertyOrder(102)]
        public string HorizontalDeltaToButton { get; set; }


        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public double JumpHorizontalK { get; set; }


        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public double JumpVerticalK { get; set; }


        public override string ToString()
        {
            return FileRelativePath;
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            base.SerializeOwnedData(writer, context);

            using (writer.EnterBlock(3))
            {
                writer.Write(HorizontalLength);
                writer.Write(VerticalDelta);
                writer.Write(HorizontalDeltaToButton);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            base.DeserializeOwnedData(reader, context);

            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 3:
                        try
                        {
                            HorizontalLength = reader.ReadString();
                            VerticalDelta = reader.ReadString();
                            HorizontalDeltaToButton = reader.ReadString();
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

        public override void FindConstants(HashSet<string> constants)
        {
            base.FindConstants(constants);

            ConstantsHelper.FindConstants(HorizontalLength,
                constants);
            ConstantsHelper.FindConstants(VerticalDelta,
                constants);
            ConstantsHelper.FindConstants(HorizontalDeltaToButton,
                constants);
        }

        public override void ReplaceConstants(IDsContainer? container)
        {
            base.ReplaceConstants(container);

            HorizontalLength = ConstantsHelper.ComputeValue(container, HorizontalLength)!;
            VerticalDelta = ConstantsHelper.ComputeValue(container, VerticalDelta)!;
            HorizontalDeltaToButton = ConstantsHelper.ComputeValue(container, HorizontalDeltaToButton)!;
        }

        #endregion
    }
}