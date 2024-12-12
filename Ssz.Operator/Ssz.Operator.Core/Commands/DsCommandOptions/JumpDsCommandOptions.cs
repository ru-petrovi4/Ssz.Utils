using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Markup;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils.Wpf;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Ssz.Operator.Core.Commands.DsCommandOptions
{
    [TypeConverter(typeof(NameValueCollectionTypeConverter<JumpDsCommandOptions>))]
    [ValueSerializer(typeof(NameValueCollectionValueSerializer<JumpDsCommandOptions>))]
    public class JumpDsCommandOptions : WindowDsCommandOptionsBase
    {
        #region construction and destruction

        public JumpDsCommandOptions()
        {
            base.TargetWindow = TargetWindow.CurrentWindow;
        }

        #endregion

        #region public functions

        [DsDisplayName(ResourceStrings.JumpDsCommandOptions_FileRelativePath)]
        [Editor(typeof(FileNameTypeEditor), typeof(FileNameTypeEditor))]
        [PropertyOrder(10)]
        public string FileRelativePath { get; set; } = @"";        

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(3))
            {
                base.SerializeOwnedData(writer, context);

                writer.Write(FileRelativePath);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {                    
                    case 3:
                        base.DeserializeOwnedData(reader, context);

                        try
                        {
                            FileRelativePath = reader.ReadString();
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

            ConstantsHelper.FindConstants(FileRelativePath,
                constants);
        }

        public override void ReplaceConstants(IDsContainer? container)
        {
            base.ReplaceConstants(container);

            FileRelativePath = ConstantsHelper.ComputeValue(container,
                FileRelativePath)!;
        }

        public override string ToString()
        {
            return base.ToString() + "; " + FileRelativePath;
        }

        #endregion
    }
}